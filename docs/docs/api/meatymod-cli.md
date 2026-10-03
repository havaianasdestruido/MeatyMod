---
id: meatymod-cli
title: MeatyMod.Cli
sidebar_label: MeatyMod.Cli
description: API reference for Program, ICommand and the nine command implementations.
---

# MeatyMod.Cli

`namespace MeatyMod.Cli` / `MeatyMod.Cli.Commands` — target `net10.0`, `OutputType=Exe`, `AssemblyName=meatymod`, `ImplicitUsings=disable`, `Nullable=disable`.

References every other project in the suite. This page documents the types; the user-facing behaviour of each command is in the [CLI reference](../cli/overview.md).

---

## Program

`public static class Program`

### `static int Main(string[] args)`

Builds the command list, dispatches on `args[0]`, returns the command's exit code.

```csharp
var commands = new List<ICommand>
{
    new PackCommand(), new InstallCommand(), new InjectCommand(),
    new RestoreCommand(), new ManifestCommand(), new VerifyCommand(),
    new ParseCommand(), new XnbCommand(), new ChecksumCommand(),
};

if (args.Length == 0) { PrintUsage(commands); return 1; }

var command = commands.Find(c => c.Name == args[0]);
if (command == null) { PrintUsage(commands); return 1; }

string[] rest = new string[args.Length - 1];
Array.Copy(args, 1, rest, 0, rest.Length);
return command.Run(rest);
```

Matching is ordinal and case-sensitive: `meatymod Pack` is not `meatymod pack`.

### `private static void PrintUsage(List<ICommand> commands)`

Writes `Usage: meatymod <command> [options]`, a blank line, `Commands:` and each `command.Name` indented by two spaces — to **stdout**. The surrounding failure path is what returns `1`.

---

## ICommand

```csharp
namespace MeatyMod.Cli.Commands;

public interface ICommand
{
    string Name { get; }
    int Run(string[] args);
}
```

| Member | Contract |
| --- | --- |
| `Name` | the literal sub-command token, lower-case |
| `Run(args)` | receives arguments **after** the command token; returns the process exit code |

Implementations must:

- print their own usage line to `stderr` and return `1` when arguments are missing;
- write results to `stdout` and diagnostics to `stderr`;
- catch their own exceptions and convert them to `1` — an exception escaping `Run` becomes an unhandled crash.

---

## Command implementations

All nine live in `src\MeatyMod.Cli\Commands\`, one file each.

| Class | `Name` | Calls into | Docs |
| --- | --- | --- | --- |
| `PackCommand` | `pack` | `ChecksumUtil`, `FileSizeGuard`, `ZipArchive` | [pack](../cli/pack.md) |
| `InstallCommand` | `install` | `FileSizeGuard`, `SHA256`, `ZipFile` | [install](../cli/install.md) |
| `InjectCommand` | `inject` | `AssemblyInjector` | [inject](../cli/inject.md) |
| `RestoreCommand` | `restore` | `File.Copy` | [restore](../cli/restore.md) |
| `ManifestCommand` | `manifest` | `AssetManifestBuilder`, `JsonSerializer` | [manifest](../cli/manifest.md) |
| `VerifyCommand` | `verify` | `AssetValidator` | [verify](../cli/verify.md) |
| `ParseCommand` | `parse` | `TxtReader`, `CameraTrackParser`, `RawReader` | [parse](../cli/parse.md) |
| `XnbCommand` | `xnb` | `XnbContentReader` | [xnb](../cli/xnb.md) |
| `ChecksumCommand` | `checksum` | `ChecksumUtil` | [checksum](../cli/checksum.md) |

### Notable internals

**`PackCommand`** — filters `bin`/`obj`/dot-segments, enforces the size guard, writes entries with `CompressionLevel.Optimal` and appends a `checksums.txt` entry written through a `StreamWriter` with `new UTF8Encoding(false)` (no BOM).

**`InstallCommand`** — two private helpers:

- `static bool IsInside(string root, string path)` — case-insensitive containment test used as the zip-slip guard; equal paths count as inside, otherwise `path` must start with `root` plus a directory separator.
- `static List<string> VerifyChecksums(ZipArchive archive)` — returns an empty list when `checksums.txt` is absent; splits each line on the first double space; reports malformed lines, missing entries and hash mismatches.

The file carries `#pragma warning disable CA1515, CA1031` at namespace level (public `ICommand` classes and catch-all `Run` exits are the established convention) and a scoped `CA5389` suppression around the extraction, justified by the containment check above it.

**`InjectCommand`** — the only non-trivial argument parser in the codebase. It first scans for a `--mod` token to choose between flagged and legacy positional mode, then builds parallel `modDllPaths` / `entryTypeNames` lists, tracking a `pendingEntry` so that `--entry` may appear before the `--mod` it belongs to. After patching it deploys each DLL and probes five levels up for `config.txt`. Duplicate DLL file names are tracked with a `HashSet<string>(StringComparer.OrdinalIgnoreCase)`.

**`ParseCommand`** — dispatches on the `.raw` extension first, then uses the heuristic `doc.GetString(0).Contains(".") && doc.Count % 6 == 0` to pick the camera-track branch. `RunRaw` guesses dimensions, falls back to 2048 × 2048 and prints width, height, sample count and the min/max height.

**`XnbCommand`** — `DumpFile` prints the header fields; `SweepDirectory` prints one line per file plus a `Total/Compressed/Failed` summary and always returns `0`; `TypeIdHex` formats the first four payload bytes as spaced upper-case hex.

**`ChecksumCommand`** — sorts directory entries with `StringComparer.Ordinal` on the full path for reproducible output, catches per-file `IOException` / `UnauthorizedAccessException` and downgrades them to a `stderr` line plus a final exit code of `1`.

---

## Adding a command

```csharp
namespace MeatyMod.Cli.Commands;

public class HashDirCommand : ICommand
{
    public string Name => "hashdir";

    public int Run(string[] args)
    {
        if (args is null || args.Length == 0)
        {
            Console.Error.WriteLine("Usage: meatymod hashdir <dir>");
            return 1;
        }

        try
        {
            // …
            return 0;
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"hashdir failed: {ex.Message}");
            return 1;
        }
    }
}
```

Then register it in `Program.Main`'s list — there is no reflection-based discovery — add tests, and document it under `docs/docs/cli/`.

---

## Test coverage

`PackInstallTests.cs`, `InstallChecksumTests.cs`, `VerifyCommandTests.cs`, `InjectEdgeTests.cs` and `InjectorTests.cs` drive the command classes directly (`new PackCommand().Run(new[] { … })`) against temp directories, asserting both exit codes and on-disk effects.
