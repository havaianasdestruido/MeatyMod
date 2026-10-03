---
id: oink
title: Oink
sidebar_label: Oink
description: Pig skin texture swap and sprint multiplier, with the UV-mismatch research behind it.
---

# Oink

Turns the player into a pig: swaps the player skin texture for the game's own pig texture and multiplies sprint speed. Toggle with `O`.

| | |
| --- | --- |
| Id / version | `oink` / `1.0.0` |
| Entry type | `Oink.OinkEntry` |
| Target | `net40`, XNA 4.0 |
| Log | `oink.log` |
| Toggle | `O` |

## Features

### Pig skin

`OinkSkin.Apply` loads `Content.Load<Texture2D>(config.PigTexture)` once (default `npc/piggy1`) and writes it to two fields on the active `Blood.BloodnBacon` screen:

- `player1Texture`
- `player1TextureOrig`

The previous values are stashed on first write (`_original`, `_originalOrig`) so `OinkSkin.Restore` can put them back when the mod is toggled off. A failed texture load logs `Pig texture load failed: <message>` and the effect is skipped.

### Sprint multiplier

`OinkSpeed.Apply` reads `myPlayer.sprint` from the gameplay screen each frame and writes back `sprint * multiplier`. The effect is skipped when the multiplier is `<= 0` or exactly `1`.

Because the game recomputes `sprint` every frame (`dirInput *= myPlayer.sprint`), the hook runs with `UpdateOrder = int.MaxValue` so the mod's write lands **after** the game's.

### Toggle

`O` (configurable) flips `_enabled`, with edge detection against the previous key state. When disabled, the skin is restored and the speed multiplier stops being applied.

## Configuration

`mods\Oink\config.txt`:

```ini
# Oink configuration
# Edit values then repack the mod.
Enabled=true
PigSkin=true
SpeedMultiplier=1.35
ToggleKey=O
PigTexture=npc/piggy1
```

| Key | Default | Effect |
| --- | --- | --- |
| `Enabled` | `true` | applied at inject time |
| `PigSkin` | `true` | enable the texture swap |
| `SpeedMultiplier` | `1.35` | per-frame multiplier on `sprint`; `<= 0` or `1` disables |
| `ToggleKey` | `O` | any `Keys` enum name, case-insensitive |
| `PigTexture` | `npc/piggy1` | content path passed to `Content.Load` |

## Build and run

```bat
mods\Oink\build.bat
:: → mods\Oink\src\Oink\bin\Release\net40\Oink.dll

mods\Oink\run.bat
:: build → restore → inject → launch → restore
```

Manual:

```bat
meatymod inject "game\Blood and Bacon\BloodandBacon.exe" ^
  --mod mods\Oink\src\Oink\bin\Release\net40\Oink.dll
meatymod restore "game\Blood and Bacon\BloodandBacon.exe"
```

## Source map

| File | Responsibility |
| --- | --- |
| `OinkEntry.cs` | `Inject(Game)`, per-frame `Update`, key toggle, screen-manager discovery, logging |
| `OinkHook.cs` | `GameComponent` with `UpdateOrder = int.MaxValue` |
| `OinkReflect.cs` | fail-soft `GetField` / `SetField` / `FindGameScreen` helpers |
| `OinkSkin.cs` | texture load, swap and restore |
| `OinkSpeed.cs` | `sprint` multiplier |
| `OinkConfig.cs` | `config.txt` parsing with defaults |

## Why the pig skin looks misaligned

This is the most thoroughly researched part of the repository, and the finding is worth reading before you file it as a bug.

**The swap does reach the visible model.** The game never draws the local player's skin from a model material texture. The visible skin is an `Effect` parameter on the skinned-model shader:

1. `executePlayer1Blood()` / `delPlayer1Blood()` draw `player1Texture` plus wound sprites into 600 × 600 render targets, then reassign `player1Texture` to the target and set `quickSkin1.Parameters["Texture"] = player1Texture`.
2. `DrawMyChar()` binds that effect to the model: `localModel.Meshes[0].MeshParts[0].Effect = quickSkin1;` then `localModel.Meshes[0].Draw();`.

So writing `player1Texture` *does* change what the model renders.

**The misalignment is a UV-layout mismatch.** Both the human skin (`texture/jon6`) and the pig skin (`npc/piggy1`) are 600 × 600 DXT1 atlases with 10 mip levels (verified from the XNB headers; the render targets are 600 × 600 too). The human model's UVs are authored against the human atlas, so sampling the pig atlas puts body regions in the wrong places — head pixels land on the arm, and so on. It is not a resolution problem and not an invisible swap.

**Timing caveat.** `quickSkin1.Texture` is only refreshed inside `executePlayer1Blood` / `delPlayer1Blood`, which run when the player has blood paint or is being cleaned. Until the player is hit at least once, the model samples the last composited target. In normal play combat applies blood within seconds.

### Options, ranked by effort

1. **Texture-only via the correct field — what is implemented.** The swapped fields are the only route to the visible model. Visible, but UV-misaligned.
2. **Tint or recolour the human atlas.** Keep the human UV layout and swap in a pig-coloured variant of `jon6`. Keeps rig, weapons, camera and alignment; the result is a pig-coloured farmer. Medium effort — runtime DXT1 decode/re-encode, or ship a baked recolour.
3. **Accept it as a cosmetic limitation.** Set `PigSkin=false` if the misalignment bothers you.
4. **Model swap — infeasible.** The player rig path hard-codes farmer bone indices (`playerBones` writes `boneTransforms[15..28]`, `DrawMyChar` binds `npc1[myPlayer.clip1].skinTransforms`, camera/hand/weapon attachments use fixed indices). The pig skeleton (Bip01) does not match, so bones, hands, weapons and camera would detach. That is an animation-system rewrite, not a texture change.

## Known limits

- The pig skin reverts to the normal character texture after a respawn when the mod is disabled — the game rebuilds the local skin on respawn.
- Game classes are `internal`; everything runs through reflection. Unknown internals are logged, never assumed.
- Effects are per-frame writes, so they stop the moment the hook stops running.

## Related

- [QuackMenu](./quackmenu.md) · [Authoring a mod](./authoring-a-mod.md)
- [ModHarness](../tools/modharness.md) — headless proof that `OinkEntry.Inject` runs
