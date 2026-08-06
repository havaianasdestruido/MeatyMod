# Known Issues List
1. CS8618 XnbReader.cs:22 — nullable field, severity LOW. CLOSED in 1.0.0 — field initialized to `string.Empty` in XnbReader.cs.
2. Oversized file check missing — severity LOW. CLOSED in 1.0.0 — FileSizeGuard added (src\MeatyMod.Core\FileSizeGuard.cs).
3. FBM/TXT parsers unimplemented — severity MED. CLOSED in 1.0.0 — TxtReader/CameraTrackParser/DialogueParser implemented; FBM format retracted (does not exist).
4. Oink pig skin UV mismatch — severity MED, OPEN. Pig skin uses the game's pig texture applied to the human model's UV layout, so parts look misaligned. Root cause: pig skeleton (Bip01) incompatible with player rig; game hardcodes farmer bone indices. See mods\Oink\README.md. No fix yet.
