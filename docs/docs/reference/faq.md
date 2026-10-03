---
id: faq
title: FAQ
sidebar_label: FAQ
description: Common questions about MeatyMod's scope, safety and compatibility.
---

# FAQ

### Is this safe?

MeatyMod itself only rewrites the game executable in one place, and always writes a `.backup` first. The risk is the **mod DLL**, which runs with your full user rights inside the game process. MeatyMod does not sandbox or scan it, on purpose. Read the source, or only inject mods you built. See the [security model](../architecture/security-model.md).

### Can I undo everything?

Yes.

```bat
meatymod restore "game\Blood and Bacon\BloodandBacon.exe"
```

For content mods, copy `<game>\Backups\MeatyMod\*` back over `Content\`. Worst case, Steam's *Verify integrity of game files* restores the original install.

### Will this get me VAC-banned?

Blood & Bacon is not a VAC-protected competitive title, but patching a game executable is always at your own risk — and it may affect co-op sessions with unmodded players. Restore before playing with others.

### Which games does it support?

Blood & Bacon only. The injector looks for the literal type `Blood.myGame`; against any other assembly it fails with `Blood.myGame type not found in game assembly.` The format readers (XNB, TXT, RAW) are generic XNA and work against other XNA titles' content.

### Can I inject more than one mod?

Yes — repeat `--mod`:

```bat
meatymod inject "…\BloodandBacon.exe" --mod A.dll --mod B.dll
```

Each gets its own `call` appended to the constructor, in argument order.

### Do mods need to know about each other?

No. They are independent: separate hooks, separate logs, separate config. The only shared surface is the game state they both reflect over, and `UpdateOrder`, which decides who writes a field last.

### Why .NET Framework 4.0 for mods but .NET 10 for the suite?

The mod DLL is loaded by the **game**, which is an XNA 4.0 / .NET Framework 4.0 process — a newer assembly would not load. The suite is a separate process with no such constraint, so it uses current .NET.

### Why Mono.Cecil instead of a runtime detour library?

Cecil patches the assembly **on disk**, so no loader, bootstrapper or injector process is needed at runtime — the patched executable is self-contained, and `restore` is a file copy. It is also the only third-party runtime dependency in the whole tool.

### Can MeatyMod convert an XNB back into a PNG?

No. It decodes the container (header + LZX) and hands you the raw payload. It does not implement XNA's type readers, so turning that payload into a texture is up to you.

### Can it create XNB files?

No. The suite decodes; it does not encode. Content mods replace files that are already XNB, or edit loose `.txt` assets.

### Where do I put the game?

`game\Blood and Bacon\` relative to the repository root — all the scripts assume it. A directory junction works:

```bat
mklink /J "game\Blood and Bacon" "C:\Program Files (x86)\Steam\steamapps\common\Blood and Bacon"
```

:::caution A junction is the live install
With a junction, `inject` patches your real game. That is fine — `restore` undoes it — but keep in mind the game folder is no longer a sandbox copy.
:::

### Why does `pack` not include my DLL?

`pack` excludes `bin\` and `obj\`. Archives carry source, manifest, config and docs; the DLL you inject is a build artefact. See [mod package](../formats/mod-package.md#content-mods-vs-code-mods).

### Why does `verify` call a valid XNB invalid?

It requires the Windows platform byte (`w`) and format version `5`. Xbox 360 (`x`), Windows Phone (`m`) and XNA 3.1 (`4`) content is rejected. Use `meatymod xnb` to read the actual header.

### Why does `xnb` on a directory always exit 0?

By design — the sweep reports a `Failed:` count instead of failing. Use `verify` when you want a non-zero exit on bad content.

### Can I run this on Linux or macOS?

The libraries are portable .NET, and `pack`, `checksum`, `parse`, `xnb`, `verify` and `manifest` will run anywhere the SDK does. `inject` produces a Windows executable, `memscan` is Win32-only, `ModHarness` needs Windows and x86 XNA, and the game is Windows-only. Treat the suite as Windows-first.

### Is there a GUI?

No, and none is planned. The `run.bat` / `launch-*.bat` scripts are the one-click path.

### How do I update after a game patch?

Re-run `meatymod verify` and `tools\smoke.ps1`; the smoke script asserts exact asset counts that a patch will change. If the injector starts failing, the game's type or constructor shape changed — the project tracks that in `.ai/maintenance/GAME_UPDATES.MD`.

### Where is the version number?

`MeatyMod.Core.VersionInfo.Version` — currently `1.0.0`. `tools\release.ps1` parses it to name the release archive.

### How do I contribute?

See [Contributing](../development/contributing.md). Good first issues: wire up QuackMenu's unused `Bosses` / `BossWeights` config keys, convert remaining block-scoped namespaces, or make `tools\release.ps1`'s staging path portable.
