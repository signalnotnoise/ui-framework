#requires -Version 7.0
param(
    [ValidateRange(3, 15)][int]$Samples = 7,
    [Parameter(Mandatory)][string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output = [IO.Path]::GetFullPath($OutputDirectory, $root)
if (Test-Path -LiteralPath $output) { throw 'Use a fresh output directory to preserve raw evidence.' }
New-Item -ItemType Directory -Path $output | Out-Null
Push-Location $root
try {
    & dotnet --info | Set-Content (Join-Path $output 'dotnet-info.txt')
    & dotnet build tools/AnimationDiagnostics/AnimationDiagnostics.csproj -c Release --disable-build-servers -m:1 -warnaserror
    if ($LASTEXITCODE -ne 0) { throw 'Animation diagnostic build failed.' }
    $binary = Join-Path $root 'tools/AnimationDiagnostics/bin/Release/net10.0-windows/AnimationDiagnostics.dll'
    Get-ChildItem (Split-Path $binary) -File | ForEach-Object {
        [pscustomobject]@{ name = $_.Name; sha256 = (Get-FileHash $_.FullName).Hash }
    } | ConvertTo-Json | Set-Content (Join-Path $output 'binary-manifest.json')
    foreach ($folder in @('UI Framework', 'UI Framework.Wpf', 'tools/AnimationDiagnostics')) {
        Get-ChildItem (Join-Path $root $folder) -Recurse -File | Where-Object {
            $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Extension -in @('.cs', '.csproj', '.txt')
        } | ForEach-Object {
            $relative = [IO.Path]::GetRelativePath($root, $_.FullName)
            $destination = Join-Path $output "source/$relative"
            New-Item -ItemType Directory -Path (Split-Path $destination) -Force | Out-Null
            Copy-Item -LiteralPath $_.FullName -Destination $destination
        }
    }
    Copy-Item -LiteralPath $PSCommandPath -Destination (Join-Path $output 'Test-AnimationPerformance.ps1')
    foreach ($name in @('global.json', 'Directory.Build.props')) {
        Copy-Item -LiteralPath (Join-Path $root $name) -Destination (Join-Path $output "source/$name")
    }
    Get-ChildItem (Join-Path $output 'source') -Recurse -File | ForEach-Object {
        [pscustomobject]@{ path = [IO.Path]::GetRelativePath((Join-Path $output 'source'), $_.FullName); sha256 = (Get-FileHash $_.FullName).Hash }
    } | ConvertTo-Json | Set-Content (Join-Path $output 'source-manifest.json')
    $runs = @()
    for ($sample = 0; $sample -le $Samples; $sample++) {
        $order = if ($sample % 2 -eq 0) { @('disabled','enabled') } else { @('enabled','disabled') }
        foreach ($mode in $order) {
            Write-Output "rapid-retarget / $sample / $mode"
            $report = Join-Path $output "$sample-$mode.json"
            $start = [Diagnostics.ProcessStartInfo]::new((Get-Command dotnet).Source)
            $start.UseShellExecute = $false
            $start.CreateNoWindow = $true
            $start.RedirectStandardOutput = $true
            $start.RedirectStandardError = $true
            foreach ($argument in @($binary, "--$mode", '--report', $report)) { $start.ArgumentList.Add($argument) }
            $process = [Diagnostics.Process]::Start($start)
            try {
                $stdout = $process.StandardOutput.ReadToEndAsync()
                $stderr = $process.StandardError.ReadToEndAsync()
                $finished = $process.WaitForExit(60000)
                if (-not $finished) {
                    $process.Kill($true)
                    $process.WaitForExit()
                }
                ($stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult()) | Set-Content (Join-Path $output "$sample-$mode.log")
                if (-not $finished) { throw "Timed out: sample $sample / $mode" }
                if ($process.ExitCode -ne 0) { throw "Diagnostic failed: sample $sample / $mode" }
            } finally { $process.Dispose() }
            $result = Get-Content $report -Raw | ConvertFrom-Json
            if ($result.Protocol -ne 'animation-retarget-v1' -or $result.Nodes -ne 32 -or $result.Operations -ne 240 -or $result.BodyBuilds -ne 240) {
                throw "Unexpected workload: $report"
            }
            if ($sample -gt 0) { $runs += [pscustomobject]@{ sample = $sample; mode = $mode; result = $result } }
        }
    }
    $runs | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $output 'runs.json')
    Write-Output "Preserved $($runs.Count) measured runs plus two warmups in $output"
} finally { Pop-Location }
