@echo off
setlocal

set "ROOT=%~dp0"

echo === [all] Building MeatyMod solution (src) ===
dotnet build "%ROOT%src\MeatyMod.sln" -c Release
if errorlevel 1 (
    echo [all] Solution build FAILED.
    exit /b 1
)

echo === [all] Building tools\modharness ===
dotnet build "%ROOT%tools\modharness\ModHarness.csproj" -c Release
if errorlevel 1 (
    echo [all] modharness build FAILED.
    exit /b 1
)

echo === [all] Building tools\memscan ===
dotnet build "%ROOT%tools\memscan\MemScan.csproj" -c Release
if errorlevel 1 (
    echo [all] memscan build FAILED.
    exit /b 1
)

echo === [all] Building mods\Oink ===
call "%ROOT%mods\Oink\build.bat"
if errorlevel 1 (
    echo [all] Oink build FAILED.
    exit /b 1
)

echo === [all] Building mods\QuackMenu ===
call "%ROOT%mods\QuackMenu\build.bat"
if errorlevel 1 (
    echo [all] QuackMenu build FAILED.
    exit /b 1
)

echo === [all] All builds OK ===
