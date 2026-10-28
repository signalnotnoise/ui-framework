$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$dotnet=(Resolve-Path (Join-Path $root '../release-alpha4/dotnet/dotnet.exe')).Path
foreach($scenario in @('full-list','themed-full-list','layout-editors')) {
  for($i=0;$i -le 3;$i++) {
    $order=if($i % 2 -eq 0){@('live','frozen')}else{@('frozen','live')}
    foreach($side in $order) {
      Write-Output "$scenario / $i / $side"
      $dll=Join-Path $root "$side/samples/Counter/bin/Release/net10.0-windows/Counter.dll"
      $report=Join-Path $root "$scenario-$i-$side.json"
      $arguments=@($dll,'--compare','--report',$report)
      if($scenario -eq 'themed-full-list'){$arguments+='--themed'}
      if($scenario -eq 'layout-editors'){$arguments+='--layout-editors'}
      & $dotnet @arguments > (Join-Path $root "$scenario-$i-$side.log") 2>&1
      if($LASTEXITCODE -ne 0){throw "Failed: $scenario $side"}
    }
  }
}
