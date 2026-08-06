# Oink

Pig transformation mod for **Blood & Bacon** (XNA 4.0, Steam app 434570).

Standalone mod. Not part of MeatyMod suite. Packed and installed through the suite CLI.

## Features

- **Pig skin** — replaces the player's skin texture with the game's own pig texture (`Content.Load<Texture2D>("npc\\piggy1")`), while keeping the human rig, animations, weapons, and camera working.
- **Pig speed** — multiplies the player's `sprint` field each frame (the game computes `dirInput *= myPlayer.sprint`), e.g. 1.35x.
- **Toggle** — press **O** (default) in-game to toggle both effects on/off.
- **Reflection-based** — game classes are `internal`/`private`; all mod logic runs through reflection with no game assembly changes beyond the single inject call.

## Build

Requires XNA 4.0 runtime (GAC) — XNA reference assemblies are bundled under `lib/xna/`.

```
dotnet build src\Oink\Oink.csproj
```

Output: `src\Oink\bin\Debug\net40\Oink.dll`

## Pack (via MeatyMod suite)

From repo root:

```
meatymod pack mods\Oink
```

This zips the mod folder into `mod.zip` per `manifest.json`.

## Install

The mod must be loaded inside the game process via IL injection into `BloodandBacon.exe`. One command does it all:

```
dotnet build src\MeatyMod.Cli\MeatyMod.Cli.csproj
meatymod inject "game\Blood and Bacon\BloodandBacon.exe" "mods\Oink\src\Oink\bin\Debug\net40\Oink.dll"
```

What `inject` does:

1. Backs up the game exe to `BloodandBacon.exe.backup`.
2. Patches `Blood.myGame` ctor to call `Oink.OinkEntry.Inject(this)` (Mono.Cecil IL patch) — the injector auto-detects the mod entry type.
3. Copies `Oink.dll` + `config.txt` next to the game exe.

Then launch the game normally (via Steam). In-game press **O** to toggle the pig effects.

**Restore the original exe:**

```
meatymod restore "game\Blood and Bacon\BloodandBacon.exe"
```

## Manual injection

Place `Oink.dll` + XNA deps beside `BloodandBacon.exe` and add the `Inject` call via any XNA4 assembly patcher.

On load the mod writes `oink.log` next to the game exe for diagnostics.

## Config

`config.txt` — edit and repack to change behavior:

```
Enabled=true
PigSkin=true
SpeedMultiplier=1.35
ToggleKey=O
PigTexture=npc/piggy1
```

## Notes / Limits

- The pig skin is the game's own pig texture applied to the human model's UV layout, so some parts may look misaligned/mismatched. This is the tradeoff: the pig's skeleton (Bip01) is incompatible with the player's rig, and the game hardcodes farmer bone indices.
- Only one mod can be injected at a time — the IL patch adds a single `Inject` call to `Blood.myGame`'s ctor.
- If Oink is disabled, the skin reverts to the normal character texture after respawn (the game rebuilds the local skin on respawn).
- Read-only research in `.ai/report/`; unknown game internals are logged, never assumed.
