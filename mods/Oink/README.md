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

- Only one mod can be injected at a time — the IL patch adds a single `Inject` call to `Blood.myGame`'s ctor.
- If Oink is disabled, the skin reverts to the normal character texture after respawn (the game rebuilds the local skin on respawn).
- Unknown game internals are logged, never assumed.

## Limitations / Why the pig skin may appear misaligned

Research finding (T6 revisit, from decompiled game source, verified against the shipped XNB assets):

**What `OinkSkin.Apply` swaps** — two fields on the active `Blood.BloodnBacon` screen:
- `player1Texture` and `player1TextureOrig` (`BloodnBacon.cs` field decl. ~lines 2572/2582). They are set per-frame to `Content.Load<Texture2D>("npc/piggy1")`.

**Do those fields feed the visible model draw? Yes — through the blood-compositing path.**
- The game never draws the local player's skin directly from a "model material texture". The visible skin is an `Effect` parameter on the skinned model shader:
  - `executePlayer1Blood()` / `delPlayer1Blood()` (`BloodnBacon.cs:24752/24802`) draw `player1Texture` (+ wound sprites) into 600x600 render targets, then reassign `player1Texture = target1/target2` and set `quickSkin1.Parameters["Texture"] = player1Texture` (`BloodnBacon.cs:24784/24830`).
  - `DrawMyChar()` (`BloodnBacon.cs:27636`) binds that effect to the visible model: `localModel.Meshes[0].MeshParts[0].Effect = quickSkin1; ... localModel.Meshes[0].Draw();`.
- So swapping `player1Texture` **does** change what the visible player model renders. The pig skin shows — it just shows on the human UV layout.

**Why parts look misaligned (the actual UV mismatch):** both the human skin `texture/jon6` and the pig skin `npc/piggy1` are 600x600 DXT1 atlases with 10 mip levels (verified from XNB headers; render targets are also 600x600). The human model's UVs map onto the human atlas layout, so sampling the pig atlas puts body regions in the wrong places (head picks up arm/torso pixels, etc.). It is a UV-layout mismatch, not a resolution/size mismatch and not an invisible swap.

**Caveat:** `quickSkin1.Texture` is only refreshed inside `executePlayer1Blood`/`delPlayer1Blood`, which run only when the player has blood paint or is being cleaned. Until the player has been hit at least once, the model samples the last composited target (or an unset parameter at game start). Combat applies blood within seconds, so the swap is effectively visible in normal play.

**Ranked options (by effort):**
1. **(a) Texture-only via the correct field — already implemented.** The swapped fields are the only route to the visible model (via `quickSkin1.Texture`). There is no separate "visible model material texture" for Oink to touch. This gives a visible but UV-misaligned pig skin.
2. **(c) Tint/recolor the human atlas instead of replacing it.** Keep the human UV layout, recolor `jon6`'s atlas (or a pre-baked pig-colored variant) and swap that in. Keeps rig, weapons, camera, and alignment; result is a pig-colored farmer rather than a true pig model. Medium effort (runtime DXT1 decode/re-encode, or ship a baked recolor with the mod).
3. **(d) Accept as cosmetic limitation.** Documented above; disable `PigSkin` if the misalignment is unacceptable.
4. **(b) Model swap (replaces `localModel` with the pig model) — infeasible.** The player rig path hardcodes farmer/human bone indices (`playerBones` writes `boneTransforms[15..28]`; `DrawMyChar` binds `npc1[myPlayer.clip1].skinTransforms`; camera/hand/weapon attachment use fixed indices). The pig skeleton (Bip01) does not match, so bones, hands, weapons, and camera would detach. Would require patching animation/bone/paint logic, not a texture change.
