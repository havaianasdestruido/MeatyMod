---
id: quickstart
title: Quickstart
sidebar_label: Quickstart
description: Build, pack, inject, play and restore — the full MeatyMod loop in five minutes.
---

# Quickstart

This walks the complete loop once: build the tools, pack a mod, patch the game, play, and put the original executable back.

:::warning
Read the [safety notes](../architecture/security-model.md) first. Injection executes third-party code inside the game process.
:::

## 1. Build

```bat
all.bat
```

## 2. Pack a mod (optional, for content mods)

`pack` zips a mod directory and embeds a `checksums.txt` manifest of every packed file.

```bat
meatymod pack mods\QuackMenu
```

```text
Packed mods\QuackMenu -> C:\…\MeatyMod\mod.zip
```

`bin`, `obj` and any dot-prefixed path segment are skipped, and files over 100 MB are reported and left out. See [`pack`](../cli/pack.md) and the [mod package format](../formats/mod-package.md).

## 3. Install content (optional)

```bat
meatymod install mod.zip "game\Blood and Bacon"
```

Entries are verified against `checksums.txt`, then extracted into `…\Content\`. Any file that would be overwritten is copied to `…\Backups\MeatyMod\` first. See [`install`](../cli/install.md).

## 4. Inject the code mod

```bat
meatymod inject "game\Blood and Bacon\BloodandBacon.exe" ^
  --mod mods\QuackMenu\src\QuackMenu\bin\Release\net40\QuackMenu.dll
```

```text
Patched C:\…\game\Blood and Bacon\BloodandBacon.exe
Backup saved to C:\…\game\Blood and Bacon\BloodandBacon.exe.backup
Mods deployed next to game exe.
```

What just happened:

1. the executable was copied to `BloodandBacon.exe.backup`;
2. `Blood.myGame`'s instance constructor was rewritten to call `QuackMenu.QuackMenuEntry.Inject(this)` just before it returns;
3. `QuackMenu.dll` and its `config.txt` were copied next to the executable (and into `Content\QuackMenu\config.txt`).

Inject several mods at once by repeating `--mod`:

```bat
meatymod inject "game\Blood and Bacon\BloodandBacon.exe" ^
  --mod mods\Oink\src\Oink\bin\Release\net40\Oink.dll ^
  --mod mods\QuackMenu\src\QuackMenu\bin\Release\net40\QuackMenu.dll
```

Calls are appended in argument order. See [`inject`](../cli/inject.md).

## 5. Play

Launch the game normally (Steam, or the patched executable directly).

| Mod | Key | Effect |
| --- | --- | --- |
| QuackMenu | `F1` | boss spawner menu — `Up`/`Down`, `Enter` to spawn, `Esc` to close |
| Oink | `O` | toggle pig skin + sprint multiplier |

Each mod appends to its own log next to the executable: `quackmenu.log`, `oink.log`. Those are the first place to look when nothing happens.

## 6. Restore

```bat
meatymod restore "game\Blood and Bacon\BloodandBacon.exe"
```

```text
Restored C:\…\game\Blood and Bacon\BloodandBacon.exe from backup.
```

:::tip Never leave the executable patched
`mods\Oink\run.bat` and `mods\QuackMenu\run.bat` do build → restore → inject → launch → restore in one command, so the game directory is clean the moment you quit. They are the recommended way to test a mod.

```bat
mods\Oink\run.bat
```

:::

## The same loop without PATH

```bat
dotnet build src\MeatyMod.Cli\MeatyMod.Cli.csproj
dotnet run --project src\MeatyMod.Cli --no-build -- pack mods\QuackMenu
dotnet run --project src\MeatyMod.Cli --no-build -- inject "game\Blood and Bacon\BloodandBacon.exe" --mod mods\QuackMenu\src\QuackMenu\bin\Debug\net40\QuackMenu.dll
dotnet run --project src\MeatyMod.Cli --no-build -- restore "game\Blood and Bacon\BloodandBacon.exe"
```

## Inspecting assets while you are here

```bat
meatymod verify "game\Blood and Bacon\Content"      :: XNB header sweep
meatymod xnb "game\Blood and Bacon\Content\npc"     :: header dump per file
meatymod manifest "game\Blood and Bacon\Content" assets.json
meatymod checksum "game\Blood and Bacon\Content"
```

## Next

- [Repository layout](./repository-layout.md) — where everything lives.
- [Authoring a mod](../mods/authoring-a-mod.md) — write your own entry point.
- [Troubleshooting](../reference/troubleshooting.md) — when the injector or the mod stays silent.
