# Known Issues List
1. CS8618 XnbReader.cs:22 — nullable field, severity LOW. CLOSED in 1.0.0 — field initialized to `string.Empty` in XnbReader.cs.
2. Oversized file check missing — severity LOW. CLOSED in 1.0.0 — FileSizeGuard added (src\MeatyMod.Core\FileSizeGuard.cs).
3. FBM/TXT parsers unimplemented — severity MED. CLOSED in 1.0.0 — TxtReader/CameraTrackParser/DialogueParser implemented; FBM format retracted (does not exist).
4. Oink pig skin UV mismatch — severity MED, OPEN. Pig skin uses the game's pig texture applied to the human model's UV layout, so parts look misaligned. Root cause: pig skeleton (Bip01) incompatible with player rig; game hardcodes farmer bone indices. See mods\Oink\README.md. No fix yet.
   T6 revisit (2026-08-07) findings:
   - What OinkSkin.Apply swaps: `player1Texture` + `player1TextureOrig` on Blood.BloodnBacon.
   - These fields DO reach the visible player model draw — via the blood-compositing path: executePlayer1Blood/delPlayer1Blood draw them into 600x600 render targets then set `quickSkin1.Parameters["Texture"] = player1Texture` (BloodnBacon.cs:24784/24830), and DrawMyChar binds quickSkin1 to localModel (BloodnBacon.cs:27638). So the swap IS visible; it is not compositing-only. Prior note that they "only feed blood compositing" was incorrect.
   - Real mismatch: human skin `texture/jon6` and pig skin `npc/piggy1` are both 600x600 DXT1 atlases (verified from XNB headers) but with different internal UV layouts; human UVs sample pig atlas regions in wrong places. Resolution/size is not the cause.
   - Caveat: quickSkin1.Texture is refreshed only when blood paint/clean runs; before first hit the model samples the last composited target (unset at game start).
   - Ranked options: (a) texture-only — already correct fields, gives visible-but-misaligned skin; (b) model swap — infeasible (Bip01 vs hardcoded bone indices 15..28 in playerBones/DrawMyChar); (c) tint/recolor human atlas (keep UV layout) — medium effort, pig-colored farmer; (d) accept cosmetic limitation (disable PigSkin).
   - NOT fixed. Doc updated in mods\Oink\README.md "Limitations".
5. Tooling (2026-08-07): tools\modharness (headless Inject proof, x86) and tools\memscan (process memory viewer) exist as QA tools; repo has all.bat + per-mod build.bat/run.bat.
