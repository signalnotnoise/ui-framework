# Native-WPF subtraction experiments only. Never use these as release comparisons.
param(
    [ValidateSet('normal', 'no-caret', 'fixed-options', 'ime-disabled', 'fixed-width')]
    [string[]]$Modes = @('normal', 'no-caret', 'fixed-options', 'ime-disabled'),
    [ValidateRange(1, 5)][int]$Repetitions = 2,
    [Parameter(Mandatory)][string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$output = [IO.Path]::GetFullPath($OutputDirectory, $root)
if (Test-Path $output) { throw 'Use a new output directory to retain prior evidence.' }
New-Item -ItemType Directory -Path $output | Out-Null
$project = Join-Path $root 'samples/EditorWaitDiagnostics/EditorWaitDiagnostics.csproj'
& dotnet build $project -c Release --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw 'Diagnostic build failed.' }
Copy-Item (Join-Path (Split-Path $project) 'Program.cs') (Join-Path $output 'Program.cs')
$executable = Join-Path (Split-Path $project) 'bin/Release/net10.0-windows/EditorWaitDiagnostics.exe'
for ($iteration = 1; $iteration -le $Repetitions; $iteration++) {
    $order = @($Modes)
    if ($iteration % 2 -eq 0) { [Array]::Reverse($order) }
    foreach ($mode in $order) {
        Write-Output "Diagnostic $iteration / $mode"
        $start = [Diagnostics.ProcessStartInfo]::new($executable)
        $start.UseShellExecute = $false
        $start.CreateNoWindow = $true
        $start.RedirectStandardOutput = $true
        $start.RedirectStandardError = $true
        $start.ArgumentList.Add($mode)
        $start.ArgumentList.Add((Join-Path $output "$iteration-$mode.json"))
        $process = [Diagnostics.Process]::Start($start)
        try {
            $stdout = $process.StandardOutput.ReadToEndAsync()
            $stderr = $process.StandardError.ReadToEndAsync()
            $completed = $process.WaitForExit(120000)
            if (-not $completed) { $process.Kill($true); $process.WaitForExit() }
            ($stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult()) | Set-Content (Join-Path $output "$iteration-$mode.log")
            [ordered]@{ mode = $mode; iteration = $iteration; completed = $completed; exitCode = $process.ExitCode } |
                ConvertTo-Json | Set-Content (Join-Path $output "$iteration-$mode-process.json")
        } finally {
            if (-not $process.HasExited) { $process.Kill($true); $process.WaitForExit() }
            $process.Dispose()
        }
    }
}
Write-Output 'Diagnostic campaign finished; inspect process metadata for timeouts. This is not a release gate.'
