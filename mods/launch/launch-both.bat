@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "REPO=%SCRIPT_DIR%..\.."
set "GAME_EXE=%REPO%\game\Blood and Bacon\BloodandBacon.exe"
set "MEATYMOD=%REPO%\src\MeatyMod.Cli\bin\Release\net10.0\meatymod.exe"
if not exist "%MEATYMOD%" set "MEATYMOD=%REPO%\src\MeatyMod.Cli\bin\Debug\net10.0\meatymod.exe"
set "OINK_DLL=%REPO%\mods\Oink\src\Oink\bin\Release\net40\Oink.dll"
set "QUACK_DLL=%REPO%\mods\QuackMenu\src\QuackMenu\bin\Release\net40\QuackMenu.dll"

if not exist "%GAME_EXE%" (
    echo Game executable not found: %GAME_EXE%
    exit /b 1
)
if not exist "%OINK_DLL%" (
    echo Oink.dll not found. Build it first: dotnet build mods\Oink\src\Oink\Oink.csproj -c Release
    exit /b 1
)
if not exist "%QUACK_DLL%" (
    echo QuackMenu.dll not found. Build it first: dotnet build mods\QuackMenu\src\QuackMenu\QuackMenu.csproj -c Release
    exit /b 1
)
if not exist "%MEATYMOD%" (
    echo meatymod.exe not found. Build it first: dotnet build src\MeatyMod.Cli\MeatyMod.Cli.csproj
    exit /b 1
)

echo [Both] Restoring original exe...
"%MEATYMOD%" restore "%GAME_EXE%" >nul 2>&1

echo [Both] Injecting Oink + QuackMenu mods...
"%MEATYMOD%" inject "%GAME_EXE%" --mod "%OINK_DLL%" --mod "%QUACK_DLL%"
if errorlevel 1 exit /b 1

echo [Both] Launching game...
start "" /wait "%GAME_EXE%"

echo [Both] Restoring original exe...
"%MEATYMOD%" restore "%GAME_EXE%" >nul 2>&1
echo Done.
