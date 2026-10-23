param([Parameter(Mandatory)][string]$Directory, [Parameter(Mandatory)][string]$Kind)
$ErrorActionPreference = 'Stop'
function Median($values) { $sorted = @($values | Sort-Object); return [double]$sorted[3] }
$left = @(1..7 | ForEach-Object { Get-Content -LiteralPath (Join-Path $Directory "$_-baseline.json") -Raw | ConvertFrom-Json })
$right = @(1..7 | ForEach-Object { Get-Content -LiteralPath (Join-Path $Directory "$_-candidate.json") -Raw | ConvertFrom-Json })
foreach ($run in @($left) + @($right)) {
    if ($run.SchemaVersion -ne 2 -or $run.Rows -ne 1000 -or $run.MixedOperations -ne 50 -or $run.Scenario -ne 'full-list') { throw 'Unexpected control workload.' }
}
$comparisons = foreach ($metric in @('MountMilliseconds','Milliseconds','MountAllocatedBytes','AllocatedBytes','MountBodyBuilds','BodyBuilds','Mounts','Unmounts')) {
    $before = Median $left.$metric
    $after = Median $right.$metric
    [pscustomobject]@{
        metric = $metric; baselineMedian = $before; candidateMedian = $after
        changePercent = 100 * ($after / $before - 1)
        baselineMin = ($left.$metric | Measure-Object -Minimum).Minimum
        baselineMax = ($left.$metric | Measure-Object -Maximum).Maximum
        candidateMin = ($right.$metric | Measure-Object -Minimum).Minimum
        candidateMax = ($right.$metric | Measure-Object -Maximum).Maximum
    }
}
[pscustomobject]@{
    kind = $Kind; releaseGate = $false; samples = 7; discardedWarmupsPerSide = 1
    rows = 1000; operations = 50; runtime = @($left.Runtime + $right.Runtime | Sort-Object -Unique)
    comparisons = @($comparisons)
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $Directory 'summary.json')
$comparisons | Format-Table metric,baselineMedian,candidateMedian,changePercent,baselineMin,baselineMax,candidateMin,candidateMax -AutoSize
