# Engine Identification — Phase 0

## Engine Identification
- **Engine**: XNA Framework 4.0 (Vanilla).
- **Architecture**: Managed .NET 4.0.
- **Steamworks**: Steamworks.NET 9.0 + native steam_api.dll.

## Executable Analysis
- **File**: `BloodandBacon.exe` (4.8MB).
- **.NET Framework**: Target 4.0.
- **Entry Point**: `Blood.Program.Main`.
- **Content Pipeline**: XNA ContentManager-based, synchronous loading.

## Launch Arguments & Debugging
- Search of decompiled source shows no explicit CLI parser. Debugging relies on in-game developer features (internal-only strings `developer`, `cheats`).

## DRM & Integrity
- Steamworks initialization via `BloodnBaconMP`. No strong anti-tamper detected.

## Evidence
- `BloodandBacon.exe.config` confirms `sku=".NETFramework,Version=v4.0"`.
- Decompilation confirms `Microsoft.Xna.Framework` references.
