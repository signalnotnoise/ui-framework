$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$output = Join-Path $PSScriptRoot 'identical-binary'
if (Test-Path $output) { throw 'Retain previous control evidence; choose a new directory.' }
New-Item -ItemType Directory -Path $output | Out-Null
$live = Join-Path $root 'samples/Counter/bin/Release/net10.0-windows'
$baseline = Join-Path $root 'artifacts/performance/compact-buttons-direct-2026-10-23/baseline-source/samples/Counter/bin/Release/net10.0-windows'
$backup = Join-Path $output 'candidate-backup'
New-Item -ItemType Directory -Path $backup | Out-Null
$names = @('Counter.dll', 'UI Framework.dll', 'UI Framework.Wpf.dll')
foreach ($name in $names) { Copy-Item -LiteralPath (Join-Path $live $name) -Destination $backup }
$env:DOTNET_ROOT = Join-Path $root 'artifacts/release-alpha4/dotnet'
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
try {
    foreach ($name in $names) { Copy-Item -LiteralPath (Join-Path $baseline $name) -Destination (Join-Path $live $name) }
    $hashes = foreach ($name in $names) {
        $left = (Get-FileHash -LiteralPath (Join-Path $baseline $name) -Algorithm SHA256).Hash
        $right = (Get-FileHash -LiteralPath (Join-Path $live $name) -Algorithm SHA256).Hash
        if ($left -cne $right) { throw "Nonidentical control assembly: $name" }
        [pscustomobject]@{ assembly = $name; baselineSha256 = $left; candidateSha256 = $right }
    }
    $hashes | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'assemblies.json')
    $sides = @{ baseline = $baseline; candidate = $live }
    for ($sample = 0; $sample -le 7; $sample++) {
        $order = if ($sample % 2 -eq 0) { @('baseline','candidate') } else { @('candidate','baseline') }
        foreach ($side in $order) {
            Write-Output "Identical binary control / sample $sample / $side"
            $report = Join-Path $output "$sample-$side.json"
            $start = [Diagnostics.ProcessStartInfo]::new((Get-Command dotnet).Source)
            $start.UseShellExecute = $false
            $start.CreateNoWindow = $true
            $start.RedirectStandardOutput = $true
            $start.RedirectStandardError = $true
            foreach ($arg in @((Join-Path $sides[$side] 'Counter.dll'),'--compare','--report',$report)) { $start.ArgumentList.Add($arg) }
            $process = [Diagnostics.Process]::Start($start)
            try {
                $stdout = $process.StandardOutput.ReadToEndAsync()
                $stderr = $process.StandardError.ReadToEndAsync()
                if (-not $process.WaitForExit(120000)) { $process.Kill($true); $process.WaitForExit(); throw 'Control benchmark timed out.' }
                ($stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult()) | Set-Content -LiteralPath (Join-Path $output "$sample-$side.log")
                if ($process.ExitCode -ne 0) { throw 'Control benchmark failed.' }
            } finally { $process.Dispose() }
        }
    }
} finally {
    foreach ($name in $names) {
        Copy-Item -LiteralPath (Join-Path $backup $name) -Destination (Join-Path $live $name)
        if ((Get-FileHash -LiteralPath (Join-Path $backup $name)).Hash -cne (Get-FileHash -LiteralPath (Join-Path $live $name)).Hash) {
            throw "Candidate binary restoration failed: $name"
        }
    }
}
