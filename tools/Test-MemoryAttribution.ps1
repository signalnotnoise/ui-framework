#requires -Version 7.0
param([Parameter(Mandatory)][string]$OutputDirectory,
    [ValidateSet('native','fresh','reused')][string]$Mode = 'native',
    [string]$DumpToolPath = 'artifacts/diagnostic-tools/dotnet-dump.exe',
    [switch]$TraceHandles,
    [ValidateRange(60,1000)][int]$NativeBatches = 60)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output = [IO.Path]::GetFullPath($OutputDirectory, $root)
if (Test-Path -LiteralPath $output) { throw 'Use a fresh diagnostic output directory.' }
$dumpTool = (Resolve-Path -LiteralPath $DumpToolPath).Path
New-Item -ItemType Directory -Path $output | Out-Null
Push-Location $root
$target = $null
function Invoke-Diagnostic([string]$Executable, [string[]]$Arguments, [string]$Log, [int]$TimeoutSeconds) {
    $info=[Diagnostics.ProcessStartInfo]::new($Executable)
    $info.UseShellExecute=$false; $info.CreateNoWindow=$true
    $info.RedirectStandardOutput=$true; $info.RedirectStandardError=$true
    foreach ($argument in $Arguments) { $info.ArgumentList.Add($argument) }
    $child=[Diagnostics.Process]::Start($info)
    try {
        $out=$child.StandardOutput.ReadToEndAsync(); $err=$child.StandardError.ReadToEndAsync()
        $finished=$child.WaitForExit($TimeoutSeconds*1000)
        if (-not $finished) { $child.Kill($true); $child.WaitForExit() }
        ($out.GetAwaiter().GetResult()+$err.GetAwaiter().GetResult()) | Set-Content -LiteralPath $Log
        if (-not $finished -or $child.ExitCode -ne 0) { throw "Diagnostic tool failed or timed out: $Log" }
    } finally { $child.Dispose() }
}
try {
    $sample = if ($Mode -eq 'native') { 'NativeLifetime' } else { 'WebViewAcceptance' }
    dotnet build "samples/$sample/$sample.csproj" -c Release --disable-build-servers -m:1 -warnaserror
    if ($LASTEXITCODE -ne 0) { throw 'Diagnostic sample build failed.' }
    dotnet build tools/MemoryDumpCollector/MemoryDumpCollector.csproj -c Release --disable-build-servers -m:1 -warnaserror
    if ($LASTEXITCODE -ne 0) { throw 'Handle-aware collector build failed.' }
    if ($TraceHandles) {
        Invoke-Diagnostic (Get-Command dotnet).Source @((Join-Path $root 'tools/MemoryDumpCollector/bin/Release/net10.0-windows/MemoryDumpCollector.dll'),'trace-check',(Join-Path $output 'trace-control.bin')) (Join-Path $output 'trace-control.log') 30
    }
    $sources = @('UI Framework','UI Framework.Wpf',"samples/$sample",'samples/MeasurementTools','tools/MemoryDumpCollector') | ForEach-Object {
        Get-ChildItem -LiteralPath $_ -Recurse -File | Where-Object { $_.Extension -in @('.cs','.csproj','.html') -and $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
    }
    $sources | ForEach-Object { [pscustomobject]@{ path=[IO.Path]::GetRelativePath($root,$_.FullName); sha256=(Get-FileHash -LiteralPath $_.FullName).Hash } } |
        ConvertTo-Json | Set-Content (Join-Path $output 'source-manifest.json')
    [pscustomobject]@{ protocol='memory-attribution-paused-v1'; mode=$Mode; releaseGate=$false;
        sdk=(& dotnet --version).Trim(); dumpTool=(& $dumpTool --version).Trim(); revision=(& git rev-parse HEAD).Trim(); nativeBatches=$NativeBatches;
        dirty=[bool](& git status --porcelain); collector='system-dbghelp-0x1006'; handleTracing=[bool]$TraceHandles; note='Paused dump collection perturbs the process; not comparable budget/performance evidence.' } |
        ConvertTo-Json | Set-Content (Join-Path $output 'campaign.json')
    $dll = Join-Path $root "samples/$sample/bin/Release/net10.0-windows/$sample.dll"
    $arguments = if ($Mode -eq 'native') { @($dll,'--mode','memory','--batches',"$NativeBatches",'--cycles','4','--rows','500','--warmups','3','--report',(Join-Path $output 'case/report.json')) }
        else { @($dll,"retention-$Mode",(Join-Path $output 'case')) }
    $start = [Diagnostics.ProcessStartInfo]::new((Get-Command dotnet).Source)
    $start.UseShellExecute=$false; $start.CreateNoWindow=$true
    $start.RedirectStandardOutput=$true; $start.RedirectStandardError=$true
    $checkpoints = Join-Path $output 'checkpoints'
    $start.Environment['UI_FRAMEWORK_MEMORY_CHECKPOINTS']=$checkpoints
    foreach ($argument in $arguments) { $start.ArgumentList.Add($argument) }
    $target = [Diagnostics.Process]::Start($start)
    $stdout=$target.StandardOutput.ReadToEndAsync(); $stderr=$target.StandardError.ReadToEndAsync()
    foreach ($phase in @('start','end','quiescent')) {
        $ready = Join-Path $checkpoints "$phase.ready.json"
        $timer = [Diagnostics.Stopwatch]::StartNew()
        while (-not (Test-Path -LiteralPath $ready)) {
            if ($target.HasExited -or $timer.Elapsed.TotalSeconds -gt 300) { throw "Checkpoint not reached: $phase" }
            Start-Sleep -Milliseconds 100
        }
        # The signal can become visible before its writer closes; wait for exclusive read access.
        $signal = $null
        while ($null -eq $signal) {
            try { $signal=[IO.File]::Open($ready,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::None) }
            catch [IO.IOException] { if ($timer.Elapsed.TotalSeconds -gt 300) { throw }; Start-Sleep -Milliseconds 50 }
        }
        $signal.Dispose()
        $state = Get-Content -LiteralPath $ready -Raw | ConvertFrom-Json
        if ($state.ProcessId -ne $target.Id) { throw 'Checkpoint belongs to another process.' }
        $dump = Join-Path $output "$phase.dmp"
        $collector = Join-Path $root 'tools/MemoryDumpCollector/bin/Release/net10.0-windows/MemoryDumpCollector.dll'
        if ($TraceHandles -and $phase -eq 'start') {
            Invoke-Diagnostic (Get-Command dotnet).Source @($collector,'trace-enable',"$($target.Id)") (Join-Path $output 'trace-enable.log') 30
        }
        Write-Output "Capturing $Mode / $phase"
        Invoke-Diagnostic (Get-Command dotnet).Source @((Join-Path $root 'tools/MemoryDumpCollector/bin/Release/net10.0-windows/MemoryDumpCollector.dll'),"$($target.Id)",$dump) "$dump.collect.log" 60
        if ($TraceHandles) {
            Invoke-Diagnostic (Get-Command dotnet).Source @($collector,'trace-read',"$($target.Id)",$dump,(Join-Path $output "$phase.handles.json"),(Join-Path $output 'start.dmp')) (Join-Path $output "$phase.trace.log") 30
        }
        New-Item -ItemType File -Path (Join-Path $checkpoints "$phase.resume") | Out-Null
    }
    if (-not $target.WaitForExit(120000)) { throw 'Diagnostic process did not finish after resume.' }
    if ($target.ExitCode -ne 0) { throw 'Diagnostic sample failed; inspect target.log.' }
    foreach ($phase in @('start','end','quiescent')) {
        Invoke-Diagnostic $dumpTool @('analyze',(Join-Path $output "$phase.dmp"),'-c','dumpheap -stat','-c','gchandles','-c','threadpool','-c','clrthreads','-c','exit') (Join-Path $output "$phase.analysis.txt") 90
    }
    Write-Output 'Diagnostic checkpoints and analysis saved. These are not release gates.'
} finally {
    if ($null -ne $target) {
        if (-not $target.HasExited) { $target.Kill($true); $target.WaitForExit() }
        ($stdout.GetAwaiter().GetResult()+$stderr.GetAwaiter().GetResult()) | Set-Content (Join-Path $output 'target.log')
        $target.Dispose()
    }
    Pop-Location
}
