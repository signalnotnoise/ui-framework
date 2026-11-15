# Viewport virtualization design history and release direction

The [framework backlog](backlog.md) is the current prioritized work list. This
document retains virtualization design questions and acceptance criteria;
implemented milestones below are not new work items.

Current status (November 15): viewport virtualization, incremental keyed updates,
editing policy, logical UI Automation discovery and large-shuffle optimization
are implemented. FW-005/006 historical physical acceptance and scoped FW-017–019
appearance, memory and adoption acceptance are complete. The next work is exact
current-source candidate validation and release preparation, not another
virtualization implementation. See [release sequence](releasing.md#next-release-candidate).

The following questions and sequence preserve the original design plan; use
[virtualization](virtualization.md) for the implemented contract and limitations.

## Objective

Keep large lists responsive by mounting only rows near the visible viewport. Current Scroll/StackPanel lists mount every row, so changing compact mode or remounting a board still performs substantial native control/layout work.

## Original design questions

| Question | Required decision |
| --- | --- |
| Item identity | Keep keys stable independently of visible indices and container reuse. |
| State lifetime | Define whether offscreen component-local state is retained or reset. External item state must persist. |
| Row heights | Support expanded checklist rows without incorrect scroll extents or jumping offsets. |
| Focus and editing | Retain the active editor while WPF keeps its container; finish composition through the original provider before detachment, then save logical state/selection. Physical IME acceptance was subsequently confirmed October 29; see the manual record. |
| Lifecycle | Distinguish offscreen virtualization from actual data removal, with documented hook behavior. |
| Recycling | Prevent a recycled native control from writing through the previous item's binding. |
| Accessibility | Published since alpha.6: logical row peers support offscreen discovery/realization; lifetime wrappers no longer interrupt Tab traversal. FW-006 manual acceptance is recorded in [the October 29 record](manual-acceptance-2026-10-29.md); later peer-cache cleanup is unpublished. |

## Original implementation sequence (completed)

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

The original prototype stage is complete. Current workloads and unchanged timing/allocation
limits are in tools/performance-baseline.json; scoped memory limits are documented
in [memory budgets](memory-budgets.md). These are workload-specific, not universal
application performance guarantees.
