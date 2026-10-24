#requires -Version 7.0
param(
    [Parameter(Mandatory)][string]$OutputDirectory,
    [ValidateSet('cold','initialized')][string]$Mode = 'initialized',
    [string]$CounterDll = 'samples/Counter/bin/Release/net10.0-windows/Counter.dll',
    [string]$TraceTool = 'artifacts/diagnostic-tools/dotnet-trace.exe',
    [switch]$Themed
)
$ErrorActionPreference='Stop'
$counter=(Resolve-Path -LiteralPath $CounterDll).Path
$tool=(Resolve-Path -LiteralPath $TraceTool).Path
$output=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $output) { throw 'Use a new trace directory to preserve evidence.' }
New-Item -ItemType Directory -Path $output | Out-Null
$report=Join-Path $output 'report.json'
$trace=Join-Path $output 'trace.nettrace'
$start=[Diagnostics.ProcessStartInfo]::new($tool)
$start.UseShellExecute=$false; $start.CreateNoWindow=$true
$start.RedirectStandardOutput=$true; $start.RedirectStandardError=$true
$arguments=@('collect','--profile','dotnet-sampled-thread-time','--providers',
    'Microsoft-Windows-DotNETRuntime:0x1C001:4,SignalNotNoise-Comparison:0xFFFFFFFFFFFFFFFF:5',
    '--output',$trace,'--',(Get-Command dotnet).Source,$counter,'--compare','--trace-phases','--report',$report)
if($Mode -eq 'cold') { $arguments+='--cold-presentation' }
if($Themed) { $arguments+='--themed' }
foreach($argument in $arguments) { $start.ArgumentList.Add($argument) }
$arguments | ConvertTo-Json | Set-Content (Join-Path $output 'arguments.json')
$process=[Diagnostics.Process]::Start($start)
try {
    $stdout=$process.StandardOutput.ReadToEndAsync(); $stderr=$process.StandardError.ReadToEndAsync()
    $completed=$process.WaitForExit(120000)
    if(-not $completed) { $process.Kill($true); $process.WaitForExit() }
    ($stdout.GetAwaiter().GetResult()+$stderr.GetAwaiter().GetResult()) | Set-Content (Join-Path $output 'collect.log')
    if(-not $completed -or $process.ExitCode -ne 0) { throw 'Comparison trace failed.' }
} finally { $process.Dispose() }
$result=Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
$phases=Get-Content -LiteralPath ($report+'.phases.json') -Raw | ConvertFrom-Json
if($result.PresentationProtocol -ne 'process-dpi-initialization-v2' -or $result.PresentationPrimed -ne ($Mode -eq 'initialized') -or -not $phases.EventsEnabled) {
    throw 'Trace condition or phase provider did not match the requested experiment.'
}
Get-ChildItem -LiteralPath (Split-Path $counter) -File | ForEach-Object {
    [pscustomobject]@{ name=$_.Name; sha256=(Get-FileHash -LiteralPath $_.FullName).Hash }
} | ConvertTo-Json | Set-Content (Join-Path $output 'binary-manifest.json')
Write-Output "Validated $Mode trace: $trace"
