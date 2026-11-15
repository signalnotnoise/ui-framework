#requires -Version 7.0
param(
    [Parameter(Mandatory)][string]$OutputDirectory,
    [string]$BaselineVersion='0.1.0-alpha.6',
    [string]$CandidateVersion='0.1.0-alpha.7',
    [string]$CandidatePackageDirectory,
    [ValidateRange(3,15)][int]$Repetitions=3
)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output=[IO.Path]::GetFullPath($OutputDirectory,$root)
if(Test-Path $output){throw 'Use a fresh evidence directory.'}
if($BaselineVersion -eq $CandidateVersion){throw 'Upgrade versions must differ.'}
foreach($version in $BaselineVersion,$CandidateVersion){if($version -notmatch '^\d+\.\d+\.\d+(-[\w.-]+)?$'){throw 'Invalid version.'}}
New-Item -ItemType Directory $output | Out-Null
$scratch=Join-Path ([IO.Path]::GetTempPath()) ('fw019-adoption-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $scratch | Out-Null
$oldPackages=$env:NUGET_PACKAGES; $oldHttp=$env:NUGET_HTTP_CACHE_PATH
$localFeed=if($CandidatePackageDirectory){(Resolve-Path -LiteralPath $CandidatePackageDirectory).Path}else{$null}
if($localFeed){
    foreach($id in 'SignalNotNoise.UI','SignalNotNoise.UI.Wpf') {
        if(-not (Test-Path (Join-Path $localFeed "$id.$CandidateVersion.nupkg"))){throw "Missing local candidate: $id"}
    }
}
function Run-Case([string]$dll,[string]$name,[string]$mode) {
    Write-Output "Workspace acceptance: $name"
    $start=[Diagnostics.ProcessStartInfo]::new('dotnet')
    $start.UseShellExecute=$false; $start.CreateNoWindow=$true
    $start.RedirectStandardOutput=$true; $start.RedirectStandardError=$true
    foreach($argument in @($dll,'--check',(Join-Path $output $name))){$start.ArgumentList.Add($argument)}
    if($mode -eq 'full-list'){$start.ArgumentList.Add('--full-list')}
    $process=[Diagnostics.Process]::Start($start)
    try {
        $stdout=$process.StandardOutput.ReadToEndAsync(); $stderr=$process.StandardError.ReadToEndAsync()
        if(-not $process.WaitForExit(180000)){$process.Kill($true);$process.WaitForExit();throw "Case timed out: $name"}
        ($stdout.GetAwaiter().GetResult()+$stderr.GetAwaiter().GetResult()) | Set-Content (Join-Path $output "$name.log")
        if($process.ExitCode -ne 0){throw "Case failed: $name"}
    } finally {$process.Dispose()}
}
try {
    $sdk=(& dotnet --version).Trim()
    $published=Invoke-RestMethod 'https://api.nuget.org/v3-flatcontainer/signalnotnoise.ui.wpf/index.json'
    $published | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $output 'published-versions.json')
    foreach($version in $BaselineVersion,$CandidateVersion){
        if($version -eq $CandidateVersion -and $localFeed){continue}
        if($published.versions -notcontains $version){throw "Not published: $version"}
    }
    $manifests=@{}
    foreach($side in 'baseline','candidate') {
        $version=if($side -eq 'baseline'){$BaselineVersion}else{$CandidateVersion}
        $consumer=Join-Path $scratch $side
        New-Item -ItemType Directory $consumer | Out-Null
        Copy-Item (Join-Path $root 'samples/PackageWorkspace/*') $consumer
        if($side -eq 'candidate' -and $localFeed){
            $escaped=[Security.SecurityElement]::Escape($localFeed)
            "<configuration><packageSources><clear /><add key=`"candidate`" value=`"$escaped`" /><add key=`"nuget.org`" value=`"https://api.nuget.org/v3/index.json`" /></packageSources></configuration>" | Set-Content (Join-Path $consumer 'NuGet.Config')
        }
        $env:NUGET_PACKAGES=Join-Path $scratch "$side-packages"
        $env:NUGET_HTTP_CACHE_PATH=Join-Path $scratch "$side-http"
        & dotnet restore (Join-Path $consumer 'PackageWorkspace.csproj') --force-evaluate --no-http-cache --configfile (Join-Path $consumer 'NuGet.Config') "-p:FrameworkPackageVersion=$version" *> (Join-Path $output "$side-restore.log")
        if($LASTEXITCODE -ne 0){throw "$side restore failed"}
        & dotnet build (Join-Path $consumer 'PackageWorkspace.csproj') -c Release --no-restore -warnaserror "-p:FrameworkPackageVersion=$version" *> (Join-Path $output "$side-build.log")
        if($LASTEXITCODE -ne 0){throw "$side build failed"}
        $assets=Get-Content (Join-Path $consumer 'obj/project.assets.json') -Raw | ConvertFrom-Json
        if(@($assets.libraries.PSObject.Properties | Where-Object {$_.Value.type -eq 'project'}).Count){throw 'Source project dependency detected.'}
        foreach($id in 'SignalNotNoise.UI','SignalNotNoise.UI.Wpf') {
            if(-not $assets.libraries.PSObject.Properties["$id/$version"]){throw "Unexpected resolved package: $id"}
        }
        Copy-Item (Join-Path $consumer 'obj/project.assets.json') (Join-Path $output "$side-assets.json")
        Copy-Item (Join-Path $consumer 'packages.lock.json') (Join-Path $output "$side-packages.lock.json")
        if($side -eq 'candidate' -and $localFeed){
            $packageHashes=foreach($id in 'SignalNotNoise.UI','SignalNotNoise.UI.Wpf'){
                $source=Join-Path $localFeed "$id.$CandidateVersion.nupkg"
                $lowerId=$id.ToLowerInvariant();$lowerVersion=$CandidateVersion.ToLowerInvariant()
                $installed=Join-Path $env:NUGET_PACKAGES "$lowerId/$lowerVersion/$lowerId.$lowerVersion.nupkg"
                $sourceHash=(Get-FileHash $source).Hash
                if((Get-FileHash $installed).Hash -ne $sourceHash){throw "Installed candidate differs from local package: $id"}
                @{id=$id;version=$CandidateVersion;sha256=$sourceHash;installedMatches=$true}
            }
            $packageHashes | ConvertTo-Json | Set-Content (Join-Path $output 'local-candidate-packages.json')
        }
        $manifests[$side]=@(Get-ChildItem (Join-Path $consumer 'bin/Release/net10.0-windows') -File | ForEach-Object { @{name=$_.Name;sha256=(Get-FileHash $_.FullName).Hash} })
    }
    $binary=Join-Path $scratch 'binary-upgrade'
    New-Item -ItemType Directory $binary | Out-Null
    Copy-Item (Join-Path $scratch 'baseline/bin/Release/net10.0-windows/*') $binary
    foreach($dll in 'UI Framework.dll','UI Framework.Wpf.dll') {
        Copy-Item (Join-Path $scratch "candidate/bin/Release/net10.0-windows/$dll") (Join-Path $binary $dll)
    }
    if((Get-FileHash (Join-Path $binary 'PackageWorkspace.dll')).Hash -ne (Get-FileHash (Join-Path $scratch 'baseline/bin/Release/net10.0-windows/PackageWorkspace.dll')).Hash){throw 'Binary consumer was rebuilt.'}
    $manifests['binary-upgrade']=@(Get-ChildItem $binary -File | ForEach-Object { @{name=$_.Name;sha256=(Get-FileHash $_.FullName).Hash} })
    $manifests | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $output 'assembly-manifests.json')
    foreach($mode in 'full-list','virtualized') {
        for($rep=0;$rep -lt $Repetitions;$rep++) {
            $order=if($rep%2 -eq 0){@('baseline','candidate')}else{@('candidate','baseline')}
            foreach($side in $order) {
                $dll=Join-Path $scratch "$side/bin/Release/net10.0-windows/PackageWorkspace.dll"
                Run-Case $dll "$mode-$rep-$side" $mode
            }
        }
        Run-Case (Join-Path $binary 'PackageWorkspace.dll') "$mode-binary-upgrade" $mode
    }
    @{sdk=$sdk;baseline=$BaselineVersion;candidate=$CandidateVersion;candidateFeed=$localFeed;repetitions=$Repetitions;scratch=$scratch;releaseGate=$false;binaryConsumerUnchanged=$true} | ConvertTo-Json | Set-Content (Join-Path $output 'campaign.json')
    python (Join-Path $root 'tools/Summarize-WorkspaceAdoption.py') $output
    if($LASTEXITCODE -ne 0){throw 'Workspace provenance/acceptance summary failed.'}
} finally { $env:NUGET_PACKAGES=$oldPackages; $env:NUGET_HTTP_CACHE_PATH=$oldHttp }
