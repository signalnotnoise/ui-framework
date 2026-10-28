# FW-006 accessibility acceptance — October 26, 2026

Owner: Codex for implementation and automated evidence; physical accessibility
review requires a human operator. Status: in progress. FW-005's physical IME
acceptance remains independent and pending.

## Initial audit and implementation

Existing checks exercised native edit/value, toggle and invoke patterns, explicit
name replacement/removal, compact focus appearance and splitter/picker input.
The default WPF item peer did not derive names from the framework's logical row
description. A name search for an unrealized labeled row returned no provider.

`VirtualListAutomationPeer` and `VirtualRowAutomationPeer` now use WPF's native
ItemsControl/Item peers. Row names come from `AccessibleName` or direct `Content`
without realizing controls or capturing the data item strongly. WPF owns the
weak caches, item enumeration and virtualization pattern. No public API changed;
the core remains independent of WPF.

The longer keyboard check found two invisible Tab destinations: the row
presenter and its `ViewHost`. Both lifetime wrappers now default to
`Focusable=false` and `IsTabStop=false` via type metadata. Explicit native local
values retain their normal precedence. Forward and reverse traversal exercise
30 rows beyond the initial viewport, rather than only the initially realized
controls. NavigationSurface already opts out of focus; native islands use a
non-focusable Border wrapper and keep their own control's keyboard behavior.

The implementation follows the installed WPF 10.0.13 behavior in
[ItemsControlAutomationPeer](https://github.com/dotnet/wpf/blob/v10.0.13/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Automation/Peers/ItemsControlAutomationPeer.cs)
and [ItemAutomationPeer](https://github.com/dotnet/wpf/blob/v10.0.13/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Automation/Peers/ItemAutomationPeer.cs).
The framework supplies row metadata; it does not replace those native providers.

Provider tests initialize the visible window's peer hierarchy before requesting
raw providers. A collapsed ComboBox does not necessarily generate item peers;
the selection test opens the popup before checking its item-selection providers.
Earlier harness failures are retained and are not presented as product defects.

## Acceptance matrix

| Scenario | Required behavior | Evidence / status |
| --- | --- | --- |
| 1,000 labeled virtual rows | Find row 999 by name, realize it, reach its named button; fewer than 100 native buttons remain realized | Automated regression passes |
| Rename/reorder/remove/reinsert | Discovery uses current name; removed name is absent; reinserted key realizes current row | Automated regression passes |
| Keyboard form and virtual rows | Forward/reverse traversal reaches editor, toggle and button; disabled button skipped; theme replacement preserves focused toggle; traverse 30 rows in both directions | Two automated regressions pass |
| Native states | Retained edit provider reports read-only and rejects writes; button reports disabled; picker selection-item states track model changes | Automated regression passes |
| Screen reader | Narrator announces names, types, values, selection, disabled/read-only state and focus changes; virtualized actions remain reachable | User reports passed October 28, 2026; detailed observations not supplied |
| Contrast themes | System contrast settings keep text, focus, selection, popup and disabled states distinguishable; dynamic changes follow supported policy | Implemented locally: three contrast checks pass; mount budgets fail and physical contrast acceptance is pending (see accessibility-2026-10-28.md) |
| Display scaling | 100%, 150%, 200% physical scale and monitor transitions preserve readable/unclipped controls and usable keyboard focus | User reports passed October 28, 2026; exact scale and monitor combinations not supplied |

## Manual procedure

Use the framework-owned `tools/AccessibilityAcceptance` window, built from the
working tree. Record source identity, OS build, .NET runtime, screen reader and
version, Windows contrast setting, actual monitor scaling, language, and observed
results. This is not a published-package acceptance tool. Keep IME language-pack
installation separate from accessibility results.

From the repository root, with the .NET 10 SDK selected:

```powershell
dotnet run --project tools/AccessibilityAcceptance -c Release
```

The window writes uniquely named JSONL files under `artifacts/fw006`. Its
observation field saves operator notes with the current DPI and system contrast
flag. F6 switches application light/dark styles without intentionally moving
focus. `--smoke` exercises setup and provider calls only; its logs explicitly
identify that mode and do not satisfy any physical acceptance row.

1. With Narrator running, use Tab and Shift+Tab through the form, picker and
   row actions. Confirm spoken names match the active item and current value.
   Disabled actions must be skipped; read-only text must remain discoverable.
2. Use the picker with arrow keys; toggle the checkbox with Space; invoke an
   action from the keyboard. Confirm the announced state and visible result.
3. Scroll through the list with keyboard/screen-reader navigation. Verify the
   final row is reachable and named correctly after recycling. A direct provider
   realization check does not establish Narrator's own discovery/navigation.
4. Change the application's theme while an editor is focused and has selection.
   Check focus, selection, spoken context and control identity remain useful.
5. Enable a Windows contrast theme using Settings, then repeat focus, edit,
   selection, popup and disabled-state checks. Record current failures. The app's
   light/dark buttons are not substitutes for a system contrast theme.
6. Repeat at real 100%, 150%, 200% display scale where available; move the window
   between differently scaled monitors. Record unavailable configurations as
   untested, not passed.

Do not mark FW-006 complete until failures are resolved or its supported scope
is explicitly decided and all required evidence is recorded. No manual result
has been collected in this record yet.

## Validation and performance

Final runtime correctness: all 139 framework tests, 15 visual checks, 21 stress
assertions and the actual-window startup/editing/disposal check pass. Release
builds have no warnings or errors. The five new checks pass; against the isolated
pre-change runtime they produce three expected failures (two discovery failures
and the invisible row-presenter Tab stop), with the two native-control checks
passing. The acceptance window builds cleanly and its setup/provider/focus smoke
check passes. At the original audit, no physical Narrator/contrast/scaling acceptance was performed. On October 28 the user reported successful Narrator and scaling checks. Contrast-theme acceptance remains open.

Raw TRX files retain earlier harness failures: provider trees must be connected,
popup children must be generated and their peer cache refreshed, and desktop
focus checks must be isolated from another visible-window test process. Those
failures are separate from the three reproduced framework gaps. The first
manual-window smoke also selected the picker as an ItemsControl; selecting the
named Records list corrected that harness error.

The physical IME window was already open when an optional rebuild was attempted;
Windows locked its old assembly. The running window was left alone. That build
attempt is not a failing framework regression or evidence of IME acceptance.
Its existing binary/source provenance remains separate from this FW-006 runtime.

The initial peer-only iteration passed all 32 predecessor comparisons. It
predates the wrapper-focus fix and cannot clear performance for the final code.
The first final-runtime comparison failed two mount-time gates: virtualized
mount +29.82% and editor mount +19.30%, against the unchanged 10% time budget.
All allocation and component-work checks passed. The failure is preserved.

Seven paired mount changes spanned -34.84% to +192.57% for virtualized rows and
-22.14% to +37.34% for editors. The medians of paired changes were +2.09% and
-0.82%, respectively; those descriptive statistics do not replace the failed
ratio-of-medians gates. A candidate virtual mount of 1,069.87 ms versus 365.68 ms
used similar total process CPU (1.64 s versus 1.72 s); this suggests a waiting or
scheduling contribution, but total process CPU does not identify a mount-phase
cause.

An identical-binary control (three measured pairs plus a discarded warmup on
each side) passed its 24 descriptive checks, but shifted virtualized mount by
-13.83% and editor mount by -9.98%. Payload hashes match. This confirms that
mount timing can move without a code difference; three pairs do not establish
the absence of a runtime regression. A seven-pair focused predecessor repeat,
retaining the full-list workload alongside virtualization and editors, passed
all 24 comparisons with component work unchanged:

| Final-source repeat | Mount time | Update time | Mount allocation | Update allocation |
| --- | ---: | ---: | ---: | ---: |
| Full list | +0.96% | +0.48% | -0.02% | -0.06% |
| Virtualized | +3.33% | -0.41% | +0.00% | +0.01% |
| Editors | -21.26% | -0.56% | -0.00% | -0.00% |

This repeat does not erase the failed 32-check run or establish a causal editor
speedup. Mount repeatability remains an acceptance uncertainty. No budget or
accepted baseline has changed, and none of these comparisons is release-eligible.

The comparison baseline is
a local measurement-only snapshot of the existing FW-005 working-tree runtime,
`fe0efac8649b5d9c9565e1404b251516551d5a5f`. It updates no branch or accepted
baseline. This isolates the FW-006 runtime delta from the still-uncommitted
FW-005 changes. All raw iterations and source/binary identity are retained under
`artifacts/fw006/performance-predecessor`, `performance-final`,
`performance-identical-control` and `performance-focused-repeat`.

Runs use SDK 10.0.402 / runtime 10.0.13 on the same Windows machine, with the
same Counter harness, 1,000 rows, 50 mixed operations and the
`process-dpi-initialization-v2` protocol. Each side has one discarded warmup;
measured process order alternates. Source and payload hashes accompany the raw
records. A custom predecessor baseline or focused/control run is not an
accepted-source release gate, regardless of its budget outcome.

The standard guardrails measure the existing full-list, virtualized, themed and
editor workloads. They do not establish Narrator navigation latency, UIA search
throughput, or active assistive-technology overhead. Discovery scans logical
rows and can allocate item peers even though native realization stays bounded.

Archived raw reports, process CPU/wall timings, pair statistics, manifests,
source ZIPs, expected failures and successful checks are in
[the evidence directory](performance-evidence/2026-10-26-accessibility/summary.md).
