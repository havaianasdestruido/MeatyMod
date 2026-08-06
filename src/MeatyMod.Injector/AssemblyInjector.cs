using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MeatyMod.Injector;

public static class AssemblyInjector
{
    public static bool Patch(string exePath, string modDllPath, string outputPath, string backupPath, string entryTypeName = null)
        => Patch(exePath, new[] { modDllPath }, new[] { entryTypeName }, outputPath, backupPath);

    public static bool Patch(string exePath, string[] modDllPaths, string[] entryTypeNames, string outputPath, string backupPath)
    {
        if (!File.Exists(exePath))
        {
            throw new FileNotFoundException("Game executable not found.", exePath);
        }
        if (modDllPaths.Length != entryTypeNames.Length)
        {
            throw new ArgumentException("modDllPaths and entryTypeNames must have the same length.", nameof(entryTypeNames));
        }
        foreach (var modDllPath in modDllPaths)
        {
            if (!File.Exists(modDllPath))
            {
                throw new FileNotFoundException("Mod DLL not found.", modDllPath);
            }
        }

        File.Copy(exePath, backupPath, overwrite: true);

        bool inPlace = string.Equals(Path.GetFullPath(outputPath), Path.GetFullPath(exePath), StringComparison.OrdinalIgnoreCase);
        string writePath = inPlace ? outputPath + ".tmp" + Guid.NewGuid().ToString("N") : outputPath;

        try
        {
            using (var gameModule = ModuleDefinition.ReadModule(exePath))
            {
                var gameType = gameModule.Types.FirstOrDefault(t => t.FullName == "Blood.myGame");
                if (gameType == null)
                {
                    throw new InvalidOperationException("Blood.myGame type not found in game assembly.");
                }

                var ctor = gameType.Methods.FirstOrDefault(m => m.IsConstructor && !m.IsStatic);
                if (ctor == null)
                {
                    throw new InvalidOperationException("Blood.myGame instance constructor not found.");
                }

                var il = ctor.Body.GetILProcessor();
                var ret = ctor.Body.Instructions.LastOrDefault(i => i.OpCode == OpCodes.Ret);
                if (ret == null)
                {
                    throw new InvalidOperationException("No ret instruction found in constructor.");
                }

                for (int i = 0; i < modDllPaths.Length; i++)
                {
                    using var modModule = ModuleDefinition.ReadModule(modDllPaths[i]);

                    var entryType = ResolveEntryType(modModule, entryTypeNames[i]);
                    if (entryType == null)
                    {
                        throw new InvalidOperationException(string.IsNullOrEmpty(entryTypeNames[i])
                            ? $"No mod entry type found in mod DLL: {modDllPaths[i]}"
                            : $"Mod entry type not found: {entryTypeNames[i]}");
                    }

                    var inject = entryType.Methods.FirstOrDefault(m => m.IsStatic && m.Name == "Inject" && m.Parameters.Count == 1);
                    if (inject == null)
                    {
                        throw new InvalidOperationException($"{entryType.FullName}.Inject(Game) method not found in mod DLL.");
                    }

                    var injectRef = gameModule.ImportReference(inject);
                    il.InsertBefore(ret, il.Create(OpCodes.Ldarg_0));
                    il.InsertBefore(ret, il.Create(OpCodes.Call, injectRef));
                }

                gameModule.Write(writePath);
            }

            if (inPlace)
            {
                File.Copy(writePath, outputPath, overwrite: true);
            }
        }
        finally
        {
            if (inPlace && File.Exists(writePath))
            {
                File.Delete(writePath);
            }
        }

        return true;
    }

    private static TypeDefinition ResolveEntryType(ModuleDefinition modModule, string entryTypeName)
    {
        if (!string.IsNullOrEmpty(entryTypeName))
        {
            return modModule.Types.FirstOrDefault(t => t.FullName == entryTypeName);
        }

        var legacy = modModule.Types.FirstOrDefault(t => t.FullName == "QuackMenu.QuackMenuEntry");
        if (legacy != null)
        {
            return legacy;
        }

        var candidates = modModule.Types
            .Where(t => t.Methods.Any(m => m.IsStatic && m.Name == "Inject" && m.Parameters.Count == 1))
            .OrderByDescending(t => t.Name.EndsWith("Entry"));
        return candidates.FirstOrDefault();
    }
}
