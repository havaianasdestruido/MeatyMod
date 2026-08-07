# Issue Tracker Summary
- CLOSED: CS8618 nullable field (XnbReader.cs) — fixed.
- CLOSED: oversized file guard missing — implemented (FileSizeGuard).
- CLOSED: FBM/TXT parsers unimplemented — TXT parsers implemented; FBM retracted (no such format).
- OPEN LOW: Oink pig skin UV mismatch on human model.
- DONE in 1.0.0: injector supports only one mod at a time — multi-mod inject via `--mod a --mod b` implemented and tested (InjectCommand repeats --mod/--entry; IL probe confirmed 2 Inject calls; InjectorTests + InjectEdgeTests cover it).
- DONE in 1.0.0: only one mod injectable at a time (IL ctor patch) — multi-mod inject, --mod/--entry.
- Lifecycle: all tracked, none critical.
- Tooling (2026-08-07): tools\modharness (headless Inject proof, x86) and tools\memscan (process memory viewer) exist as QA tools; repo has all.bat + per-mod build.bat/run.bat.
