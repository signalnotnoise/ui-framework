# Decision 0001: selective observation and explicit component inputs

Date: September 17, 2026  
Status: implemented

## Problem

The stress app exposed avoidable rebuilding in three places. A projected binding observed its entire containing record. Parent updates rebuilt retained child components even when their inputs were unchanged. Every row observed the full selected-item state, so selection changes invalidated all rows.

The historical 200-operation workload produced 132,570 body builds. Body count alone did not establish where elapsed time was spent, so validation also records allocations and native WPF layout time.

## Decision

- Compare derived results before notifying readers. State-backed binding chains are flattened onto their known source; Computed handles dynamic/multiple sources.
- Keep derived subscriptions only while observed. Retain unchanged dependencies between builds.
- Add explicit `.Memo(inputs)` to component descriptions. Equal immutable inputs skip parent-driven configure/Body work, while local invalidations continue normally.
- Represent each row's selected state as a boolean selector so only the previous and newly selected rows receive change notifications.
- Preserve existing stack margins instead of resetting and immediately restoring them.

## Why explicit inputs

Automatically comparing arbitrary configure delegates cannot establish whether captured values changed. Comparing only component identity would miss new props or callbacks. Explicit immutable inputs make the skipping rule reviewable without changing default component behavior.

Keys and Memo serve different purposes. Keys determine instance identity and state lifetime. Memo decides whether that retained instance needs a parent-driven rebuild.

## Correctness requirements

Projection writes use the latest parent record. Unrelated record fields remain intact. Conditional derived dependencies switch even if the selected value stays equal. A read by one dependent must not consume another dependent's pending notification. Prop-driven reads update the baseline used for subsequent notifications.

Memo must allow local updates, changed props, and changed callback inputs. A child that is already dirty must render when its parent updates, without duplicate work. Removal must detach subscriptions and cancel queued work regardless of memoization.

These cases are exercised in [OptimizationChecks.cs](../../tests/UI%20Framework.Checks/OptimizationChecks.cs), alongside existing binding and component checks.

## Tradeoffs and limits

Selectors still run to determine whether their results changed. State-backed selectors must depend on their supplied source and stable ordinary inputs; multi-source observation requires Computed or a custom binding getter. Comparisons use normal equality semantics.

Memo is opt-in and can leave stale UI when an input is omitted. Mutating an ordinary object in place is not detected by comparing that same reference. Observable objects can be stable inputs because their values are tracked separately.

The initial general-purpose observer added enough overhead to worsen timings. Flattening known-source projections removed avoidable tracking work. The matched comparison ultimately showed a modest speed improvement rather than a speedup proportional to the body-count reduction. Both successful and unfavorable measurements are retained in [performance-results.md](../performance-results.md).

## Deferred

Viewport virtualization, native container reuse, and frame-level responsiveness measurement remain separate work. This pass kept all 1,000 rows mounted to preserve the workload.
