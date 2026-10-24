#requires -Version 7.0
param([switch]$VerifyGuard)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location $root
try {
    dotnet build 'UI Framework.slnx' -c Release --disable-build-servers -m:1 -warnaserror
    if ($LASTEXITCODE -ne 0) { throw 'Public API validation failed.' }
    if ($VerifyGuard) {
        $baseline = Join-Path $root 'UI Framework/PublicAPI.Shipped.txt'
        $original = [IO.File]::ReadAllBytes($baseline)
        $text = [IO.File]::ReadAllText($baseline)
        try {
            foreach ($diagnostic in @('RS0016', 'RS0017')) {
                $probe = if ($diagnostic -eq 'RS0016') {
                    ($text -split "`n" | Where-Object { $_.Trim() -ne 'UI_Framework.View' }) -join "`n"
                } else { $text + "`nUI_Framework.ApiCompatibilityMissing`n" }
                [IO.File]::WriteAllText($baseline, $probe)
                $output = & dotnet build 'UI Framework/UI Framework.csproj' -c Release --no-restore --disable-build-servers 2>&1
                $code = $LASTEXITCODE
                if ($code -eq 0 -or ($output -join "`n") -notmatch "error $diagnostic") {
                    throw "API guard did not reject the $diagnostic probe. $output"
                }
                Write-Output "PASS: $diagnostic rejected an unreviewed API baseline change."
                [IO.File]::WriteAllBytes($baseline, $original)
            }
        } finally { [IO.File]::WriteAllBytes($baseline, $original) }
        dotnet build 'UI Framework.slnx' -c Release --no-restore --disable-build-servers -m:1 -warnaserror
        if ($LASTEXITCODE -ne 0) { throw 'Restored API baseline did not pass.' }
    }
    Write-Output 'Public API validation passed.'
} finally { Pop-Location }
