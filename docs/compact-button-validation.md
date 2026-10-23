# Compact button validation

FW-002, October 23, 2026. Source change is Unreleased; published alpha.4 packages
are unchanged. Performance verification is in progress.

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
mount/unmount counts matched in every scenario. All allocation changes were
between −0.004% and +0.02% except mount allocation changes down to −0.04%.

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
the cause of the timing increase. An identical-binary control at the same two
execution paths and an independent accepted-baseline CI comparison are pending.
FW-002 remains In progress until that investigation and verification finish.
