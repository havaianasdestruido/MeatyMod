# MODDING.MD

Practical guide for authoring Blood & Bacon mods for the MeatyMod suite. Read this before writing a mod. Source of truth: `mods\Oink`, `mods\QuackMenu`, `src\MeatyMod.Injector\AssemblyInjector.cs`, `src\MeatyMod.Cli\Commands\*.cs`, `src\MeatyMod.Core\ModManifest.cs`.

## What a mod is

A mod is a .NET Framework 4.0 class library DLL that is compiled against the bundled XNA 4.0 reference assemblies in `lib\xna\` and loaded inside the game process. It is not a separate program: the suite rewrites `BloodandBacon.exe` so the game's own `Blood.myGame` constructor calls a static `Inject(Game)` method in the mod DLL. Everything after that happens inside the game's address space, so mods reach game state through reflection (game classes are `internal`/`private`; see the decompiled references).

## Mod anatomy

A mod lives in `mods\<Mod>\` and mirrors `mods\QuackMenu`:

```
mods\<Mod>\
  manifest.json          # suite metadata (id, name, version, author, ...)
  config.txt             # key=value runtime options
  README.md              # features, build/install steps, limits
  .gitignore             # bin/, obj/, *.log, mod.zip
  lib\xna\               # bundled XNA 4.0 assemblies (shared, one copy per mod)
    Microsoft.Xna.Framework.dll
    Microsoft.Xna.Framework.Game.dll
    Microsoft.Xna.Framework.Graphics.dll
    Microsoft.Xna.Framework.Storage.dll
    Microsoft.Xna.Framework.Xact.dll
  src\<Mod>\
    <Mod>.csproj
    <Mod>Entry.cs        # static Inject(Game) entry point
    <Mod>Hook.cs         # GameComponent, per-frame Update -> Entry.Update()
    <Mod>Config.cs       # config.txt loader (key=value, # and ; comments)
    <Mod>Reflect.cs      # helper for field get/set via reflection
    <Mod>*.cs            # feature classes
```

The csproj is an SDK-style project targeting `net40` and referencing the bundled XNA assemblies by HintPath (`mods\Oink\src\Oink\Oink.csproj`):

```xml
<TargetFramework>net40</TargetFramework>
<AssemblyName>Oink</AssemblyName>
<RootNamespace>Oink</RootNamespace>
<LangVersion>7.3</LangVersion>
<Reference Include="Microsoft.Xna.Framework">
  <HintPath>$(MSBuildThisFileDirectory)..\..\lib\xna\Microsoft.Xna.Framework.dll</HintPath>
</Reference>
<Reference Include="Microsoft.Xna.Framework.Game"> ... </Reference>
<Reference Include="Microsoft.Xna.Framework.Graphics"> ... </Reference>
<Reference Include="Microsoft.Xna.Framework.Storage"> ... </Reference>
```

Only the four assemblies above are referenced in code; Xact is bundled but not currently used by either example mod. Build output lands at `src\<Mod>\bin\Debug\net40\<Mod>.dll`.

`manifest.json` fields come from `ModManifest` (`src\MeatyMod.Core\ModManifest.cs`, case-insensitive deserialization): `Id`, `Name`, `Version`, `Author`, `Description`, `Replaces` (string array, empty by default). Example from `mods\Oink\manifest.json`.

`config.txt` uses `key=value` lines; blank lines and lines starting with `#` or `;` are ignored. Config files are plain fields on a config class with defaults; keys are matched lowercase (`OinkConfig.cs` switch on `key.ToLowerInvariant()`).

## Scaffolding a new mod

Mirror `mods\QuackMenu` step by step:

1. Create `mods\<Mod>\lib\xna\` and copy the five XNA DLLs from another mod's `lib\xna\`.
2. Create `mods\<Mod>\src\<Mod>\<Mod>.csproj` with the template above (adjust `AssemblyName`/`RootNamespace`).
3. Write `<Mod>Entry.cs` with `public static void Inject(Game game)` (see Entry contract).
4. Write `<Mod>Hook.cs` — a `GameComponent` whose `Update` calls `Entry.Update()`.
5. Write `<Mod>Config.cs` — copy the `OinkConfig` pattern: defaults, load from `Content\<Mod>\config.txt`, fall back to `<mod>.txt`, parse `key=value`.
6. Write feature classes. Game internals live behind reflection helpers (copy `OinkReflect`).
7. Add `manifest.json`, `config.txt`, `README.md`, `.gitignore`.
8. Build, pack, and inject per the workflow below.

## Entry contract

`AssemblyInjector.ResolveEntryType` selects the entry type in this order (`src\MeatyMod.Injector\AssemblyInjector.cs`):

1. Exact match: `--entry <TypeName>` must equal a type's `FullName` (fully qualified, e.g. `Oink.OinkEntry`). If the name does not match a type exactly, injection fails.
2. Legacy fallback: `QuackMenu.QuackMenuEntry` (the historical default).
3. Auto-detect: the first type (preferring names ending in `Entry`, `OrderByDescending(t => t.Name.EndsWith("Entry"))`) that has a static method named `Inject` with exactly one parameter.

The chosen type must have a static `Inject` method with exactly one parameter, or patching fails with `"<Type>.Inject(Game) method not found"`. The injector checks only the parameter count, not the parameter type; both working examples use `public static void Inject(Game game)`.

What `Inject(Game game)` receives: the patching rewrites the `Blood.myGame` instance constructor (`src\MeatyMod.Injector\AssemblyInjector.cs` finds the first non-static ctor and its last `ret`, then inserts `ldarg_0; call Inject` before it). So `game` is the running `Blood.myGame` instance itself. From it you can reach `game.Content` (root directory `"Content"`, see decompiled `myGame.cs`), `game.Components`, and `game.Window`.

Both example entries use the same bootstrap shape (`QuackMenuEntry.cs`, `OinkEntry.cs`):

```csharp
private static bool _injected;
public static void Inject(Game game)
{
    if (_injected) return;
    _game = game;
    _config = XConfig.Load();
    FindScreenManager(game);            // scan game.Components for FullName == "Blood.ScreenManager"
    if (!game.Components.Contains(_hook)) { _hook = new XHook(game); game.Components.Add(_hook); }
    _injected = true;
}
```

### Per-frame hook

`Inject` runs once, at construction. To run every frame, add a `GameComponent` to `game.Components`. `OinkHook` sets `UpdateOrder = int.MaxValue` (last), `QuackMenuHook` uses `int.MinValue` (first); both just forward to the entry's static `Update(GameTime)`:

```csharp
public class OinkHook : GameComponent
{
    public OinkHook(Game game) : base(game) { UpdateOrder = int.MaxValue; }
    public override void Update(GameTime gameTime) { OinkEntry.Update(); base.Update(gameTime); }
}
```

### Finding game types and fields

Decompiled game references live at `C:\Users\mcmco\AppData\Local\Temp\opencode\bb_dec\Blood\` (one `.cs` per game type, plus `Program.cs`, `myGame.cs`, `ScreenManager.cs`). They are read-only research; treat the real game install dir as read-only too (see Workflow).

- The active gameplay screen is `Blood.BloodnBacon : GameScreen`. Reach it by scanning `Blood.ScreenManager`'s `screens` list (a `List<GameScreen>`) for an element whose type `FullName` starts with `Blood.BloodnBacon` (`OinkReflect.FindGameScreen`).
- Useful fields (verified in decompiled sources): `BloodnBacon.player1Texture`, `BloodnBacon.player1TextureOrig` (Texture2D), `BloodnBacon.myPlayer` (localPlayer), `localPlayer.sprint` (float, default 1.5).
- Game types/fields are `internal`/`private`, so access them with `BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance` through helpers like `OinkReflect.GetField`/`SetField`. Game assemblies are never modified beyond the single inject call.

## Texture / skin overrides

The Oink approach (`OinkSkin.cs`):

1. Load the replacement via the game's own content manager: `game.Content.Load<Texture2D>(config.PigTexture)` with `PigTexture = "npc/piggy1"`. Because the content root is `Content`, `npc/piggy1` resolves to `Content\npc\piggy1.xnb` — the game's own pig texture.
2. Stash the current `player1Texture` and `player1TextureOrig` fields on the active `Blood.BloodnBacon` screen, then set both to the pig texture. Setting both matters: `player1TextureOrig` is what the game restores from.
3. Apply in the per-frame hook (the screen is only active mid-game), and `Restore()` the stashed originals on toggle-off.

UV-mismatch limitation: the pig texture is drawn on the human model's UV layout, so parts look misaligned. The pig's own skeleton (Bip01) is incompatible with the player's rig and the game hardcodes farmer bone indices, so swapping the full model is not feasible. The game also rebuilds the local skin on respawn (it resets `player1Texture`/`player1TextureOrig` to `sc.whiteNPCTexture`), so the skin reapplies next frame while enabled. See `mods\Oink\README.md` for the full limits list. (Note: `mods\Oink\UV_NOTES.md` does not exist — the UV limitation is documented in the Oink README instead.)

Sprint multiplier (OinkSpeed.cs): each frame read `screen.myPlayer.sprint`, multiply by the configured factor (1.35), write it back. The game computes movement as `dirInput *= myPlayer.sprint`, so a persisted multiplier just works.

## Build, pack, deploy, inject workflow

Build the mod (note: the csproj is nested under `src\<Mod>\`, not directly under `mods\<Mod>\`):

```
dotnet build mods\<Mod>\src\<Mod>\<Mod>.csproj
```

Pack from the repo root (creates `mod.zip` in the current directory; now includes `checksums.txt` with per-file SHA-256; files over 100 MB are skipped):

```
meatymod pack mods\<Mod>
```

Install a packed zip into a game directory (extracts into `<game-path>\Content\`, backs up overwritten files to `<game-path>\Backups\MeatyMod\`):

```
meatymod install <mod.zip> <game-path>
```

Inject a built DLL into a game executable. Multi-mod is supported; each `--mod` may carry its own `--entry`:

```
meatymod inject "game\Blood and Bacon\BloodandBacon.exe" --mod mods\Oink\src\Oink\bin\Debug\net40\Oink.dll --entry Oink.OinkEntry
meatymod inject "game\Blood and Bacon\BloodandBacon.exe" --mod a.dll --entry ModA.EntryA --mod b.dll --entry ModB.EntryB
```

A positional legacy form also works: `meatymod inject <exe> <dll> [output-exe]`. What inject does:

1. Backs up the target exe to `<exe>.backup`.
2. Patches the `Blood.myGame` ctor to call each mod's `Inject(this)` (Mono.Cecil IL patch). The default output path is the exe itself; a trailing positional arg changes the output path.
3. Copies each mod DLL next to the exe and deploys its `config.txt` to both `<ModName>.txt` and `Content\<ModName>\config.txt` (the config is located five levels up from the built DLL: `mods\<Mod>\config.txt`). Duplicate DLL filenames are skipped with a warning.

Restore the original exe from backup:

```
meatymod restore "game\Blood and Bacon\BloodandBacon.exe"
```

The suite also registers `pack`, `install`, `inject`, `restore`, `manifest`, `verify`, and `parse` commands (`src\MeatyMod.Cli\Program.cs`).

### Staging rule

The real game install directory is a READ-ONLY reference. Never point `install` or `inject` at the actual install. Copy the game to a scratch/staging folder (e.g. under `C:\Users\mcmco\AppData\Local\Temp\opencode`) and inject/install there, then run the staged copy to test.

## Troubleshooting

- `No mod entry type found in mod DLL` / `Mod entry type not found: X` — the DLL has no static one-parameter `Inject`, or the `--entry` name does not exactly match a type's `FullName`. Pass `--entry <Namespace>.<Type>`.
- `Blood.myGame type not found` / `Blood.myGame instance constructor not found` / `No ret instruction found in constructor` — the exe is not the expected Blood & Bacon build, or the ctor was already altered. Restore from backup and re-inject.
- `No backup found: <exe>.backup` — inject created `<exe>.backup` next to the (patched) exe; restore uses that exact path. If the exe was moved, move the backup with it.
- Config not deployed — the config lookup requires the DLL to be at `mods\<Mod>\src\<Mod>\bin\Debug\net40\<Mod>.dll` (five levels below `mods\<Mod>\config.txt`). Building to a custom output dir breaks deployment. The in-game loader reads `Content\<Mod>\config.txt` first, then `<mod>.txt` next to the exe.
- Entry not firing — mods are injected into `Blood.myGame`'s ctor, which runs before `game.Components` is fully populated; `FindScreenManager` may need to retry each frame (OinkEntry re-runs it in `Update`). Watch `<mod>.log` next to the exe; entries append diagnostics there.
- One-mod-at-a-time is lifted — earlier README text claimed a single inject call; the injector now loops over all `--mod` entries, so multiple mods can be injected in one pass.

## Discrepancies found in existing code/docs

- `mods\Oink\README.md` and the "single inject call" note are stale: multi-mod injection is supported.
- `mods\QuackMenu\README.md` shows `meaty pack`; the CLI binary is `meatymod` (command name is `pack`, invoked as `meatymod pack`).
- Build paths in both mod READMEs (`src\Oink\Oink.csproj`) are correct relative to repo root; the task shorthand `mods\<Mod>\<Mod>.csproj` is not — the real path is `mods\<Mod>\src\<Mod>\<Mod>.csproj`.
- `mods\Oink\UV_NOTES.md` does not exist; the UV-mismatch limitation is documented in `mods\Oink\README.md`.
- The decompiled reference set is incomplete: `Blood\Twin.cs` (quoted in QuackMenu README) is absent from `bb_dec\Blood\`.
