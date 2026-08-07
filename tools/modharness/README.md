# ModHarness

Headless harness that proves a mod's `OinkEntry.Inject()` code executes **without launching the game**.

It loads `Oink.dll` plus the XNA assemblies via reflection, builds a dynamic `ShimGame` subclass of `Microsoft.Xna.Framework.Game`, invokes `Inject`, and verifies the hook was really installed and runs. The game executable is never started; the only touch of `game\...` is one read-only existence check.

## Architecture notes

- **x86 (`PlatformTarget`)** — XNA 4.0 assemblies are x86-only, so the harness must also be x86. See `<PlatformTarget>x86</PlatformTarget>` in `ModHarness.csproj`.
- **Target framework:** `net10.0-windows`, output under `bin\Release\net10.0-windows\`.
- **GAC Touch.dll requirement:** `Microsoft.Xna.Framework.Input.Touch.dll` is MSIL-only and not shipped in `mods\Oink\lib\xna`. It is loaded from a GAC copy that lives at `tools\modharness\Microsoft.Xna.Framework.Input.Touch.dll`; the csproj copies it to the output dir via `<None CopyToOutputDirectory="Always">`. Do not add extra copies from the GAC — this one already exists in the repo.

## Build

```
dotnet build tools\modharness\ModHarness.csproj -c Release
```

## Run

```
tools\modharness\bin\Release\net10.0-windows\ModHarness.exe
```

Exit code 0 = 0 FAIL; non-zero = at least one FAIL.

## What the 18 checks prove

| Step | Proves |
| ---- | ------ |
| 0 | Config files copied into the harness's own output dir (`Content\Oink\config.txt` + `oink.txt`) so `OinkConfig.Load` finds them |
| 1 | `Oink.dll` + all XNA references load (Framework, Graphics, Storage, Input.Touch from GAC, Game) |
| 2 | `Oink.OinkEntry` type and `Inject(Game)` / `Enabled` resolve |
| 3 | A real `Microsoft.Xna.Framework.Game` subclass constructs headlessly; `Components` and `Content` are accessible |
| 4 | `Inject(shimGame)` executes without throwing |
| 4a | Exactly one `Oink.OinkHook` was added to `Components` with `UpdateOrder == int.MaxValue` |
| 5 | `oink.log` is freshly written with "Oink injected." |
| 6 | `OinkHook.Update(GameTime)` runs via reflection and produces log output |
| 7 | `OinkEntry.Enabled` getter executes |
| 8 | `OinkConfig.Load()` returns the expected `config.txt` values |
| 9 | `game\...\Content\npc\piggy1.xnb` exists (read-only; it is NOT loaded — that needs a graphics device) |

## Filesystem safety

The harness writes only inside its own output directory: `oink.log`, `oink.txt`, and `Content\Oink\config.txt`. It never writes into `game\`, and its only access to `game\` is the read-only `File.Exists` check in Step 9.
