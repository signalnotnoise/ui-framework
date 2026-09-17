# Next milestone: viewport virtualization

Status: basic viewport virtualization and incremental list updates implemented. See [current behavior and verification](virtualization.md). Remaining work includes visible-window focus/IME, accessibility, large-shuffle optimization and comparative performance measurements.

## Objective

Keep large lists responsive by mounting only rows near the visible viewport. Current Scroll/StackPanel lists mount every row, so changing compact mode or remounting a board still performs substantial native control/layout work.

## Design questions to resolve first

| Question | Required decision |
| --- | --- |
| Item identity | Keep keys stable independently of visible indices and container reuse. |
| State lifetime | Define whether offscreen component-local state is retained or reset. External item state must persist. |
| Row heights | Support expanded checklist rows without incorrect scroll extents or jumping offsets. |
| Focus and editing | Define behavior when an actively edited row leaves the viewport; preserve selection and IME composition where feasible. |
| Lifecycle | Distinguish offscreen virtualization from actual data removal, with documented hook behavior. |
| Recycling | Prevent a recycled native control from writing through the previous item's binding. |
| Accessibility | Preserve keyboard navigation and screen-reader access to list items. |

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
