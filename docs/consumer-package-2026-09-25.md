# Local consumer package adoption, September 26, 2026

`0.1.0-alpha.3-local.2` is an immutable local integration package built from the
dirty working tree at framework revision
`2db7a9a6cdde0fa52c458a9206219348f4ae0bcd`. It adds the production
`WpfComCleanupPolicy`; it is not yet a NuGet.org release.

Lab-Feedback-WPF pins `SignalNotNoise.UI.Wpf` exactly to
`[0.1.0-alpha.3-local.2]`, with core resolved transitively to the same version.
The app creates one cleanup owner before `MainWindow`, reports cleanup failures
to its existing diagnostics log, disposes application owners before final
cleanup, and returns a nonzero exit code if cleanup failed. The duplicated
app-local prototype and tests were removed; framework package tests now own that
behavior.

## Paired application comparison

Fifteen measured pairs per scenario plus discarded warmups compared
`0.1.0-alpha.3-local.1` with `0.1.0-alpha.3-local.2`. The exact candidate policy
source was overlaid into the old-package snapshot so both sides used identical
cleanup behavior and the comparison isolated package/runtime differences. SDK
10.0.401, consumer revision
`3c5aea06eaac8b7f2587691c13b748c29c7a0c58`, workloads, order alternation, and
budgets were shared.

All 28 checks passed:

| Scenario | Startup | Mount | Update | Process |
| --- | ---: | ---: | ---: | ---: |
| Window | -1.96% | +4.37% | -1.37% | -0.80% |
| Splitter | +1.22% | +5.19% | -2.61% | -0.10% |
| Full tree | -0.97% | -1.60% | +1.53% | +0.05% |
| Virtualized tree | -0.06% | -0.91% | -2.02% | -1.56% |

All startup, mount, and update allocation changes remained within the unchanged
2% allowance. Full-tree realization remains beside virtualized realization.
The harness does not instrument component builds, so no component-work reduction
is claimed.

Evidence:
`docs/performance-evidence/2026-09-26-editor-cleanup-release/consumer/summary.json`

The application Release build completed with zero warnings and all 189 tests
passed. The isolated package smoke check also passed rendering, state updates,
rich themed button content, retained native control ownership, and construction
of the packaged cleanup owner.
