@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "CSPROJ=%SCRIPT_DIR%src\QuackMenu\QuackMenu.csproj"

echo [QuackMenu] Building mod...
dotnet build "%CSPROJ%" -c Release
if errorlevel 1 (
    echo [QuackMenu] Build FAILED.
    exit /b 1
)

echo [QuackMenu] Build OK: %SCRIPT_DIR%src\QuackMenu\bin\Release\net40\QuackMenu.dll
