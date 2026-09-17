$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$version = ([xml](Get-Content (Join-Path $projectRoot 'Directory.Build.props') -Raw)).Project.PropertyGroup.Version
if ($version -notmatch '^\d+\.\d+\.\d+(?:-[A-Za-z0-9.]+)?$') { throw 'Invalid release version.' }
$rootFiles = @('README.md', 'LICENSE', 'CHANGELOG.md', 'CONTRIBUTING.md', 'AGENTS.md', '.gitignore', '.editorconfig', 'global.json', 'Directory.Build.props', 'UI Framework.slnx')
$folders = @('UI Framework', 'UI Framework.Wpf', 'samples', 'tests', 'docs', 'tools', '.github')
$files = @($rootFiles | ForEach-Object {
    $path = Join-Path $projectRoot $_
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Required release file missing: $_" }
    Get-Item -LiteralPath $path
})
foreach ($folder in $folders) {
    $files += Get-ChildItem -LiteralPath (Join-Path $projectRoot $folder) -Recurse -File | Where-Object {
        $_.FullName -notmatch '[\\/](bin|obj|TestResults|\.vs|artifacts)[\\/]' -and
        $_.Extension -in @('.cs', '.csproj', '.md', '.json', '.ps1', '.yml', '.yaml') -and
        $_.Name -ne 'Class1.cs'
    }
}
$artifactDirectory = Join-Path $projectRoot 'artifacts'
[IO.Directory]::CreateDirectory($artifactDirectory) | Out-Null
$archivePath = Join-Path $artifactDirectory "ui-framework-$version-source.zip"
if (Test-Path -LiteralPath $archivePath) { throw "Archive already exists: $archivePath. Move it before making another candidate." }
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::Open($archivePath, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in ($files | Sort-Object FullName -Unique)) {
        $relative = $file.FullName.Substring($projectRoot.Length + 1).Replace('\', '/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, "ui-framework-$version/$relative", [IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
} finally { $archive.Dispose() }
$check = [IO.Compression.ZipFile]::OpenRead($archivePath)
try {
    if ($check.Entries.FullName -match '/(bin|obj|TestResults|\.vs|artifacts)/') { throw 'Archive contains excluded directories.' }
    Write-Output "Created source archive with $($check.Entries.Count) files: $archivePath"
} finally { $check.Dispose() }
Get-FileHash -LiteralPath $archivePath -Algorithm SHA256
