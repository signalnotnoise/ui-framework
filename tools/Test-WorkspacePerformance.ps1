#requires -Version 7.0
param([ValidateRange(3,15)][int]$Samples = 7, [Parameter(Mandatory)][string]$OutputDirectory, [string]$BaselineRef,
    [ValidateSet('workspace','dock','column','split')][string]$Isolation = 'workspace', [switch]$ProbeLayout,
    [switch]$CompareSubmissionVirtualization)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location $projectRoot
try {
    $output = [IO.Path]::GetFullPath($OutputDirectory, $projectRoot)
    if (Test-Path $output) { throw 'Use a new directory to retain previous evidence.' }
    New-Item -ItemType Directory -Path $output | Out-Null
    $referenceSide = 'native-adapters'
    if ($CompareSubmissionVirtualization) {
        if ($BaselineRef -or $Isolation -ne 'workspace') { throw 'Submission virtualization compares the same framework workspace and requires no BaselineRef.' }
        $referenceSide = 'full-list'
    }
    $baselineRevision = $null
    $candidateExecutable = Join-Path $projectRoot 'samples/WorkspaceComparison/bin/Release/net10.0-windows/WorkspaceComparison.dll'
    $referenceExecutable = $candidateExecutable
    if ($BaselineRef) {
        $baselineRevision = (& git rev-parse --verify "$BaselineRef^{commit}").Trim()
        if ($LASTEXITCODE -ne 0) { throw 'Baseline revision must resolve locally.' }
        $baselineRoot = Join-Path $output 'baseline-source'
        $archive = Join-Path $output 'baseline.zip'
        git archive --format=zip "--output=$archive" $baselineRevision
        if ($LASTEXITCODE -ne 0) { throw 'Baseline archive failed.' }
        Expand-Archive -LiteralPath $archive -DestinationPath $baselineRoot
        $baselineSample = Join-Path $baselineRoot 'samples/WorkspaceComparison'
        New-Item -ItemType Directory -Path $baselineSample -Force | Out-Null
        Get-ChildItem samples/WorkspaceComparison -File | Where-Object Extension -in @('.cs','.csproj') | Copy-Item -Destination $baselineSample
        dotnet build (Join-Path $baselineSample 'WorkspaceComparison.csproj') -c Release --disable-build-servers -m:1 -warnaserror
        if ($LASTEXITCODE -ne 0) { throw 'Baseline workspace build failed.' }
        $referenceSide = 'baseline-primitives'
        $referenceExecutable = Join-Path $baselineSample 'bin/Release/net10.0-windows/WorkspaceComparison.dll'
    }
    dotnet build samples/WorkspaceComparison/WorkspaceComparison.csproj -c Release --disable-build-servers -m:1 -warnaserror
    if ($LASTEXITCODE -ne 0) { throw 'Workspace comparison build failed.' }
    $sourceRoot = Join-Path $output 'source'
    New-Item -ItemType Directory -Path $sourceRoot -Force | Out-Null
    foreach ($file in @('Directory.Build.props','global.json')) { Copy-Item -LiteralPath $file -Destination $sourceRoot }
    $manifest = foreach ($folder in @('UI Framework','UI Framework.Wpf','samples/WorkspaceComparison','tools')) {
        Get-ChildItem -LiteralPath $folder -Recurse -File | Where-Object {
            $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Extension -in @('.cs','.csproj','.ps1','.json')
        } | ForEach-Object {
            $relative = [IO.Path]::GetRelativePath($projectRoot, $_.FullName)
            $destination = Join-Path $sourceRoot $relative
            New-Item -ItemType Directory -Path (Split-Path $destination) -Force | Out-Null
            Copy-Item -LiteralPath $_.FullName -Destination $destination
            [pscustomobject]@{ path=$relative; sha256=(Get-FileHash -LiteralPath $destination).Hash }
        }
    }
    $manifest | ConvertTo-Json | Set-Content (Join-Path $output 'source-manifest.json')
    $runs = [Collections.Generic.List[object]]::new()
    for ($sample = 0; $sample -le $Samples; $sample++) {
        $order = if ($sample % 2 -eq 0) { @($referenceSide,'framework-primitives') } else { @('framework-primitives',$referenceSide) }
        foreach ($side in $order) {
            Write-Output "Workspace / sample $sample / $side"
            $report = Join-Path $output "$sample-$side.json"
            $start = [Diagnostics.ProcessStartInfo]::new((Get-Command dotnet).Source)
            $start.UseShellExecute = $false; $start.CreateNoWindow = $true
            $start.RedirectStandardOutput = $true; $start.RedirectStandardError = $true
            $executable = if ($side -eq $referenceSide) { $referenceExecutable } else { $candidateExecutable }
            $arguments = @($executable,'--report',$report,'--isolate',$Isolation)
            if ($side -eq 'native-adapters') { $arguments += '--native' }
            if ($ProbeLayout) { $arguments += '--probe-layout' }
            if ($CompareSubmissionVirtualization) {
                $arguments += @('--submission-list', $(if ($side -eq $referenceSide) { 'full' } else { 'virtualized' }))
            }
            foreach ($argument in $arguments) { $start.ArgumentList.Add($argument) }
            $process = [Diagnostics.Process]::Start($start)
            try {
                $stdout = $process.StandardOutput.ReadToEndAsync(); $stderr = $process.StandardError.ReadToEndAsync()
                $finished = $process.WaitForExit(120000)
                if (-not $finished) { $process.Kill($true); $process.WaitForExit() }
                ($stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult()) | Set-Content (Join-Path $output "$sample-$side.log")
                if (-not $finished -or $process.ExitCode -ne 0) { throw "Workspace comparison failed: $sample / $side" }
            } finally { $process.Dispose() }
            $result = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
            $expectedScenario = if ($side -eq 'native-adapters') { 'native-adapters' } else { 'framework-primitives' }
            if ($result.Rows -ne 1000 -or $result.EditorLines -ne 1000 -or $result.Operations -ne 50 -or $result.Scenario -ne $expectedScenario) {
                throw 'Unexpected workspace workload.'
            }
            if ($result.Isolation -ne $Isolation) { throw 'Unexpected isolation scenario.' }
            if ($CompareSubmissionVirtualization) {
                $expectedList = if ($side -eq $referenceSide) { 'full' } else { 'virtualized' }
                if ($result.SubmissionList -ne $expectedList -or -not $result.SubmissionBehaviorChecksPassed) { throw 'Submission behavior validation failed.' }
            }
            if ($sample -gt 0) { $runs.Add([pscustomobject]@{ side=$side; result=$result }) }
        }
    }
    function Median($values) {
        $sorted = @($values | Sort-Object); $middle = [int][Math]::Floor($sorted.Count / 2)
        if ($sorted.Count % 2) { return [double]$sorted[$middle] }
        return ([double]$sorted[$middle - 1] + [double]$sorted[$middle]) / 2
    }
    $comparisons = foreach ($metric in @('MountMilliseconds','Milliseconds','MountAllocatedBytes','AllocatedBytes','MountBodyBuilds','BodyBuilds','NativeCreates','NativeReleasesDuringUpdates','MountRealizedRows','UpdateRealizedRows')) {
        $before = @($runs | Where-Object side -eq $referenceSide | ForEach-Object { [double]$_.result.$metric })
        $after = @($runs | Where-Object side -eq 'framework-primitives' | ForEach-Object { [double]$_.result.$metric })
        $left = Median $before; $right = Median $after
        [pscustomobject]@{ metric=$metric; baselineMedian=$left; frameworkMedian=$right
            changePercent=if($left -ne 0){100*($right/$left-1)}elseif($right -eq 0){0}else{$null}
            baselineMin=($before | Measure-Object -Minimum).Minimum; baselineMax=($before | Measure-Object -Maximum).Maximum
            frameworkMin=($after | Measure-Object -Minimum).Minimum; frameworkMax=($after | Measure-Object -Maximum).Maximum }
    }
    [ordered]@{ kind='representative-adapter-comparison-not-release-gate'; samples=$Samples; warmupsPerSide=1
        baselineKind=$referenceSide; baselineRevision=$baselineRevision
        isolation=$Isolation; probesEnabled=[bool]$ProbeLayout
        submissionVirtualization=[bool]$CompareSubmissionVirtualization
        sdk=(& dotnet --version).Trim(); runtime=@($runs.result.Runtime | Sort-Object -Unique)
        os=[Environment]::OSVersion.VersionString; processorCount=[Environment]::ProcessorCount
        revision=(& git rev-parse HEAD).Trim(); dirty=[bool](& git status --porcelain)
        rows=1000; editorLines=1000; operations=50; comparisons=@($comparisons)
    } | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $output 'summary.json')
    $comparisons | Format-Table -AutoSize
    Write-Output 'Comparison complete. This representative workload does not clear the revision performance gate or validate the complete consumer app.'
} finally { Pop-Location }
