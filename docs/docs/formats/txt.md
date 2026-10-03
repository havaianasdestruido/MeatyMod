---
id: txt
title: TXT assets
sidebar_label: TXT assets
description: The game's positional plain-text assets — day files, camera tracks and dialogue.
---

# TXT assets

Blood & Bacon keeps a surprising amount of tuning data in loose `.txt` files next to its compiled content. They share one convention: **meaning is positional**. There are no keys, no sections and no comments — line *n* means whatever the game's loader decides line *n* means.

## Normalisation

Every reader in `MeatyMod.Formats` applies the same two rules before indexing:

1. trim leading and trailing whitespace from each line;
2. drop lines that are then empty.

So blank lines and indentation never shift an index. `TxtReader.Read(path)` returns a [`TxtDocument`](../api/meatymod-formats.md#txtdocument) over the normalised lines.

```csharp
var doc = TxtReader.Read(@"Content\day1.txt");

int   waves     = doc.GetInt(0);
float spawnRate = doc.GetFloat(1);
bool  bossDay   = doc.GetBool(2);        // non-zero int == true
string label    = doc.GetString(3);      // "" when the line is missing
```

Out-of-range indices return the type default rather than throwing, which mirrors how the game tolerates short files.

## Sub-formats

### Day / tuning files

Flat lists of numbers. `meatymod parse` prints every line with its index and reports whether the whole file is numeric:

```text
Lines: 12
0: 3
1: 1
2: 450
…
All values numeric: True
```

That last line only appears when every line parses as `int` or `float`.

### Camera tracks

Six floats per keyframe, one value per line, in the order **position X, Y, Z** then **target X, Y, Z**:

```text
12.5
3.25
-40
0
2
0
12.1
3.25
-38.4
0
2
0
```

[`CameraTrackParser.Parse`](../api/meatymod-formats.md#cameratrackparser) emits one `CameraKeyframe` per complete group of six. Unparsable lines are skipped and a trailing partial group is discarded, so a truncated file yields the keyframes it can.

`meatymod parse` auto-detects this shape with the heuristic *line 0 contains a `.` **and** the line count is a multiple of 6*, then prints the keyframe count and the first three keyframes.

### Dialogue

One spoken line per file line. [`DialogueParser.Parse`](../api/meatymod-formats.md#dialogueparser) returns them as `IReadOnlyList<string>`; there is no speaker, timing or markup syntax to parse. `meatymod parse` does not special-case dialogue because the generic line dump already shows exactly this.

## Editing TXT assets

These files are **not** XNB — they can be edited with any text editor and shipped in a mod package as-is:

```bat
meatymod pack mods\MyTuning
meatymod install mod.zip "game\Blood and Bacon"
```

`install` backs up the original to `Backups\MeatyMod\` before overwriting. Rules of thumb:

- keep the line **count and order** identical unless you know the loader tolerates more;
- use `.` as the decimal separator — parsing is `CultureInfo.InvariantCulture`;
- do not add comments; there is no comment syntax. `TxtReader` drops only blank lines, so a `# note` line is kept as a normal entry: it takes an index of its own and **pushes every later field down by one**. A typed getter will return its default (`0`) for the comment's own position, but the real damage is the shift — everything after it is read from the wrong line;
- re-run `meatymod parse` on the edited file to confirm it still reads the way you expect.

:::tip Mod config files are a different thing
`config.txt` inside a mod directory is MeatyMod's own `key=value` format, not a game asset. See [mod config](./mod-config.md).
:::

## Related

- [`meatymod parse`](../cli/parse.md)
- [`MeatyMod.Formats` API](../api/meatymod-formats.md#txtreader)
