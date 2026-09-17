param([switch]$Check)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$graph = Get-Content -LiteralPath (Join-Path $projectRoot 'docs/knowledge-graph.json') -Raw | ConvertFrom-Json
if ($graph.schemaVersion -ne 1) { throw 'Unsupported graph schema version.' }
$nodesById = @{}
foreach ($node in $graph.nodes) {
    if ($node.id -cnotmatch '^[a-z][a-z0-9_]*$' -or $nodesById.ContainsKey($node.id)) { throw "Invalid or duplicate node: $($node.id)" }
    if ($node.status -notin @('implemented', 'partial', 'planned')) { throw "Invalid status: $($node.id)" }
    if (-not $node.label -or -not $node.summary -or @($node.sources).Count -eq 0) { throw "Incomplete node: $($node.id)" }
    foreach ($source in $node.sources) {
        $absolute = [IO.Path]::GetFullPath((Join-Path $projectRoot $source))
        if (-not $absolute.StartsWith($projectRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or -not (Test-Path -LiteralPath $absolute -PathType Leaf)) { throw "Missing or unsafe source: $source" }
    }
    $nodesById[$node.id] = $node
}
$edgeKeys = @{}
foreach ($edge in $graph.edges) {
    if (-not $nodesById.ContainsKey($edge.from) -or -not $nodesById.ContainsKey($edge.to)) { throw 'Edge references an unknown node.' }
    if ($edge.relation -cnotmatch '^[a-z][a-z -]*$') { throw 'Invalid relation.' }
    $edgeKey = "$($edge.from)|$($edge.relation)|$($edge.to)"
    if ($edgeKeys.ContainsKey($edgeKey)) { throw "Duplicate edge: $edgeKey" }
    $edgeKeys[$edgeKey] = $true
}

function Escape-Label([string]$text) { $text.Replace('&', '&amp;').Replace('"', '&quot;').Replace('<', '&lt;').Replace('>', '&gt;') }
function Escape-Cell([string]$text) { $text.Replace('|', '&#124;').Replace("`r", '').Replace("`n", ' ') }
$lines = [Collections.Generic.List[string]]::new()
$lines.Add('# UI Framework knowledge graph')
$lines.Add('')
$lines.Add('Generated from knowledge-graph.json. Edit that file and run tools/Update-KnowledgeGraph.ps1. This is a maintained architecture graph; planned nodes describe future work, not implemented APIs.')
$lines.Add('')
$lines.Add('Green: implemented. Amber: partial. Gray: planned.')
$lines.Add('')
$lines.Add('```mermaid')
$lines.Add('flowchart LR')
foreach ($status in @('implemented', 'partial', 'planned')) {
    $lines.Add("  subgraph $status[$status]")
    foreach ($node in $graph.nodes | Where-Object status -eq $status) {
        $lines.Add(('    {0}["{1}"]' -f $node.id, (Escape-Label $node.label)))
    }
    $lines.Add('  end')
}
foreach ($edge in $graph.edges) { $lines.Add(('  {0} -->|{1}| {2}' -f $edge.from, $edge.relation, $edge.to)) }
$lines.Add('  classDef implemented fill:#dcfce7,stroke:#15803d,color:#14532d')
$lines.Add('  classDef partial fill:#fef3c7,stroke:#b45309,color:#78350f')
$lines.Add('  classDef planned fill:#f1f5f9,stroke:#64748b,color:#334155')
foreach ($status in @('implemented', 'partial', 'planned')) {
    $ids = ($graph.nodes | Where-Object status -eq $status | ForEach-Object id) -join ','
    $lines.Add("  class $ids $status")
}
$lines.Add('```')
$lines.Add('')
$lines.Add('## Source index')
$lines.Add('')
$lines.Add('| Concept | Status | Contract / limitation | Sources |')
$lines.Add('| --- | --- | --- | --- |')
foreach ($node in $graph.nodes) {
    $sources = ($node.sources | ForEach-Object {
        $path = $_
        $link = (($path -split '/') | ForEach-Object { [Uri]::EscapeDataString($_) }) -join '/'
        '[' + (Escape-Cell $path) + '](../' + $link + ')'
    }) -join ', '
    $lines.Add(('| {0} | {1} | {2} | {3} |' -f (Escape-Cell $node.label), $node.status, (Escape-Cell $node.summary), $sources))
}
$text = ($lines -join "`n") + "`n"
$output = Join-Path $projectRoot 'docs/knowledge-graph.md'
if ($Check) {
    if (-not (Test-Path -LiteralPath $output) -or (Get-Content -LiteralPath $output -Raw).Replace("`r`n", "`n") -cne $text) { throw 'Generated knowledge graph is stale. Run this script without -Check.' }
} else { [IO.File]::WriteAllText($output, $text, [Text.UTF8Encoding]::new($false)) }
Write-Output "Validated $($graph.nodes.Count) nodes, $($graph.edges.Count) relationships, and source paths."
