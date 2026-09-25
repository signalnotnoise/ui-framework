#requires -Version 7.0
param([Parameter(Mandatory)][string]$ConsumerProject,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [ValidateRange(3,15)][int]$Samples = 3, [switch]$CompareFileVirtualization)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$consumer = [IO.Path]::GetFullPath($ConsumerProject)
$consumerRoot = Split-Path (Split-Path $consumer)
$output = [IO.Path]::GetFullPath($OutputDirectory, $projectRoot)
if (Test-Path $output) { throw 'Use a new output directory.' }
New-Item -ItemType Directory -Path $output | Out-Null
Push-Location $projectRoot
try {
    $sourceManifest = foreach ($folder in @((Split-Path $consumer), (Join-Path $consumerRoot 'Lab Feedback Runner'),
        (Join-Path $projectRoot 'samples/ConsumerDiagnostics'))) {
        Get-ChildItem -LiteralPath $folder -Recurse -File | Where-Object {
            $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Extension -in @('.cs','.xaml','.csproj','.props','.targets','.xshd','.config')
        } | ForEach-Object {
            $relative = [IO.Path]::GetRelativePath((Split-Path $folder), $_.FullName)
            $destination = Join-Path $output "source/$relative"
            New-Item -ItemType Directory -Path (Split-Path $destination) -Force | Out-Null
            Copy-Item -LiteralPath $_.FullName -Destination $destination
            [pscustomobject]@{ path=$_.FullName; snapshot=$relative; sha256=(Get-FileHash $_.FullName).Hash }
        }
    }
    Copy-Item samples/WorkspaceComparison/LayoutProbe.cs (Join-Path $output 'source/ConsumerDiagnostics/LayoutProbe.cs')
    Copy-Item tools/Test-ConsumerLayout.ps1 (Join-Path $output 'source/Test-ConsumerLayout.ps1')
    $sourceManifest | ConvertTo-Json | Set-Content (Join-Path $output 'source-manifest.json')
    dotnet build samples/ConsumerDiagnostics/ConsumerDiagnostics.csproj -c Release "-p:ConsumerProject=$consumer" --configfile (Join-Path $consumerRoot 'NuGet.Config') --disable-build-servers -m:1
    if ($LASTEXITCODE -ne 0) { throw 'Consumer diagnostic build failed.' }
    $executable = Join-Path $projectRoot 'samples/ConsumerDiagnostics/bin/Release/net10.0-windows/ConsumerDiagnostics.dll'
    $runs = [Collections.Generic.List[object]]::new()
    $scenarios = if ($CompareFileVirtualization) { @('full-tree','virtualized-tree') } else { @('window','splitter') }
    foreach ($mode in @('clean','probes')) {
        for ($sample=0; $sample -le $Samples; $sample++) {
            $order = if ($sample % 2) { @($scenarios[1],$scenarios[0]) } else { $scenarios }
            foreach ($scenario in $order) {
                $name = "$mode-$sample-$scenario"
                Write-Output $name
                $report = Join-Path $output "$name.json"
                $start = [Diagnostics.ProcessStartInfo]::new((Get-Command dotnet).Source)
                $start.UseShellExecute=$false; $start.CreateNoWindow=$true
                $start.RedirectStandardOutput=$true; $start.RedirectStandardError=$true
                $arguments = @($executable,'--report',$report)
                if ($mode -eq 'probes') { $arguments += '--probe-layout' }
                if ($scenario -eq 'splitter' -or $CompareFileVirtualization) { $arguments += '--resize-navigation' }
                if ($scenario -eq 'virtualized-tree') { $arguments += '--virtualize-files' }
                if ($scenario -eq 'full-tree') { $arguments += '--full-files' }
                foreach ($argument in $arguments) { $start.ArgumentList.Add($argument) }
                $process = [Diagnostics.Process]::Start($start)
                try {
                    $stdout=$process.StandardOutput.ReadToEndAsync(); $stderr=$process.StandardError.ReadToEndAsync()
                    $finished=$process.WaitForExit(120000)
                    if (-not $finished) { $process.Kill($true); $process.WaitForExit() }
                    ($stdout.GetAwaiter().GetResult()+$stderr.GetAwaiter().GetResult()) | Set-Content (Join-Path $output "$name.log")
                    if (-not $finished -or $process.ExitCode -ne 0) { throw "Diagnostic failed: $name" }
                } finally { $process.Dispose() }
                $result = Get-Content $report -Raw | ConvertFrom-Json
                if ($result.Students -ne 1000 -or $result.Files -ne 1000 -or $result.Operations -ne 50) { throw 'Unexpected consumer workload.' }
                if ($sample -gt 0) { $runs.Add([pscustomobject]@{mode=$mode; scenario=$scenario; result=$result}) }
            }
        }
    }
    function Median($values) { $sorted=@($values | Sort-Object); $middle=[int][Math]::Floor($sorted.Count/2); if ($sorted.Count%2) { return $sorted[$middle] }; return ($sorted[$middle-1]+$sorted[$middle])/2 }
    $metrics = foreach ($mode in @('clean','probes')) { foreach ($scenario in $scenarios) {
        $selected = @($runs | Where-Object { $_.mode -eq $mode -and $_.scenario -eq $scenario })
        [pscustomobject]@{ mode=$mode; scenario=$scenario; samples=$Samples
            startupMs=Median $selected.result.measurements.StartupMilliseconds
            startupBytes=Median $selected.result.measurements.StartupAllocatedBytes
            updateMs=Median $selected.result.measurements.UpdateMilliseconds
            updateMin=($selected.result.measurements.UpdateMilliseconds | Measure-Object -Minimum).Minimum
            updateMax=($selected.result.measurements.UpdateMilliseconds | Measure-Object -Maximum).Maximum
            updateBytes=Median $selected.result.measurements.UpdateAllocatedBytes
            dispatcherMs=Median $selected.result.measurements.DispatcherMilliseconds
            explicitLayoutMs=Median $selected.result.measurements.ExplicitLayoutMilliseconds
            realizedStudents=Median $selected.result.RealizedStudents; realizedFiles=Median $selected.result.RealizedFiles
            fileMeasureMs=Median $selected.result.measurements.Files.MeasureMilliseconds
            fileArrangeMs=Median $selected.result.measurements.Files.ArrangeMilliseconds
            editorMeasureMs=Median $selected.result.measurements.Editor.MeasureMilliseconds
            editorArrangeMs=Median $selected.result.measurements.Editor.ArrangeMilliseconds
            rootMeasureMs=Median $selected.result.measurements.Root.MeasureMilliseconds
            rootArrangeMs=Median $selected.result.measurements.Root.ArrangeMilliseconds }
    } }
    $changed = @($sourceManifest | Where-Object { (Get-FileHash -LiteralPath $_.path).Hash -ne $_.sha256 })
    if ($changed.Count) { throw 'Consumer source changed during measurement; results require investigation.' }
    [ordered]@{ kind='consumer-synthetic-data-diagnostic-not-release-gate'; samples=$Samples
        sdk=(& dotnet --version).Trim(); consumerRevision=(& git -C $consumerRoot rev-parse HEAD).Trim()
        consumerDirty=[bool](& git -C $consumerRoot status --porcelain)
        frameworkPackage=([xml](Get-Content -LiteralPath $consumer -Raw)).SelectSingleNode("/Project/ItemGroup/PackageReference[@Include='SignalNotNoise.UI.Wpf']").Version
        metrics=@($metrics) } | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $output 'summary.json')
    $metrics | Format-Table mode,scenario,startupMs,updateMs,updateBytes,realizedStudents,realizedFiles -AutoSize
} finally { Pop-Location }
