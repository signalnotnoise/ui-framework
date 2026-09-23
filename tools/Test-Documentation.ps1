$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$version = [string]([xml](Get-Content (Join-Path $projectRoot 'Directory.Build.props') -Raw)).Project.PropertyGroup.Version
$readme = Get-Content (Join-Path $projectRoot 'README.md') -Raw
if ($readme -notmatch ('\*\*Experimental · ' + [regex]::Escape($version) + ' preview')) { throw 'README version differs from package version.' }
$changelog = Get-Content (Join-Path $projectRoot 'CHANGELOG.md') -Raw
$release = [regex]::Match($changelog, '(?m)^## (\d+\.\d+\.\d+[^\s]*)')
if ($release.Groups[1].Value -cne $version) { throw 'Latest changelog release differs from package version.' }
$graph = Get-Content (Join-Path $projectRoot 'docs/knowledge-graph.json') -Raw | ConvertFrom-Json
if ($graph.releaseVersion -cne $version) { throw 'Knowledge graph release version differs from package version.' }
foreach ($workflow in Get-ChildItem (Join-Path $projectRoot '.github/workflows') -Filter '*.yml') {
    foreach ($match in [regex]::Matches((Get-Content $workflow.FullName -Raw), '(?m)^\s*-?\s*uses:\s+([^\s#]+)')) {
        $action = $match.Groups[1].Value
        if ($action -notmatch '^\./' -and $action -notmatch '^[\w.-]+/[\w./-]+@[a-f0-9]{40}$') {
            throw "Workflow action is not pinned to a commit: $action"
        }
    }
}
Write-Output 'Documentation versions and workflow action pins are consistent.'
