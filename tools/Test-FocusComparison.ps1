#requires -Version 7.0
param([Parameter(Mandatory)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output=[IO.Path]::GetFullPath($OutputDirectory,$root)
if(Test-Path $output){throw 'Use a fresh directory to preserve evidence.'}
New-Item -ItemType Directory $output | Out-Null
$dotnet=(Get-Command dotnet).Source
$audit=@()
function Audit($phase) {
    $apps=@(Get-Process | Where-Object {$_.ProcessName -match 'GuidedGrade|Lab.Feedback'} | Select-Object Id,ProcessName)
    $script:audit += [pscustomobject]@{phase=$phase;utc=[DateTime]::UtcNow.ToString('o');apps=$apps}
    $script:audit | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $output 'app-audit.json')
    if($apps.Count){throw 'Close GuidedGrade before running this comparison.'}
}
foreach($kind in 'Plain','Framework') {
    & $dotnet build (Join-Path $root "samples/FocusComparison/$kind/$kind.csproj") -c Release *> (Join-Path $output "$kind-build.log")
    if($LASTEXITCODE -ne 0){throw "Build failed: $kind"}
}
$hashes=@(foreach($kind in 'Plain','Framework') { Get-ChildItem (Join-Path $root "samples/FocusComparison/$kind/bin/Release/net10.0-windows") -File | Get-FileHash | Select-Object Path,Hash })
[pscustomobject]@{sdk=(& $dotnet --version);binaries=$hashes;releaseGate=$false} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $output 'manifest.json')
$cases=@('Plain-quiet','Framework-quiet','Plain-focus','Framework-focus','Framework-focus','Plain-focus','Plain-focus','Framework-focus')
for($i=0;$i -lt $cases.Count;$i++) {
    $kind,$mode=$cases[$i].Split('-');$name="${i}-${kind}-${mode}"
    Audit "$name-start"
    $start=[Diagnostics.ProcessStartInfo]::new($dotnet)
    $start.UseShellExecute=$false;$start.CreateNoWindow=$true
    $start.RedirectStandardOutput=$true;$start.RedirectStandardError=$true
    foreach($arg in @((Join-Path $root "samples/FocusComparison/$kind/bin/Release/net10.0-windows/$kind.dll"),(Join-Path $output $name),$mode)){$start.ArgumentList.Add($arg)}
    $process=[Diagnostics.Process]::Start($start)
    $stdout=$process.StandardOutput.ReadToEndAsync();$stderr=$process.StandardError.ReadToEndAsync()
    try {
        Write-Output "Running $name (PID $($process.Id))"
        if(-not $process.WaitForExit(150000)){throw "Timed out: $name"}
        if($process.ExitCode -ne 0){throw "Failed: $name; inspect its result and log."}
        Audit "$name-exit"
        Write-Output "Passed $name"
    } finally {
        if(-not $process.HasExited){$process.Kill($true);$process.WaitForExit()}
        ($stdout.GetAwaiter().GetResult()+$stderr.GetAwaiter().GetResult()) | Set-Content (Join-Path $output "$name.log")
        $process.Dispose()
    }
}
