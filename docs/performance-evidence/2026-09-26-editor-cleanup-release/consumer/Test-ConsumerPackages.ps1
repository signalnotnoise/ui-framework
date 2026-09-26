#requires -Version 7.0
param(
    [Parameter(Mandatory)][string]$ConsumerRoot,
    [Parameter(Mandatory)][string]$PackageDirectory,
    [Parameter(Mandatory)][string]$CandidateVersion,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [string]$BaselineVersion,
    [ValidateRange(3,15)][int]$Samples = 7,
    [switch]$UseWorkingTree,
    [switch]$ReportOnly
)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$consumer = (Resolve-Path -LiteralPath $ConsumerRoot).Path
$feed = (Resolve-Path -LiteralPath $PackageDirectory).Path
$output = [IO.Path]::GetFullPath($OutputDirectory, $root)
if (Test-Path $output) { throw 'Use a new output directory to retain evidence.' }
$dirty = [bool](& git -C $consumer status --porcelain)
if ($dirty -and -not $UseWorkingTree) { throw 'UseWorkingTree is required to snapshot uncommitted integration changes.' }
if ($LASTEXITCODE -ne 0) { throw 'Cannot read consumer status.' }
$revision = (& git -C $consumer rev-parse HEAD).Trim()
$relativeProject = 'Lab Feedback WPF/Lab Feedback WPF.csproj'
$referenceVersion = ([xml](Get-Content (Join-Path $consumer $relativeProject) -Raw)).SelectSingleNode("/Project/ItemGroup/PackageReference[@Include='SignalNotNoise.UI.Wpf']").Version
$installedVersion = $referenceVersion.Trim([char[]]'[]')
$oldVersion = if ($BaselineVersion) { $BaselineVersion.Trim([char[]]'[]') } else { $installedVersion }
if ($CandidateVersion -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z]+([.-][0-9A-Za-z]+)*)?$') { throw 'Invalid candidate version.' }
if ($oldVersion -eq $CandidateVersion) { throw 'Candidate must differ from the baseline package.' }
$packages = foreach ($id in @('SignalNotNoise.UI','SignalNotNoise.UI.Wpf')) {
    Get-Item -LiteralPath (Join-Path $feed "$id.$CandidateVersion.nupkg")
}
New-Item -ItemType Directory -Path $output | Out-Null
$archive = Join-Path $output 'consumer.zip'
& git -C $consumer archive --format=zip "--output=$archive" $revision
if ($LASTEXITCODE -ne 0) { throw 'Consumer archive failed.' }
$snapshot = Join-Path $output 'consumer-source'
Expand-Archive -LiteralPath $archive -DestinationPath $snapshot
if ($UseWorkingTree) {
    $deleted = @((& git -C $consumer -c core.quotepath=false ls-files --deleted) | ForEach-Object { $_.Trim('"') })
    if ($LASTEXITCODE -ne 0) { throw 'Cannot enumerate deleted consumer sources.' }
    foreach ($file in $deleted) {
        $destination = Join-Path $snapshot $file
        if (Test-Path -LiteralPath $destination) { Remove-Item -LiteralPath $destination -Force }
    }
    $files = (& git -C $consumer -c core.quotepath=false ls-files --cached --others --exclude-standard) |
        ForEach-Object { $_.Trim('"') } | Where-Object { $_ -notin $deleted }
    if ($LASTEXITCODE -ne 0) { throw 'Cannot enumerate consumer sources.' }
    foreach ($file in $files) {
        $source = Join-Path $consumer $file
        if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Missing consumer source: $file" }
        $destination = Join-Path $snapshot $file
        New-Item -ItemType Directory -Path (Split-Path $destination) -Force | Out-Null
        Copy-Item -LiteralPath $source -Destination $destination
    }
}
$executables = @{}
$sourceHashes = @()
foreach ($side in @('baseline','candidate')) {
    $sideRoot = Join-Path $output $side
    $app = Join-Path $sideRoot 'consumer'
    New-Item -ItemType Directory -Path $sideRoot | Out-Null
    Copy-Item -LiteralPath $snapshot -Destination $app -Recurse
    $project = Join-Path $app $relativeProject
    $targetVersion = if ($side -eq 'baseline') { $oldVersion } else { $CandidateVersion }
    if ($targetVersion -ne $installedVersion) {
        $text = [IO.File]::ReadAllText($project)
        $oldReference = 'Include="SignalNotNoise.UI.Wpf" Version="' + $referenceVersion + '"'
        if (-not $text.Contains($oldReference)) { throw 'Expected package reference not found.' }
        [IO.File]::WriteAllText($project, $text.Replace($oldReference,
            'Include="SignalNotNoise.UI.Wpf" Version="' + $targetVersion + '"'))
    }
    if ($side -eq 'baseline') {
        $policy = Join-Path $app 'Lab Feedback WPF/Services/WpfComCleanupPolicy.cs'
        Copy-Item -LiteralPath (Join-Path $root 'UI Framework.Wpf/WpfComCleanupPolicy.cs') -Destination $policy
    }
    if ($side -eq 'candidate') {
        foreach ($package in $packages) {
            $destination = Join-Path $app "vendor/ui-framework/$($package.Name)"
            if (Test-Path -LiteralPath $destination) {
                if ((Get-FileHash -LiteralPath $destination).Hash -ne (Get-FileHash -LiteralPath $package.FullName).Hash) {
                    throw 'An archived package has different bytes.'
                }
            } else {
                Copy-Item -LiteralPath $package.FullName -Destination $destination
            }
        }
    }
    $harness = Join-Path $sideRoot 'ConsumerDiagnostics'
    New-Item -ItemType Directory -Path $harness | Out-Null
    Get-ChildItem (Join-Path $root 'samples/ConsumerDiagnostics') -File | Where-Object { $_.Extension -in '.cs','.csproj' } |
        Copy-Item -Destination $harness
    New-Item -ItemType Directory -Path (Join-Path $sideRoot 'WorkspaceComparison') | Out-Null
    Copy-Item (Join-Path $root 'samples/WorkspaceComparison/LayoutProbe.cs') (Join-Path $sideRoot 'WorkspaceComparison/LayoutProbe.cs')
    $sourceHashes += Get-ChildItem $app,$harness,(Join-Path $sideRoot 'WorkspaceComparison') -Recurse -File | Where-Object {
        $_.Extension -in '.cs','.csproj','.nupkg','.config','.xshd' -and $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
    } | ForEach-Object { [pscustomobject]@{side=$side; path=[IO.Path]::GetRelativePath($sideRoot,$_.FullName); sha256=(Get-FileHash $_.FullName).Hash} }
    & dotnet build (Join-Path $harness 'ConsumerDiagnostics.csproj') -c Release "-p:ConsumerProject=$project" --configfile (Join-Path $app 'NuGet.Config') --disable-build-servers -m:1 -warnaserror
    if ($LASTEXITCODE -ne 0) { throw "$side consumer build failed." }
    $assets = Get-Content (Join-Path $app 'Lab Feedback WPF/obj/project.assets.json') -Raw | ConvertFrom-Json
    $version = if ($side -eq 'baseline') { $oldVersion } else { $CandidateVersion }
    foreach ($id in @('SignalNotNoise.UI','SignalNotNoise.UI.Wpf')) {
        if (-not $assets.libraries.PSObject.Properties["$id/$version"]) { throw "Wrong resolved $id version for $side." }
    }
    $executables[$side] = Join-Path $harness 'bin/Release/net10.0-windows/ConsumerDiagnostics.dll'
}
$sourceHashes | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $output 'source-manifest.json')
Copy-Item -LiteralPath $PSCommandPath -Destination (Join-Path $output 'Test-ConsumerPackages.ps1')
$scenarios = [ordered]@{
    window=@(); splitter=@('--resize-navigation')
    'full-tree'=@('--resize-navigation','--full-files')
    'virtualized-tree'=@('--resize-navigation','--virtualize-files')
}
$runs = [Collections.Generic.List[object]]::new()
foreach ($scenario in $scenarios.Keys) {
    for ($sample=0; $sample -le $Samples; $sample++) {
        $order = if ($sample % 2) { @('candidate','baseline') } else { @('baseline','candidate') }
        foreach ($side in $order) {
            $name = "$scenario-$sample-$side"
            Write-Output $name
            $report = Join-Path $output "$name.json"
            $start = [Diagnostics.ProcessStartInfo]::new((Get-Command dotnet).Source)
            $start.UseShellExecute=$false; $start.CreateNoWindow=$true
            $start.RedirectStandardOutput=$true; $start.RedirectStandardError=$true
            foreach ($argument in (@($executables[$side],'--report',$report) + $scenarios[$scenario])) { $start.ArgumentList.Add($argument) }
            $watch = [Diagnostics.Stopwatch]::StartNew()
            $process = [Diagnostics.Process]::Start($start)
            try {
                $stdout=$process.StandardOutput.ReadToEndAsync(); $stderr=$process.StandardError.ReadToEndAsync()
                $completed=$process.WaitForExit(120000)
                if (-not $completed) { $process.Kill($true); $process.WaitForExit() }
                $watch.Stop()
                ($stdout.GetAwaiter().GetResult()+$stderr.GetAwaiter().GetResult()) | Set-Content (Join-Path $output "$name.log")
                [ordered]@{completed=$completed;exitCode=$process.ExitCode;processMilliseconds=$watch.Elapsed.TotalMilliseconds} |
                    ConvertTo-Json | Set-Content (Join-Path $output "$name-process.json")
                if (-not $completed -or $process.ExitCode -ne 0) { throw "Package comparison failed: $name" }
            } finally { $process.Dispose() }
            $result=Get-Content $report -Raw | ConvertFrom-Json
            if ($result.ApplicationOwnedComCleanup -ne $true -or $result.Students -ne 1000 -or $result.Files -ne 1000 -or
                $result.Operations -ne 50 -or -not $result.FileTreeBehaviorChecksPassed) { throw "Unexpected workload or cleanup in $name" }
            if ($sample -gt 0) { $runs.Add([pscustomobject]@{scenario=$scenario;side=$side;sample=$sample;result=$result;processMs=$watch.Elapsed.TotalMilliseconds}) }
        }
    }
}
function Median($values) { $sorted=@($values | Sort-Object); $middle=[int][Math]::Floor($sorted.Count/2); if ($sorted.Count%2) {return [double]$sorted[$middle]}; return ($sorted[$middle-1]+$sorted[$middle])/2 }
$budget=Get-Content (Join-Path $root 'tools/performance-baseline.json') -Raw | ConvertFrom-Json
$comparisons=foreach($scenario in $scenarios.Keys) {
    foreach($metric in @('StartupMilliseconds','StartupAllocatedBytes','MountLayoutMilliseconds','MountLayoutAllocatedBytes','UpdateMilliseconds','UpdateAllocatedBytes','ProcessMilliseconds')) {
        $values=@{}
        foreach($side in @('baseline','candidate')) {
            $selected=@($runs | Where-Object {$_.scenario -eq $scenario -and $_.side -eq $side})
            $values[$side]=@($selected | ForEach-Object { if($metric -eq 'ProcessMilliseconds'){$_.processMs}else{$_.result.measurements.$metric} })
            foreach($value in $values[$side]) { if($null -eq $value -or -not [double]::IsFinite([double]$value) -or $value -lt 0) {throw 'Invalid measurement.'} }
        }
        $before=Median $values.baseline; $after=Median $values.candidate
        $limit=if($metric.EndsWith('Bytes')){$budget.allocationRegressionPercent}else{$budget.timeRegressionPercent}
        [pscustomobject]@{scenario=$scenario;metric=$metric;baselineMedian=$before;candidateMedian=$after
            changePercent=100*($after/$before-1);budgetPercent=$limit;regression=$after -gt $before*(1+$limit/100)
            baselineMin=($values.baseline | Measure-Object -Minimum).Minimum;baselineMax=($values.baseline | Measure-Object -Maximum).Maximum
            candidateMin=($values.candidate | Measure-Object -Minimum).Minimum;candidateMax=($values.candidate | Measure-Object -Maximum).Maximum}
    }
}
foreach($entry in $sourceHashes) {
    if((Get-FileHash (Join-Path $output "$($entry.side)/$($entry.path)")).Hash -ne $entry.sha256) {throw 'Measured source changed.'}
}
$summary=[ordered]@{kind='consumer-package-comparison-not-public-release-gate';consumerRevision=$revision;consumerDirty=$dirty;baselineVersion=$oldVersion
    candidateVersion=$CandidateVersion;samples=$Samples;warmupPairsPerScenario=1;experimentalCleanup=$false
    sdk=(& dotnet --version).Trim();packages=@($packages | ForEach-Object { @{name=$_.Name;sha256=(Get-FileHash $_.FullName).Hash} })
    passed=@($comparisons | Where-Object regression).Count -eq 0;comparisons=@($comparisons)}
$summary | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $output 'summary.json')
$comparisons | Format-Table scenario,metric,changePercent,regression -AutoSize
if(-not $summary.passed -and -not $ReportOnly) {throw 'Consumer package performance budget exceeded.'}
