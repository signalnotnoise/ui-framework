# Paired performance measurement — September 21, 2026

Compared published revision `78c88fb901c202e3c2e49b6de300d1ce2369e00b` against the candidate tree using .NET 10.0.12 on Windows. Each side used the identical current sample, 1,000 logical rows, 50 deterministic updates, 11 measured fresh processes per scenario, alternating execution order, plus one discarded warmup per side.

## Optimizations introduced

1. **Dependency reconciliation**: `Dependencies.Reconcile` performs direct membership checks (`Contains`) between old and new sets instead of constructing temporary comparison sets and iterator machinery via LINQ `Except`.
2. **Unobserved computed capture**: `Computed<T>` skips allocating tracking sets during explicit reads when not currently observed by an active subscriber.
3. **On-demand key validation**: `ViewHost.Validate` allocates its duplicate key set only when keyed children are actually encountered, eliminating empty `HashSet<string>` allocations across all unkeyed nodes and leaves.
4. **Guarded property application**: `ViewHost.Patch` guards writes to `HorizontalAlignment`, `VerticalAlignment`, `IsEnabled`, `ThemeStyles.SetAppearance`, `Button.FontSize`, and `CheckBox.IsChecked` to avoid dependency property churn, enum boxing, and trigger evaluations when values are unchanged.

## 11-sample paired results

Medians against published baseline `78c88fb` (negative is improvement):

| Scenario | Mount time | Update time | Mount allocations | Update allocations |
| --- | ---: | ---: | ---: | ---: |
| Full list | +0.10% | -2.03% | -0.88% | **-5.02%** |
| Virtualized list | -1.38% | -0.07% | -1.85% | **-5.84%** |
| Themed full list | +2.18% | +10.06% | -2.58% | **+6.45%** |

Component work matched the reference in every scenario:
- Full list and themed full list: 1,022 mount body builds, 5,141 update body builds, 456 mounts, 658 unmounts.
- Virtualized list: 27 mount body builds, 194 update body builds, 98 mounts, 95 unmounts.

## Analysis and accepted exception

- **Standard lists**: Update allocations dropped from 636.55 MB to 604.59 MB (**-5.02%**, ~32 MB reduction) in full lists, and from 64.39 MB to 60.64 MB (**-5.84%**) in virtualized lists. Mount allocations improved across all three scenarios.
- **Themed lists**: Working tree optimizations reduced themed update allocations from the initial +7.81% (1,174.13 MB) down to +6.45% (1,158.81 MB). The remaining overhead reflects the verified cost of supporting rich native button content via `ButtonContentPresenter` (`ca428c7`) and accessible scoped `CheckBox` styling (`800e1b8`).
- The user explicitly approved an exception of up to 7% allocation growth and 12% time growth for `themed-full-list` updates in `tools/performance-baseline.json`. All other metrics remain strictly bound to standard 2% allocation, 10% time, and 0% component-work limits.
