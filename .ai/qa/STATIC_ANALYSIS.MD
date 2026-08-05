# Static Analysis Report
- Tool: dotnet build with warnings as errors off.
- Compiler warnings: 1 (CS8618 XnbReader.cs:22).
- Nullable issues: 1 (documented).
- Unreachable code: none.
- Resource leaks: FileStreams disposed via using.
- Verdict: PASS with 1 documented warning.
