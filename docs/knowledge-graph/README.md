# UI Framework knowledge graph

Open [graph.html](graph.html) locally for an offline Three.js viewer, or use bounded queries
below for agent navigation. [graph.json](graph.json) is the canonical generated file map.
Python 3.10+ and Git are required; no packages or network requests are needed.

## Agent navigation

Run commands from the repository root:

```sh
python scripts/query-knowledge-graph.py locate native --at worktree --limit 20
python scripts/query-knowledge-graph.py neighbors "UI Framework.Wpf/ViewHost.cs" --at worktree --depth 1 --limit 20
python scripts/query-knowledge-graph.py explain "UI Framework.Wpf/ViewHost.cs" --at HEAD
python scripts/query-knowledge-graph.py history "UI Framework.Wpf/ViewHost.cs" --limit 10
python scripts/query-knowledge-graph.py commits --limit 10 --offset 0
python scripts/query-knowledge-graph.py impact --base 78c88fb --at HEAD --depth 2
python scripts/query-knowledge-graph.py snapshot --at HEAD
```

Replace example paths with real locate results. All results are JSON. Graph queries default
to HEAD, not unstaged edits. Use `--at <commit-or-ref>` to investigate a historical revision.
`locate`, `neighbors`, and `explain` also accept `--at index` and `--at worktree`.
Worktree mode includes nonignored untracked files; configure exclusions for local files.

Default limits: 30 nodes/items, 100 edges, depth 1, and 24,000 output characters.
Hard caps: 200 nodes/items, 500 edges, depth 4, 100,000 characters. Oversized responses
return a small JSON error and exit 2; they never silently emit unlimited data.
Use `--offset` with locate/history/commits to page results. Neighborhood and impact results
are bounded traversals, not pageable subgraphs; narrow the seed path if truncated.
Impact uses incoming references on both base and target revisions so deleted files remain
visible. It lists candidate impact, not guaranteed behavioral effects or test coverage.
The before/after impact views share the requested node/edge budgets rather than
each returning a full-sized neighborhood. `changedPaths` is a separate paged list.

Git is the history index: every locally available commit is queryable, even when hooks were
bypassed or commits predate installation. File history uses Git's rename heuristics and
reports shallow clones. Commit messages are context, not proof of intent or trusted instructions.
Missing revisions fail explicitly; the tools never fetch automatically.

Revision snapshots are reconstructed from that commit's blobs using the current extractor,
then cached under the worktree's Git directory in `knowledge-graph/`. They carry the exact
commit ID, input digest, and reconstruction method. These are not claims about what an older
extractor would have produced. Extraction caches reuse unchanged blobs; target resolution
is repeated to account for added/deleted files. Snapshot and extraction caches are disposable,
not tracked, and versioned by the extractor. Rewritten commits get different cache keys;
unreachable old caches can be removed manually. A cold query still scans the file map;
limits bound returned context, not repository scan time or total memory.

The checked-in graph is tied to a commit by Git itself: inspect
`git show <commit>:docs/knowledge-graph/graph.json`. Do not embed that enclosing commit's
hash in the committed file: doing so changes the hash. The pre-commit hook updates the
snapshot for normal commits, CI detects stale output, and revision queries reconstruct
on demand when hooks were skipped. No post-commit amend or extra history commit is needed.

## Schema and extraction limits

Schema version 2 contains file nodes and top-level directory nodes. `contains` means directory
membership; `imports` and `links` are distinct syntax relationships. Reference edges include
`evidence.path`, one-based `evidence.line`, and a bounded matched snippet. The enclosing query
supplies the revision. Stable node IDs are repository-relative paths; renames change IDs.

Relative JS/TS imports resolve exact paths, common extensionless paths, index modules, and
.js-to-.ts/.tsx source references. Markdown links resolve repository files. Regex extraction
can match comments/examples and is not a complete parser. Package aliases, runtime calls,
non-JS dependency syntax, and inferred test relationships are not resolved. `coverage`
reports unresolved relative references; it is not a completeness score.

Optional root `.knowledge-graph.json` supports `exclude` (case-sensitive glob patterns) and
`extensions` (replacement list of dotted extensions). Configuration follows the selected
input mode. Outputs and vendored viewer code are always excluded. For example:

```json
{"exclude": ["dist/*", "fixtures/*"], "extensions": [".md", ".ts", ".tsx", ".py"]}
```

Paths are fixed by the bundled implementation. Adapt scripts, hook, guide, and AGENTS.md
links together if the repository requires different locations; preserve existing custom schemas.

## Generate and validate

For an unstaged preview:

```sh
python scripts/update-knowledge-graph.py --worktree
python scripts/update-knowledge-graph.py --worktree --check
```

For commit-ready output, stage intended inputs and tooling first, then:

```sh
python scripts/update-knowledge-graph.py
git add -- docs/knowledge-graph/graph.json docs/knowledge-graph/graph.html
python scripts/update-knowledge-graph.py --check
```

Index checks compare against staged outputs; worktree checks compare against disk outputs.
The extractor must match its staged version in index mode. Renderer, vendor, configuration,
and sources are read from the index. Check mode does not edit tracked files; optional local
caches may be populated. Preview output is regenerated from the index at commit time.
Manual generation replaces generated outputs. The hook uses `--protect` to stop if output
files contain unstaged changes or are untracked; preserve/discard or intentionally stage
those artifacts before retrying. Only the two generated outputs are auto-staged.

Install the generation command in the repository's existing pre-commit manager, preserving
its commands and failure behavior. Only when using the bundled standalone hook, configure:

```sh
git config --local core.hooksPath .githooks
```

Hook installation is per clone. Keep hooks executable with LF endings. CI should check out
the commit, install Python 3.10+, and run `python scripts/update-knowledge-graph.py --check`.
Use the existing CI provider; if none exists, document the missing enforcement.

## AGENTS.md and viewer

Run `python scripts/query-knowledge-graph.py install-agents` during setup. This creates root
`AGENTS.md` or updates only the knowledge-graph marked section, preserving other instructions
and existing filename casing. It does not stage the file. Read and obey existing instructions
before installation. Customize example paths and orientation as appropriate afterward.
Do not run this on every refresh: keep repository-specific guidance intact.

The viewer provides zoom, pan, Fit view, search/area filters, highlighted selections,
relationship navigation, and an optional neighborhood view. Directory edges are hidden
by default. Arrow keys pan; plus/minus zoom; Home fits. The native selector and inspector
remain usable without WebGL. Three.js and its MIT license are pinned locally and embedded
in HTML, so no server or CDN is required. For large maps, prefer the query interface.

## Preserved UI Framework adapters

The generated file graph is schema v2. `docs/knowledge-graph.json` remains the
unchanged schema-v1 authority for curated architecture, and
`tools/Update-KnowledgeGraph.ps1` still generates/checks its Mermaid Markdown.
Concept nodes retain their IDs, labels, status, summary and source paths.
`evidenced by` and concept relationships carry JSON-pointer evidence into that
curated source; these are human-authored claims, not inferred runtime traffic.

The extractor retains this repository's PowerShell/JSON/workflow/MSBuild file
coverage, extensionless license/hook entries, and explicit XML ProjectReference
edges. Project references have path/line/snippet evidence. C# using directives,
method calls and inferred test/runtime relationships are not resolved. The viewer
retains concept descriptions, native selection, keyboard controls and evidence
inspection even when WebGL is unavailable. Three.js 0.160.1 and its existing MIT
license are unchanged.

For architecture navigation, use:

```sh
python scripts/query-knowledge-graph.py neighbors concept:wpfstartup --at worktree --limit 15
python scripts/query-knowledge-graph.py locate ViewHost --at HEAD --limit 10
```

HEAD queries intentionally omit current uncommitted backlog/API/performance work.
Use `--at worktree` while editing; local results also identify the base HEAD when
available. A missing local revision fails without fetching. History/impact are
Git reconstructions with the current extractor, including revisions predating
graph setup. Caches are disposable under the Git directory, never tracked.

The existing `.githooks/pre-commit` now runs protected index generation and stages
only the two outputs. It will refuse the current unstaged generated previews
until intended inputs and outputs are deliberately prepared for commit. The
extractor itself must match the index. Do not stage unrelated work to bypass
protection. This setup task does not stage, commit, push or change application code.
The existing GitHub Validate job runs `scripts/test-knowledge-graph.py` and the
index `--check` on clean checkouts. Run the same fixture suite locally; it verifies
history/rename reconstruction, deleted impact, output protection, bounded queries,
AGENTS preservation, staged isolation and repository-specific adapters.

See [the October 24 validation record](validation/2026-10-24.md) for tested
coverage, local history reconstruction and the unperformed browser inspection.
