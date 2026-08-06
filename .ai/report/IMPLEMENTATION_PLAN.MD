# IMPLEMENTATION_PLAN.MD
- Phase 1: Format lib (XNB + TXT parsers) — DONE.
- Phase 2: Asset extractor/manifest — DONE.
- Phase 3: Injection (Mono.Cecil, auto entry detection) — DONE.
- Phase 4: CLI (pack/install/inject/restore/manifest/verify/parse) — DONE.
- Phase 5: Release hardening (multi-mod injection, XNB decompression + xnb command, RawReader/.raw parse, checksums + checksum command, release packaging via tools\release.ps1 + THIRD_PARTY_NOTICES.md, game smoke suite via tools\smoke.ps1, security fixes, expanded test suite) — DONE.
