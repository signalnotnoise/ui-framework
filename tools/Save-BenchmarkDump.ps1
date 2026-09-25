# Called only after a benchmark has failed its time limit. Never sample a passing run.
param(
    [Parameter(Mandatory)][int]$ProcessId,
    [Parameter(Mandatory)][string]$DumpToolPath,
    [Parameter(Mandatory)][string]$OutputPath,
    [ValidateRange(1, 60)][int]$CaptureTimeoutSeconds = 45
)
$ErrorActionPreference = 'Stop'
$collector = $null
$result = [ordered]@{ status = 'failed'; path = $OutputPath; log = "$OutputPath.log"; timeoutSeconds = $CaptureTimeoutSeconds }
try {
    $start = [Diagnostics.ProcessStartInfo]::new($DumpToolPath)
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    foreach ($argument in @('collect', '--process-id', "$ProcessId", '--type', 'Full', '--output', $OutputPath)) {
        $start.ArgumentList.Add($argument)
    }
    $collector = [Diagnostics.Process]::Start($start)
    $stdout = $collector.StandardOutput.ReadToEndAsync()
    $stderr = $collector.StandardError.ReadToEndAsync()
    if (-not $collector.WaitForExit($CaptureTimeoutSeconds * 1000)) {
        $result.status = 'capture-timeout'
        $collector.Kill($true)
        $collector.WaitForExit()
    } else {
        $result.exitCode = $collector.ExitCode
        if ($collector.ExitCode -eq 0 -and (Test-Path -LiteralPath $OutputPath) -and (Get-Item -LiteralPath $OutputPath).Length -gt 0) {
            $result.status = 'captured'
        }
    }
    ($stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult()) | Set-Content -LiteralPath $result.log
} catch {
    $result.error = $_.Exception.Message
} finally {
    if ($null -ne $collector) { $collector.Dispose() }
}
[pscustomobject]$result
