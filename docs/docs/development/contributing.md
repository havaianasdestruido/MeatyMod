---
id: contributing
title: Contributing
sidebar_label: Contributing
description: How to propose a change to MeatyMod, and what a reviewable change looks like.
---

# Contributing

## Before you start

- Read [Coding standards](./coding-standards.md) — style is enforced at build time.
- Read the [architecture overview](../architecture/overview.md) so your change lands in the right layer.
- Check `.ai/maintenance/ISSUES.MD` and `.ai/plan/ROADMAP.MD` in the repository for work that is already planned.

## Setup

```bat
git clone https://github.com/havaianasdestruido/MeatyMod.git
cd MeatyMod
all.bat
dotnet test src\MeatyMod.Tests\MeatyMod.Tests.csproj
```

A clean checkout must build and pass the tests before you change anything — if it does not, that is the first bug.

## The loop

```bat
:: 1. change code
:: 2. format + build + test
dotnet format src\MeatyMod.sln
dotnet build src\MeatyMod.sln
dotnet test src\MeatyMod.Tests\MeatyMod.Tests.csproj

:: 3. if you touched mods or injection
mods\Oink\run.bat
tools\modharness\bin\Release\net10.0-windows\ModHarness.exe
```

## What a good change looks like

| Expectation | Why |
| --- | --- |
| one concern per pull request | reviewable |
| tests for new behaviour **and** for the failure path | the suite asserts error strings |
| documentation updated in the same change | the docs quote real output; drift is invisible otherwise |
| no new package reference in `Core` / `Formats` / `Assets` / `Verifier` | those stay dependency-free |
| analyzer suppressions justified in place | see [coding standards](./coding-standards.md#analyzers) |
| no generated artefacts committed | `bin`, `obj`, `dist`, `_site`, `node_modules` are ignored |
| `game\` untouched | it is read-only input and git-ignored |

## Adding a CLI command

1. implement [`ICommand`](../api/meatymod-cli.md#icommand) in `src\MeatyMod.Cli\Commands\`;
2. register it in the list in `Program.Main`;
3. print usage to `stderr` and return `1` on bad input; catch everything and return `1`;
4. add tests that assert exit codes and on-disk effects;
5. add a page under `docs/docs/cli/` and an entry in `docs/sidebars.ts`;
6. update the command table in `docs/docs/cli/overview.md` and in the root `README.md`.

## Adding a format reader

1. add it to `MeatyMod.Formats` with no package references;
2. throw `InvalidDataException` with a specific message on malformed input;
3. add a fixture if the format is binary — see `Fixtures\darkFog3_0.xnb`;
4. document the byte layout under `docs/docs/formats/`.

## Contributing a mod

The repository ships two examples; new mods are welcome as separate directories under `mods\`:

- follow the [authoring guide](../mods/authoring-a-mod.md) and the shared directory shape;
- target `net40` / `LangVersion 7.3` and vendor the XNA references under your mod's `lib\xna\`;
- include `manifest.json`, `config.txt`, `build.bat`, `run.bat` and a `README.md`;
- never throw from `Inject`;
- add your mod to `all.bat` if it should be part of the one-shot build.

## Documentation-only changes

```bash
cd docs
npm install
npm start          # http://localhost:3000/MeatyMod/docs/
npm run build      # must pass: broken links fail the build
```

The marketing site lives in `site/` and is built by the same workflow. See [Documentation site](./documentation-site.md).

## Reporting bugs

Open an issue at [github.com/havaianasdestruido/MeatyMod/issues](https://github.com/havaianasdestruido/MeatyMod/issues) with:

- the exact command and its full output;
- `meatymod` version (`MeatyMod.Core.VersionInfo.Version`) and .NET SDK version;
- for mod problems, the relevant lines of `oink.log` / `quackmenu.log`;
- for injection problems, whether `restore` succeeded.

Walk through [Troubleshooting](../reference/troubleshooting.md) first — it covers the common cases.

## Security

MeatyMod intentionally does not sandbox or scan mods; "this mod is malicious" is not a MeatyMod bug. A *bypass of a guard MeatyMod does implement* — path containment, checksum verification, the size guard — is. Say so in the issue title and leave working exploit payloads out of the report. See the [security model](../architecture/security-model.md).
