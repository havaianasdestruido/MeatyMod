# memscan — MeatyMod process memory viewer

`memscan` is a small read-only Windows console tool that verifies at runtime that a
modded game process actually loaded the mod DLL and that the mod's marker string is
present in the process heap (proving the mod DLL executed).

The MeatyMod pipeline (`meatymod inject`) patches the game exe on disk so the mod DLL
loads at startup. `memscan` is the complementary check against the **running** process:
it confirms the module is loaded and the marker the mod writes into memory exists.

- Target framework: `net10.0`
- Windows-only (uses `kernel32` P/Invoke)
- **Read-only**: it never writes to the target process, the repo, or the game directory.
  The only access requested is `PROCESS_QUERY_INFORMATION | PROCESS_VM_READ`.

## Build

```bat
dotnet build tools\memscan\MemScan.csproj -c Release
```

Output: `tools\memscan\bin\Release\net10.0\memscan.exe`

## Usage

```bat
memscan <process-name|pid> [--marker <string>] [--find-module <name>] [--max-hits <n>]
```

| Argument | Description | Default |
| --- | --- | --- |
| `<process-name\|pid>` | Target process name (without `.exe` is fine) or numeric PID. If a name matches multiple processes, the first is used (a note is printed). | required |
| `--marker <string>` | String to search for in the target's committed, readable memory. Searched as both UTF-16LE and UTF-8 byte sequences. | `Oink injected` |
| `--find-module <name>` | Module name that must appear in the target's loaded-module list. Case-insensitive; a file extension on the query is optional (`DummyTarget` matches `DummyTarget.exe`). | `Oink.dll` |
| `--max-hits <n>` | How many hit addresses to print (with hex context). | `5` |
| `-h`, `--help` | Print usage and exit. | |

Examples:

```bat
memscan BloodandBacon
memscan 2468 --marker "Oink injected" --find-module Oink.dll
```

### Against the real game (BloodandBacon.exe)

1. Launch the modded game with `mods\launch\launch-oink.bat`
   (restores the original exe, injects Oink, starts the game, then restores the exe
   when the game exits).
2. While the game is **running**, scan it:
   ```bat
   tools\memscan\bin\Release\net10.0\memscan.exe BloodandBacon
   ```
   A PASS looks like:
   ```
   [FOUND MODULE] Oink.dll -> 0x...
   marker "Oink injected" : FOUND (N hit(s))
   RESULT: PASS
   ```
3. Scan **before** the game exits: `launch-oink.bat` restores the exe as soon as the
   game closes, and the process (module + heap marker) is gone.

## Exit codes

| Code | Meaning |
| --- | --- |
| `0` | Scan completed and all requested checks passed (module + marker found). |
| `1` | Error — process not found, access denied, `OpenProcess`/snapshot failed, bad usage. |
| `2` | Scan completed but a requested check did not pass (module or marker missing). |

## How it works / P/Invoke surface

All interop goes through `kernel32` (via `Program.NativeMethods`, `SetLastError = true`):

| API | Use |
| --- | --- |
| `OpenProcess` | Open the target with `PROCESS_QUERY_INFORMATION (0x0400) | PROCESS_VM_READ (0x0010)`. |
| `CreateToolhelp32Snapshot` | Snapshot of the target's loaded modules (`TH32CS_SNAPMODULE | TH32CS_SNAPMODULE32`, so 32-bit target modules are listed too). |
| `Module32FirstW` / `Module32NextW` | Enumerate `MODULEENTRY32W` entries (module name + full path). |
| `VirtualQueryEx` | Walk the target's address space; only `MEM_COMMIT` regions that are readable (not `PAGE_NOACCESS`, not `PAGE_GUARD`) are scanned. |
| `ReadProcessMemory` | Read committed regions in 64 KiB chunks and search for the marker bytes (UTF-16LE and UTF-8). |
| `IsWow64Process` | Report target bitness (64-bit vs 32-bit WoW64). |
| `CloseHandle` | Release process + snapshot handles (in `finally`). |

Notes:

- Built AnyCPU; on x64 Windows this runs 64-bit, so it can open both 64-bit and
  32-bit targets.
- Needs `PROCESS_QUERY_INFORMATION | PROCESS_VM_READ` on the target. An elevated
  shell may be required for protected / other-user processes (`OpenProcess` error 5
  prints an "access denied" hint).
- It reads **only** committed, readable memory and writes nothing to the target.

## Dummy-process verification procedure

End-to-end test without launching the game. The dummy target is a tiny console app
that keeps the marker string alive in its managed heap and sleeps.

Setup (one time, outside the repo — e.g. `%TEMP%\opencode\memscan-dummy\`):

1. Create a `net10.0` console project (AssemblyName `DummyTarget`), e.g.
   `Program.cs`:
   ```csharp
   internal static string MarkerA = "OinkInjected_" + Guid.NewGuid().ToString("N")[..8];
   internal static string MarkerB = new string("Oink injected".ToCharArray());
   static int Main() { Console.WriteLine($"DUMMY pid={Environment.ProcessId}"); Console.ReadLine(); Thread.Sleep(Timeout.Infinite); return 0; }
   ```
   (Build markers at runtime — via `new string(...)` / `Concat` — so the CLR does not
   intern them and they live in the normal managed heap.)
2. Build: `dotnet build -c Release` → `DummyTarget.exe`.

Then, per run:

```bat
start DummyTarget.exe        REM note the printed PID (or read it from tasklist)
memscan <pid> --marker "Oink injected" --find-module DummyTarget
taskkill /PID <pid> /F
```

Expected output (abridged):

```
[FOUND MODULE] DummyTarget.exe -> 0x...
marker "Oink injected" : FOUND (5 hit(s))
RESULT: PASS
```

with exit code `0`. This confirms both the module enumeration (Toolhelp snapshot) and
the heap scan (`VirtualQueryEx` + `ReadProcessMemory`) work against a live process.
