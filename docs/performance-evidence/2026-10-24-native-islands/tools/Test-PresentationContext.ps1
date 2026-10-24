#requires -Version 7.0
param(
    [Parameter(Mandatory)][string]$PrototypeDll,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [ValidateRange(3,15)][int]$Samples = 7
)
$ErrorActionPreference = 'Stop'
$prototype = (Resolve-Path -LiteralPath $PrototypeDll).Path
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) { throw 'Use a new output directory to preserve evidence.' }
New-Item -ItemType Directory -Path $output | Out-Null
Get-ChildItem -LiteralPath (Split-Path $prototype) -File | ForEach-Object {
    [pscustomobject]@{ name = $_.Name; sha256 = (Get-FileHash -LiteralPath $_.FullName).Hash }
} | ConvertTo-Json | Set-Content (Join-Path $output 'binary-manifest.json')
$results = @()
for ($sample = 0; $sample -le $Samples; $sample++) {
    $order = if ($sample % 2 -eq 0) { @('unattached','attached') } else { @('attached','unattached') }
    foreach ($mode in $order) {
        Write-Output "Presentation context / sample $sample / $mode"
        $report = Join-Path $output "$sample-$mode.json"
        $start = [Diagnostics.ProcessStartInfo]::new((Get-Command dotnet).Source)
        $start.UseShellExecute = $false
        $start.CreateNoWindow = $true
        $start.RedirectStandardOutput = $true
        $start.RedirectStandardError = $true
        foreach ($argument in @($prototype, '--compare', '--themed', '--report', $report)) { $start.ArgumentList.Add($argument) }
        if ($mode -eq 'attached') { $start.ArgumentList.Add('--presentation-source') }
        $process = [Diagnostics.Process]::Start($start)
        try {
            $stdout = $process.StandardOutput.ReadToEndAsync()
            $stderr = $process.StandardError.ReadToEndAsync()
            $completed = $process.WaitForExit(120000)
            if (-not $completed) { $process.Kill($true); $process.WaitForExit() }
            ($stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult()) | Set-Content (Join-Path $output "$sample-$mode.log")
            if (-not $completed -or $process.ExitCode -ne 0) { throw "Presentation probe failed: $sample / $mode" }
        } finally { $process.Dispose() }
        $result = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
        if ($result.Rows -ne 1000 -or $result.MixedOperations -ne 50 -or $result.Scenario -ne 'themed-full-list') {
            throw 'Unexpected prototype workload.'
        }
        $results += [pscustomobject]@{ sample = $sample; mode = $mode; result = $result }
    }
    $pair = @($results | Where-Object sample -eq $sample)
    foreach ($field in @('Rows','MixedOperations','MountBodyBuilds','BodyBuilds','Mounts','Unmounts','Runtime')) {
        if ($pair[0].result.$field -ne $pair[1].result.$field) { throw "Prototype changed workload: $field" }
    }
}
function Median($values) {
    $sorted = @($values | Sort-Object)
    $middle = [int][Math]::Floor($sorted.Count / 2)
    if ($sorted.Count % 2) { return [double]$sorted[$middle] }
    return ([double]$sorted[$middle - 1] + [double]$sorted[$middle]) / 2
}
$comparisons = foreach ($metric in @('MountMilliseconds','Milliseconds','MountAllocatedBytes','AllocatedBytes')) {
    $left = @($results | Where-Object { $_.sample -gt 0 -and $_.mode -eq 'unattached' } | ForEach-Object { $_.result.$metric })
    $right = @($results | Where-Object { $_.sample -gt 0 -and $_.mode -eq 'attached' } | ForEach-Object { $_.result.$metric })
    $a = Median $left; $b = Median $right
    [pscustomobject]@{ metric = $metric; unattachedMedian = $a; attachedMedian = $b; changePercent = 100 * ($b / $a - 1)
        unattachedMin = ($left | Measure-Object -Minimum).Minimum; unattachedMax = ($left | Measure-Object -Maximum).Maximum
        attachedMin = ($right | Measure-Object -Minimum).Minimum; attachedMax = ($right | Measure-Object -Maximum).Maximum }
}
[ordered]@{ kind = 'exploratory-presentation-context'; releaseEligible = $false; samples = $Samples
    sdk = (& dotnet --version).Trim(); prototype = $prototype
    note = 'Different presentation contexts; not a renderer before/after comparison or replacement release workload. Geometry/DPI acceptance remains separate.'
    comparisons = @($comparisons) } | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $output 'summary.json')
