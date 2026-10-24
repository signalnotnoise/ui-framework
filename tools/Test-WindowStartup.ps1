#requires -Version 7.0
param(
    [Parameter(Mandatory)][string]$OutputDirectory,
    [ValidateRange(3,15)][int]$Samples = 7,
    [string]$CounterDll = 'samples/Counter/bin/Release/net10.0-windows/Counter.dll'
)
$ErrorActionPreference='Stop'
$counter=(Resolve-Path -LiteralPath $CounterDll).Path
$output=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $output) { throw 'Use a new output directory to preserve evidence.' }
New-Item -ItemType Directory -Path $output | Out-Null
Get-ChildItem -LiteralPath (Split-Path $counter) -File | ForEach-Object {
    [pscustomobject]@{ name=$_.Name; sha256=(Get-FileHash -LiteralPath $_.FullName).Hash }
} | ConvertTo-Json | Set-Content (Join-Path $output 'binary-manifest.json')
$runs=@()
for($sample=0;$sample -le $Samples;$sample++) {
    $order=if($sample % 2 -eq 0) { @('late','early') } else { @('early','late') }
    foreach($mode in $order) {
        Write-Output "Window startup / sample $sample / $mode"
        $report=Join-Path $output "$sample-$mode.json"
        $start=[Diagnostics.ProcessStartInfo]::new((Get-Command dotnet).Source)
        $start.UseShellExecute=$false; $start.CreateNoWindow=$true
        $start.RedirectStandardOutput=$true; $start.RedirectStandardError=$true
        foreach($argument in @($counter,'--startup-check','--report',$report)) { $start.ArgumentList.Add($argument) }
        if($mode -eq 'late') { $start.ArgumentList.Add('--cold-window-startup') }
        $process=[Diagnostics.Process]::Start($start)
        try {
            $stdout=$process.StandardOutput.ReadToEndAsync(); $stderr=$process.StandardError.ReadToEndAsync()
            $completed=$process.WaitForExit(120000)
            if(-not $completed) { $process.Kill($true); $process.WaitForExit() }
            ($stdout.GetAwaiter().GetResult()+$stderr.GetAwaiter().GetResult()) | Set-Content (Join-Path $output "$sample-$mode.log")
            if(-not $completed -or $process.ExitCode -ne 0) { throw "Startup failed: $sample / $mode" }
        } finally { $process.Dispose() }
        $result=Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
        if($result.Rows -ne 50 -or -not $result.NativeEditingPassed -or $result.ActiveAfterDisposal -ne 0) { throw 'Startup workload or lifetime mismatch.' }
        $runs += [pscustomobject]@{ sample=$sample; mode=$mode; result=$result }
    }
    $pair=@($runs | Where-Object sample -eq $sample)
    if($pair[0].result.BodyBuilds -ne $pair[1].result.BodyBuilds) { throw 'Startup body work differs.' }
    $a=Get-Content -LiteralPath (Join-Path $output "$sample-late.json.phases.json") -Raw | ConvertFrom-Json
    $b=Get-Content -LiteralPath (Join-Path $output "$sample-early.json.phases.json") -Raw | ConvertFrom-Json
    if($a.Geometry.startup.Sha256 -ne $b.Geometry.startup.Sha256) { throw 'Actual-window startup geometry differs.' }
}
function Median($values) {
    $sorted=@($values | Sort-Object); $middle=[int][Math]::Floor($sorted.Count/2)
    if($sorted.Count % 2) { return [double]$sorted[$middle] }
    return ([double]$sorted[$middle-1]+[double]$sorted[$middle])/2
}
$comparisons=foreach($metric in @('Milliseconds','AllocatedBytes')) {
    $a=Median @($runs | Where-Object { $_.sample -gt 0 -and $_.mode -eq 'late' } | ForEach-Object { $_.result.$metric })
    $b=Median @($runs | Where-Object { $_.sample -gt 0 -and $_.mode -eq 'early' } | ForEach-Object { $_.result.$metric })
    [pscustomobject]@{ metric=$metric; lateMedian=$a; earlyMedian=$b; changePercent=100*($b/$a-1) }
}
[ordered]@{ kind='actual-window-startup-order'; samples=$Samples; releaseEligible=$false
    sdk=(& dotnet --version).Trim(); rows=50; geometryMatches=$true; nativeEditingPassed=$true
    note='Same sample binary and actual window; handle before versus after ViewHost construction. Includes window creation, attachment, Show and layout. No framework-owned hidden window.'
    comparisons=@($comparisons) } | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $output 'summary.json')
