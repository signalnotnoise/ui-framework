#requires -Version 7.0
param([Parameter(Mandatory)][string]$OutputDirectory, [switch]$Control, [switch]$ThreadExit)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output=[IO.Path]::GetFullPath($OutputDirectory,$root)
if(Test-Path $output){throw 'Use a new directory to preserve evidence.'}
New-Item -ItemType Directory $output | Out-Null
$collector=Join-Path $root 'tools/MemoryDumpCollector/bin/Release/net10.0-windows/MemoryDumpCollector.dll'
$dll=Join-Path $root 'samples/NativeLifetime/bin/Release/net10.0-windows/NativeLifetime.dll'
$dotnet=(Get-Command dotnet).Source
$checkpoints=Join-Path $output 'checkpoints'
$target=$null
$audit=@()
function Capture-AppState($phase){
 $apps=@(Get-Process | Where-Object {$_.ProcessName -match 'GuidedGrade|Lab.Feedback'} | Select-Object Id,ProcessName)
 $script:audit+= [pscustomobject]@{phase=$phase;utc=[DateTime]::UtcNow.ToString('o');apps=$apps}
 $script:audit | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $output 'app-audit.json')
 if($apps.Count){throw 'GuidedGrade is running; preserve this campaign and close it before a new run.'}
}
try {
 Capture-AppState 'launch'
 $start=[Diagnostics.ProcessStartInfo]::new($dotnet)
 $start.UseShellExecute=$false;$start.CreateNoWindow=$true;$start.RedirectStandardOutput=$true;$start.RedirectStandardError=$true
 foreach($arg in @($dll,'--mode','memory','--report',(Join-Path $output 'report.json'),'--batches','600','--rows','500','--cycles','4','--warmups','3')){$start.ArgumentList.Add($arg)}
 $start.Environment['UI_FRAMEWORK_MEMORY_CHECKPOINTS']=$checkpoints
 if($ThreadExit){$start.Environment['UI_FRAMEWORK_TRACE_THREAD_EXIT']='1'}
 $start.Environment['UI_FRAMEWORK_FOCUS_TRACE']=if($Control){'control'}else{'1'}
 $target=[Diagnostics.Process]::Start($start)
 $stdout=$target.StandardOutput.ReadToEndAsync();$stderr=$target.StandardError.ReadToEndAsync()
 [pscustomobject]@{pid=$target.Id;kind='paused-focus-native-resource-attribution';control=[bool]$Control;threadExit=[bool]$ThreadExit;releaseGate=$false;sdk=(& $dotnet --version);sampleHash=(Get-FileHash $dll).Hash;collectorHash=(Get-FileHash $collector).Hash} | ConvertTo-Json | Set-Content (Join-Path $output 'campaign.json')
 $phases=if($Control){@('start','end','quiescent','post-shutdown')}else{@('start','first-activation','end','quiescent','post-shutdown')}
 if($ThreadExit){$phases+= 'post-thread'}
 foreach($phase in $phases){
  $ready=Join-Path $checkpoints "$phase.ready.json"
  $timer=[Diagnostics.Stopwatch]::StartNew()
  while(-not(Test-Path $ready)){
   if($target.HasExited -or $timer.Elapsed.TotalSeconds -gt 600){throw "Checkpoint not reached: $phase"}
   Start-Sleep -Milliseconds 100
  }
  $stream=$null
  while($null -eq $stream){try{$stream=[IO.File]::Open($ready,'Open','Read','None')}catch [IO.IOException]{if($timer.Elapsed.TotalSeconds -gt 600){throw};Start-Sleep -Milliseconds 50}}
  $stream.Dispose()
  $signal=Get-Content $ready -Raw | ConvertFrom-Json
  if($signal.ProcessId -ne $target.Id){throw 'Unexpected checkpoint owner.'}
  Capture-AppState $phase
  & $dotnet $collector gui-read $target.Id (Join-Path $output $phase) *> (Join-Path $output "$phase.gui.log")
  if($LASTEXITCODE -ne 0){throw "GUI inventory failed: $phase"}
  & $dotnet $collector $target.Id (Join-Path $output "$phase.dmp") *> (Join-Path $output "$phase.dump.log")
  if($LASTEXITCODE -ne 0){throw "Dump failed: $phase"}
  New-Item -ItemType File (Join-Path $checkpoints "$phase.resume") | Out-Null
  Write-Output "Captured $phase; process $($target.Id) resumed."
 }
 if(-not $target.WaitForExit(120000) -or $target.ExitCode -ne 0){throw 'Focus trace failed after final resume.'}
 $report=Get-Content (Join-Path $output 'report.json') -Raw | ConvertFrom-Json
 if(-not($report.Passed -and $report.DispatcherShutdownFinished -and $report.CleanupPolicyClosed)){throw 'Incomplete shutdown.'}
 if($Control -and @($report.Result.Samples | Where-Object {$_.Input.Activations -ne 0 -or $_.Input.MouseButtons -ne 0 -or $_.Input.KeyDowns -ne 0}).Count){throw 'Control received activation or input; do not label it untouched.'}
 Capture-AppState 'exit'
 Write-Output 'Focus resource trace passed; snapshots are diagnostic, not performance gates.'
}finally{
 if($null -ne $target){
  if(-not $target.HasExited){$target.Kill($true);$target.WaitForExit()}
  ($stdout.GetAwaiter().GetResult()+$stderr.GetAwaiter().GetResult()) | Set-Content (Join-Path $output 'target.log')
  $target.Dispose()
 }
}
