---
id: third-party
title: Third-party notices
sidebar_label: Third-party notices
description: Licenses of the components MeatyMod depends on or redistributes.
---

# Third-party notices

Authoritative copy: `THIRD_PARTY_NOTICES.md` at the repository root. This page mirrors it for convenience.

## Redistributed in the release archive

### Mono.Cecil — MIT/X11

Mono.Cecil is the only significant third-party runtime dependency of the `meatymod` tool. It is included in the release archive under `lib\`.

> Mono.Cecil — Copyright © 2008-2021 Jb Evain
>
> Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
>
> The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
>
> THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

## Test-only dependencies

Used solely by `MeatyMod.Tests`; **not** included in the release archive.

| Package | Version | License |
| --- | --- | --- |
| xUnit.net | 2.9.3 | Apache-2.0 (the `xunit.assert` component is MIT) — © .NET Foundation and Contributors |
| xunit.runner.visualstudio | 3.1.4 | Apache-2.0 |
| coverlet.collector | 6.0.4 | MIT |
| Microsoft.NET.Test.Sdk | 17.14.1 | MIT |

## MonoGame / XNA

Blood & Bacon — not this tool — is built on XNA 4.0 / MonoGame. Those are dependencies of the **game**, not of `meatymod`, and are not distributed by this project, which makes no claim over their licenses.

The XNA reference assemblies vendored under `mods\<Name>\lib\xna\` and `tools\modharness\Microsoft.Xna.Framework.Input.Touch.dll` are Microsoft redistributables present so the mods can compile against the framework the game loads.

## Documentation site

The site is built with tooling that is **not** redistributed with the tool:

| Component | License |
| --- | --- |
| [Docusaurus](https://docusaurus.io/) 3 (`/docs`) | MIT — © Meta Platforms, Inc. and affiliates |
| [Jekyll](https://jekyllrb.com/) (landing site) | MIT |
| `jekyll-seo-tag`, `jekyll-sitemap` | MIT |
| Inter, IBM Plex Mono (web fonts) | SIL Open Font License 1.1 |
| Mermaid (via `@docusaurus/theme-mermaid`) | MIT |

## Game content

No Blood & Bacon assets, code or binaries are distributed by this repository. `game\` is git-ignored and must be supplied from your own legally obtained copy. Type and field names documented here were obtained by reverse engineering for interoperability; the research notes live in `.ai/report/`.
