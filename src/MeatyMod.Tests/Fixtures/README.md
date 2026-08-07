# Fixtures

## darkFog3_0.xnb

- **Provenance:** copied verbatim from `game\Blood and Bacon\Content\darkFog3_0.xnb` (the game's own content directory, which is gitignored). Small enough (~224 bytes) that embedding a copy here is practical and avoids any dependency on the game being installed.
- **Format:** XNB v5, platform `0x77` (Windows), flags `0x81` (LZX-compressed | hi-def). Decompressed size from header: 893 bytes.
- **Fixture hash (the .xnb file itself):** `5FBB4FB8416E84429A7E7ED286D5FCD4DBD5D0ED5F370E1E752D567135E7B752`
- **Expected decompressed SHA-256 (captured at authoring time):** `FB2F56A61B40ADB076C09B27128494ADF106C511B6C778191909D8A9248E2692`
- **Why it exists:** gives `LzxFixtureTests.cs` a deterministic, self-contained positive LZX decompression case that runs on machines without the game installed (open item R2 in `.ai\maintenance\REGRESSIONS.MD`).
