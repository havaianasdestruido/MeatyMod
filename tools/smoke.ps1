$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$game = Join-Path $repoRoot 'game\Blood and Bacon'
$cliProject = Join-Path $repoRoot 'src\MeatyMod.Cli\MeatyMod.Cli.csproj'
$exe = Join-Path $repoRoot 'src\MeatyMod.Cli\bin\Debug\net10.0\meatymod.exe'
$contentDir = Join-Path $game 'Content'
$tmpOut = Join-Path $env:TEMP ('meatymod_smoke_{0}.json' -f [guid]::NewGuid().ToString('N'))

$failures = New-Object System.Collections.ArrayList
function Assert-True($condition, $detail)
{
    if (-not $condition)
    {
        [void]$failures.Add($detail)
    }
}

Write-Output "Building $cliProject ..."
& dotnet build $cliProject -c Debug | Out-Host
Assert-True ($LASTEXITCODE -eq 0) ("dotnet build failed (exit {0})" -f $LASTEXITCODE)

if (-not (Test-Path -LiteralPath $exe))
{
    [void]$failures.Add("built exe not found: $exe")
}
else
{
    $verifyOut = & $exe verify $game 2>&1
    $verifyExit = $LASTEXITCODE
    $verifyText = $verifyOut -join "`n"
    $vm = [regex]::Match($verifyText, 'Valid:\s*(\d+)\s+Invalid:\s*(\d+)')
    Assert-True ($verifyExit -eq 0) ("verify exit {0} (expected 0)" -f $verifyExit)
    Assert-True $vm.Success "verify output missing 'Valid:/Invalid:' line: $verifyText"
    if ($vm.Success)
    {
        Assert-True ($vm.Groups[1].Value -eq '1860') ("verify Valid {0} (expected 1860)" -f $vm.Groups[1].Value)
        Assert-True ($vm.Groups[2].Value -eq '0') ("verify Invalid {0} (expected 0)" -f $vm.Groups[2].Value)
    }

    $manifestOut = & $exe manifest $contentDir $tmpOut 2>&1
    $manifestExit = $LASTEXITCODE
    Assert-True ($manifestExit -eq 0) ("manifest exit {0} (expected 0)" -f $manifestExit)
    if (Test-Path -LiteralPath $tmpOut)
    {
        $manifestText = Get-Content -LiteralPath $tmpOut -Raw
        $keyCount = [regex]::Matches($manifestText, '(?m)^\s*"((?:[^"\\]|\\.)*)"\s*:').Count
        Assert-True ($keyCount -eq 1353) ("manifest key count {0} (expected 1353 unique basenames)" -f $keyCount)
        Remove-Item -LiteralPath $tmpOut -Force
    }
    else
    {
        [void]$failures.Add("manifest did not write $tmpOut")
    }

    $parseTxtOut = & $exe parse (Join-Path $game 'ABCDE3\2.txt') 2>&1
    $parseTxtExit = $LASTEXITCODE
    $parseTxtText = $parseTxtOut -join "`n"
    Assert-True ($parseTxtExit -eq 0) ("parse 2.txt exit {0} (expected 0)" -f $parseTxtExit)
    Assert-True ($parseTxtText -match 'Lines:\s*\d+') ("parse 2.txt output missing Lines count: $parseTxtText")

    $parseCamOut = & $exe parse (Join-Path $game 'ABCDE3\camMove1.txt') 2>&1
    $parseCamExit = $LASTEXITCODE
    Assert-True ($parseCamExit -eq 0) ("parse camMove1.txt exit {0} (expected 0)" -f $parseCamExit)

    foreach ($name in @('earth.raw', 'earth2.raw'))
    {
        $rawPath = Join-Path $contentDir ("astro\brushes\{0}" -f $name)
        if (Test-Path -LiteralPath $rawPath)
        {
            $len = (Get-Item -LiteralPath $rawPath).Length
            Assert-True ($len -eq 8000000) ("{0} length {1} (expected 8000000)" -f $name, $len)
        }
        else
        {
            [void]$failures.Add("raw heightmap missing: $rawPath")
        }
    }

    $wmvCount = @(Get-ChildItem -LiteralPath $game -Recurse -Filter *.wmv -File).Count
    Assert-True ($wmvCount -eq 4) ("wmv count {0} (expected 4)" -f $wmvCount)

    $xnbContentCount = @(Get-ChildItem -LiteralPath $contentDir -Recurse -Filter *.xnb -File).Count
    Assert-True ($xnbContentCount -eq 1396) ("xnb under Content count {0} (expected 1396)" -f $xnbContentCount)
}

if ($failures.Count -gt 0)
{
    Write-Output ('SMOKE FAIL ' + ($failures -join '; '))
    exit 1
}

Write-Output 'SMOKE PASS'
exit 0
