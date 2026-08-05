# Revised Architecture Overview

- **Entry Point**: CLI that parses commands and loads configuration.
- **Core Engine**: `MeatyMod.Core` handles plugin lifecycle, dependency graph, and mod discovery.
- **Asset I/O**: `MeatyMod.Formats` provides parsers/writers for XNB, FBM, and custom binary formats.
- **Patch System**: `MeatyMod.Injector` performs IL weaving, method detours, and runtime code injection.
- **Extensibility**: Modular service container allows third‑party extensions via well‑defined interfaces.
- **Testing Harness**: `MeatyMod.Testing` supplies mock environments for unit and integration tests.

