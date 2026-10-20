# UI Framework backlog

Last updated: October 20, 2026.

This is the running, prioritized work list for the core library, WPF renderer,
framework tests, samples, documentation and package releases. GuidedGrade UI,
its package adoption, browser integration and grading workflows belong in that
application's backlog.

Start with FW-001. Work within each priority follows the order below; performance
and correctness requirements apply to every runtime change, regardless of order.
Dates below record evidence or updates, not delivery commitments.

## Work queue

| Order | ID | Priority | Work | Status |
| ---: | --- | --- | --- | --- |
| 1 | FW-001 | P0 | Release the implemented native style and compact input fixes | In progress |
| 2 | FW-002 | P1 | Fix compact button padding and glyph clipping | Open |
| 3 | FW-003 | P1 | Investigate direct comparison timing increases | Investigation |
| 4 | FW-004 | P1 | Measure native style isolation with representative islands | Open |
| 5 | FW-005 | P1 | Strengthen editing through virtualized row recycling | Open |
| 6 | FW-006 | P1 | Expand UI Automation and accessibility coverage | Open |
| 7 | FW-007 | P2 | Improve large keyed shuffles | Open |
| 8 | FW-008 | P2 | Add a text submit callback | Proposed |
| 9 | FW-009 | P2 | Evaluate an editable picker API | Proposed |
| 10 | FW-010 | P2 | Evaluate asynchronous native host integration gaps | Investigation |
| 11 | FW-011 | P2 | Benchmark and prototype opt-in lazy computed values | Investigation |
| 12 | FW-012 | P2 | Reconcile stale roadmap and release claims | Open |
| 13 | FW-013 | P3 | Evaluate additional reusable control APIs | Deferred |
| 14 | FW-014 | P3 | Evaluate general animation and other rendering backends | Deferred |

P0 finishes the pending release; P1 addresses known correctness, measurement and
usability gaps; P2 improves scale and APIs; P3 needs a concrete use case before
implementation. Proposed and Investigation items do not commit to a new API.

## Completion criteria

### FW-001 Release the implemented fixes

The style boundary and compact input padding fix are implemented in `8bb20a2`.
Release preparation started October 20, 2026 for `0.1.0-alpha.4`. Publication
and fresh NuGet.org installation remain pending; this item is not Done.
Fresh local Release, all 106 tests, 15 visual checks, 21 stress assertions and
package-only checks for both overloads, scope changes, compact input and cleanup
have passed. Hosted release gates are next. See the
[release record](releases/0.1.0-alpha.4.md).
The October 11 [validation](native-style-validation.md) passed 106 correctness
tests, 15 visual checks, 21 stress assertions and all configured performance
gates. Direct comparison timing concerns remain tracked by FW-003.

- Assign a fresh prerelease version to both packages and update release metadata.
- Verify the new overload and existing overload in a fresh package-only consumer,
  including compact input rendering and native identity, selection and cleanup.
- Run the [release workflow](releasing.md), including its correctness, package
  and performance gates, against the exact release commit.
- Publish both packages and symbols through Trusted Publishing, then verify
  installation from NuGet.org using an empty cache and record the release evidence.

### FW-002 Fix compact button clipping

Make compact controls respect their native padding and intentional dimensions.
Cover small close/icon buttons, complete glyph visibility, rich content,
keyboard focus, light/dark themes and display scaling. Preserve normal button
appearance and accessibility. The completed input fix does not resolve this
separate [button chrome limitation](layout-styling.md).

### FW-003 Investigate timing increases

Reproduce and profile the immediate-predecessor comparison: the seven-sample
repeat measured virtualized updates +7.66% and themed updates +6.55%, within
existing budgets. Preserve the earlier runs and investigate variance before
assigning a cause. Attempt a focused improvement where the evidence identifies
one; record improvements, regressions and inconclusive results. Do not advance
the accepted baseline or relax budgets to clear this item.

### FW-004 Measure native style isolation

Add a representative native-island workload that exercises the opt-in scope,
retained updates, outer theme replacement, scope changes and removal. Measure
mount/update time, UI-thread allocations and component work. Keep the existing
full-list, virtualized, themed and editor workloads alongside it. The existing
benchmark does not measure the cost of using `PreserveNative` in a specialist
island. Validate lifecycle and local style precedence after any optimization.

### FW-005 Strengthen virtualized editing

Define the behavior of active edits when a row leaves the viewport. Exercise
actual recycling, forced regeneration, scrolling, reorder, removal and disposal
with selection, focus and IME start/update/commit/cancel. Ensure edits never
reach a recycled row's former binding and stale focus work cannot steal focus.
Keep the existing retained-state and lifecycle contracts explicit. See
[virtualization](virtualization.md).

### FW-006 Expand accessibility coverage

Add focused UI Automation checks for virtualized item discovery, names, states,
keyboard navigation and focus changes. Verify visible-window screen-reader
behavior, high contrast and scaling for changed templates. Record manual
acceptance separately from automated peer tests. Existing `AccessibilityLabel`
support and the alpha.3 manual acceptance are already delivered; this item
extends coverage rather than reopening those completed changes.

### FW-007 Improve large keyed shuffles

Measure the collection-edit cost of large reorder/filter workloads, then reduce
the documented potentially quadratic move work without losing identity,
bindings, selection, state or cleanup. Compare latency, allocations and
component work with the same full-list and virtualized data and operations.

### FW-008 Add text submission

Define and implement a TextField submit/Enter callback with current callback
replacement, enabled-state and disposal tests. Specify behavior during IME
composition and distinguish submission from multiline TextEditor newlines.
Preserve the existing text binding contract.

### FW-009 Evaluate editable pickers

Define free-text versus selected-item state, selection identity, normalization,
keyboard behavior and popup ownership before introducing an API. Preserve the
existing indexed, non-editable Picker contract. Implement only after the design
has focused correctness, theme and accessibility acceptance criteria.

### FW-010 Evaluate asynchronous native hosting

Use a framework-owned integration sample to test native initialization that
completes after unmount, initialization failure, retained identity, explicit
disposal, focus, child popup ownership and sizing/overlay boundaries. Establish
whether the existing callbacks suffice. Add a framework API only for a
reproduced host-contract limitation; keep platform dependencies outside core.
Application login/SSO, browser navigation and grading integration are out of
scope. See the [native host contract](native-interop.md).

### FW-011 Investigate lazy computed values

Follow the [reactivity experiment plan](reactivity-performance-investigation.md):
establish core-only baselines for repeated reads, chains, diamonds, fan-out and
dependency churn, then prototype opt-in caching while preserving current
`Computed` behavior for ordinary component inputs. Test batching, cycles,
exceptions, dependency changes, disposal and thread ownership. Adoption requires
measured benefits with the existing renderer workloads, not just fewer getters
or an unsupported parity claim with another signals implementation.

### FW-012 Reconcile documentation

Update stale implementation and package-availability claims in the roadmap,
layout/native-interop guides and release instructions. Clearly separate current
contracts from dated measurements. Keep this backlog as the work-status source,
retain specialized design documents, and synchronize the architecture graph
when responsibilities or APIs change. Do not recreate completed vertical-fill,
docking or splitter tasks.

### FW-013 Evaluate reusable controls

Collect concrete framework use cases for tabs, link presentation, content
alignment, trees, menus and progress controls. Distinguish outer frame alignment
(already supported) from content alignment. Prefer existing native interop
until the reusable API and ownership are clear; specialized timelines, charts
and dials need demonstrated demand.

### FW-014 Evaluate broader expansion

General animatable properties and non-Windows renderers remain deferred.
Require an architecture proposal, a real consumer need, a measurement plan and
an explicit scope decision before promoting either into implementation work.
Existing screen-entry transitions are already implemented.

## Definition of done

- Behavior changes have focused regression checks and updated API documentation.
- Runtime, renderer, control, layout, styling, state and virtualization changes
  follow [performance guardrails](performance-guardrails.md): same harness,
  machine, SDK, data and operations; alternating repeated raw runs; unchanged
  accepted baseline and budgets. Keep the full-list comparison and include
  themed controls when templates change.
- Report mount/update latency, UI-thread allocations and component work. Passing
  budgets is not proof of zero regression; explain measured increases and noise.
- Accessibility, application behavior and platform/core ownership are preserved.
- Mark implementation, validation, packaging and publication separately. Only
  mark an item Done when its completion criteria and evidence are recorded.

## Maintaining the list

Keep IDs stable. When starting work, change its status to In progress and record
an owner if assigned. Use Blocked only with a concrete dependency and next
action. Add discovered framework work here rather than leaving it only in chat.
After verification, move completed items to the completed-work table with the
date, commit, evidence and package version when applicable. Update the date at
the top whenever priority, scope or status changes. Do not delete completed IDs
or silently remove unresolved findings. Priorities are relative order, not
delivery dates.

## Completed work

Existing delivered capabilities include viewport virtualization, vertical fill,
Dock, SplitPane, retained native hosting, rich button content, accessibility
labels and the explicit application-owned editor cleanup policy. See the
[changelog](../CHANGELOG.md). The style boundary and compact input fix are
implemented but remain under FW-001 until publication.

| ID | Completed | Result | Evidence |
| --- | --- | --- | --- |
| — | — | Future completed backlog items will be retained here. | — |
