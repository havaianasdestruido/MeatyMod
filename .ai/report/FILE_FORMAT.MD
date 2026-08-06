# FILE_FORMAT.MD
- XNB: Magic 'XNB', version byte 5 (XNA 4.0), flags; reader type IDs + shared resources (MonoGame layout).
- TXT (ABCDE3): day files = line-based int/float fields (line 1 'am'/'pm'); camMove/camBoss = 6 floats/keyframe (pos xyz + target xyz); farmer_/cutty_/blimp/target = dialogue text lines.
- RAW (Content/astro/brushes): raw heightmap data, no header (8 MB each). 'fbm' retracted — no .fbm files exist in the shipped game.
