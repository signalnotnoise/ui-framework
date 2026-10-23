# Compact button validation

FW-002 completed October 23, 2026 in `f917ca6`. Source change is Unreleased;
published alpha.4 packages are unchanged. Correctness and accepted-baseline CI
gates pass. The initial failed direct timing result is retained below.

## Behavior

The button template previously ignored native `Padding` and always used
`theme.ControlPadding`. A zero-padding close button therefore kept 10 DIPs of
padding on each edge. The theme also imposed a 36-DIP minimum even when a
native button explicitly requested an 18-DIP height. The initial reproduction
failed with actual chrome padding `(10, 10, 10, 10)` instead of `(0, 0, 0, 0)`.
The retained initial test log also includes an exploratory assertion that theme
padding should override the renderer's existing local padding; the final test
instead preserves normal WPF local-value precedence. No renderer change remains.

The template now binds chrome padding to the native property. A style trigger
retains the 36-DIP minimum for automatic native Height, while explicit Height
uses the native minimum of zero unless the application supplies its own
MinHeight. The input and picker styles retain their existing minimums. No
renderer, core API, content presenter or lifecycle implementation changed.

Local padding and bindings retain WPF precedence. Ordinary native buttons
without overrides retain theme spacing, and existing framework-created buttons
retain their native `(12, 6, 12, 6)` padding. That explicit value now affects
the chrome, so their vertical padding is smaller than the previously drawn
10 DIPs per side. This is a geometry correction, not evidence of a faster
reconciliation algorithm. View frame sizing/padding remains separate from
native button sizing/padding.

## Correctness and rendering

The Release build passed without warnings/errors. All 119 correctness tests,
15 visual checks and 21 stress assertions passed. Thirteen new cases cover:

- Full glyph layout bounds inside an 18-by-18-DIP native button with zero
  padding, a 10-DIP font, retained rich content and a contextual automation name.
- Default theme spacing, native padding overrides, live binding updates,
  clearing overrides, theme replacement and retained declarative controls.
- Explicit Height, restoring automatic Height and explicit MinHeight precedence.
- Light/dark offscreen renders at simulated root DPI scales of 100%, 125%,
  150% and 200%, checking DPI inheritance and the complete text layout bounds.
- A real offscreen Window taking keyboard focus, focus-outline color, native
  UI Automation Invoke, click routing, disabled state and theme replacement.

The rendered light and dark glyphs were inspected. These automated/offscreen
checks do not establish manual multi-monitor or screen-reader acceptance. Font,
border and padding still need sufficient space; the framework does not shrink
glyphs to hide an impossible size constraint.

| Light at 200% simulated DPI | Dark at 200% simulated DPI |
| --- | --- |
| ![Complete close glyph in light theme](performance-evidence/2026-10-23-compact-buttons/renders/light-200.png) | ![Complete close glyph in dark theme](performance-evidence/2026-10-23-compact-buttons/renders/dark-200.png) |

## Performance

The direct comparison uses pre-fix revision `13a5b98cad533bd9b33dcbd5b5f234d03033985c`.
The accepted baseline remains `78c88fb901c202e3c2e49b6de300d1ce2369e00b`.
Both comparisons use `tools/Test-Performance.ps1` with seven alternating measured
processes per side/scenario plus warmups. Each comparison holds machine and SDK
constant between revisions; the direct run is local and the accepted-baseline
check runs in CI. Both retain the same candidate harness, 1,000 rows, 50 mixed operations and production editor
cleanup policy. Full-list, virtualized, themed full-list and layout/editor
workloads are retained. Budgets and the accepted baseline are unchanged.
The local direct run used SDK 10.0.402/runtime 10.0.13. Component builds and
mount/unmount counts matched in every scenario. All mount/update UI allocation
changes were within ±0.04%.

| Scenario | Mount time | Update time | Result |
| --- | ---: | ---: | --- |
| Full list | +0.58% | **+28.86%** | Update-time gate failed |
| Virtualized | +2.02% | +3.05% | Within budget |
| Themed full list | −2.15% | +2.63% | Within budget |
| Layout/editors | −2.37% | −1.64% | Within budget |

The direct run failed one of 32 budgets; it is not a passing comparison. Its
[summary and raw runs](performance-evidence/2026-10-23-compact-buttons/direct/summary.md)
are retained. The full-list workload does not apply ThemeStyles and therefore
does not execute the changed template; this observation alone does not establish
the cause of the timing increase. The identical-binary control and focused
repeat below investigate that signal; the independent accepted-baseline CI
comparison passed. The first comparison remains recorded as a failure.

## Identical binary control

Seven alternating measured full-list processes per side plus warmups used
SHA-256-identical Counter, core and WPF assemblies at the original two execution
paths. Runtime configuration and dependency files were also equal. Application
sources and workload were unchanged. The candidate output assemblies were
backed up and restored, with hash verification, before the focused repeat.

The control's update medians were 14,400.70 versus 13,567.43 ms (−5.79%) and
mount medians 8,695.97 versus 8,731.07 ms (+0.40%). Update times ranged from
10,079.96–14,932.00 ms on one path and 10,278.32–13,871.21 ms on the other.
Update UI allocations differed +0.53%; component work was identical. This
demonstrates substantial measurement variation with identical code; it does
not by itself establish the cause of the first failed comparison or prove the
fix is regression-free. [Control results, hashes, raw runs and scripts](performance-evidence/2026-10-23-compact-buttons/identical-binary/summary.json).

## Focused full list repeat

After the control restored the actual candidate assemblies, another seven
alternating measured full-list processes per side plus warmups ran the same
Counter workload against the original baseline. Assembly manifests confirm
the candidate was restored before the repeat; the workload and source were
unchanged. This is a focused diagnostic, not a replacement release gate.

Mount medians were 8,145.62 versus 8,285.97 ms (+1.72%), and update medians
12,086.76 versus 11,534.64 ms (−4.57%). Mount UI allocations changed −0.03%
and update allocations +0.09%; component builds and mounts/unmounts matched.
Update ranges were 3,944.63–16,401.83 versus 3,889.96–12,804.75 ms. The original
28.86% update increase did not reproduce, but these wide ranges do not support
a reliable speedup claim. Timing variability and the earlier concerns remain
tracked under FW-003. [Repeat results, hashes, raw runs and scripts](performance-evidence/2026-10-23-compact-buttons/focused-full-list/summary.json).

## Accepted baseline CI

[Validate run 39919264703](https://github.com/signalnotnoise/ui-framework/actions/runs/39919264703)
passed Windows correctness, package validation, graph validation and all
32 performance gates for commit `f917ca6d46467430fc0bbb9bc583c227e2e87748`.
It used SDK 10.0.402/runtime 10.0.13 with seven measured processes per side and
discarded warmups. Its unchanged accepted baseline was `78c88fb`.

| Scenario | Mount time | Update time | Mount UI allocation | Update UI allocation |
| --- | ---: | ---: | ---: | ---: |
| Full list | +2.26% | −28.53% | −1.06% | −26.21% |
| Virtualized | +2.50% | +1.04% | −1.61% | −5.10% |
| Themed full list | −0.93% | −57.23% | −2.74% | −57.16% |
| Layout/editors | −0.63% | −4.12% | +0.09% | −3.29% |

Component builds and mounts/unmounts were unchanged. Positive values are
increases within budget; improvements include earlier renderer work and cannot
be attributed solely to the button fix. No budget, baseline, workload size or
operation count changed. [CI summary and raw runs](performance-evidence/2026-10-23-compact-buttons/ci/summary.md).

FW-002 is complete for source implementation and validation. A new package
version and the normal publication workflow are required before NuGet consumers
receive it. FW-003 remains open for performance investigation.
