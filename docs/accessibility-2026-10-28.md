# FW-006 contrast-theme implementation — October 28, 2026

Status: implemented locally; acceptance remains open. No package was published.

## Scope and ownership

`ThemeStyles.Apply` installs a scoped palette alongside its native styles.
`ThemePalette` maps supplied tokens to Windows window/text, gray-text and
highlight/highlight-text colors while system contrast is active. System parameter
and user-preference notifications enqueue updates on the palette's dispatcher.
The global registry contains weak references, not controls or roots. Binding-backed shared brushes remain updateable after WPF seals templates. Changing the
palette preserves templates, native editor contents and focus; leaving contrast
mode restores the original tokens. There is no new public API or core dependency.

Disabled buttons, editors and picker items now use explicit muted foreground and
surface background at full opacity, in ordinary themes as well as contrast themes.
Existing disabled-opacity assertions were updated to that deliberate behavior.
Application-owned local colors, styles, backgrounds and native islands retain
precedence and remain the application's accessibility responsibility.

## Correctness evidence

- 142 tests run: 140 pass, two virtual-row automation discovery tests fail.
- All three contrast tests pass: colors/selection/template identity/local override;
  real-window keyboard focus and open-popup selection; unused palette collectability.
- An isolated copy with the previous three styling files and without the new
  palette reproduces the same two failures (three of five accessibility tests pass).
  This establishes that these failures occur without the contrast implementation,
  but does not resolve them or establish their cause.
- 15 visual checks and 21 stress assertions pass. Actual-window native editing
  passes and active work after disposal is zero.
- User-reported Narrator and physical scaling acceptance remains recorded from
  October 28; detailed scale/monitor observations were not supplied.
- Actual Windows contrast switching remains a physical acceptance step. Injected
  palette transitions are not evidence of real operating-system notifications or
  active IME composition acceptance.

## Performance evidence

The first seven-pair run stopped during themed sample 4 without a summary.
Its raw results remain under `artifacts/fw006-contrast/performance`. It includes
an out-of-budget virtualized mount comparison and must not be described as passing.
The complete binding-backed run is retained under `performance-final`.
A scoped dynamic-resource alternative is retained under `performance-resources`;
it performed worse overall and was reverted. Current runtime sources match the
`performance-final/candidate-source` snapshot byte for byte.
Both use the accepted revision and unchanged budgets, same SDK/runtime and
harness, alternating fresh processes, full-list/virtualized/themed/editor workloads.
Results are cumulative against the accepted baseline, not isolated attribution
of all improvements or regressions to the contrast change.


### Completed measurements and remaining gates

The retained implementation passes 29 of 32 budgets. Failures:

| Workload / metric | Change versus accepted baseline | Budget |
| --- | ---: | ---: |
| Themed mount time | -1.13% | +10% |
| Themed mount allocations | **+8.64%** | +2% |
| Editor mount time | **+11.41%** | +10% |
| Editor mount allocations | **+6.88%** | +2% |

The themed mount-time row is included for context and passes. The resource
alternative passes 28/32, failing themed mount time (+16.92%) and allocations
(+9.12%), plus editor mount time (+21.97%) and allocations (+26.65%). Both
experiments preserve identical component work. Allocations identify a repeatable
cost requiring optimization; cross-run timing variation remains unresolved.
No budgets or accepted baseline were changed. This implementation is not cleared
for release. Next: profile live-color expression/brush instantiation costs and
reduce mount allocations while retaining live updates and editing identity;
resolve the two reproduced discovery failures; collect real contrast-switching
acceptance. The existing Narrator/scaling reports remain recorded.


Raw runs, source archives, TRX reports, logs and the unchanged policy are retained
in [the evidence archive](performance-evidence/2026-10-28-contrast/manifest.json).
The archive includes interrupted and unsuccessful attempts; no failed run was
replaced. The retained runtime was rebuilt with zero warnings/errors and its
source matched the completed binding-backed run before final test verification.
An updated manual acceptance application is built at
`artifacts/fw006-contrast/acceptance/AccessibilityAcceptance.dll`.
