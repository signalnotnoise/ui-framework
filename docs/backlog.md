# UI Framework backlog

Last updated: October 29, 2026.

Alpha.6 libraries and symbols were accepted by NuGet from `fd72534` after
both hosted workflows passed (146 tests, 32 performance gates each). A fresh
NuGet.org installation passed; DLL hashes match the published artifacts. A separate local Release
build `0.1.0-alpha.6-local.1` is ready for GuidedGrade. [Release record](releases/0.1.0-alpha.6.md).
FW-005 and FW-006 are complete after the user confirmed passing physical IME
and Windows contrast checks on October 29. [Manual acceptance](manual-acceptance-2026-10-29.md).

This is the running, prioritized work list for the core library, WPF renderer,
framework tests, samples, documentation and package releases. GuidedGrade UI,
its package adoption, browser integration and grading workflows belong in that
application's backlog.

FW-003 was reopened October 24 and is now resolved with an identified startup
bottleneck, a tested initialization-order fix and passing local gates. No later
backlog item was started during that investigation. FW-005 and FW-006 now have user-confirmed physical acceptance and are complete.
FW-001, FW-002, FW-004,
FW-015, FW-016 and FW-020 are also complete within their recorded scopes. Work within each priority follows the order below; performance
and correctness requirements apply to every runtime change, regardless of order.
Dates below record evidence or updates, not delivery commitments.

## Work queue

| Order | ID | Priority | Work | Status |
| ---: | --- | --- | --- | --- |
| 1 | FW-010 | P1 | Verify asynchronous native host ownership | In progress |
| 2 | FW-018 | P1 | Verify resource lifetime and improve diagnostics | In progress |
| Done | FW-005 | P1 | Strengthen editing through virtualized row recycling | Done — user-confirmed physical IME pass |
| Done | FW-006 | P1 | Expand UI Automation and accessibility coverage | Done — user-confirmed contrast pass |
| Done | FW-022 | P1 | Identify editor mount cost and initialization-order effects | Done — exact-commit hosted gate passed |
| 3 | FW-007 | P2 | Improve large keyed shuffles | Done |
| 4 | FW-008 | P2 | Add a text submit callback | Proposed |
| 5 | FW-009 | P2 | Evaluate an editable picker API | Proposed |
| 7 | FW-011 | P2 | Benchmark and prototype opt-in lazy computed values | Investigation |
| 8 | FW-012 | P2 | Reconcile stale roadmap and release claims | Open |
| 9 | FW-017 | P2 | Establish cohesive theme and visual acceptance | Proposed |
| 11 | FW-019 | P2 | Validate independent sample adoption and upgrades | Proposed |
| 12 | FW-013 | P3 | Evaluate additional reusable control APIs | Deferred |
| 13 | FW-014 | P3 | Evaluate general animation and other rendering backends | Deferred |

P0 is reserved for release blockers. FW-003's startup bottleneck is resolved
locally; a new publication still needs exact committed-source CI and package
checks. Previously published alpha.5 evidence remains preserved. P1 addresses known correctness, measurement and
usability gaps; P2 improves scale and APIs; P3 needs a concrete use case before
implementation. Proposed and Investigation items do not commit to a new API.

## Draft v1 scope â€” October 23, 2026

Proposed direction for discussion: a stable declarative C# UI layer for Windows
and WPF, with retained components, predictable state/bindings, workspace layout,
virtualized lists, coherent styling and reliable native-control integration.
This is a scope proposal, not a v1 readiness claim or an implementation commitment.

Candidate release requirements: explain and control performance variability
(FW-003/004/007), define editing and IME behavior during recycling (FW-005),
verify keyboard/UI Automation and assistive-technology behavior (FW-006),
and establish API, visual, diagnostic and adoption contracts (FW-016â€“019).
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
| API and compatibility â€” FW-016 | Inventory public core/WPF APIs; compile and run a consumer upgrade | Explicit decision on direct View construction and supported extension points; approved API baseline; incompatible changes detected automatically | Reviewed preview contract and compatibility guards complete; binary upgrade/RC platform acceptance remains FW-019 | Core/API maintainer |
| Performance â€” FW-003/004/007 | Same-binary controls and predecessor comparisons; full-list, virtualized, themed, editors, native scopes and keyed shuffles | Report latency, UI allocations and component work; explain reproducible regressions and measurement uncertainty within defined workloads | FW-003 identifies/avoids cold WPF DPI discovery; 32 local accepted-source gates pass; native-scope measurement complete. FW-007 keyed shuffles complete; FW-022 hosted source-first editor gate passes 32/32. Local timing failures retained; broader memory/RC acceptance remains open | Performance maintainer |
| Virtualized editing â€” FW-005 | Edit while scrolling, recycling, reordering, filtering and removing rows; exercise IME composition | No write reaches a former item's binding; explicit tested focus, selection and IME commit/cancel/preservation policy | Provider completion before detachment and original-focus ownership implemented; real-window recycling, stale input and routed composition checks pass. User confirmed physical IME pass October 29; provider/build details not supplied | Codex / WPF/input maintainer |
| Accessibility â€” FW-006 | Keyboard-only workspace, virtual item discovery, screen reader, high contrast and scaling | Expected names, states, focus order and item access; actionable controls remain usable | Automated checks pass; user reported Narrator/scaling October 28 and contrast switching October 29 | Accessibility reviewer |
| Native ownership â€” FW-010/018 | Successful/failed creation, partial initialization, completion after removal, repeated mount/unmount | Explicit resource owner; detached controls cannot receive stale updates; cleanup and late completion obey documented ownership | Controlled async races and real-window shutdown pass; provider-specific/native airspace acceptance open | WPF interop maintainer |
| Memory â€” FW-018 | Sustained scrolling through new rows, navigation, removal and workspace disposal | Distinguish bounded native realization, O(item-count) descriptions and retained visited-row state; demonstrate release of removed/disposed resources | Repeated collectability, native buffers and visited-key measurements pass; representative memory budgets remain open | Runtime maintainer |
| Default appearance â€” FW-017 | Representative forms, compact toolbar, lists and split workspace under themes/scaling | Coherent typography/spacing and usable focus/disabled/error states; theme changes preserve input and compatible control identity | Compact glyph/layout checks pass; cohesive visual acceptance pending | Theme/accessibility reviewer |
| Adoption and upgrades â€” FW-019 | Clean published-package starter, previous-version upgrade, independent workspace and shutdown | Documented setup succeeds; supported interactions and cleanup pass; deployed package provenance is verified | Package smoke tests pass; independent upgrade acceptance pending | Release/sample maintainer |

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
retained content, focus and automation are covered at simulated 100â€“200% DPI.

The first direct full-list update gate failed (+28.86%). It is retained;
an identical-binary control showed substantial variability and a seven-sample
focused repeat did not reproduce it (âˆ’4.57% update median). FW-003 retains the
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
identical-binary control showed broad timing ranges and a âˆ’5.79% median
difference between paths. The flag is retained, not dismissed as proof of
noise. A seven-sample focused before/after repeat measured âˆ’4.57% full-list
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

Status: **Done**. Completed October 29, 2026; user-confirmed physical IME acceptance.

Started October 24, 2026 after commit/push `be6e1d1`. Owner: Codex (WPF/input maintainer).
Focus-driven native completion before detachment, source identity across native
event objects and original-focus ownership are implemented. Nested editors
finish before snapshots capture their final state and selection.
Real-window checks cover actual recycling, stale input/focus work, routed
composition lifecycle, nested lists and provider failure. All 134 regression
tests, 15 visual checks, 21 stress assertions and startup/edit/disposal validation
pass. Seven cases fail against unchanged predecessor framework sources and pass
with the fix. Three further native-shape cases fail against the intermediate
completion implementation and pass with the focus-loss correction. The
delayed-action manual acceptance window builds without warnings
and passes its window/binding/regeneration/scroll setup smoke test.

The first, pre-nested-ownership-fix seven-pair full comparison passes 31/32 checks: full-list mount +14.75% fails the
unchanged 10% budget. An identical-binary control passes; a seven-pair full-list
predecessor repeat passes with mount +4.38% and update +4.37%. Preserve the first
failure and timing uncertainty; this is not a clean full release gate or proof
of a speedup. Virtualized update in that initial run is -7.01%, allocations
+0.08%, with unchanged component work. A follow-up accepted-source run was stopped
to test/fix duplicate provider completion in nested lists; another was stopped
to correct native event-object and deferred-completion assumptions. Their partial
data/interruption records are retained and are not gates. Final-code accepted-source
validation (`performance-native-focus-final`) passes all 32 checks: updates
-31.01% full-list, -1.21% virtualized, -68.98% themed and -16.14% editors, with
unchanged component work. Editor mount +3.57% and mount allocation +0.14% remain
within their unchanged budgets. These cumulative accepted-source comparisons do
not isolate FW-005's contribution. No baseline or budgets were advanced.

The user explicitly confirmed passing the physical IME editing/recycling check
on October 29. This is separate from the routed composition tests and closes the
remaining acceptance requirement. Exact provider/build and per-case logs were
not supplied. See [manual acceptance](manual-acceptance-2026-10-29.md) and the
[implementation record](virtualized-editing-2026-10-24.md).

Define the behavior of active edits when a row leaves the viewport. Exercise
actual recycling, forced regeneration, scrolling, reorder, removal and disposal
with selection, focus and IME start/update/commit/cancel. Ensure edits never
reach a recycled row's former binding and stale focus work cannot steal focus.
Keep the existing retained-state and lifecycle contracts explicit. See
[virtualization](virtualization.md).

### FW-006 Expand accessibility coverage

Status: **Done**. Owner: Codex. Started October 26; completed October 29, 2026.

Current implementation provides logical virtual-row discovery, keyboard traversal
without wrapper focus stops, live scoped contrast colors and shared frozen
normal TextBox brushes. Theme publication batches resource updates, joining
caller-owned initialization and closing only scopes it owns.

**Latest validation:** all **146/146 framework tests pass**, including five
accessibility acceptance tests. The two formerly failing discovery tests also
pass in two additional fresh test hosts (2/2 each). The setup now queries native
UIA on an MTA client while the window dispatcher pumps, then refreshes cached
peers. Original discovery, realization and row-identity assertions are unchanged.
Build: zero warnings/errors. All 15 visual checks, 21 stress assertions,
actual-window startup/edit/disposal, 25 graph-tooling tests, performance-analysis
script checks, API compatibility guards and local package smoke validation pass.
See [diagnosis, correction and evidence](discovery-initialization-2026-10-28.md).

The latest seven-pair performance comparison remains **32/32 passing** against
the unchanged accepted baseline: editor mount allocations -3.72%, themed mount
allocations +0.20% (within 2%), component work unchanged. This correction changes
only tests and documentation, so no additional runtime benchmark was required.
See [resource batching and verification](theme-resource-batching-2026-10-28.md).

**Manual acceptance complete:** the user confirmed passing physical Windows
contrast switching on October 29, following reported Narrator and scaling passes
on October 28. FW-005 has its own separately confirmed IME pass. These are
user-reported results; exact build/theme and per-case logs were not supplied.
See [dated acceptance](manual-acceptance-2026-10-29.md).

Preserved investigation history:

- [October 26 automation/keyboard work](accessibility-2026-10-26.md): initial
  139-test pass, predecessor comparisons and unresolved timing variability.
- [Initial contrast implementation](accessibility-2026-10-28.md): 29/32 gates;
  a dynamic-resource alternative performed worse and was reverted.
- [Allocation cause](contrast-allocation-investigation-2026-10-28.md): per-control
  bound-brush clones identified with diagnostic frozen brushes; freezing alone
  disables live contrast and was not shipped as a fix.
- [TextBox prototype](textbox-contrast-2026-10-28.md): shared frozen normal brushes
  plus conditional contrast resources improved results to 30/32, leaving editor
  allocations roughly 245 KB over budget.
- [Residual phase investigation](theme-resource-batching-2026-10-28.md): theme
  publication caused repeated resource invalidation walks. Trigger removal was
  ineffective; batching saved about 7.5 MB of editor mount allocation and cleared
  both remaining allocation gates. All failed and diagnostic runs remain archived.

### FW-007 Improve large keyed shuffles

Status: **Done**. Owner: Codex. Started October 28; completed/last updated
October 29, 2026. Implementation and validation complete; not yet published.

- Indexed AVL storage removes linear rank searches and array shifts. Row-embedded
  links replace separate nodes and a redundant membership dictionary. Successor
  traversal and a read cursor serve unchanged/WPF reads. Removal/disposal sever
  links, verified by stale-iterator retention and membership tests.
- Initial mounts omit the permutation buffer. Optional component/focus snapshots
  share sparse storage; the presenter retains lifecycle and composition ownership.
  Per-row collection notifications remain. Batch initialization was rejected:
  lower mount allocations shifted Gen 1 GC pauses into the first update.
- Guarded single forward moves preserve active-editor safeguards. Own/nested
  focus, capture, pending restoration, composition and failures keep the safe path.
- Seven-pair 10,000-row mount allocations fall **680,216 bytes / 2.56%** versus the
  preceding guarded source, and **22,416 bytes** below phase one. This removes
  the previous approximately 0.66 MB mount-allocation regression. Real-window
  reversal/shuffle improve **84.16% / 72.31%** versus phase one; rotation improves
  **90.84%**. Versus the guard, updates range from -1.46% to +1.15%. At 1,000 rows,
  mount allocations remain 7,104 bytes / 0.13% above phase one; saved-state holder
  and detached-filter allocation increases are disclosed in the report.
- Full release validation passes: **159 tests**, **15 visual checks**, **21 stress
  assertions**, zero build warnings/errors, documentation checks and actual-window
  startup/editing/disposal. Final source manifests and supplemental snapshots match.
- Broad alpha.6 comparison is **31/32**, with editor mount time +14.74%. That
  workload does not use virtualization. A seven-pair same-binary control shows
  +6.39% mount variation; the separate unchanged-source editor confirmation passes
  **8/8** (mount -14.03%). Preserve the original failure; do not claim a retroactive
  32/32 broad pass or an editor speedup. Mixed virtualized updates improve **51.04%**
  and allocate **20.09%** less. Full-list update timing is +5.59%, within budget.
- Hosted verification on October 29: `5fd4a2a` passed all three Validate jobs,
  including API/package/graph checks and **32/32 accepted-baseline performance
  gates**. Hosted editor mount is -0.21%. See [FW-022 and hosted evidence](editor-mount-investigation-2026-10-29.md).
- Accepted revision and budgets are unchanged. These direct alpha.6/supplemental
  comparisons are not a public-release gate. Physical IME/contrast acceptance
  was subsequently confirmed by the user on October 29 (FW-005/FW-006).

Evidence and history:

- [Final allocation investigation, Git history and validation](fw007-mount-allocations-2026-10-29.md).
- [Immutable raw results and source snapshots](performance-evidence/2026-10-29-mount-allocations/README.md).
- [Guarded move and previous validation](fw007-guarded-move-2026-10-28.md).
- [Indexed investigation and preserved mixed-update failure](fw007-indexed-shuffles-2026-10-28.md).
- [Initial keyed-move work and composition-safe narrowing](fw007-keyed-shuffles-2026-10-28.md).

### FW-022 Identify editor mount cost and initialization-order effects

Status: **Done — exact-commit hosted gate passed**. Owner: Codex. Updated October 29, 2026.
FW-007 commit `5fd4a2a` passed [hosted Validate](https://github.com/signalnotnoise/ui-framework/actions/runs/40839920952):
all three jobs succeed, including API/package/graph checks and 32/32 accepted-baseline
performance gates. Hosted editor mount time is -0.21%; the local +14.74% failure
and focused confirmation remain preserved.

Measure host construction, native-source creation/attachment, themes, dispatcher
and layout with UI CPU/allocation and process GC telemetry. Compare host-first
with source-first initialization of the same 1,000-editor/50-update workload,
including both startup costs. Verify geometry and work; separate traced attribution
from untraced repeated comparisons. Profile the dominant phase before changing
framework behavior. Keep the accepted baseline and all budgets unchanged.

Finding: host-first editor construction repeats native DPI discovery before
HwndTarget cache initialization. Source-first avoids the sampled fallback and
reduces construction CPU. Seven untraced pairs measure mount **-8.43%**, mount
allocations **-0.60%**; updates are **+2.00%** and allocate **+0.87%**. Five of seven
mount pairs improve; all traced geometry/native-editor counts match. The prior
+14.74% source-version difference is not wholly attributed to this shared path.
Local validation passes: 159 tests, 15 visual checks, 21 stress assertions and
8/8 focused editor checks. The latter overlays the same harness and is not an
instrumentation-overhead comparison. Diagnostic analysis fixtures pass.
User authorized source-first as the benchmark default on October 29.
Protocol `source-before-editors-v1` applies to both framework revisions; the
runner rejects cold or traced editor reports. Full seven-pair revalidation passes
30/32: themed mount +13.94% and editor mount +10.97% exceed the 10% limit.
Editor update time improves 9.35% and allocations improve 3.78%. Themed mount
is slower in 6/7 pairs and editor mount in 4/7; variation does not clear either
failure. Preserve these local mount differences as diagnostic follow-up;
no automatic retry or baseline reset. The user designated GitHub Actions as
the release performance gate on October 29. Require the full hosted job on the
new exact commit; local timing failures alone will not block after it passes. Default/cold phase order and geometry
checks pass, retaining 1,000 native editors. After adoption, 159 tests, 15 visual
checks, 21 stress assertions and startup/editing/disposal pass. [Investigation and reproduction](editor-mount-investigation-2026-10-29.md).

Hosted completion: `f7c7320` passed [Validate run 40867541344](https://github.com/signalnotnoise/ui-framework/actions/runs/40867541344)
on October 29: all three jobs green, **32/32 performance gates**, 159 tests,
15 visual checks, 21 stress assertions, startup, API guards and package checks.
Themed mount **+2.36%** and editor mount **+2.59%** are within the unchanged 10%
budget. The user-approved hosted performance gate is satisfied for this exact
implementation. Local 30/32 failures remain diagnostic evidence; different hosted
SDK/runtime/hardware means this is not proof that background activity caused them.
FW-022 is complete within its investigation, protocol and validation scope;
FW-005/FW-006 manual acceptance remains separate. [Preserved hosted evidence](performance-evidence/2026-10-29-editor-hosted/README.md).

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

Status: **In progress**. Owner: Codex. Started October 29, 2026. Promoted to P1
and first in the active queue at the user's request. Initial slice: controlled
async completion/removal races, partial initialization and failure cleanup through
existing native callbacks. Keep native resources and cancellation application-owned.
Thirteen focused native/lifetime checks pass without framework runtime changes;
full Release validation passes 172 tests, 15 visual checks, 21 stress assertions
and startup/editing/disposal. Updated October 29, 2026: real-window focus, selection,
popup HWND release, resizing and managed overlay checks pass, alongside five
shutdown cases (late success/fault, cancellation, throwing cancellation callback,
timeout/retry). Existing callbacks suffice for this controlled WPF adapter.
Provider-specific/native-airspace acceptance or an explicit v1 scope decision
remains open. [Window and memory evidence](native-window-memory-2026-10-29.md).
[First-slice evidence](native-lifetime-2026-10-29.md).

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

Status: **In progress**. Owner: Codex. Started October 29, 2026. Promoted to P1
and second in the active queue, alongside FW-010. Initial slice: weak-reference
collectability with live external publishers, queued refreshes, native removal
and repeated disposal. Callback balance alone is insufficient.
The initial checks collect disposed hosts, visited native rows and late async
controls with external publishers/tasks still alive; an intentional unsubscribe
omission confirms probe sensitivity. Sustained budgets and diagnostic decisions
remain open. Updated October 29, 2026: three sustained runs pass across 756
workspace cycles and 57,456 native mounts including warmup, with zero tracked
survivors/owned native bytes after each batch and orderly dispatcher shutdown.
After visiting 500 keys, 500 logical states and 10 native editors remain; removing
keys releases state. Post-GC heap grows 25,344–77,168 bytes across measured batches;
process trends are observations, not approved budgets or whole-process leak proof.
[Window and memory evidence](native-window-memory-2026-10-29.md).
[First-slice evidence](native-lifetime-2026-10-29.md).

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

### FW-021 Correlate Git history and graph structure visually

Done locally October 27, 2026. Owner: Codex. Added file/concept history, a commit
timeline with change highlighting and two-revision relationship comparisons.
Separate bounded offline exports preserve canonical tracked graph data without
enclosing commit metadata. Deleted nodes, parallel references, file content/mode
changes and stable layout positions are retained. All 25 tooling fixtures pass,
including actual viewer handlers in a no-WebGL DOM fixture. Curated/generated
graphs and documentation checks pass. Browser visual review remains unperformed
because local-file URL policy blocks the browser tool; no workaround attempted.
No framework runtime, package, baseline, hook configuration or unrelated
FW-005/006 changes were altered by this item. Changes remain uncommitted.
See [usage](knowledge-graph/README.md#visual-git-history) and
[validation/limits](knowledge-graph/validation/2026-10-27.md).

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
| FW-020 | October 24, 2026 | Installed revision-aware bounded navigation, protected generation and preserved architecture/XML adapters. Committed/pushed in be6e1d1; browser inspection blocked. | [Guide](knowledge-graph/README.md), [validation](knowledge-graph/validation/2026-10-24.md) |
| FW-021 | October 27, 2026 | Added offline visual Git history, commit highlights and revision comparisons; 25 fixtures pass. Local changes uncommitted; visual browser review blocked by file-URL policy. | [Usage](knowledge-graph/README.md#visual-git-history), [validation](knowledge-graph/validation/2026-10-27.md) |
| FW-007 | October 29, 2026 | Committed/pushed as `5fd4a2a`; indexed keyed moves, active-editor safeguards and lower 10,000-row mount allocation. Hosted correctness/package/graph jobs and 32/32 accepted-baseline checks pass. Local timing caveats retained. | [Implementation/evidence](fw007-mount-allocations-2026-10-29.md), [hosted verification](editor-mount-investigation-2026-10-29.md) |
| FW-022 | October 29, 2026 | Identified cold DPI lookup, adopted measured source-first editor initialization and designated exact-commit hosted performance as release gate. `f7c7320` passes all three hosted jobs and 32/32 performance checks; local failures preserved. | [Investigation](editor-mount-investigation-2026-10-29.md), [hosted evidence](performance-evidence/2026-10-29-editor-hosted/README.md) |
| FW-005 | October 29, 2026 | Existing editing safeguards and automated checks supplemented by user-confirmed physical IME editing/recycling pass. | [Manual acceptance](manual-acceptance-2026-10-29.md) |
| FW-006 | October 29, 2026 | Existing accessibility/contrast implementation and checks supplemented by user-confirmed Windows contrast pass; earlier Narrator/scaling passes retained. | [Manual acceptance](manual-acceptance-2026-10-29.md) |
