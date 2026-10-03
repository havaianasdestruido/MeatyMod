---
id: memscan
title: memscan
sidebar_label: memscan
description: Read-only process memory viewer that proves a mod DLL loaded and executed in the live game.
---

# memscan

`tools\memscan` is a small read-only Windows console tool that verifies, **at runtime**, that a modded game process actually loaded the mod DLL and that the mod's marker string is present in the process heap — i.e. that the mod code executed.

`meatymod inject` patches the executable on disk; `memscan` is the complementary check against the running process.

| Property | Value |
| --- | --- |
| Target framework | `net10.0` (AnyCPU, so on x64 Windows it can open both 32- and 64-bit targets) |
| Platform | Windows only (`kernel32` P/Invoke) |
| Access requested | `PROCESS_QUERY_INFORMATION` + `PROCESS_VM_READ` |
| Writes | nothing — not to the target, the repository or the game folder |

## Build

```bat
dotnet build tools\memscan\MemScan.csproj -c Release
:: → tools\memscan\bin\Release\net10.0\memscan.exe
```

## Usage

```bat
memscan <process-name|pid> [--marker <string>] [--find-module <name>] [--max-hits <n>]
```

| Argument | Description | Default |
| --- | --- | --- |
| `<process-name\|pid>` | process name (the `.exe` suffix is optional) or a numeric PID. If a name matches several processes the first is used and a note is printed | required |
| `--marker <string>` | string to search for in committed, readable memory — searched as both UTF-16LE and UTF-8 byte sequences | `Oink injected` |
| `--find-module <name>` | module that must appear in the loaded-module list; case-insensitive, extension optional (`DummyTarget` matches `DummyTarget.exe`) | `Oink.dll` |
| `--max-hits <n>` | how many hit addresses to print, with hex context | `5` |
| `-h`, `--help` | print usage and exit | |

## Example

```bat
:: launch a modded session first
mods\launch\launch-oink.bat

:: then, in another shell
memscan BloodandBacon
memscan BloodandBacon --marker "Oink injected" --find-module Oink.dll --max-hits 10
```

```text
[FOUND MODULE] Oink.dll -> 0x…
marker "Oink injected" : FOUND (5 hit(s))
RESULT: PASS
```

## Exit codes

| Code | Meaning |
| --- | --- |
| `0` | every requested check passed |
| `1` | error — process not found, access denied, bad usage |
| `2` | the scan ran but a check failed (module missing, or marker not found) |

Exit `2` is the interesting one: the tool worked, the mod did not.

## How it works

1. resolve the target by PID or name;
2. `OpenProcess` with query + read rights only — on failure, an `OpenProcess` error `5` prints an access-denied hint (an elevated shell may be needed for protected or other-user processes);
3. enumerate loaded modules with a Toolhelp snapshot and look for `--find-module`;
4. walk the address space with `VirtualQueryEx`, and for every **committed, readable** region `ReadProcessMemory` it and search for the marker as UTF-16LE and UTF-8;
5. print up to `--max-hits` addresses with surrounding hex context, then a `RESULT: PASS` / `FAIL` line.

## Verifying memscan itself

The repository documents a dummy-target procedure so the tool can be validated without launching the game. Build a tiny `net10.0` console app that keeps marker strings alive on the managed heap and sleeps:

```csharp
internal static string MarkerA = "OinkInjected_" + Guid.NewGuid().ToString("N")[..8];
internal static string MarkerB = new string("Oink injected".ToCharArray());

static int Main()
{
    Console.WriteLine($"DUMMY pid={Environment.ProcessId}");
    Console.ReadLine();
    Thread.Sleep(Timeout.Infinite);
    return 0;
}
```

Build the markers at runtime (`new string(...)`, `Concat`) so the CLR does not intern them and they live in the ordinary managed heap. Then:

```bat
start DummyTarget.exe
memscan <pid> --marker "Oink injected" --find-module DummyTarget
taskkill /PID <pid> /F
```

Expected: `[FOUND MODULE] DummyTarget.exe`, `marker … FOUND`, `RESULT: PASS`, exit `0` — which confirms both the Toolhelp module enumeration and the `VirtualQueryEx` + `ReadProcessMemory` heap scan work against a live process.

## Using it with your own mod

```bat
memscan BloodandBacon --find-module MyMod.dll --marker "MyMod injected"
```

Make your mod write a distinctive marker string during `Inject` (the shipped mods log `"<Name> injected."`), and keep it off the intern pool if you want it on the heap.

## Related

- [ModHarness](./modharness.md) · [Injection pipeline](../architecture/injection-pipeline.md) · [Troubleshooting](../reference/troubleshooting.md)
