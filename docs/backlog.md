# UI Framework backlog

Last updated: October 24, 2026.

This is the running, prioritized work list for the core library, WPF renderer,
framework tests, samples, documentation and package releases. GuidedGrade UI,
its package adoption, browser integration and grading workflows belong in that
application's backlog.

FW-003 was reopened October 24 and is now resolved with an identified startup
bottleneck, a tested initialization-order fix and passing local gates. No later
backlog item was started during this investigation. FW-001, FW-002, FW-004,
FW-015, FW-016 and FW-020 are also complete within their recorded scopes. Work within each priority follows the order below; performance
and correctness requirements apply to every runtime change, regardless of order.
Dates below record evidence or updates, not delivery commitments.

## Work queue

| Order | ID | Priority | Work | Status |
| ---: | --- | --- | --- | --- |
| 1 | FW-005 | P1 | Strengthen editing through virtualized row recycling | Open |
| 2 | FW-006 | P1 | Expand UI Automation and accessibility coverage | Open |
| 3 | FW-007 | P2 | Improve large keyed shuffles | Open |
| 4 | FW-008 | P2 | Add a text submit callback | Proposed |
| 5 | FW-009 | P2 | Evaluate an editable picker API | Proposed |
| 6 | FW-010 | P2 | Evaluate asynchronous native host integration gaps | Investigation |
| 7 | FW-011 | P2 | Benchmark and prototype opt-in lazy computed values | Investigation |
| 8 | FW-012 | P2 | Reconcile stale roadmap and release claims | Open |
| 9 | FW-017 | P2 | Establish cohesive theme and visual acceptance | Proposed |
| 10 | FW-018 | P2 | Improve runtime diagnostics and lifetime verification | Proposed |
| 11 | FW-019 | P2 | Validate independent sample adoption and upgrades | Proposed |
| 12 | FW-013 | P3 | Evaluate additional reusable control APIs | Deferred |
| 13 | FW-014 | P3 | Evaluate general animation and other rendering backends | Deferred |

P0 is reserved for release blockers. FW-003's startup bottleneck is resolved
locally; a new publication still needs exact committed-source CI and package
checks. Previously published alpha.5 evidence remains preserved. P1 addresses known correctness, measurement and
usability gaps; P2 improves scale and APIs; P3 needs a concrete use case before
implementation. Proposed and Investigation items do not commit to a new API.

## Draft v1 scope — October 23, 2026

Proposed direction for discussion: a stable declarative C# UI layer for Windows
and WPF, with retained components, predictable state/bindings, workspace layout,
virtualized lists, coherent styling and reliable native-control integration.
This is a scope proposal, not a v1 readiness claim or an implementation commitment.

Candidate release requirements: explain and control performance variability
(FW-003/004/007), define editing and IME behavior during recycling (FW-005),
verify keyboard/UI Automation and assistive-technology behavior (FW-006),
and establish API, visual, diagnostic and adoption contracts (FW-016–019).
Existing successful alpha.5 gates remain valid evidence for that preview;
they do not establish all of these broader guarantees.

Text submission (FW-008) is a strong candidate for a small v1 addition.
Editable pickers, lazy computed values, additional declarative controls,
general animation, hot reload and other backends require separate scope and
benefit decisions. They are not automatically v1 blockers. Keep native controls
available where specialized functionality already exists.

## Proposed v1 acceptance matrix

This matrix defines evidence to collect, not completed acceptance. Owners below
are responsibility roles; a named assignee must be recorded before work starts.
Each row needs links to dated evidence for the release candidate. Unresolved
numeric performance/memory targets must be established from representative
measurements before claiming v1 acceptance; existing budgets remain enforced.

| Contract / backlog | Workload | Required outcome | Evidence / current gap | Owner role |
| --- | --- | --- | --- | --- |
| API and compatibility — FW-016 | Inventory public core/WPF APIs; compile and run a consumer upgrade | Explicit decision on direct View construction and supported extension points; approved API baseline; incompatible changes detected automatically | Reviewed preview contract and compatibility guards complete; binary upgrade/RC platform acceptance remains FW-019 | Core/API maintainer |
| Performance — FW-003/004/007 | Same-binary controls and predecessor comparisons; full-list, virtualized, themed, editors, native scopes and keyed shuffles | Report latency, UI allocations and component work; explain reproducible regressions and measurement uncertainty within defined workloads | FW-003 identifies/avoids cold WPF DPI discovery; 32 local accepted-source gates pass; native-scope measurement complete. Keyed shuffles and broader memory/startup acceptance remain open | Performance maintainer |
| Virtualized editing — FW-005 | Edit while scrolling, recycling, reordering, filtering and removing rows; exercise IME composition | No write reaches a former item's binding; explicit tested focus, selection and IME commit/cancel/preservation policy | Focus snapshots exist; active composition and actual focused-row recycling need acceptance | WPF/input maintainer |
| Accessibility — FW-006 | Keyboard-only workspace, virtual item discovery, screen reader, high contrast and scaling | Expected names, states, focus order and item access; actionable controls remain usable | Automated peers provide partial evidence; dated manual acceptance required | Accessibility reviewer |
| Native ownership — FW-010/018 | Successful/failed creation, partial initialization, completion after removal, repeated mount/unmount | Explicit resource owner; detached controls cannot receive stale updates; cleanup and late completion obey documented ownership | Existing retained-host tests; failure/race integration and resource-retention evidence pending | WPF interop maintainer |
| Memory — FW-018 | Sustained scrolling through new rows, navigation, removal and workspace disposal | Distinguish bounded native realization, O(item-count) descriptions and retained visited-row state; demonstrate release of removed/disposed resources | Callback balance is partial evidence; collectability and retained-state budgets pending | Runtime maintainer |
| Default appearance — FW-017 | Representative forms, compact toolbar, lists and split workspace under themes/scaling | Coherent typography/spacing and usable focus/disabled/error states; theme changes preserve input and compatible control identity | Compact glyph/layout checks pass; cohesive visual acceptance pending | Theme/accessibility reviewer |
| Adoption and upgrades — FW-019 | Clean published-package starter, previous-version upgrade, independent workspace and shutdown | Documented setup succeeds; supported interactions and cleanup pass; deployed package provenance is verified | Package smoke tests pass; independent upgrade acceptance pending | Release/sample maintainer |

Basic actionable error context and clear ownership are v1 requirements. A full
rebuild inspector, sophisticated theme customization, broad control catalog,
general animation, hot reload, other backends and unproven lazy-computed
optimization can follow v1. Existing Computed reevaluation semantics must be
preserved unless a separately specified API introduces explicit caching.

Sequence: FW-016 API decisions and the reopened FW-003 causal investigation/fix
are complete within their documented scope. Then settle
editing/accessibility/native ownership guarantees, and prove the combined
contract through the reference workspace and release-candidate campaign.

## Completion criteria

### FW-015 Publish the compact button preview

Done October 23, 2026: `0.1.0-alpha.5` published from `fbf5204`. Both
hosted workflows, all 32 performance gates and fresh NuGet.org installation
passed. Compact-button package checks and hosted assembly matching passed.
Both packages and symbols published through Trusted Publishing. The accepted
performance baseline and budgets are unchanged.

- [x] Run local Release, correctness, visual, stress and isolated package checks.
- [x] Commit and push the release source; pass hosted correctness and performance gates.
- [x] Publish both packages and symbols through Trusted Publishing.
- [x] Verify fresh NuGet.org installation with isolated package/HTTP caches and
  compare installed assemblies with hosted artifacts. Record the release evidence.

See [release record](releases/0.1.0-alpha.5.md). The subsequent FW-003 investigation
is recorded below, including unresolved measurement uncertainty.

### FW-001 Release the implemented fixes

The style boundary and compact input padding fix are implemented in `8bb20a2`.
Done October 20, 2026: `0.1.0-alpha.4` was published from
`039d3bb220e2dbddd3a11f452b0a12a673836565`. Both hosted workflows and all
performance gates passed; Trusted Publishing pushed both packages and symbols.
Both public packages passed a fresh consumer with isolated package and HTTP
caches, and installed assemblies match the exact hosted artifacts.
Fresh local Release, all 106 tests, 15 visual checks, 21 stress assertions and
package-only checks for both overloads, scope changes, compact input and cleanup
have passed. Public indexing and installation verification also passed. See the
[release record](releases/0.1.0-alpha.4.md).
The October 11 [validation](native-style-validation.md) passed 106 correctness
tests, 15 visual checks, 21 stress assertions and all configured performance
gates. Direct comparison timing concerns remain tracked by FW-003.

- [x] Assign a fresh prerelease version to both packages and update release metadata.
- [x] Verify the new overload and existing overload in a fresh package-only consumer,
  including compact input rendering and native identity, selection and cleanup.
- [x] Run the [release workflow](releasing.md), including its correctness, package
  and performance gates, against the exact release commit.
- [x] Publish both packages and symbols through Trusted Publishing, then verify
  installation from NuGet.org using an empty cache and record the release evidence.

### FW-002 Fix compact button clipping

Done October 23, 2026 in `f917ca6`: source fix, 13 new regression cases, 119
passing correctness tests, 15 visual checks, 21 stress assertions and passing
accepted-baseline CI gates. Chrome respects native Padding and live bindings;
automatic native Height keeps the 36-DIP minimum, explicit Height permits
compact controls, and local MinHeight wins. Normal native theme spacing,
retained content, focus and automation are covered at simulated 100–200% DPI.

The first direct full-list update gate failed (+28.86%). It is retained;
an identical-binary control showed substantial variability and a seven-sample
focused repeat did not reproduce it (−4.57% update median). FW-003 retains the
timing concern. See [validation evidence](compact-button-validation.md).
Published in `0.1.0-alpha.5` from `fbf5204`; exact-source release gates and
fresh public package checks passed. See [release evidence](releases/0.1.0-alpha.5.md).

Make compact controls respect their native padding and intentional dimensions.
Cover small close/icon buttons, complete glyph visibility, rich content,
keyboard focus, light/dark themes and display scaling. Preserve normal button
appearance and accessibility. The completed input fix does not resolve this
separate [button chrome limitation](layout-styling.md).

### FW-003 Investigate timing increases

Done October 24, 2026 after reopening. Owner: Codex. WPF's process-DPI cache stayed unset before the first
presentation target, repeatedly opening process handles/querying DPI while
creating and reconciling the detached tree. Public target initialization removes
that path; no private cache, DPI policy, GC policy or ViewHost API is modified.
The actual sample now initializes its real window handle before ViewHost.
Seven actual-window startup pairs measured -19.80% time/-2.50% UI allocation,
with identical geometry/body work, working native editing and complete disposal.

Seven same-binary cold/initialized standard-workload pairs measured updates
-22.52% full-list, -28.40% virtualized and -24.13% themed, with lower allocations
and matching work. Corrected UI-thread phase profiles identify the avoided
resource/DPI fallback; actual contention events do not support seconds-long
managed lock waits. The normal harness now initializes both revisions equally,
includes its cost, versions the protocol and retains explicit cold diagnostics.
All 32 gates against unchanged accepted source 78c88fb pass; data sizes,
operation counts and budgets are unchanged. Historical failures remain failed.
See [causal finding, fix and raw evidence](timing-bottleneck-2026-10-24.md).

Validation: zero-warning Release, 119 tests, 15 visual checks, 21 stress
assertions, new actual-window startup integration, phase/suspension fixtures,
documentation and curated/generated graph checks passed. Code and evidence
remain uncommitted; no package was changed or published. This resolves a
demonstrated initialization bottleneck, not exact attribution for every
historical noisy sample or broader physical-DPI/consumer acceptance.

Reopening request and acceptance criteria (preserved): October 24, 2026. The bounded
October 23 investigation did not identify the bottleneck; that is insufficient
to move on. The October 24 +43.29% full-list failure remains failed; the
identical-binary -26.41% difference demonstrates variability, not its cause.
Keep FW-005 and subsequent work queued until this investigation identifies and
explains the costly path and validates any proposed fix. No release clearance,
baseline promotion, workload reduction or budget exception is authorized.

Current approach: record mutation, dispatcher/render and layout phase elapsed
time, UI-thread CPU, allocations and GC activity; correlate fast/slow phases
with UI-thread sampled stacks and runtime events. Test a specific hypothesis
with an isolated intervention before claiming a cause. Preserve failed,
negative and inconclusive experiments. Complete only with a supported causal
conclusion, a tested focused fix where practical, and repeated unchanged-workload
performance/correctness evidence; otherwise leave this item In progress.

Historical October 23 investigation (owner: Codex). Three full seven-sample
campaigns, separate sampled-thread profiles and a presentation-context prototype
are preserved in [the investigation report](timing-variance-investigation.md).
The first fresh direct run failed themed mount/update gates (+26.48%/+117.30%);
its complete failed evidence is retained. A full matched repeat passed all 32
gates, with themed updates -1.67%. The original +28.86% full-list increase did
not recur in either fresh direct run (-1.23%, +2.66%). Identical binaries produced
+8.06% virtualized and -13.48% themed update differences; this control is never
release eligible. All campaigns retained matching component work and original
budgets. The precise source of timing bursts remains inconclusive.

Added verified binary controls, process CPU/wall telemetry, complete API build
inputs in source capture, and descriptive paired diagnostics with CI fixtures.
The hidden-presentation prototype is promising single-profile evidence only;
it is not a shipped renderer optimization or a replacement release workload.
Follow repeated real-presentation validation through FW-004/FW-019 before
adopting that measurement change. Failed ordinary gates always remain failures;
reopen investigation for sustained source-related increases.

Validation: zero-warning Release build, 119 tests, 15 visual checks, 21 stress
assertions, paired-diagnostic tests and curated/generated graph checks passed.
No runtime implementation or accepted baseline changed. This closes the bounded
investigation with documented uncertainty, not all v1 performance acceptance.

Original scope and historical observations (retained):


Reproduce and profile the immediate-predecessor comparison: the seven-sample
repeat measured virtualized updates +7.66% and themed updates +6.55%, within
existing budgets. Preserve the earlier runs and investigate variance before
assigning a cause. Attempt a focused improvement where the evidence identifies
one; record improvements, regressions and inconclusive results. Do not advance
the accepted baseline or relax budgets to clear this item.

October 23 follow-up: FW-002's first local direct run flagged unthemed full-list
updates (+28.86%) with essentially unchanged allocations/work. A seven-sample
identical-binary control showed broad timing ranges and a −5.79% median
difference between paths. The flag is retained, not dismissed as proof of
noise. A seven-sample focused before/after repeat measured −4.57% full-list
update time with wide timing ranges, and all accepted-baseline CI gates passed.
Investigate the unstable measurements without claiming a reliable speedup or
zero regression; see [compact button performance investigation](compact-button-validation.md).

### FW-004 Measure native style isolation

Done October 24, 2026. Owner: Codex. Added an opt-in 32-island workload with
separate retained-update, theme-replacement, scope-transition and removal phases.
Seven alternating same-binary scope pairs measured preserve-first updates
-41.82% time/-20.36% UI allocations; theme replacement -74.14% time. Root/native
work matched; identity, selection, local styles and all 112 releases passed.
Restoring the preserved scope allocated +2.73% in that phase. These compare
different intentional style policies, not a new renderer optimization.

The alpha.5 revision campaign kept all four existing workloads and failed
full-list update time (+43.29%); 47/48 checks stayed within budget. Its failed
evidence is retained. An identical-binary full-list control produced an apparent
-26.41% update difference, demonstrating variation without assigning the exact
cause or clearing the failure. Future release gates remain mandatory.

Seven unprofiled presentation-context pairs measured -31.96% update time but
+20.33% mount time, +63.49% mount allocation and +3.79% update allocation. The
prototype is not adopted. Geometry/DPI/visible-window acceptance and explicit
startup boundaries remain FW-019 work. No runtime, baseline or budget changed.

Validation: zero-warning Release, 119 tests, 15 visual checks, 21 stress
assertions, paired-analysis fixtures, syntax and both graph checks passed.
The native scenario is compiled out of ordinary runs, preserving accepted
alpha.1 harness build compatibility. Supplemental and focused diagnostic runs
cannot qualify a release. See [methods and evidence](native-island-performance.md).

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

### FW-016 Define v1 API compatibility and support contracts

Done October 23, 2026. Owner: Codex. Retained direct View construction,
published namespaces and existing behavior; documented supported extension
boundaries, platform/runtime policy, compatibility/deprecation and migration rules
in [the API contract](api-contract.md). Added 266 core and 25 WPF declarations
with nullability/defaults to reviewed baselines, private pinned API analyzers,
and build errors for unreviewed additions/removals. CI exercises both negative
guards; the existing Native optional-overload exception is narrowly scoped.

Validation: zero-warning Release build, 119 tests, 15 visual checks and 21 stress
assertions passed. Both guard probes failed as expected and the restored baseline
built successfully. [Evidence](api-evidence/fw016/) preserves logs and results.
No runtime implementation changed; no new performance measurement is claimed.
Binary consumer upgrades and the concrete v1 RC platform matrix remain FW-019
acceptance work. This establishes the reviewed preview contract, not v1 release.


Proposed October 23, 2026 after reviewing the public core/WPF surface, project
packaging and CI. Decide which types and behaviors are supported contracts,
including identity, lifecycle, snapshots, threading, binding readback, sizing,
style precedence and failure recovery. Audit naming before freezing it; do not
rename APIs merely to make names uniform. Establish supported platform/runtime
policy, compatibility expectations, deprecation guidance and migration examples.
Add an approved public API inventory and automated compatibility checks before
a stable release. Existing functional/package checks do not freeze public APIs.

### FW-017 Establish cohesive theme and visual acceptance

Proposed October 23, 2026. Build on existing ThemeTokens, native style isolation
and corrected compact controls. Define a consistent typography/spacing/density
system with light/dark appearances and complete interaction states. Verify
representative forms, compact toolbars, lists and split workspaces with long
text, disabled/focused/validation states, high contrast and display scaling.
Review real-window captures alongside layout assertions; a template that fits
its glyph is not by itself a visually coherent application. Integrate with
FW-006 accessibility acceptance and retain theme/local-value precedence.

### FW-018 Improve runtime diagnostics and lifetime verification

Proposed October 23, 2026. Existing failure recovery and benchmark tooling are
implemented; evaluate what application developers still need to locate invalid
views, unexpected rebuilds and rendering/cleanup failures. Design opt-in,
low-overhead diagnostics with actionable component/key context and explicit
ownership. Measure enabled and disabled overhead if instrumentation is added.
Extend sustained mount/unmount, navigation, visited virtual-row state and native
island verification to detect retained subscriptions, controls and application
resources after disposal. Do not mistake balanced callback counts for proof of
collectability, or add automatic recovery that masks application errors.

### FW-019 Validate independent sample adoption and upgrades

Proposed October 23, 2026. Maintain a framework-owned workspace sample restored
only from published packages, exercising forms, lists, editing, split panes,
navigation and at least one representative native island. Verify a clean setup
and upgrade from the previous supported version, including disposal and the
application-owned cleanup policy. Document supported behavior and acceptance
limits, with visible interaction and representative latency/allocation/memory
evidence. Keep application-specific grading/browser workflows in their own
backlogs. Existing package smoke checks and GuidedGrade adoption are useful
inputs, not a substitute for a reproducible independent acceptance fixture.

### FW-020 Update revision-aware repository graph navigation

Done October 24, 2026. Owner: Codex. Applied the updated generate skill's bounded
revision/history queries, fingerprinted disposable cache, protected index hook
and marked AGENTS entry. Preserved all 33 prior architecture concepts/schema v1,
XML project references, existing viewer/vendor/license, hook configuration and
CI; added navigation ownership as concept 34 through a schema-v2 adapter.
No application/runtime code or real index entries changed in this task.

Validation: 17 full-suite fixture tests plus the added cache-identity test
passed (18 cases), and the unmodified skill bundle passed its 12 tests. Live
current/historical queries, curated/generated worktree checks and documentation
validation passed. Browser visual inspection was blocked by local-file URL
policy and remains unperformed; no workaround was attempted. See
[validation and limitations](knowledge-graph/validation/2026-10-24.md).

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
[changelog](../CHANGELOG.md). The style boundary and compact input fix shipped
in `0.1.0-alpha.4` under FW-001.

| ID | Completed | Result | Evidence |
| --- | --- | --- | --- |
| FW-001 | October 20, 2026 | Released `0.1.0-alpha.4` from `039d3bb`; packages and symbols published, public installation verified. | [Release record, workflow and checksums](releases/0.1.0-alpha.4.md) |
| FW-002 | October 23, 2026 | Fixed compact native button chrome in `f917ca6`; released in alpha.5 from `fbf5204` after correctness, CI and public package checks passed. Timing concerns retained under FW-003. | [Validation, renders and raw performance evidence](compact-button-validation.md) |
| FW-015 | October 23, 2026 | Released alpha.5 from `fbf5204`; both packages and symbols, hosted gates and fresh public assembly matching passed. | [Release record and evidence](releases/0.1.0-alpha.5.md) |
| FW-016 | October 23, 2026 | Reviewed API contract and 291 declarations; build/CI guards reject additions and removals; correctness checks pass. | [Contract](api-contract.md), [evidence](api-evidence/fw016/) |
| FW-003 | October 24, 2026, after reopening | Identified repeated native DPI discovery before WPF target/cache initialization; fixed sample startup order and made benchmark initialization symmetric. Seven-pair causal/startup verification, 32 unchanged accepted-source gates and correctness pass. Earlier failed/invalid evidence retained. | [Cause, fix and verification](timing-bottleneck-2026-10-24.md), [historical report](timing-variance-investigation.md) |
| FW-004 | October 24, 2026 | Measured native scope phases, retained identity/styles and exact release balance; existing PreserveNative policy lowers measured theme-update cost. Full-list failure and presentation startup tradeoff retained; no runtime change or release clearance. | [Methods, repeated runs and validation](native-island-performance.md) |
| FW-020 | October 24, 2026 | Installed revision-aware bounded navigation, protected generation and preserved architecture/XML adapters. Fixture and worktree checks pass; browser inspection blocked. Uncommitted, unstaged. | [Guide](knowledge-graph/README.md), [validation](knowledge-graph/validation/2026-10-24.md) |
