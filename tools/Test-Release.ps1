param([switch]$NoRestore)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location $projectRoot
try {
    if (-not $NoRestore) {
        dotnet restore 'UI Framework.slnx' --disable-build-servers
        if ($LASTEXITCODE -ne 0) { throw 'Restore failed.' }
    }
    dotnet build 'UI Framework.slnx' -c Release --no-restore --disable-build-servers -m:1 -warnaserror
    if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }
    dotnet test 'tests/UI Framework.Checks/UI Framework.Checks.csproj' -c Release --no-build --no-restore --results-directory artifacts/release-validation/tests --logger 'trx;LogFileName=regression.trx' --blame-hang-timeout 2m
    if ($LASTEXITCODE -ne 0) { throw 'Regression tests failed.' }
    & (Join-Path $PSScriptRoot 'Update-KnowledgeGraph.ps1') -Check
    dotnet 'samples/Counter/bin/Release/net10.0-windows/Counter.dll' --visual-check
    if ($LASTEXITCODE -ne 0) { throw 'Visual stress checks failed.' }
    dotnet 'samples/Counter/bin/Release/net10.0-windows/Counter.dll' --stress --report artifacts/release-validation/stress.json --snapshot artifacts/release-validation/dashboard.png
    if ($LASTEXITCODE -ne 0) { throw 'Full stress workload failed.' }
    Write-Output 'Release validation passed. This command does not publish anything.'
} finally {
    Pop-Location
}
