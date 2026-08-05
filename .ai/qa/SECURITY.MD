# Security Assessment
- Path traversal: validated (relative path normalization).
- Malformed XNB: header check rejects invalid magic.
- Corrupt archives: zip exceptions handled.
- Oversized files: not enforced (TODO).
- Verdict: PASS with 1 TODO.
