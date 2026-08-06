# Functional Testing Report
- Features tested: pack, install, multi-mod inject, restore, manifest build, verify, parse (txt + raw), xnb (header + decompression), checksum.
- Expected: commands produce zip (with checksums.txt), extract with backup, parse manifest, validate XNB, dump XNB info, print SHA-256 hashes.
- Observed: all pass (unit + manual smoke).
- Edge cases: unknown command prints usage (verified).
- Verdict: PASS
