# FW-018 virtual-row automation cache cleanup — November 15, 2026

Status: cleanup implemented; correctness and all 32 local performance checks pass.

The pre-change native sample reproduced ten retained disposed editors when a
logical row, list or editor peer was held. Releasing that peer collected them.
The production change removes the indirect cached-child retention: three fresh
candidate processes report zero retained editors for held row/list peers and one
for a directly held editor peer. Releasing that direct peer collects its editor.
Native resource releases remain balanced in all cases.

## Ownership and implementation

`VirtualRowAutomationPeer` registers weak references in an optional conditional
weak table keyed by logical row. The registry roots neither row nor peer/list;
it supports multiple live peers for a row and prunes dead references on refresh.
There is no additional field on every logical row and no peer creation during
ordinary teardown. Registry initialization is safe across independent dispatchers;
each row and its peer list remain owned by their WPF dispatcher.

After clearing presenter/content ownership, row release resets registered peer
children. The row peer returns no actionable children while its presenter is absent,
even if the WPF generator still has an old container association. Existing capture
and cleanup failures are preserved, and native disposal still runs if peer refresh
fails. On attach, only rows with registered peers subscribe to one layout update;
the handler detaches itself before refreshing. Release cancels a pending handler.
WPF continues to own item discovery, realization and ordinary child enumeration.

## Correctness evidence

All 176 framework tests pass, including four new regression cases. The same four
tests fail against isolated unchanged baseline libraries, demonstrating sensitivity:
held row/list/editor peers retain unrelated controls and recycled child discovery is
stale. Candidate checks cover held row/list peers, the deliberate direct-editor root,
three recycle/realize rounds, removal and key reuse, and bounded native realization.
Existing discovery, keyboard, editing/composition and failure tests also pass.
Additional checks pass: 15 visual checks, 21 stress assertions, real-window
startup/disposal and native-window ownership/dispatcher shutdown.

Three fresh candidate native runs cover 21 peer-retention cases. The earlier
pre-change run and intermediate candidates are retained separately. Two focused
test failures exposed premature refresh before layout. A subsequent full-suite
failure exposed recursive UIA calls caused by resetting a wrapper from inside child
enumeration; the final implementation uses WPF's normal enumeration instead.
The early performance attempt was stopped and excluded because its candidate did
not pass that full suite. It is not final-candidate performance evidence.

Provider tests do not establish what Narrator speaks. No new physical screen-reader
acceptance is claimed, and directly retained editor peers remain intentional roots.
The separate fixed GDI focus signature is not part of this cleanup.

## Performance and remaining scope

The paired performance run uses the unchanged current HEAD as the before-change
reference, seven measured pairs with alternating order, the same candidate harness,
SDK/runtime and machine, with full-list, virtualized, themed and layout-editor cases.
Accepted release baseline and budgets remain unchanged. All 32 checks pass against
pre-change HEAD, with raw runs retained. Mount timing changes are +2.17% full-list,
+4.91% virtualized, -0.34% themed and +2.04% editors; update changes are -2.10%,
-0.75%, -3.79% and +2.48%, respectively. Allocation changes are within 0.05% and
component builds/mounts/unmounts are identical. These are measured differences, not
a demonstrated speedup; small timing regressions remain within unchanged budgets.
The standard workload does not continuously query automation peers, so this does
not establish an active-screen-reader overhead budget. It is a local before/after
comparison, not the exact-commit hosted gate against accepted baseline 78c88fb.

FW-018 still requires representative native/browser budgets and contextual failure
diagnostics. This change addresses a specific peer-cache lifetime defect, not every
process-resource increase. See [prior attribution](fw018-automation-retention-2026-11-15.md)
and [plain-WPF control](fw018-plain-wpf-focus-2026-11-15.md).

[Preserved verification evidence](verification-evidence/2026-11-15-peer-cache-cleanup/README.md).
