---
title: Mods
eyebrow: Bundled
description: QuackMenu and Oink — the two reference mods that ship with MeatyMod, what they do, how to configure them, and how to write your own.
---

Two mods ship with the repository. Neither is a toy: both are complete implementations of
the hook pattern every MeatyMod code mod uses, with configuration, logging and fail-soft
reflection against the game's internals.

## QuackMenu

An in-game boss spawner and creative-mode toggle.

| | |
| --- | --- |
| Source | `mods\QuackMenu\` |
| Entry type | `QuackMenu.QuackMenuEntry` |
| Output | `mods\QuackMenu\src\QuackMenu\bin\Release\net40\QuackMenu.dll` |
| Log | `quackmenu.log` next to the game executable |

Press <kbd>F1</kbd> to open the overlay, <kbd>↑</kbd> / <kbd>↓</kbd> to move, <kbd>Enter</kbd> to
spawn and <kbd>Esc</kbd> to close. Four bosses are catalogued — Cutty, Princess, Boar King and
Twin. For each one the spawner reflects over the boss type's constructor and supplies the
arguments it can infer, then registers the result with the game's screen manager so it joins
the live encounter.

Creative mode flips the game's own cheat fields — all weapons, developer flags, cheats
enabled — rather than reimplementing them.

```ini
# mods\QuackMenu\config.txt
CreativeMode=false
MenuKey=F1
```

[Full QuackMenu documentation &rarr;]({{ site.docs_url | append: 'mods/quackmenu' | relative_url }})

## Oink

A cosmetic mod: pig skin and sprint multiplier.

| | |
| --- | --- |
| Source | `mods\Oink\` |
| Entry type | `Oink.OinkEntry` |
| Output | `mods\Oink\src\Oink\bin\Release\net40\Oink.dll` |
| Log | `oink.log` next to the game executable |

Press <kbd>O</kbd> to toggle. The skin swap retargets the player's texture field at the game's
own `npc/piggy1` asset, stashing the original reference so it can be put back without
reloading content. The speed change multiplies the player's sprint value rather than
assigning an absolute one, so it composes with the game's own modifiers.

```ini
# mods\Oink\config.txt
PigSkin=true
SpeedMultiplier=1.5
ToggleKey=O
```

The pig texture is drawn for a different model, so the UVs do not line up on the player mesh.
That is expected, and the reason is analysed line by line in the mod's documentation.

[Full Oink documentation &rarr;]({{ site.docs_url | append: 'mods/oink' | relative_url }})

## How both of them work

Every code mod follows the same four stages:

1. **Inject** — `meatymod inject` appends a call to your static `Inject(Game)` method at the end
   of the game's constructor.
2. **Hook** — `Inject` adds a `GameComponent` to `Game.Components`, which XNA then calls every
   frame. No `Update` method is patched.
3. **React** — the component watches for an edge-triggered key press and acts.
4. **Reflect** — the mod has no compile-time reference to the game, so it reads and writes the
   game's internal fields by name, logging and skipping anything it cannot find.

That last rule is what keeps a game update from crashing your render loop: a renamed field
produces a line in the log, not an exception.

[Mod runtime model &rarr;]({{ site.docs_url | append: 'architecture/mod-runtime' | relative_url }})

## Writing your own

The documentation has a start-to-finish guide: project layout, the `net40` target, the
manifest, the hook skeleton, config loading, logging, and verifying the result with
`ModHarness` before you ever launch the game.

[Authoring a mod &rarr;]({{ site.docs_url | append: 'mods/authoring-a-mod' | relative_url }})

## Content mods

Not every mod needs code. Replacement textures, models and loose text assets are packaged
as archives and installed with checksum verification and automatic backups:

```bat
meatymod pack mods\MyTextures
meatymod install mod.zip "game\Blood and Bacon"
```

[Mod package format &rarr;]({{ site.docs_url | append: 'formats/mod-package' | relative_url }})
