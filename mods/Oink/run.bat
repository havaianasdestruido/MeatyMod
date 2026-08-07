@echo off
setlocal EnableDelayedExpansion

set "SCRIPT_DIR=%~dp0"
set "REPO=%SCRIPT_DIR%..\.."

call "%SCRIPT_DIR%build.bat"
if errorlevel 1 exit /b 1

set "GAME_EXE=%REPO%\game\Blood and Bacon\BloodandBacon.exe"
set "MEATYMOD_REL=%REPO%\src\MeatyMod.Cli\bin\Release\net10.0\meatymod.exe"
set "MEATYMOD_DBG=%REPO%\src\MeatyMod.Cli\bin\Debug\net10.0\meatymod.exe"
set "MEATYMOD=%MEATYMOD_REL%"
if not exist "%MEATYMOD_REL%" set "MEATYMOD=%MEATYMOD_DBG%"
if exist "%MEATYMOD_REL%" if exist "%MEATYMOD_DBG%" (
    for %%A in ("%MEATYMOD_REL%") do set "REL_T=%%~tA"
    for %%A in ("%MEATYMOD_DBG%") do set "DBG_T=%%~tA"
    if "!DBG_T!" GTR "!REL_T!" set "MEATYMOD=%MEATYMOD_DBG%"
)
set "MOD_DLL=%SCRIPT_DIR%src\Oink\bin\Release\net40\Oink.dll"

if not exist "%GAME_EXE%" (
    echo Game executable not found: %GAME_EXE%
    exit /b 1
)
if not exist "%MOD_DLL%" (
    echo Oink.dll not found after build: %MOD_DLL%
    exit /b 1
)
if not exist "%MEATYMOD%" (
    echo meatymod.exe not found. Build it first: all.bat
    exit /b 1
)

echo [Oink] Restoring original exe...
"%MEATYMOD%" restore "%GAME_EXE%" >nul 2>&1

echo [Oink] Injecting Oink mod...
"%MEATYMOD%" inject "%GAME_EXE%" "%MOD_DLL%"
if errorlevel 1 exit /b 1

echo [Oink] Launching game...
start "" /wait "%GAME_EXE%"

echo [Oink] Restoring original exe...
"%MEATYMOD%" restore "%GAME_EXE%" >nul 2>&1
echo Done.
