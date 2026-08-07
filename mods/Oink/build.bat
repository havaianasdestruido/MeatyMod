@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "CSPROJ=%SCRIPT_DIR%src\Oink\Oink.csproj"

echo [Oink] Building mod...
dotnet build "%CSPROJ%" -c Release
if errorlevel 1 (
    echo [Oink] Build FAILED.
    exit /b 1
)

echo [Oink] Build OK: %SCRIPT_DIR%src\Oink\bin\Release\net40\Oink.dll
