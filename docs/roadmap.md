# Next milestone: viewport virtualization

The [framework backlog](backlog.md) is the current prioritized work list. This
document retains virtualization design questions and acceptance criteria;
implemented milestones below are not new work items.

Status: basic viewport virtualization, incremental list updates, focused-row restoration and provider completion before editor detachment implemented. See [current behavior and verification](virtualization.md). Remaining work includes physical IME acceptance, UI Automation coverage, large-shuffle optimization and broader performance acceptance.

## Objective

Keep large lists responsive by mounting only rows near the visible viewport. Current Scroll/StackPanel lists mount every row, so changing compact mode or remounting a board still performs substantial native control/layout work.

## Design questions to resolve first

| Question | Required decision |
| --- | --- |
| Item identity | Keep keys stable independently of visible indices and container reuse. |
| State lifetime | Define whether offscreen component-local state is retained or reset. External item state must persist. |
| Row heights | Support expanded checklist rows without incorrect scroll extents or jumping offsets. |
| Focus and editing | Retain the active editor while WPF keeps its container; finish composition through the original provider before detachment, then save logical state/selection. Physical IME acceptance remains pending. |
| Lifecycle | Distinguish offscreen virtualization from actual data removal, with documented hook behavior. |
| Recycling | Prevent a recycled native control from writing through the previous item's binding. |
| Accessibility | Unreleased: logical row peers support offscreen discovery/realization; lifetime wrappers no longer interrupt Tab traversal. Narrator, system contrast integration and physical scaling remain FW-006 work. See [acceptance](accessibility-2026-10-26.md). |

## Suggested implementation sequence

1. Introduce a keyed list primitive with an explicit virtualization policy; retain the current fully mounted list for comparison.
2. Implement a WPF virtualized backend, beginning with a documented row-height policy.
3. Establish state/lifecycle and binding rules before enabling container reuse.
4. Add focus, scrolling, conditional-height, and recycling checks.
5. Compare fully mounted and virtualized modes using identical data and actions.

## Acceptance criteria

- Mounted native rows stay bounded by viewport and overscan, rather than total item count.
- Edits remain attached to the correct item after scroll, shuffle, filter, removal, and reuse.
- Offscreen state/lifecycle behavior matches the documented policy.
- Disposing the list releases observers, timers, and queued work.
- Reports include body builds, allocations, elapsed time, and visible-window interaction measurements.
- Existing binding, component, and full-list regression checks continue to pass.

No implementation date or numerical latency target has been committed. Establish those from a measured prototype on a defined machine.
