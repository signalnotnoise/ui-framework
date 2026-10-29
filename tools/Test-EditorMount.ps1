#requires -Version 7.0
param(
    [Parameter(Mandatory)][string]$OutputDirectory,
    [ValidateRange(3,15)][int]$Samples = 7,
    [switch]$TracePhases,
    [string]$CounterDll = 'samples/Counter/bin/Release/net10.0-windows/Counter.dll'
)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output = [IO.Path]::GetFullPath($OutputDirectory, $root)
if (Test-Path -LiteralPath $output) { throw 'Use a new output directory to retain evidence.' }
$counter = (Resolve-Path -LiteralPath $CounterDll).Path
New-Item -ItemType Directory -Path $output | Out-Null
Get-ChildItem -LiteralPath (Split-Path $counter) -File | ForEach-Object {
    [pscustomobject]@{ name=$_.Name; sha256=(Get-FileHash -LiteralPath $_.FullName).Hash }
} | ConvertTo-Json | Set-Content (Join-Path $output 'binary-manifest.json')
$results = @()
foreach ($sample in 0..$Samples) {
    $order = if ($sample % 2 -eq 0) { @('host-first','source-first') } else { @('source-first','host-first') }
    foreach ($mode in $order) {
        Write-Output "Editor mount / sample $sample / $mode / trace=$TracePhases"
        $report = Join-Path $output "$sample-$mode.json"
        $start = [Diagnostics.ProcessStartInfo]::new((Get-Command dotnet).Source)
        $start.UseShellExecute=$false; $start.CreateNoWindow=$true
        $start.RedirectStandardOutput=$true; $start.RedirectStandardError=$true
        $arguments = @($counter,'--layout-editors','--report',$report)
        if ($mode -eq 'host-first') { $arguments += '--editor-host-first' }
        if ($TracePhases) { $arguments += '--trace-phases' }
        foreach ($argument in $arguments) { $start.ArgumentList.Add($argument) }
        $watch = [Diagnostics.Stopwatch]::StartNew()
        $process = [Diagnostics.Process]::Start($start)
        try {
            $stdout=$process.StandardOutput.ReadToEndAsync(); $stderr=$process.StandardError.ReadToEndAsync()
            $completed=$process.WaitForExit(120000)
            if (-not $completed) { $process.Kill($true); $process.WaitForExit() }
            $watch.Stop()
            ($stdout.GetAwaiter().GetResult()+$stderr.GetAwaiter().GetResult()) | Set-Content ($report+'.log')
            [pscustomobject]@{ wallSeconds=$watch.Elapsed.TotalSeconds; cpuSeconds=$process.TotalProcessorTime.TotalSeconds; exitCode=$process.ExitCode } |
                ConvertTo-Json | Set-Content ($report+'.process.json')
            if (-not $completed -or $process.ExitCode -ne 0) { throw "Editor mount diagnostic failed: $report" }
        } finally { $process.Dispose() }
        $result = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
        if ($result.InitializationOrder -ne $mode -or $result.PhaseTracing -ne [bool]$TracePhases -or
            $result.Rows -ne 1000 -or $result.MixedOperations -ne 50 -or $result.MountBodyBuilds -ne 1 -or $result.BodyBuilds -ne 50) {
            throw 'Diagnostic condition or workload does not match.'
        }
        $result | Add-Member sample $sample
        $result | Add-Member warmup ($sample -eq 0)
        $results += $result
    }
}
[pscustomobject]@{ kind='editor-initialization-order-experiment-not-release-gate'; samples=$Samples; tracePhases=[bool]$TracePhases; sdk=(& dotnet --version).Trim(); results=$results } |
    ConvertTo-Json -Depth 6 | Set-Content (Join-Path $output 'comparison.json')
