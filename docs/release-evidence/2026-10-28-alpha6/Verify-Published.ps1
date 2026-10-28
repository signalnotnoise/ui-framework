$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$output = Join-Path $PSScriptRoot ('public-consumer-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $output | Out-Null
$source = Join-Path $projectRoot 'artifacts/package-validation/8eb62d9be10441a4a149066337c06318'
Copy-Item -LiteralPath (Join-Path $source 'PackageSmoke.csproj'), (Join-Path $source 'Program.cs') -Destination $output
$consumerProject = Join-Path $output 'PackageSmoke.csproj'
(Get-Content $consumerProject -Raw).Replace('0.1.0-alpha.6-local.1', '0.1.0-alpha.6') | Set-Content $consumerProject
'<configuration><packageSources><clear /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources></configuration>' | Set-Content -LiteralPath (Join-Path $output 'NuGet.Config')
$env:DOTNET_ROOT = Join-Path $projectRoot 'artifacts/release-alpha4/dotnet'
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
$env:NUGET_HTTP_CACHE_PATH = Join-Path $output 'http-cache'
Push-Location $projectRoot
try {
    dotnet restore (Join-Path $output 'PackageSmoke.csproj') --configfile (Join-Path $output 'NuGet.Config') --packages (Join-Path $output 'packages') --no-http-cache --disable-build-servers
    if ($LASTEXITCODE -ne 0) { throw 'Published package restore failed.' }
    dotnet run --project (Join-Path $output 'PackageSmoke.csproj') -c Release --no-restore --disable-build-servers
    if ($LASTEXITCODE -ne 0) { throw 'Published package smoke check failed.' }
    $manifest = foreach ($id in @('signalnotnoise.ui', 'signalnotnoise.ui.wpf')) {
        $package = Join-Path $output "packages/$id/0.1.0-alpha.6"
        $dll = Get-ChildItem -LiteralPath (Join-Path $package 'lib') -Recurse -Filter '*.dll' | Select-Object -First 1
        $nupkg = Join-Path $package "$id.0.1.0-alpha.6.nupkg"
        $hostedCandidates = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'hosted-packages') -Filter '*.nupkg' |
            Where-Object { $_.Name -ieq "$id.0.1.0-alpha.6.nupkg" })
        if ($hostedCandidates.Count -ne 1) { throw "Unexpected hosted package count for $id" }
        $hostedPackage = $hostedCandidates[0]
        $archive = [IO.Compression.ZipFile]::OpenRead($hostedPackage.FullName)
        try {
            $entry = @($archive.Entries | Where-Object { $_.FullName.StartsWith('lib/') -and $_.Name -ceq $dll.Name })
            if ($entry.Count -ne 1) { throw "Unexpected hosted package assembly count for $id" }
            $stream = $entry[0].Open()
            try { $hostedAssemblySha256 = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream)) }
            finally { $stream.Dispose() }
        } finally { $archive.Dispose() }
        $assemblySha256 = (Get-FileHash -LiteralPath $dll.FullName -Algorithm SHA256).Hash
        if ($assemblySha256 -cne $hostedAssemblySha256) { throw "Published assembly differs from hosted artifact: $id" }
        [pscustomobject]@{
            package = $id; version = '0.1.0-alpha.6'; source = 'https://api.nuget.org/v3/index.json'
            assembly = $dll.Name; assemblySha256 = $assemblySha256; hostedAssemblyMatches = $true
            hostedPackageSha256 = (Get-FileHash -LiteralPath $hostedPackage.FullName -Algorithm SHA256).Hash
            packageSha256 = (Get-FileHash -LiteralPath $nupkg -Algorithm SHA256).Hash
        }
    }
    [pscustomobject]@{ checkedAtUtc = [DateTime]::UtcNow.ToString('o'); sdk = (& dotnet --version).Trim(); isolatedConsumer = $output; packages = @($manifest); passed = $true } |
        ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'public-installation.json')
    Write-Output "Fresh public package installation passed: $output"
} finally { Pop-Location }
