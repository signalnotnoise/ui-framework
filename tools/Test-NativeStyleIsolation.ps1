#requires -Version 7.0
param(
    [string]$CounterDll = 'samples/Counter/bin/Release/net10.0-windows/Counter.dll',
    [Parameter(Mandatory)][string]$OutputDirectory,
    [ValidateRange(3,15)][int]$Samples = 7
)
$ErrorActionPreference = 'Stop'
$counter = (Resolve-Path -LiteralPath $CounterDll).Path
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) { throw 'Use a new output directory to preserve evidence.' }
New-Item -ItemType Directory -Path $output | Out-Null
Get-ChildItem -LiteralPath (Split-Path $counter) -File | ForEach-Object {
    [pscustomobject]@{ name = $_.Name; sha256 = (Get-FileHash -LiteralPath $_.FullName).Hash }
} | ConvertTo-Json | Set-Content (Join-Path $output 'binary-manifest.json')
$runs = @()
for ($sample = 0; $sample -le $Samples; $sample++) {
    $order = if ($sample % 2 -eq 0) { @('inherit','preserve') } else { @('preserve','inherit') }
    foreach ($mode in $order) {
        Write-Output "Native styles / sample $sample / $mode"
        $report = Join-Path $output "$sample-$mode.json"
        $start = [Diagnostics.ProcessStartInfo]::new((Get-Command dotnet).Source)
        $start.UseShellExecute = $false; $start.CreateNoWindow = $true
        $start.RedirectStandardOutput = $true; $start.RedirectStandardError = $true
        foreach ($argument in @($counter, '--native-islands', '--report', $report)) { $start.ArgumentList.Add($argument) }
        if ($mode -eq 'preserve') { $start.ArgumentList.Add('--preserve-native') }
        $process = [Diagnostics.Process]::Start($start)
        try {
            $stdout = $process.StandardOutput.ReadToEndAsync(); $stderr = $process.StandardError.ReadToEndAsync()
            $completed = $process.WaitForExit(120000)
            if (-not $completed) { $process.Kill($true); $process.WaitForExit() }
            ($stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult()) | Set-Content (Join-Path $output "$sample-$mode.log")
            if (-not $completed -or $process.ExitCode -ne 0) { throw "Native style probe failed: $sample / $mode" }
        } finally { $process.Dispose() }
        $result = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
        if ($result.Rows -ne 32 -or $result.MixedOperations -ne 50 -or $result.TotalCreates -ne 112 -or $result.TotalReleases -ne 112) {
            throw 'Unexpected native workload/lifecycle.'
        }
        $runs += [pscustomobject]@{ sample = $sample; mode = $mode; result = $result }
    }
    $pair = @($runs | Where-Object sample -eq $sample)
    foreach ($field in @('Rows','MixedOperations','MountBodyBuilds','BodyBuilds','Mounts','Unmounts','Runtime')) {
        if ($pair[0].result.$field -ne $pair[1].result.$field) { throw "Native workload differs: $field" }
    }
}
function Median($values) {
    $sorted = @($values | Sort-Object); $middle = [int][Math]::Floor($sorted.Count / 2)
    if ($sorted.Count % 2) { return [double]$sorted[$middle] }
    return ([double]$sorted[$middle - 1] + [double]$sorted[$middle]) / 2
}
$comparisons = foreach ($phase in @('total','retained-updates','outer-theme-replacement','scope-switch','scope-restore','remove-remount')) {
    $metrics = if ($phase -eq 'total') { @('MountMilliseconds','MountAllocatedBytes','Milliseconds','AllocatedBytes') } else { @('Milliseconds','AllocatedBytes') }
    foreach ($metric in $metrics) {
        $values = @{}
        foreach ($mode in @('inherit','preserve')) {
            $values[$mode] = @($runs | Where-Object { $_.sample -gt 0 -and $_.mode -eq $mode } | ForEach-Object {
                if ($phase -eq 'total') { $_.result.$metric }
                else { ($_.result.Phases | Where-Object Name -eq $phase).$metric }
            })
        }
        $a = Median $values.inherit; $b = Median $values.preserve
        $changes = @(for ($index = 0; $index -lt $Samples; $index++) { 100 * ($values.preserve[$index] / $values.inherit[$index] - 1) })
        [pscustomobject]@{ phase = $phase; metric = $metric; inheritMedian = $a; preserveMedian = $b
            changePercent = 100 * ($b / $a - 1); pairedChangesPercent = $changes }
    }
}
[ordered]@{ kind = 'native-style-scope-comparison'; releaseEligible = $false; samples = $Samples
    sdk = (& dotnet --version).Trim(); counter = $counter
    note = 'Same binary, different intentional style policies. Describes this workload, not a renderer speedup or equivalent visual output.'
    comparisons = @($comparisons) } | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $output 'summary.json')
