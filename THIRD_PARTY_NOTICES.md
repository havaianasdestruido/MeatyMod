# Third-Party Notices

This file documents third-party components used by the MeatyMod repository and
the `meatymod` release archive, with their copyright and license information.

## Runtime dependency (redistributed in the release archive)

### Mono.Cecil — MIT/X11

Mono.Cecil is the only significant third-party runtime dependency of the
`meatymod` tool. It is included in the release archive under `lib\`.

Mono.Cecil — Copyright (c) 2008-2021 Jb Evain

Licensed under the MIT/X11 License:

> Permission is hereby granted, free of charge, to any person obtaining a copy
> of this software and associated documentation files (the "Software"), to deal
> in the Software without restriction, including without limitation the rights
> to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
> copies of the Software, and to permit persons to whom the Software is
> furnished to do so, subject to the following conditions:
>
> The above copyright notice and this permission notice shall be included in all
> copies or substantial portions of the Software.
>
> THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
> IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
> FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
> AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
> LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
> OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
> SOFTWARE.

## Test-only dependencies (not redistributed in the release archive)

Used solely by the `MeatyMod.Tests` project; not included in the `meatymod`
release archive.

- **xUnit.net** (xunit 2.9.3, xunit.runner.visualstudio 3.1.4) — Apache-2.0,
  Copyright (c) .NET Foundation and Contributors. The `xunit.assert` component
  is MIT. See https://licenses.nuget.org/Apache-2.0
- **coverlet.collector** (6.0.4) — MIT.
  See https://licenses.nuget.org/MIT
- **Microsoft.NET.Test.Sdk** (17.14.1) — MIT.
  See https://licenses.nuget.org/MIT

## Note on MonoGame / XNA

The Blood & Bacon game (not this tool) is built on XNA 4.0 / MonoGame. Those are
dependencies of the game, not of the `meatymod` tool, and are not distributed by
this project. This project makes no claim over their licenses.
