# UI Framework knowledge graph

Open [graph.html](graph.html) in a browser. Search paths, filter by area, and
select nodes to inspect relationships. The Three.js viewer supports wheel zoom,
drag pan, Fit view, highlighted connections, and a selected-neighborhood filter.
Directory edges are hidden by default; enable them to see membership. Arrow
keys pan, plus/minus zoom, and Home fits the view. The node selector and
relationship buttons also work without WebGL. [graph.json](graph.json) is the data
source; the HTML includes a generated copy so it also works offline. A pinned Three.js build and its MIT license live in
`scripts/knowledge-graph-vendor/`; generation embeds the library in the HTML.
No CDN or server is required, and vendor code is excluded from the file map.

The graph maps source files and runbooks. `contains` edges group files by
top-level directory. `references` edges come from relative ES module imports
and Markdown links. It describes repository structure, not runtime traffic.
Add links between related runbooks to make their relationships visible.

Set up automatic updates once per clone (Python 3 and Git are required):

```sh
git config core.hooksPath .githooks
```

The pre-commit hook reads the Git index, regenerates both outputs, and stages
them. Stage your intended changes before committing. Unstaged edits are not
included. A generation failure stops the commit. CI checks that committed
outputs match committed files, including when a hook was bypassed.

To refresh manually after staging changes:

```sh
python scripts/update-knowledge-graph.py
```

To preview changes before staging them:

```sh
python scripts/update-knowledge-graph.py --worktree
```

Edit the renderer in [scripts/knowledge-graph.html](../../scripts/knowledge-graph.html)
and the extractor in [scripts/update-knowledge-graph.py](../../scripts/update-knowledge-graph.py).
The generated outputs should not be edited directly.

## Architecture and extraction boundaries

The existing `docs/knowledge-graph.json` remains the curated architecture source;
its schema and PowerShell-generated Mermaid view are preserved. Interactive
`concept:` nodes retain its status, summary and source paths. `evidenced by` edges
connect these concepts to files. Concept relationships are copied from that
curated source; generation does not infer new architectural claims.

File nodes are grouped by top-level directory. Explicit XML `ProjectReference`
entries add project-reference edges. C# `using` directives and method calls are
not resolved. Markdown extraction is best effort, not a complete Markdown parser.
The map includes tracked performance evidence, which can make some areas large.

Do not overwrite another hook manager: integrate the generation command into
that pipeline instead. Hook setup is local to each clone. Generation and asset
loading use the index by default; `--worktree` uses disk, including nonignored
untracked files. Review preview artifacts before sharing. No timestamps or commit
IDs are embedded, keeping regeneration deterministic.

Verify the tooling with `python scripts/test-knowledge-graph.py`. The disposable
fixture checks staged/unstaged separation, stale detection, embedded data,
determinism, project references, and hook staging scope.
