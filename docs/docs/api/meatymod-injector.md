---
id: meatymod-injector
title: MeatyMod.Injector
sidebar_label: MeatyMod.Injector
description: API reference for AssemblyInjector — Mono.Cecil IL patching and mod entry-type resolution.
---

# MeatyMod.Injector

`namespace MeatyMod.Injector` — target `net10.0`, `ImplicitUsings=disable`, `Nullable=disable`, package reference **Mono.Cecil 0.11.6**.

One public type. It rewrites the game assembly and nothing else: copying DLLs, deploying configs and printing output are the CLI's job.

---

## AssemblyInjector

`public static class AssemblyInjector`

### `static bool Patch(string exePath, string modDllPath, string outputPath, string backupPath, string entryTypeName = null)`

Single-mod convenience overload. Forwards to the array overload with one-element arrays.

### `static bool Patch(string exePath, string[] modDllPaths, string[] entryTypeNames, string outputPath, string backupPath)`

Patches every mod in `modDllPaths` into `exePath` and writes the result to `outputPath`.

| Parameter | Meaning |
| --- | --- |
| `exePath` | the game executable to read |
| `modDllPaths` | mod assemblies, patched in array order |
| `entryTypeNames` | parallel array; `null` at an index means auto-detect |
| `outputPath` | where to write; may equal `exePath` for an in-place patch |
| `backupPath` | copy of the original, written before anything else |

**Returns** `true` on success (it never returns `false` — failures throw).

**Throws**

| Exception | Condition |
| --- | --- |
| `FileNotFoundException("Game executable not found.", exePath)` | `exePath` missing |
| `ArgumentException("modDllPaths and entryTypeNames must have the same length.", nameof(entryTypeNames))` | arrays differ in length |
| `FileNotFoundException("Mod DLL not found.", modDllPath)` | any mod missing |
| `InvalidOperationException("Blood.myGame type not found in game assembly.")` | host type absent |
| `InvalidOperationException("Blood.myGame instance constructor not found.")` | no instance `.ctor` |
| `InvalidOperationException("No ret instruction found in constructor.")` | unexpected IL |
| `InvalidOperationException("No mod entry type found in mod DLL: <path>")` | auto-detect found nothing |
| `InvalidOperationException("Mod entry type not found: <name>")` | explicit `entryTypeName` did not match |
| `InvalidOperationException("<Type>.Inject(Game) method not found in mod DLL.")` | entry type has no static one-argument `Inject` |

#### Algorithm

```csharp
File.Copy(exePath, backupPath, overwrite: true);

bool inPlace   = Path.GetFullPath(outputPath).Equals(Path.GetFullPath(exePath), OrdinalIgnoreCase);
string writePath = inPlace ? outputPath + ".tmp" + Guid.NewGuid().ToString("N") : outputPath;

using (var gameModule = ModuleDefinition.ReadModule(exePath))
{
    var gameType = gameModule.Types.FirstOrDefault(t => t.FullName == "Blood.myGame");
    var ctor     = gameType.Methods.FirstOrDefault(m => m.IsConstructor && !m.IsStatic);
    var il       = ctor.Body.GetILProcessor();
    var ret      = ctor.Body.Instructions.LastOrDefault(i => i.OpCode == OpCodes.Ret);

    for (int i = 0; i < modDllPaths.Length; i++)
    {
        using var modModule = ModuleDefinition.ReadModule(modDllPaths[i]);

        var entryType = ResolveEntryType(modModule, entryTypeNames[i]);
        var inject    = entryType.Methods.FirstOrDefault(
            m => m.IsStatic && m.Name == "Inject" && m.Parameters.Count == 1);

        var injectRef = gameModule.ImportReference(inject);
        il.InsertBefore(ret, il.Create(OpCodes.Ldarg_0));
        il.InsertBefore(ret, il.Create(OpCodes.Call, injectRef));
    }

    gameModule.Write(writePath);
}

if (inPlace) { File.Copy(writePath, outputPath, overwrite: true); }
// finally: delete the temp file
```

Points worth noting:

- **The backup is written first**, before the module is even read, so a failure half-way still leaves a pristine copy.
- **In-place patching** goes through a GUID-named temp file because Cecil cannot write to the file it has open. The temp file is deleted in a `finally` block.
- **All mods share one anchor**, the last `ret` of the constructor, so the calls execute in array order.
- `ImportReference` adds the assembly reference to the mod DLL; the DLL must therefore sit next to the executable at runtime (what [`inject`](../cli/inject.md) copies).

### `private static TypeDefinition ResolveEntryType(ModuleDefinition modModule, string entryTypeName)`

Not public, but its behaviour is part of the contract:

1. if `entryTypeName` is non-empty → `Types.FirstOrDefault(t => t.FullName == entryTypeName)`, **no fallback**;
2. else if a type named `QuackMenu.QuackMenuEntry` exists → use it (legacy shortcut from before `--entry` existed);
3. else take all types having a `static Inject` method with exactly one parameter, `OrderByDescending(t => t.Name.EndsWith("Entry"))`, and return the first.

Only *arity* is checked, not the parameter type — a `static Inject(string)` would be selected and fail at runtime.

---

## Using it directly

```csharp
using MeatyMod.Injector;

AssemblyInjector.Patch(
    exePath:   @"game\Blood and Bacon\BloodandBacon.exe",
    modDllPaths:    new[] { @"build\Oink.dll", @"build\QuackMenu.dll" },
    entryTypeNames: new[] { null, "QuackMenu.QuackMenuEntry" },
    outputPath: @"game\Blood and Bacon\BloodandBacon.exe",
    backupPath: @"game\Blood and Bacon\BloodandBacon.exe.backup");
```

Remember to copy the mod DLLs next to the output executable yourself — `Patch` does not.

---

## Test coverage

`InjectorTests.cs` (4 tests) and `InjectEdgeTests.cs` (13 tests) build synthetic assemblies at runtime with Cecil — a fake `Blood.myGame` host and fake mod DLLs — and assert the inserted IL, the ordering of multiple mods, in-place patching, backup creation and every failure message above. No real game executable is needed. See [Testing](../development/testing.md).

## Related

- [Injection pipeline](../architecture/injection-pipeline.md) — the same process end to end, including deployment.
- [`meatymod inject`](../cli/inject.md) — the CLI wrapper.
