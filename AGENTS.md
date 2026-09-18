# Source organization

- Use one declared type per C# file, including records, enums, interfaces and test helpers. Name the file after the type.
- Group files by responsibility (for example Components, Models, Rendering and Virtualization). Keep the core independent of WPF.
- Keep platform lifecycle, control creation, virtualization and application behavior in their respective layers.
- Add focused regression checks for behavior changes; retain a full-list baseline when measuring virtualization.
- Update the relevant documentation and knowledge graph when architectural responsibilities move.

# Performance is a standing requirement

- For runtime, renderer, control, layout, styling, state, or virtualization changes, measure before and after using `tools/Test-Performance.ps1`. Use the same benchmark harness, machine, SDK, data sizes and operations for both revisions; alternate order and retain raw repeated runs.
- Always keep the full-list comparison alongside virtualization, and measure themed controls when changing templates. Report mount/update time, UI-thread allocations and component work; do not present fewer builds as proof of lower latency.
- Aim to beat the accepted baseline. Investigate measured regressions and use the evidence to attempt a focused improvement where practical. Preserve correctness, accessibility and application behavior; never reduce workload, loosen budgets, or silently advance the baseline to hide a regression.
- Run focused correctness checks after optimization. Report improvements, regressions and inconclusive/noisy results candidly. Documentation-only changes do not need another benchmark unless they change benchmark instructions or claims.
- Keep the accepted baseline revision and budgets in `tools/performance-baseline.json`. A new baseline needs explicit justification and preserved comparison evidence. CI checks performance separately from correctness.
