#requires -Version 7.0
param(
    [Parameter(Mandatory)][string]$OutputDirectory,
    [ValidateRange(3,10)][int]$Repetitions = 3,
    [ValidateRange(5,100)][int]$Batches = 12,
    [ValidateRange(1,20)][int]$CyclesPerBatch = 4,
    [ValidateRange(100,5000)][int]$Rows = 500,
    [ValidateRange(1,20)][int]$WarmupBatches = 3
)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output = [IO.Path]::GetFullPath($OutputDirectory, $root)
if (Test-Path -LiteralPath $output) { throw 'Use a new output directory to retain evidence.' }
New-Item -ItemType Directory -Path $output | Out-Null
Push-Location $root
try {
    dotnet build samples/NativeLifetime/NativeLifetime.csproj -c Release --disable-build-servers -m:1 -warnaserror
    if ($LASTEXITCODE -ne 0) { throw 'Native lifetime build failed.' }
    $dll = Join-Path $root 'samples/NativeLifetime/bin/Release/net10.0-windows/NativeLifetime.dll'
    $files = @('UI Framework','UI Framework.Wpf','samples/NativeLifetime') | ForEach-Object {
        Get-ChildItem -LiteralPath $_ -Recurse -File | Where-Object { $_.Extension -in @('.cs','.csproj') -and $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
    }
    $files | ForEach-Object { [pscustomobject]@{ path=[IO.Path]::GetRelativePath($root,$_.FullName); sha256=(Get-FileHash -LiteralPath $_.FullName).Hash } } |
        ConvertTo-Json | Set-Content (Join-Path $output 'source-manifest.json')
    Get-ChildItem -LiteralPath (Split-Path $dll) -Filter '*.dll' | ForEach-Object {
        [pscustomobject]@{ name=$_.Name; sha256=(Get-FileHash -LiteralPath $_.FullName).Hash }
    } | ConvertTo-Json | Set-Content (Join-Path $output 'binary-manifest.json')
    [pscustomobject]@{ kind='native-lifetime-memory-observation-not-performance-gate'; repetitions=$Repetitions;
        batches=$Batches; cycles=$CyclesPerBatch; rows=$Rows; warmups=$WarmupBatches; includesRetention=$true; sdk=(& dotnet --version).Trim();
        revision=(& git rev-parse HEAD).Trim(); dirty=[bool](& git status --porcelain) } |
        ConvertTo-Json | Set-Content (Join-Path $output 'campaign.json')
    foreach ($name in @('window','negative','retained') + @(1..$Repetitions | ForEach-Object { "memory-$_" })) {
        $mode = if ($name.StartsWith('memory-')) { 'memory' } else { $name }
        $report = Join-Path $output "$name.json"
        $arguments = @($dll,'--mode',$mode,'--report',$report,'--rows',"$Rows",'--batches',"$Batches",'--cycles',"$CyclesPerBatch",'--warmups',"$WarmupBatches")
        if ($mode -eq 'negative') { $arguments = @($dll,'--mode',$mode,'--report',$report,'--rows',"$Rows",'--batches','1','--cycles','2','--warmups','0') }
        Write-Output "Native lifetime: $name"
        $start = [Diagnostics.ProcessStartInfo]::new((Get-Command dotnet).Source)
        $start.UseShellExecute=$false; $start.CreateNoWindow=$true
        $start.RedirectStandardOutput=$true; $start.RedirectStandardError=$true
        foreach ($argument in $arguments) { $start.ArgumentList.Add($argument) }
        $process = [Diagnostics.Process]::Start($start)
        try {
            $stdout=$process.StandardOutput.ReadToEndAsync(); $stderr=$process.StandardError.ReadToEndAsync()
            $finished=$process.WaitForExit(300000)
            if (-not $finished) { $process.Kill($true); $process.WaitForExit() }
            ($stdout.GetAwaiter().GetResult()+$stderr.GetAwaiter().GetResult()) | Set-Content (Join-Path $output "$name.log")
            [pscustomobject]@{ exitCode=$process.ExitCode; cpuSeconds=$process.TotalProcessorTime.TotalSeconds; arguments=$arguments } |
                ConvertTo-Json | Set-Content (Join-Path $output "$name-process.json")
            if (-not $finished -or $process.ExitCode -ne 0) { throw "Native lifetime case failed: $name" }
        } finally { $process.Dispose() }
        $result = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
        if (-not $result.Passed -or -not $result.DispatcherShutdownFinished -or -not $result.CleanupPolicyClosed) { throw "Incomplete shutdown: $name" }
    }
    python tools/Summarize-NativeMemory.py $output --output (Join-Path $output 'analysis.json')
    if ($LASTEXITCODE -ne 0) { throw 'Memory evidence validation failed.' }
} finally { Pop-Location }
