#requires -Version 7.0
param([Parameter(Mandatory)][string]$OutputDirectory, [ValidateRange(1,10)][int]$Repetitions = 3,
    [switch]$IncludeInteractive,
    [ValidateCount(1,19)]
    [ValidateSet('environment-boundary-close','control-close','popup-success','popup-parent-close','popup-child-close','focus-keyboard','airspace','initialization-recovery','runtime-missing-recovery','navigation-recovery','renderer-recovery','browser-recovery','stalled-success-close','stalled-fault-close','script-child-close','script-parent-close','retention-fresh','retention-reused','pointer-bounds')]
    [string[]]$Modes = @('environment-boundary-close','control-close','popup-success','popup-parent-close','popup-child-close'))
$ErrorActionPreference = 'Stop'
if ($IncludeInteractive -and -not $PSBoundParameters.ContainsKey('Modes')) { $Modes += @('focus-keyboard','airspace','pointer-bounds') }
if (-not $IncludeInteractive -and ($Modes -contains 'focus-keyboard' -or $Modes -contains 'airspace' -or $Modes -contains 'pointer-bounds')) {
    throw 'Interactive cases activate a desktop window, inject test keystrokes and capture its pixels. Run explicitly with -IncludeInteractive only when ready for desktop interaction.'
}
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output = [IO.Path]::GetFullPath($OutputDirectory, $root)
if (Test-Path -LiteralPath $output) { throw 'Use a new output directory to preserve evidence and isolate profiles.' }
New-Item -ItemType Directory -Path $output | Out-Null
Push-Location $root
try {
    dotnet build samples/WebViewAcceptance/WebViewAcceptance.csproj -c Release --disable-build-servers -m:1 -warnaserror
    if ($LASTEXITCODE -ne 0) { throw 'WebView acceptance build failed.' }
    $dll = Join-Path $root 'samples/WebViewAcceptance/bin/Release/net10.0-windows/WebViewAcceptance.dll'
    $sources = @('UI Framework','UI Framework.Wpf','samples/WebViewAcceptance') | ForEach-Object {
        Get-ChildItem -LiteralPath $_ -Recurse -File | Where-Object { $_.Extension -in @('.cs','.csproj','.html') -and $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
    }
    $sources | ForEach-Object { [pscustomobject]@{ path=[IO.Path]::GetRelativePath($root,$_.FullName); sha256=(Get-FileHash -LiteralPath $_.FullName).Hash } } |
        ConvertTo-Json | Set-Content (Join-Path $output 'source-manifest.json')
    [pscustomobject]@{ revision=(& git rev-parse HEAD).Trim(); dirty=[bool](& git status --porcelain);
        sdk=(& dotnet --version).Trim(); repetitions=$Repetitions; modes=$Modes; kind='real-webview-local-page-acceptance'; releaseGate=$false } |
        ConvertTo-Json | Set-Content (Join-Path $output 'campaign.json')
    $reports = @()
    foreach ($iteration in 1..$Repetitions) {
        foreach ($mode in $Modes) {
            $name = "$iteration-$mode"
            Write-Output "WebView acceptance: $name"
            $caseOutput = Join-Path $output $name
            $start = [Diagnostics.ProcessStartInfo]::new((Get-Command dotnet).Source)
            $start.UseShellExecute=$false; $start.CreateNoWindow=$true
            $start.RedirectStandardOutput=$true; $start.RedirectStandardError=$true
            foreach ($argument in @($dll,$mode,$caseOutput)) { $start.ArgumentList.Add($argument) }
            if ($mode -in @('focus-keyboard','airspace','pointer-bounds')) { $start.ArgumentList.Add('--interactive') }
            $process = [Diagnostics.Process]::Start($start)
            try {
                $stdout=$process.StandardOutput.ReadToEndAsync(); $stderr=$process.StandardError.ReadToEndAsync()
                $finished=$process.WaitForExit(120000)
                if (-not $finished) { $process.Kill($true); $process.WaitForExit() }
                ($stdout.GetAwaiter().GetResult()+$stderr.GetAwaiter().GetResult()) | Set-Content (Join-Path $output "$name.log")
                [pscustomobject]@{ exitCode=$process.ExitCode; timedOut=(-not $finished); mode=$mode } |
                    ConvertTo-Json | Set-Content (Join-Path $output "$name-process.json")
                if (-not $finished -or $process.ExitCode -ne 0) { throw "WebView acceptance failed: $name. See preserved log/profile." }
            } finally { $process.Dispose() }
            $report = Get-Content -LiteralPath (Join-Path $caseOutput 'report.json') -Raw | ConvertFrom-Json
            if (-not $report.Passed -or -not $report.DispatcherShutdownFinished -or -not $report.CleanupPolicyClosed) { throw "Incomplete browser shutdown: $name" }
            $reports += $report
        }
    }
    [pscustomobject]@{ passed=$true; cases=$reports.Count; releaseGate=$false; reports=$reports } |
        ConvertTo-Json -Depth 12 | Set-Content (Join-Path $output 'summary.json')
    Write-Output "Passed $($reports.Count) real WebView2 cases. Isolated profiles retained with evidence."
} finally { Pop-Location }
