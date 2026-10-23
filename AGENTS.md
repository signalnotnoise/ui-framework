# Source organization

- Use one declared type per C# file, including records, enums, interfaces and test helpers. Name the file after the type.
- Group files by responsibility (for example Components, Models, Rendering and Virtualization). Keep the core independent of WPF.
- Keep platform lifecycle, control creation, virtualization and application behavior in their respective layers.
- Add focused regression checks for behavior changes; retain a full-list baseline when measuring virtualization.
- Update the relevant documentation and knowledge graph when architectural responsibilities move.

# Framework work tracking

- Use `docs/backlog.md` as the running, prioritized framework work list. Keep application-specific work in the application's backlog.
- When starting, completing or discovering framework work, update the corresponding backlog status, completion evidence and last-updated date. Preserve stable item IDs and retain completed items.

# Knowledge graph workflow

- Before starting each backlog item, consult the relevant concepts, relationships and source paths in `docs/knowledge-graph.json` or `docs/knowledge-graph.md`, and the corresponding neighborhood in `docs/knowledge-graph/graph.json` or its HTML viewer. Identify affected layers, dependencies, tests and ownership boundaries before implementing changes; verify these against the current source.
- Revisit the graph when scope changes or new dependencies are discovered. Update the curated architecture source as APIs, responsibilities, dependencies or verification coverage change during the work, rather than waiting until the final commit. Correct stale or missing relationships in the areas being changed.
- Edit `docs/knowledge-graph.json` and run `tools/Update-KnowledgeGraph.ps1` to refresh the architecture Markdown. Refresh the interactive graph from staged changes through the existing pre-commit hook or `python scripts/update-knowledge-graph.py`; follow `docs/knowledge-graph/README.md`. Do not hand-edit generated graph outputs.
- Before marking a backlog item complete, validate the curated graph with `tools/Update-KnowledgeGraph.ps1 -Check` and the generated graph with `python scripts/update-knowledge-graph.py --check`. Keep the graph, documentation and backlog consistent with the implemented and verified behavior.

# Performance is a standing requirement

- For runtime, renderer, control, layout, styling, state, or virtualization changes, measure before and after using `tools/Test-Performance.ps1`. Use the same benchmark harness, machine, SDK, data sizes and operations for both revisions; alternate order and retain raw repeated runs.
- Always keep the full-list comparison alongside virtualization, and measure themed controls when changing templates. Report mount/update time, UI-thread allocations and component work; do not present fewer builds as proof of lower latency.
- Aim to beat the accepted baseline. Investigate measured regressions and use the evidence to attempt a focused improvement where practical. Preserve correctness, accessibility and application behavior; never reduce workload, loosen budgets, or silently advance the baseline to hide a regression.
- Run focused correctness checks after optimization. Report improvements, regressions and inconclusive/noisy results candidly. Documentation-only changes do not need another benchmark unless they change benchmark instructions or claims.
- Keep the accepted baseline revision and budgets in `tools/performance-baseline.json`. A new baseline needs explicit justification and preserved comparison evidence. CI checks performance separately from correctness.
