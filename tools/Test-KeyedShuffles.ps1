#requires -Version 7.0
param(
    [string]$BaselineRef,
    [string]$BaselineSourceDirectory,
    [ValidateSet("refresh","notified-storage","storage","layout","full-layout")][string]$Mode = "refresh",
    [int[]]$Counts = @(1000,10000),
    [ValidateSet("rotate","reverse","shuffle","interleave","filter")][string[]]$Scenarios,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [ValidateRange(3,15)][int]$Samples = 7
)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output = [IO.Path]::GetFullPath($OutputDirectory, $root)
if (Test-Path -LiteralPath $output) { throw 'Use a new output directory to preserve evidence.' }
New-Item -ItemType Directory -Path $output | Out-Null
Push-Location $root
try {
    if ([string]::IsNullOrWhiteSpace($BaselineRef) -eq [string]::IsNullOrWhiteSpace($BaselineSourceDirectory)) { throw 'Specify exactly one baseline ref or source snapshot.' }
    $baseline = Join-Path $output 'baseline'
    if ($BaselineSourceDirectory) {
        $snapshot = (Resolve-Path -LiteralPath $BaselineSourceDirectory).Path
        Copy-Item -LiteralPath $snapshot -Destination $baseline -Recurse
        $revision = 'source-snapshot:' + $snapshot
    } else {
    $revision = (& git rev-parse --verify "$BaselineRef^{commit}").Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Baseline must be a local commit.' }
    & git archive --format=zip "--output=$output/baseline-source.zip" $revision
    if ($LASTEXITCODE -ne 0) { throw 'Baseline archive failed.' }
    $baseline = Join-Path $output 'baseline'
    Expand-Archive -LiteralPath "$output/baseline-source.zip" -DestinationPath $baseline
    }
    $harness = 'tools/KeyedShuffleDiagnostics'
    New-Item -ItemType Directory -Path (Join-Path $baseline $harness) -Force | Out-Null
    Copy-Item "$harness/*.cs", "$harness/*.csproj" -Destination (Join-Path $baseline $harness)
    $candidate = Join-Path $output 'candidate-source'
    foreach ($folder in @('UI Framework','UI Framework.Wpf','tools/KeyedShuffleDiagnostics')) {
        Get-ChildItem $folder -Recurse -File | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } | ForEach-Object {
            $relative = [IO.Path]::GetRelativePath($root, $_.FullName)
            $target = Join-Path $candidate $relative
            New-Item -ItemType Directory -Path (Split-Path $target) -Force | Out-Null
            Copy-Item -LiteralPath $_.FullName -Destination $target
        }
    }
    Copy-Item Directory.Build.props,global.json -Destination $candidate
    foreach ($build in @($baseline,$candidate)) {
        & dotnet build (Join-Path $build "$harness/KeyedShuffleDiagnostics.csproj") -c Release --disable-build-servers -m:1 -warnaserror
        if ($LASTEXITCODE -ne 0) { throw 'Diagnostic build failed.' }
    }
    $results = @()
    foreach ($count in $Counts) {
        foreach ($scenario in $(if ($Scenarios) { $Scenarios } elseif ($Mode -eq 'refresh') { @('rotate','reverse','shuffle','filter') } else { @('reverse','shuffle') })) {
            for ($sample = 0; $sample -le $Samples; $sample++) {
                $order = if ($sample % 2 -eq 0) { @('baseline','candidate') } else { @('candidate','baseline') }
                foreach ($side in $order) {
                    $source = if ($side -eq 'baseline') { $baseline } else { $candidate }
                    $log = Join-Path $output "$count-$scenario-$sample-$side.json"
                    $start = [Diagnostics.ProcessStartInfo]::new((Get-Command dotnet).Source)
                    $start.UseShellExecute = $false
                    $start.CreateNoWindow = $true
                    $start.RedirectStandardOutput = $true
                    $start.RedirectStandardError = $true
                    foreach ($argument in @((Join-Path $source "$harness/bin/Release/net10.0-windows/KeyedShuffleDiagnostics.dll"), [string]$count, $scenario, $Mode)) { $start.ArgumentList.Add($argument) }
                    $process = [Diagnostics.Process]::Start($start)
                    try {
                        $stdout = $process.StandardOutput.ReadToEndAsync()
                        $stderr = $process.StandardError.ReadToEndAsync()
                        if (-not $process.WaitForExit(120000)) {
                            $process.Kill($true)
                            $process.WaitForExit()
                            $stdout.GetAwaiter().GetResult() | Set-Content $log
                            $stderr.GetAwaiter().GetResult() | Set-Content ($log + '.stderr.log')
                            throw "Diagnostic exceeded 120 seconds: $log"
                        }
                        $stdout.GetAwaiter().GetResult() | Set-Content $log
                        $stderr.GetAwaiter().GetResult() | Set-Content ($log + '.stderr.log')
                        if ($process.ExitCode -ne 0) { throw "Diagnostic failed: $log" }
                    } finally { $process.Dispose() }
                    $result = Get-Content $log -Raw | ConvertFrom-Json
                    $result | Add-Member side $side
                    $result | Add-Member sample $sample
                    $result | Add-Member warmup ($sample -eq 0)
                    $results += $result
                }
            }
        }
    }
    [pscustomobject]@{ baseline=$revision; mode=$Mode; samples=$Samples; sdk=(& dotnet --version).Trim(); scope='Supplemental diagnostic with explicit mode; not a release gate. Retain full-list and virtualized Test-Performance comparisons.'; results=$results } |
        ConvertTo-Json -Depth 6 | Set-Content (Join-Path $output 'comparison.json')
} finally { Pop-Location }
