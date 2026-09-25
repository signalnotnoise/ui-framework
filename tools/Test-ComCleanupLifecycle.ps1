# Diagnostic integration checks only; requires VS C++ build tools for the counted COM fixture.
param([string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
if (-not $OutputDirectory) { $OutputDirectory = 'artifacts/com-lifecycle-' + [Guid]::NewGuid().ToString('N') }
$output = [IO.Path]::GetFullPath($OutputDirectory, $root)
if (Test-Path $output) { throw 'Use a new output directory to preserve evidence.' }
New-Item -ItemType Directory -Path $output | Out-Null
$project = Join-Path $root 'samples/ComCleanupLifecycle/ComCleanupLifecycle.csproj'
& dotnet build $project -c Release --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw 'Lifecycle probe build failed.' }
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
$vs = (& $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath)
if (-not $vs) { throw 'Visual Studio C++ build tools are required.' }
$vcvars = Join-Path $vs 'VC/Auxiliary/Build/vcvars64.bat'
$compilerEnvironment = & $env:ComSpec /d /s /c ('"{0}" >nul && set' -f $vcvars)
if ($LASTEXITCODE -ne 0) { throw 'C++ environment setup failed.' }
$compiler = Get-ChildItem (Join-Path $vs 'VC/Tools/MSVC/*/bin/Hostx64/x64/cl.exe') | Sort-Object FullName -Descending | Select-Object -First 1
$binaryDirectory = Join-Path (Split-Path $project) 'bin/Release/net10.0-windows'
$start = [Diagnostics.ProcessStartInfo]::new($compiler.FullName)
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
foreach ($line in $compilerEnvironment) {
    if ($line -match '^([^=]+)=(.*)$') { $start.Environment[$matches[1]] = $matches[2] }
}
foreach ($argument in @('/nologo', '/EHsc', '/LD', (Join-Path (Split-Path $project) 'ComLifetimeProbe.cpp'),
    "/Fo:$output/ComLifetimeProbe.obj", "/Fe:$binaryDirectory/ComLifetimeProbe.dll", '/link', 'uuid.lib')) {
    $start.ArgumentList.Add($argument)
}
$compilerProcess = [Diagnostics.Process]::Start($start)
try { $compilerProcess.WaitForExit(); if ($compilerProcess.ExitCode -ne 0) { throw 'Native COM fixture build failed.' } }
finally { $compilerProcess.Dispose() }
$start = [Diagnostics.ProcessStartInfo]::new((Join-Path $binaryDirectory 'ComCleanupLifecycle.exe'))
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
$start.RedirectStandardOutput = $true
$start.RedirectStandardError = $true
$start.ArgumentList.Add((Join-Path $output 'result.json'))
$process = [Diagnostics.Process]::Start($start)
try {
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    $completed = $process.WaitForExit(60000)
    if (-not $completed) { $process.Kill($true); $process.WaitForExit() }
    ($stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult()) | Tee-Object (Join-Path $output 'run.log')
    if (-not $completed -or $process.ExitCode -ne 0) { throw "Lifecycle check failed; see $output" }
} finally {
    if (-not $process.HasExited) { $process.Kill($true); $process.WaitForExit() }
    $process.Dispose()
}
