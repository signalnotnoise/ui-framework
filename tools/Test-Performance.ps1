#requires -Version 7.0
param(
    [string]$BaselineRef,
    [ValidateRange(3, 15)][int]$Samples = 7,
    [string]$OutputDirectory,
    [switch]$ReportOnly,
    [switch]$IncludeLayoutEditors,
    [string]$DumpToolPath
)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location $projectRoot
try {
    if ($DumpToolPath) { $DumpToolPath = (Resolve-Path -LiteralPath $DumpToolPath -ErrorAction Stop).Path }
    $budget = Get-Content tools/performance-baseline.json -Raw | ConvertFrom-Json
    if (-not $BaselineRef) { $BaselineRef = $budget.revision }
    $baselineCommit = (& git rev-parse --verify "$BaselineRef^{commit}").Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Baseline revision must resolve locally. Fetch full history first.' }
    if (-not $OutputDirectory) { $OutputDirectory = 'artifacts/performance/' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8) }
    $output = [IO.Path]::GetFullPath($OutputDirectory, $projectRoot)
    if (Test-Path $output) { throw 'Use a new output directory to preserve previous evidence.' }
    New-Item -ItemType Directory -Path $output | Out-Null
    $baseline = Join-Path $output 'baseline-source'
    $archive = Join-Path $output 'baseline.zip'
    & git archive --format=zip "--output=$archive" $baselineCommit
    if ($LASTEXITCODE -ne 0) { throw 'Baseline archive failed.' }
    Expand-Archive -LiteralPath $archive -DestinationPath $baseline
    # Run exactly the candidate's sample workload against both framework revisions.
    # Never copy build outputs or modify the actual baseline checkout/history.
    $sample = Join-Path $projectRoot 'samples/Counter'
    Get-ChildItem -LiteralPath $sample -Recurse -File | Where-Object {
        $_.Extension -in @('.cs', '.csproj') -and $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
    } | ForEach-Object {
        $relative = [IO.Path]::GetRelativePath($projectRoot, $_.FullName)
        $destination = Join-Path $baseline $relative
        New-Item -ItemType Directory -Path (Split-Path $destination) -Force | Out-Null
        Copy-Item -LiteralPath $_.FullName -Destination $destination
    }
    foreach ($root in @($baseline, $projectRoot)) {
        & dotnet build (Join-Path $root 'samples/Counter/Counter.csproj') -c Release --disable-build-servers -m:1 -warnaserror
        if ($LASTEXITCODE -ne 0) { throw "Benchmark build failed: $root" }
    }
    # Preserve the exact dirty candidate sources, including new files, alongside
    # the raw runs. A HEAD hash alone cannot identify a working-tree benchmark.
    $candidateSource = Join-Path $output 'candidate-source'
    $sourceFiles = @('Directory.Build.props', 'global.json', 'UI Framework.slnx') | ForEach-Object { Get-Item (Join-Path $projectRoot $_) }
    foreach ($folder in @('UI Framework', 'UI Framework.Wpf', 'samples', 'tools')) {
        $sourceFiles += Get-ChildItem (Join-Path $projectRoot $folder) -Recurse -File | Where-Object {
            $_.FullName -notmatch '[\\/](bin|obj|artifacts)[\\/]' -and $_.Extension -in @('.cs', '.csproj', '.ps1', '.json')
        }
    }
    $manifest = foreach ($file in $sourceFiles) {
        $relative = [IO.Path]::GetRelativePath($projectRoot, $file.FullName)
        $destination = Join-Path $candidateSource $relative
        New-Item -ItemType Directory -Path (Split-Path $destination) -Force | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $destination
        [pscustomobject]@{ path = $relative; sha256 = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash }
    }
    $manifest | ConvertTo-Json | Set-Content (Join-Path $output 'candidate-source-manifest.json')
    $executables = @{
        baseline = Join-Path $baseline 'samples/Counter/bin/Release/net10.0-windows/Counter.dll'
        candidate = Join-Path $projectRoot 'samples/Counter/bin/Release/net10.0-windows/Counter.dll'
    }
    $scenarios = [ordered]@{ 'full-list' = @(); 'virtualized' = @('--virtualized'); 'themed-full-list' = @('--themed') }
    if ($IncludeLayoutEditors) { $scenarios['layout-editors'] = @('--layout-editors') }
    $runs = [System.Collections.Generic.List[object]]::new()
    foreach ($scenario in $scenarios.Keys) {
        # One discarded process per side warms filesystem/runtime caches. Measured
        # processes remain fresh; mount timings intentionally include JIT/startup work.
        for ($iteration = 0; $iteration -le $Samples; $iteration++) {
            $order = if ($iteration % 2 -eq 0) { @('baseline', 'candidate') } else { @('candidate', 'baseline') }
            foreach ($side in $order) {
                $report = Join-Path $output "$scenario-$iteration-$side.json"
                $log = Join-Path $output "$scenario-$iteration-$side.log"
                Write-Output "$scenario / sample $iteration / $side"
                $arguments = @($executables[$side], '--compare', '--report', $report) + $scenarios[$scenario]
                $start = [Diagnostics.ProcessStartInfo]::new((Get-Command dotnet).Source)
                $start.UseShellExecute = $false
                $start.CreateNoWindow = $true
                $start.RedirectStandardOutput = $true
                $start.RedirectStandardError = $true
                foreach ($argument in $arguments) { $start.ArgumentList.Add($argument) }
                $elapsed = [Diagnostics.Stopwatch]::StartNew()
                $process = [Diagnostics.Process]::Start($start)
                try {
                    $stdout = $process.StandardOutput.ReadToEndAsync()
                    $stderr = $process.StandardError.ReadToEndAsync()
                    $completed = $process.WaitForExit(120000)
                    if (-not $completed) {
                        $failure = [ordered]@{
                            schemaVersion = 1; reason = 'process-timeout'; scenario = $scenario
                            sample = $iteration; side = $side; processId = $process.Id
                            timeoutSeconds = 120; elapsedSeconds = $elapsed.Elapsed.TotalSeconds
                            cpuSeconds = $process.TotalProcessorTime.TotalSeconds
                            baselineRevision = $baselineCommit; report = $report; log = $log
                        }
                        $failure | ConvertTo-Json | Set-Content (Join-Path $output 'failure.json')
                        try {
                            if ($DumpToolPath) {
                                Write-Output "Benchmark timed out; capturing diagnostics before termination."
                                $failure.dump = & (Join-Path $PSScriptRoot 'Save-BenchmarkDump.ps1') -ProcessId $process.Id -DumpToolPath $DumpToolPath -OutputPath (Join-Path $output "$scenario-$iteration-$side.dmp")
                                $failure | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $output 'failure.json')
                            }
                        } finally {
                            if (-not $process.HasExited) { $process.Kill($true) }
                            $process.WaitForExit()
                        }
                    }
                    ($stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult()) | Set-Content -LiteralPath $log
                    if (-not $completed) { throw "Benchmark exceeded 120 seconds: $scenario / $iteration / $side. See $log" }
                    if ($process.ExitCode -ne 0) { throw "Benchmark failed. See $log" }
                } finally { $process.Dispose() }
                $result = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
                if ($result.SchemaVersion -ne 2 -or $result.Rows -ne 1000 -or $result.MixedOperations -ne 50 -or $result.Scenario -ne $scenario) {
                    throw "Unexpected benchmark workload in $report"
                }
                foreach ($metricName in @('MountMilliseconds', 'Milliseconds', 'MountAllocatedBytes', 'AllocatedBytes', 'MountBodyBuilds', 'BodyBuilds', 'Mounts', 'Unmounts')) {
                    $value = $result.$metricName
                    if ($null -eq $value -or $value -isnot [ValueType] -or -not [double]::IsFinite([double]$value) -or $value -lt 0) {
                        throw "Missing or invalid metric $metricName in $report"
                    }
                }
                if ($iteration -gt 0) { $runs.Add([pscustomobject]@{ scenario = $scenario; side = $side; sample = $iteration; result = $result }) }
            }
        }
    }
    function Median($values) {
        $sorted = @($values | Sort-Object)
        $middle = [int][Math]::Floor($sorted.Count / 2)
        if ($sorted.Count % 2) { return [double]$sorted[$middle] }
        return ([double]$sorted[$middle - 1] + [double]$sorted[$middle]) / 2
    }
    $metrics = [ordered]@{
        MountMilliseconds = $budget.timeRegressionPercent; Milliseconds = $budget.timeRegressionPercent
        MountAllocatedBytes = $budget.allocationRegressionPercent; AllocatedBytes = $budget.allocationRegressionPercent
        MountBodyBuilds = $budget.componentWorkRegressionPercent; BodyBuilds = $budget.componentWorkRegressionPercent
        Mounts = $budget.componentWorkRegressionPercent; Unmounts = $budget.componentWorkRegressionPercent
    }
    $comparisons = foreach ($scenario in $scenarios.Keys) {
        foreach ($metric in $metrics.Keys) {
            $left = @($runs | Where-Object { $_.scenario -eq $scenario -and $_.side -eq 'baseline' } | ForEach-Object { [double]$_.result.$metric })
            $right = @($runs | Where-Object { $_.scenario -eq $scenario -and $_.side -eq 'candidate' } | ForEach-Object { [double]$_.result.$metric })
            $reference = Median $left; $current = Median $right
            $limit = $metrics[$metric]
            $scenarioBudgets = $budget.PSObject.Properties['scenarioMetricBudgets']?.Value
            $scenarioBudget = if ($null -ne $scenarioBudgets) { $scenarioBudgets.PSObject.Properties[$scenario]?.Value } else { $null }
            $metricBudget = if ($null -ne $scenarioBudget) {
                $scenarioBudget.PSObject.Properties[$metric]?.Value
            } else { $null }
            $exception = $metricBudget
            if ($null -ne $exception) { $limit = $exception }
            $change = if ($reference -eq 0) { if ($current -eq 0) { 0 } else { $null } } else { 100 * ($current / $reference - 1) }
            [pscustomobject]@{
                scenario = $scenario; metric = $metric; baselineMedian = $reference; candidateMedian = $current
                changePercent = $change; budgetPercent = $limit
                baselineMin = ($left | Measure-Object -Minimum).Minimum; baselineMax = ($left | Measure-Object -Maximum).Maximum
                candidateMin = ($right | Measure-Object -Minimum).Minimum; candidateMax = ($right | Measure-Object -Maximum).Maximum
                regression = $current -gt ($reference * (1 + $limit / 100))
            }
        }
    }
    $summary = [ordered]@{
        schemaVersion = 1; timeUtc = [DateTime]::UtcNow.ToString('o'); baselineRevision = $baselineCommit
        candidateRevision = (& git rev-parse HEAD).Trim(); candidateDirty = [bool](& git status --porcelain)
        sdk = (& dotnet --version).Trim(); os = [Environment]::OSVersion.VersionString; processorCount = [Environment]::ProcessorCount
        samples = $Samples; warmupProcessesPerSide = 1; rows = 1000; operations = 50
        budgetPolicy = $budget
        runtimeVersions = @($runs.result.Runtime | Sort-Object -Unique)
        passed = @($comparisons | Where-Object regression).Count -eq 0; comparisons = @($comparisons)
    }
    $summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $output 'summary.json')
    $lines = @('# Paired rendering performance', '', "Baseline: $baselineCommit. Samples per side: $Samples. Negative change is an improvement.", '',
        '| Scenario | Metric | Baseline median | Candidate median | Change | Result |', '| --- | --- | ---: | ---: | ---: | --- |')
    foreach ($item in $comparisons) {
        $changeText = if ($null -eq $item.changePercent) { 'new work from zero' } else { '{0:N2}%' -f $item.changePercent }
        $resultText = if ($item.regression) { 'REGRESSION' } else { 'within budget' }
        $lines += '| {0} | {1} | {2:N2} | {3:N2} | {4} | {5} |' -f $item.scenario, $item.metric, $item.baselineMedian, $item.candidateMedian, $changeText, $resultText
    }
    $lines | Set-Content -LiteralPath (Join-Path $output 'summary.md')
    Write-Output "Comparison saved to $output"
    if (-not $summary.passed -and -not $ReportOnly) { throw 'Performance budget exceeded. Inspect summary.json and raw runs; do not silently reset the baseline.' }
} finally { Pop-Location }
