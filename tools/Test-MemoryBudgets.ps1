#requires -Version 7.0
param([Parameter(Mandatory)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
foreach($name in 'UI_FRAMEWORK_MEMORY_CHECKPOINTS','UI_FRAMEWORK_FOCUS_TRACE','UI_FRAMEWORK_TRACE_THREAD_EXIT') {
    if([Environment]::GetEnvironmentVariable($name)){throw "Remove diagnostic override $name before measuring budgets."}
}
$output=[IO.Path]::GetFullPath($OutputDirectory,$root)
if(Test-Path $output){throw 'Use a new output directory to preserve evidence.'}
New-Item -ItemType Directory $output | Out-Null
Copy-Item (Join-Path $root 'tools/memory-budgets.json') (Join-Path $output 'frozen-policy.json')
Push-Location $root
try {
    foreach($rows in 500,5000) {
        & ./tools/Test-NativeLifetime.ps1 -OutputDirectory (Join-Path $output "native-$rows") -Rows $rows -Batches 180 -Repetitions 3 -CaseTimeoutSeconds 900
    }
    & ./tools/Test-WebViewAcceptance.ps1 -OutputDirectory (Join-Path $output 'browser') -Modes @('retention-fresh','retention-reused') -RetentionCycles 132 -Repetitions 3 -CaseTimeoutSeconds 900
    python tools/Test-MemoryBudgets.py $output --policy (Join-Path $output 'frozen-policy.json') --output (Join-Path $output 'budget-results.json')
    if($LASTEXITCODE -ne 0){throw 'Memory acceptance failed; inspect preserved budget-results.json.'}
} finally { Pop-Location }
