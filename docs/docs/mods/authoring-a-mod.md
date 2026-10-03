---
id: authoring-a-mod
title: Authoring a mod
sidebar_label: Authoring a mod
description: Build a Blood & Bacon mod from an empty folder to a running in-game feature.
---

# Authoring a mod

This builds a mod from scratch. The only hard requirement the injector imposes is a type exposing:

```csharp
public static void Inject(Microsoft.Xna.Framework.Game game)
```

Everything else is convention — but the conventions below are what `inject`, `run.bat` and the harness expect.

## 1. Create the directory

```text
mods/MyMod/
├─ manifest.json
├─ config.txt
├─ build.bat
├─ .gitignore
├─ lib/xna/              copy from mods\Oink\lib\xna
└─ src/MyMod/
   ├─ MyMod.csproj
   ├─ MyModEntry.cs
   └─ MyModHook.cs
```

The depth matters: `inject` finds `config.txt` by walking **five** directories up from the built DLL (`bin\Release\net40` → `src\MyMod` → `MyMod`).

```bat
mkdir mods\MyMod\src\MyMod
xcopy /E /I mods\Oink\lib mods\MyMod\lib
```

## 2. `manifest.json`

```json
{
  "Id": "mymod",
  "Name": "My Mod",
  "Version": "1.0.0",
  "Author": "you",
  "Description": "What it does, in one line.",
  "Replaces": []
}
```

`Id` must match `^[A-Za-z0-9_.-]+$`. See [mod manifest](../formats/mod-manifest.md).

## 3. `MyMod.csproj`

Target `net40` with `LangVersion 7.3` — the game runs on .NET Framework 4.0 and will not load a newer assembly.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net40</TargetFramework>
    <AssemblyName>MyMod</AssemblyName>
    <RootNamespace>MyMod</RootNamespace>
    <LangVersion>7.3</LangVersion>
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <GenerateAssemblyInfo>true</GenerateAssemblyInfo>
    <EnableDefaultCompileItems>true</EnableDefaultCompileItems>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include="Microsoft.Xna.Framework">
      <HintPath>$(MSBuildThisFileDirectory)..\..\lib\xna\Microsoft.Xna.Framework.dll</HintPath>
    </Reference>
    <Reference Include="Microsoft.Xna.Framework.Game">
      <HintPath>$(MSBuildThisFileDirectory)..\..\lib\xna\Microsoft.Xna.Framework.Game.dll</HintPath>
    </Reference>
    <Reference Include="Microsoft.Xna.Framework.Graphics">
      <HintPath>$(MSBuildThisFileDirectory)..\..\lib\xna\Microsoft.Xna.Framework.Graphics.dll</HintPath>
    </Reference>
    <Reference Include="Microsoft.Xna.Framework.Storage">
      <HintPath>$(MSBuildThisFileDirectory)..\..\lib\xna\Microsoft.Xna.Framework.Storage.dll</HintPath>
    </Reference>
  </ItemGroup>

</Project>
```

:::note Why `LangVersion 7.3`
It is the highest C# version fully supported on .NET Framework 4.0 without extra compiler shims. File-scoped namespaces, records and nullable annotations are not available here — the suite's `.editorconfig` rules apply to `src\`, not to the mods.
:::

## 4. The entry point

```csharp
using System;
using System.IO;
using Microsoft.Xna.Framework;

namespace MyMod
{
    public static class MyModEntry
    {
        private static bool _injected;
        private static Game _game;
        private static object _screenManager;
        private static MyModHook _hook;

        // The injector calls exactly this signature.
        public static void Inject(Game game)
        {
            if (_injected) { return; }      // a double patch must be harmless

            _game = game;

            if (!game.Components.Contains(_hook))
            {
                _hook = new MyModHook(game);
                game.Components.Add(_hook);
            }

            _injected = true;
            Log("MyMod injected.");
        }

        public static void Update()
        {
            if (!_injected) { return; }

            if (_screenManager == null) { FindScreenManager(_game); }
            // per-frame work here
        }

        private static void FindScreenManager(Game game)
        {
            try
            {
                foreach (var c in game.Components)
                {
                    if (c != null && c.GetType().FullName == "Blood.ScreenManager")
                    {
                        _screenManager = c;
                        return;
                    }
                }
            }
            catch (Exception ex) { Log("FindScreenManager failed: " + ex.Message); }
        }

        public static void Log(string message)
        {
            try
            {
                string dir = AppDomain.CurrentDomain.BaseDirectory;
                File.AppendAllText(Path.Combine(dir, "mymod.log"),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + message + Environment.NewLine);
            }
            catch { }
        }
    }
}
```

**Rules for `Inject`:**

| Do | Don't |
| --- | --- |
| load config files | call `Content.Load` — the device may not exist yet |
| add a `GameComponent` | expect `Blood.ScreenManager` to be in `Components` |
| guard with `_injected` | throw — an exception here kills the game's constructor |
| log one line so you can prove it ran | do heavy work; the game has not started |

## 5. The hook

```csharp
using Microsoft.Xna.Framework;

namespace MyMod
{
    public class MyModHook : GameComponent
    {
        public MyModHook(Game game) : base(game)
        {
            UpdateOrder = int.MaxValue;   // after the game; use int.MinValue to run before it
        }

        public override void Update(GameTime gameTime)
        {
            MyModEntry.Update();
            base.Update(gameTime);
        }
    }
}
```

Need to draw? Derive from `DrawableGameComponent`, set `DrawOrder` high, create your `SpriteBatch` in `LoadContent`, and see `BossMenuScreen.cs` for a complete overlay.

## 6. Reflection helpers

Game types are `internal`, so everything goes through reflection — and everything fails soft:

```csharp
using System.Reflection;

private const BindingFlags Any =
    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

public static object GetField(object target, string name)
{
    try
    {
        var f = target?.GetType().GetField(name, Any);
        if (f == null) { MyModEntry.Log("No field " + name); return null; }
        return f.GetValue(target);
    }
    catch (Exception ex) { MyModEntry.Log("GetField " + name + " failed: " + ex.Message); return null; }
}

public static void SetField(object target, string name, object value)
{
    try
    {
        var f = target?.GetType().GetField(name, Any);
        if (f == null) { MyModEntry.Log("No field " + name); return; }
        f.SetValue(target, value);
    }
    catch (Exception ex) { MyModEntry.Log("SetField " + name + " failed: " + ex.Message); }
}
```

Known field and type names are tabulated in [Mod runtime model](../architecture/mod-runtime.md#stage-4--reflection-against-internal-game-types).

## 7. `config.txt`

```ini
# MyMod configuration
Enabled=true
Intensity=1.0
ToggleKey=M
```

Parse it with the pattern in [mod config](../formats/mod-config.md): probe `Content\MyMod\config.txt` then `mymod.txt`, store values as strings, convert with `TryParse` plus a fallback.

## 8. `build.bat`

```bat
@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "CSPROJ=%SCRIPT_DIR%src\MyMod\MyMod.csproj"

echo [MyMod] Building mod...
dotnet build "%CSPROJ%" -c Release
if errorlevel 1 (
    echo [MyMod] Build FAILED.
    exit /b 1
)

echo [MyMod] Build OK: %SCRIPT_DIR%src\MyMod\bin\Release\net40\MyMod.dll
```

Copy `mods\Oink\run.bat` and change the three paths for a build → restore → inject → launch → restore loop.

## 9. Build, inject, test

```bat
mods\MyMod\build.bat

meatymod inject "game\Blood and Bacon\BloodandBacon.exe" ^
  --mod mods\MyMod\src\MyMod\bin\Release\net40\MyMod.dll

:: play, then always:
meatymod restore "game\Blood and Bacon\BloodandBacon.exe"
```

If the injector cannot pick your entry type unambiguously, name it explicitly:

```bat
meatymod inject … --mod MyMod.dll --entry MyMod.MyModEntry
```

Verify it actually ran:

```bat
type "game\Blood and Bacon\mymod.log"
tools\memscan\bin\Release\net10.0\memscan.exe BloodandBacon --find-module MyMod.dll --marker "MyMod injected"
```

## 10. Package it

```bat
meatymod pack mods\MyMod
```

Remember `pack` excludes `bin\`, so the archive carries source, manifest, config and docs — not the DLL. See [mod package](../formats/mod-package.md).

## Checklist

- [ ] `public static void Inject(Game game)` exists and is idempotent
- [ ] exactly one type with a static one-argument `Inject` (or pass `--entry`)
- [ ] `Inject` never throws and never loads content
- [ ] a `GameComponent` hook with a deliberate `UpdateOrder`
- [ ] all game access through fail-soft reflection, logged on failure
- [ ] `manifest.json` passes validation
- [ ] `config.txt` at the mod root, five levels above the DLL
- [ ] a log line on inject, so "did it run" is answerable
- [ ] `restore` leaves the game clean

## Pitfalls

| Symptom | Cause |
| --- | --- |
| `No mod entry type found in mod DLL` | no type has a static one-argument `Inject` |
| wrong type selected | several candidates — pass `--entry` |
| game crashes on launch | `Inject` threw, or the DLL targets the wrong framework |
| mod DLL not found at runtime | it was not copied next to the exe (`inject` does this; manual copies must too) |
| config ignored | `config.txt` is not five levels above the DLL, or the key name does not match the `switch` |
| effect flickers | another system overwrites your field — adjust `UpdateOrder` |
| nothing in the log | `Inject` never ran; verify the patch, then [troubleshoot](../reference/troubleshooting.md) |
