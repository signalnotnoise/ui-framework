$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$dotnet=(Resolve-Path (Join-Path $root '../release-alpha4/dotnet/dotnet.exe')).Path
for($i=0;$i -le 3;$i++) {
 $order=if($i % 2 -eq 0){@('accepted','current','trimmed')}else{@('trimmed','current','accepted')}
 foreach($side in $order) {
  Write-Output "$i / $side"
  & $dotnet (Join-Path $root "$side/samples/Counter/bin/Release/net10.0-windows/Counter.dll") --compare --layout-editors --report (Join-Path $root "$i-$side.json") > (Join-Path $root "$i-$side.log") 2>&1
  if($LASTEXITCODE -ne 0){throw "$i / $side failed"}
 }
}
