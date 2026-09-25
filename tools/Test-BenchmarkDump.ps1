# Integration checks for the timeout diagnostic path; does not run performance workloads.
param([string]$DumpToolPath = 'artifacts/diagnostic-tools/dotnet-dump.exe')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$tool = (Resolve-Path -LiteralPath $DumpToolPath).Path
$output = Join-Path $root ('artifacts/dump-check-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $output | Out-Null
'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>' | Set-Content (Join-Path $output 'SleepingProcess.csproj')
'System.Threading.Thread.Sleep(180000);' | Set-Content (Join-Path $output 'Program.cs')
& dotnet build (Join-Path $output 'SleepingProcess.csproj') -c Release --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw 'Diagnostic fixture build failed.' }
$fixture = Join-Path $output 'bin/Release/net10.0/SleepingProcess.exe'
$start = [Diagnostics.ProcessStartInfo]::new($fixture)
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
$target = [Diagnostics.Process]::Start($start)
try {
    Start-Sleep -Seconds 1
    $dump = Join-Path $output 'synthetic.dmp'
    $result = & "$PSScriptRoot/Save-BenchmarkDump.ps1" -ProcessId $target.Id -DumpToolPath $tool -OutputPath $dump
    if ($result.status -ne 'captured') { throw "Capture failed: $($result | ConvertTo-Json -Compress)" }
    $analysis = Join-Path $output 'analysis.log'
    & $tool analyze $dump -c 'clrstack -all' -c exit > $analysis
    if ($LASTEXITCODE -ne 0 -or (Get-Content $analysis -Raw) -notmatch 'Thread.Sleep') {
        throw 'Captured dump did not provide the sleeping managed stack.'
    }
    $result = & "$PSScriptRoot/Save-BenchmarkDump.ps1" -ProcessId $target.Id -DumpToolPath (Join-Path $output 'missing.exe') -OutputPath (Join-Path $output 'missing.dmp')
    if ($result.status -ne 'failed' -or -not $result.error) { throw 'Missing collector failure was not recorded.' }
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $result = & "$PSScriptRoot/Save-BenchmarkDump.ps1" -ProcessId $target.Id -DumpToolPath $fixture -OutputPath (Join-Path $output 'stalled.dmp') -CaptureTimeoutSeconds 1
    if ($result.status -ne 'capture-timeout' -or $watch.Elapsed.TotalSeconds -gt 10) {
        throw 'Stalled collector was not bounded.'
    }
    if ($target.HasExited) { throw 'Collector checks unexpectedly terminated the diagnostic target.' }
    Write-Output "Dump capture, readable managed stack, missing collector and bounded timeout checks passed. Evidence: $output"
} finally {
    if (-not $target.HasExited) { $target.Kill($true); $target.WaitForExit() }
    $target.Dispose()
}
