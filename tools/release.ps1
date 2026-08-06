# MeatyMod release packager.
# Builds the solution in Release, stages a distributable layout, writes SHA-256
# sums, and compresses it into dist\meatymod-<version>.zip.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File tools\release.ps1

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$script:exitCode = 0

$RepoRoot    = Split-Path -Parent $PSScriptRoot
$Solution    = Join-Path $RepoRoot 'src\MeatyMod.sln'
$VersionFile = Join-Path $RepoRoot 'src\MeatyMod.Core\VersionInfo.cs'
$BuildOut    = Join-Path $RepoRoot 'src\MeatyMod.Cli\bin\Release\net10.0'
$StagingBase = 'C:\Users\mcmco\AppData\Local\Temp\opencode\meatymod-release'
$DistDir     = Join-Path $RepoRoot 'dist'
$StagingDir  = $null

function Get-RelativePath {
    param([string]$Root, [string]$Path)
    return $Path.Substring($Root.TrimEnd('\').Length + 1)
}

try {
    Write-Host '[1/6] Building solution (Release)...'
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw 'dotnet not found on PATH.'
    }
    & dotnet build $Solution -c Release
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE"
    }

    Write-Host '[1/6] Reading version ...'
    $raw = Get-Content -Raw -LiteralPath $VersionFile
    $match = [regex]::Match($raw, 'Version\s*=\s*"([^"]+)"')
    if (-not $match.Success) {
        throw "Could not parse version from $VersionFile"
    }
    $Version = $match.Groups[1].Value

    if (-not (Test-Path -LiteralPath $BuildOut)) {
        throw "Build output directory not found: $BuildOut"
    }

    $StagingDir = Join-Path $StagingBase ("meatymod-" + $Version)
    if (Test-Path -LiteralPath $StagingDir) {
        Remove-Item -LiteralPath $StagingDir -Recurse -Force
    }
    $LibDir = Join-Path $StagingDir 'lib'
    New-Item -ItemType Directory -Path $LibDir -Force | Out-Null

    Write-Host "[2/6] Staging release under $StagingDir ..."
    foreach ($name in 'meatymod.exe', 'meatymod.dll', 'meatymod.runtimeconfig.json', 'meatymod.deps.json') {
        $src = Join-Path $BuildOut $name
        if (Test-Path -LiteralPath $src) {
            Copy-Item -LiteralPath $src -Destination $LibDir
        }
    }
    Get-ChildItem -LiteralPath $BuildOut -Filter 'MeatyMod.*.dll' | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $LibDir
    }
    Get-ChildItem -LiteralPath $BuildOut -Filter 'Mono.Cecil.dll' | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $LibDir
    }

    Write-Host '[3/6] Copying documentation ...'
    Copy-Item -LiteralPath (Join-Path $RepoRoot 'THIRD_PARTY_NOTICES.md') -Destination $StagingDir
    Copy-Item -LiteralPath (Join-Path $RepoRoot 'README.md') -Destination $StagingDir

    Write-Host '[4/6] Computing SHA-256 sums ...'
    $sumLines = @()
    foreach ($file in (Get-ChildItem -LiteralPath $StagingDir -Recurse -File | Sort-Object FullName)) {
        $rel = Get-RelativePath -Root $StagingDir -Path $file.FullName
        $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $file.FullName).Hash.ToLowerInvariant()
        $sumLines += ('{0}  {1}' -f $hash, $rel)
    }
    $sumLines | Set-Content -LiteralPath (Join-Path $StagingDir 'SHA256SUMS.txt') -Encoding Ascii
    Write-Host ("SHA256SUMS.txt entries: {0}" -f $sumLines.Count)

    Write-Host '[5/6] Compressing staging directory ...'
    if (-not (Test-Path -LiteralPath $DistDir)) {
        New-Item -ItemType Directory -Path $DistDir | Out-Null
    }
    $ZipPath = Join-Path $DistDir ("meatymod-" + $Version + '.zip')
    if (Test-Path -LiteralPath $ZipPath) {
        Remove-Item -LiteralPath $ZipPath -Force
    }
    Compress-Archive -Path $StagingDir -DestinationPath $ZipPath -CompressionLevel Optimal

    Write-Host '[6/6] Done.'
    Write-Host ("Version      : {0}" -f $Version)
    Write-Host ("Archive      : {0}" -f $ZipPath)
    Write-Host ("Staged files : {0}" -f $sumLines.Count)
}
catch {
    Write-Error $_.Exception.Message
    $script:exitCode = 1
}
finally {
    if ($StagingDir -and (Test-Path -LiteralPath $StagingDir)) {
        Remove-Item -LiteralPath $StagingDir -Recurse -Force
    }
}

exit $script:exitCode
