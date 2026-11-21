# UI Framework backlog

Last updated: November 21, 2026.

FW-040/FW-041 shipped in 1.3.0 on November 21 from 9aad40d. Both hosted
workflows passed, including 32/32 checks each against unchanged baseline and
budgets. Fresh public restore and exact DLL/README verification passed.
[Release record and archived evidence](releases/1.3.0.md).

Version 1.2.0 libraries and symbols were published November 20 from `5be84d6`.
Local and both hosted full comparisons pass 32/32 checks against unchanged
accepted baseline and budgets. Fresh public icon/token installation and 14
source/binary upgrade processes pass; installed DLLs and embedded READMEs match
publication artifacts. [Detailed release record](releases/1.2.0.md).
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
FW-010, FW-015, FW-016, FW-017, FW-018, FW-019 and FW-020 are also complete within their recorded scopes. Work within each priority follows the order below; performance
and correctness requirements apply to every runtime change, regardless of order.
Dates below record evidence or updates, not delivery commitments.

## Work queue

### FW-041 Clarify compact and flat icon-button sizing

Done November 21, 2026. P2. Follow-up FW-040 is also complete below.
Owner: framework documentation / WPF styling maintainer. Follow-up to FW-037/038.
Prioritize this bounded clarification first: it addresses an existing consumer
integration without first extending the public API or changing sizing semantics.

GuidedGrade reports that IconButton.Width(32) clipped icons under its scoped
native Button template with inherited padding. Removing native padding reportedly
left a 16-DIP native width despite the 32-DIP outer frame. Explicit native sizing,
zero padding and transparent borders with hover/focus feedback resolved its
320/800-wide editor captures. This is consumer-reported evidence from its 1.2.0
integration; captures have not been independently inspected in this task, and
it is not a reproduced framework-only defect. No package changes are claimed.
Current ViewHost source applies DesiredWidth to Node.Frame, not directly to the
native Button; native content, padding, alignment and template constraints matter.

Source investigation: Node creates declarative Buttons with local padding and
left alignment; ordinary style padding setters therefore cannot override it.
The compiled toolbar-only recipe now uses explicit native dimensions and a
Loaded padding-ownership handoff. It keeps the base template and follows system
surface colors in contrast mode. All 43 focused cases pass (10 new sizing/recipe
cases); 24 captures are retained with representative inspection. Three reference
fixtures and reference/documentation checks pass. Runtime and performance inputs
are unchanged; both knowledge graphs validate.
[Evidence and limits](fw041-icon-button-sizing-2026-11-21.md).

Completion criteria:
- Document outer frame, native control, icon viewport and padding separately in
  docs/icons.md and docs/layout-styling.md; explain what Width guarantees.
- Verify a minimal compact/flat recipe under both the default theme and a scoped
  consumer-style template. Inspect wide/compact widths, scaling and contrast;
  preserve keyboard activation, accessible names, focus/hover/disabled feedback
  and an intentional hit target. A transparent border must not erase feedback.
- Reuse IconTests, CompactButtonTests and layout/style coverage; add only focused
  checks for uncovered behavior. Open a separately evidenced defect if a default-
  theme reproduction fails; do not silently change established Width semantics.
- Keep reusable sizes, spacing and styling defaults at their owning layer as
  described below; synchronize the framework skill/behavior examples with the
  verified recipe. Do not turn a consumer workaround into a universal requirement.

### FW-040 Add independently configurable declarative tooltips

Done November 21, 2026. P2. Implemented after FW-041; published in 1.3.0.
Owner: core API / WPF renderer maintainer. Follow-up to FW-037 and FW-016.
A concrete consumer needs descriptions and keyboard hints without a native
adapter; this is higher priority than speculative controls or lazy computation.

Scope correction from source review: IconButton already requires an accessible
name and uses it as its native tooltip. IconTests verifies that fallback and its
removal when content changes to a labelled button. Published 1.2.0 has no separate
public tooltip description/modifier. The gap is explicit tooltip content for
Button/IconButton, independently of the accessible name, not total absence of
tooltips. GuidedGrade's pointer-event/drag adapter remains a separate concern.

Started with clean runtime sources at HEAD 09cd20b; the prior FW-041 staged
documentation/example/test work is preserved. Saved runtime source hashes before
editing. User selected Button/IconButton first. Implemented an additive text
modifier with explicit override, null restoration and empty suppression; native
presentation/timing defaults are retained with no new policy constants. A small
renderer helper now owns tooltip precedence, separate from icon content.
All 229 framework checks pass, including seven focused tooltip cases and native
focus traversal in a real offscreen window. API guards and isolated local package
installation/update checks pass. Seven alternating sample pairs against pre-change
09cd20b pass 32/32 local comparisons; timings range -3.83% to +3.04%, allocations
+0.01% to +1.03%, with unchanged component work. This is measured overhead within
unchanged budgets, not zero regression or hosted release acceptance.
See [tooltip contract](tooltips.md) and [validation/evidence](fw040-tooltips-2026-11-21.md).
Physical keyboard input, visible popup and screen-reader acceptance are not claimed
from programmatic traversal/native policy assertions. The released 1.3.0 API inventory and generated skill reference now include the
new API; 1.2.0 consumers must upgrade before using it. Both knowledge graphs validated before closure.

Completed scope and acceptance criteria:
- Review whether a platform-neutral text description/modifier belongs on View
  generally or only supported actionable kinds. Define unspecified, explicit
  text and removal/suppression semantics before adding API. Preserve the existing
  icon-only accessible-name fallback unless explicitly overridden; tooltips must
  supplement accessible names rather than replace them.
- Keep the core free of WPF ToolTip objects. WPF owns native application, update,
  clearing and lifecycle. Decide precedence relative to IconButtonContent.Patch,
  which currently sets/clears ToolTip, and to native styling/interop values.
- Verify retained tooltip updates, transitions among plain/icon/combined buttons,
  row recycling/removal and cleanup without stale hints or remounts. Exercise
  keyboard access and applicable disabled-control/contrast behavior; document
  supported behavior without claiming physical screen-reader acceptance from
  property assertions. Extend existing focused tests rather than blanket guard tests.
- Review additive public API declarations/compatibility, update docs and generated
  skill API reference, and verify the packaged API. Keep application descriptions,
  localized strings and shortcut labels application-owned.
- Measure renderer changes before/after with the same harness, retaining full-list,
  virtualized and themed coverage, UI allocations and component work. Existing
  budgets and exact-candidate hosted release requirements remain unchanged.

Shared constants and ownership requirement for FW-040/FW-041 (user request):
Reuse ThemeTokens and its Icons/Spacing/Radii/Typography groups where the meaning
matches, and keep geometry defaults in IconDefaults. Do not scatter literal
sizes, gaps, padding, colors or tooltip timing values across renderer code, tests
and examples. Prefer native tooltip defaults where adequate; any necessary WPF-
specific policy belongs in one named renderer-owned location, not the core.
Consumer-only toolbar/hit-target metrics belong in a named sample/theme owner.
Do not couple unrelated roles merely because both currently equal 32, or create
an all-purpose constants bag. Acceptance should check ownership and override
behavior rather than duplicating production constants as expected values.

Shared implementation constraints for FW-040/FW-041 (user request): keep one
declared type per C# file, named after the type, including test helpers. Apply
single responsibility: core descriptions, WPF property/lifecycle behavior, styling
and consumer composition stay in their existing layers. Prefer the smallest
additive design; avoid speculative abstractions, parallel policy systems or broad
refactors. Ask for clarification when an unresolved public behavior or ownership
choice materially changes the feature; routine choices follow existing contracts.
Completion evidence for each item is recorded above; these constraints remain applicable to follow-ups.

### FW-039 Validate and publish icons and shared tokens as 1.2.0

Done November 20, 2026. Committed and pushed `5be84d6` to main, then published
1.2.0. Validate 44865868498 and Publish NuGet 44867087790 both pass, including
32/32 hosted performance checks each. The exact candidate also passes all 32
local accepted-baseline checks. Local release passes 212 tests and sample/stress/
cleanup checks. Fresh public installation, icon/token smoke and 14 source/binary
upgrade processes pass; public DLL/README and installed assembly hashes match
hosted artifacts. Both libraries and symbol packages were accepted. Skill and
shipped API reference are synchronized. [Release evidence](releases/1.2.0.md).

### FW-038 Shared design-token scales

Done November 20, 2026; published in 1.2.0 under FW-039. Added immutable `Spacing`,
`Typography`, `Radii` and `Icons` scales to `ThemeTokens`, with shared default
instances and explicit composition. Native icon-label spacing follows scoped
styles without replacing controls. Existing defaults and explicit view sizes
remain unchanged. Skill and installed copy now use the shared groups.
All 212 tests pass, including six token regressions; the extracted skill recipe
builds without warnings. Four default previews are byte-identical; custom scales
were inspected at wide/compact widths. All 24 local comparisons pass across
seven alternating pairs against the saved pre-token source; allocations change
by at most +0.14%, with unchanged component work. Both graphs validate.
[Evidence and limits](performance-evidence/2026-11-20-design-tokens/README.md).
The implementation evidence is local; FW-039 records subsequent publication gates.

### FW-037 Vector icons and icon buttons

Done November 20, 2026; published in 1.2.0 under FW-039. Added reusable immutable
`VectorIcon` definitions, `Icon`, icon/text `Button` and accessible `IconButton`.
WPF owns weak-key frozen geometry caching, retained drawing/content and native
button behavior. Shared defaults and skill guidance now use named values at the
owning layer. All 206 tests pass, including ten icon regressions. Inspected
wide/compact light/dark native captures; 24 local performance comparisons pass
across seven alternating pairs, retaining full-list and themed coverage.
Allocations increase at most 1.04%; component work is unchanged. Small timing
differences are inconclusive. [Evidence](performance-evidence/2026-11-20-icons/README.md).
Both knowledge graphs validate. FW-039 records subsequent hosted acceptance; no new physical Narrator acceptance
is claimed; full SVG loading and bundled icon collections remain outside scope.

### FW-036 Merge and publish animation support as 1.1.0

Done November 20, 2026. Owner: Codex. Merged animation support into main and
published 1.1.0 from `7c1fff5`. Shipped API declarations, skill and package docs
are current. Validate 44698350780 and Publish NuGet 44698350462 both pass all
32 hosted performance checks against the unchanged baseline. Fresh public
installation, animation smoke and 14 source/binary upgrade processes pass.
Public DLL/README hashes match hosted artifacts. FW-029 is also complete.
[Release evidence](releases/1.1.0.md).

### Agent skill and showcase phases

FW-030 follow-up — Done November 20, 2026: skill guidance prefers vector icons,
consistent existing icon sets, theme-aware colors and accessible icon-only actions.
WPF vector/SVG integration stays at the platform boundary; no new API is implied.
Repository and installed skill copies match; skill validation and graph checks pass.

FW-035 — Done (November 19, 2026): README leads with the Trace screenshot
linked to the committed 30-second walkthrough, Windows/stable .NET 10
prerequisites, the source-run command and skill installation links. The agent
pilot summary links its method and limitations. The committed MP4 matches the
FW-034 verified recording byte-for-byte. Documentation, reference freshness and
both graph checks pass. Scope is documentation/media, with no runtime/API
changes. The follow-up post is drafted locally at
`artifacts/showcase-review/reddit-followup-draft.md` for review and has not been
posted or included in the public repository.

FW-034 — Done (November 19, 2026): completed all four showcase follow-through items:
inspect exact-commit hosted CI and fix failures, exercise the live desktop
workflow, record a 20–30 second walkthrough, and retain a controlled same-model,
same-package coding comparison with and without the skill. Initial inspection:
`49c623a` passed hosted performance and graph checks; Windows validation failed
at the demo's keyboard-splitter assertion; the correction and acceptance follow.

- CI correction committed/pushed as `e624f76`: compare actual laid-out width,
  exercise Left/Right at wide and compact sizes, and check native/bound extent
  agreement. All 66 sample checks pass locally. [Hosted run 44522481239](https://github.com/signalnotnoise/ui-framework/actions/runs/44522481239)
  passes all three jobs on exact commit `e624f76`: graph checks, Windows
  validation (180 framework tests, 66 demo checks, full release validation,
  API guards and package checks) and the full unchanged-baseline performance gate.
  A separate local full-suite attempt aborted in
  `HeldPeerDoesNotRetainUnrelatedDisposedEditors` after its 60-second timeout;
  this is retained as an unresolved local result, not a passing suite. That
  timeout did not reproduce in the hosted full run.
- Agent pilot complete: same model/package/task, skill-backed first draft passes
  ten checks; no-skill draft fails compilation and still fails after one repair.
  [Method, limitations and raw-evidence location](agent-skill.md#controlled-first-draft-pilot--november-19-2026-fw-034).
- Live desktop pass completed after the user unlocked Windows: dragged both
  splitters, resized the inspector by keyboard, used Tab/Down/Space for severity
  and clear-filter controls, filtered during ingestion, selected an event,
  retained its inspector through filtering, paused, and parsed the copied JSON
  as the selected `evt-002373`. Compact window resizing, inspector scrolling,
  thin scrollbar dragging and source collapse/restore worked. No new demo
  defect was found. This is scoped live interaction evidence, not a Narrator,
  IME or full accessibility campaign.
- Recorded and inspected a continuous 30-second, target-window-only walkthrough:
  ingestion, search, event inspection, pause and Copy JSON. Local artifact:
  `artifacts/showcase-review/trace-walkthrough.mp4` (1424 × 862, H.264, 15 fps,
  450 decoded frames, 407004 bytes; no audio). SHA-256:
  `2292a85c65e6bd663bc56769bececf3f5364a2036502ca9e014bc01f1cf01a3c`.
  The original recording, encoder/decoder logs and inspected contact sheet are
  retained alongside it. This is live capture, not a still-image sequence or a
  performance measurement. Media remains local; no social post was published.

FW-033 follow-up — Done November 15: sample-owned slim dark vertical scrollbar
chrome replaces the bright default rails. All 60 native checks pass, including
page commands and thumb dragging; wide/compact captures are refreshed.
Framework templates remain unchanged.

- **Phase 1 — FW-030: Done.** Generated public API reference, compiled behavioral
  examples and release freshness checks. Commit `86295e4` passed full local and
  hosted validation. [Implementation and verification](agent-skill.md#phase-1-verification--november-15-2026-fw-030).
- **Phase 2 — FW-033: Done (November 15, 2026).** [Trace log viewer](../samples/LogViewer/README.md):
  compact dark theme, source/severity/text filtering, stable keyed virtual rows,
  retained selection/JSON inspector, split panes, bounded simulated ingestion
  and empty states. The solution builds warning-free; 60 native sample checks
  and all 180 framework tests pass. Wide/compact/empty captures were inspected;
  run/verify commands, ownership and raw sample diagnostics are documented.
  Both graph checks, reference freshness and documentation validation pass.
  No framework API/template changes, hosted benchmark claim or new physical
  accessibility acceptance. Prepared for commit/push November 17; no package
  publication or new hosted performance acceptance is claimed.
  No additional phases are agreed.

Related CI-cost work is tracked separately under FW-031/FW-032 below.

FW-032 — Done (November 15, 2026): locally reviewed `[docs only]` push marker
retains fast graph/documentation/reference checks and skips Windows release
validation and performance before runner allocation. Seven selector tests pass,
including rejecting code/tool/test edits under the stricter marker and checking
real staged/committed ranges. Reference, documentation and both graph checks
pass. [Usage and exclusions](releasing.md#fast-documentation-and-skill-content-pushes).
Unmarked pushes, PRs, manual and release workflows retain their full validation;
hosted validation of this workflow change is separate.

FW-031 — Done (November 15, 2026): local full-pushed-range review authorizes the
`[skip performance]` tip marker for known non-runtime changes. The push job
condition skips before runner allocation; unmarked pushes, PRs, manual runs
and publication retain full comparisons. Six selector tests pass, including
real staged/committed multi-commit ranges. Documentation and both graph checks
pass. [Policy and commands](releasing.md#decide-performance-coverage-before-pushing).
Baseline, budgets, workloads and correctness/package checks are unchanged;
hosted validation of this workflow change is separate.

FW-030 — Done (November 15, 2026): generated, version-labelled core/WPF shipped
API reference and compiled behavioral examples now ground the portable skill.
CI/release freshness gates are wired; 34 focused .NET tests, three generator
tests, documentation consistency and both graph validators pass locally.
[Completion evidence and commands](agent-skill.md#phase-1-verification--november-15-2026-fw-030).
Scope is tooling, tests and documentation; runtime and demo redesign are
unchanged. Full local release validation also passes: 180 tests, 15 visual
checks, 21 stress assertions and startup/native lifetime controls. Hosted CI
remains a separate exact-commit check after push.

| Order | ID | Priority | Work | Status |
| ---: | --- | --- | --- | --- |
| Done | FW-041 | P2 | Clarify compact/flat icon-button sizing and token ownership | Done — compiled recipe, 43 focused checks and inspected renders |
| Done | FW-040 | P2 | Add independent declarative tooltip descriptions | Done — 229 checks, API/package acceptance, 32/32 local comparisons; shipped in 1.3.0 |
| Done | FW-033 | P2 | Phase 2: multi-pane log viewer showcase | Done — 60 native checks, 180 regression tests and inspected captures |
| Done | FW-032 | P2 | Keep docs-only pushes on fast validation | Done — seven selector tests; documented local marker |
| Done | FW-031 | P2 | Choose push performance coverage locally before allocating runners | Done — six checks and documented marker policy |
| Done | FW-030 | P2 | Generate skill API reference and verify behavioral examples | Done — local contract and freshness checks pass |
| Done | FW-028 | P0 | Validate and publish v1 with end-to-end evidence | Done — both hosted gates and fresh public installation pass |
| Done | FW-027 | P1 | Package current source and validate against alpha.7 | Done — local validation, hosted gate separate |
| Done | FW-018 | P1 | Verify resource lifetime and scoped memory budgets | Done — 12/12 scoped memory runs |
| Done | FW-010 | P1 | Verify asynchronous native host ownership | Done — scoped native/WebView2 acceptance |
| Done | FW-005 | P1 | Strengthen editing through virtualized row recycling | Done — user-confirmed physical IME pass |
| Done | FW-006 | P1 | Expand UI Automation and accessibility coverage | Done — user-confirmed contrast pass |
| Done | FW-022 | P1 | Identify editor mount cost and initialization-order effects | Done — exact-commit hosted gate passed |
| Done | FW-007 | P2 | Improve large keyed shuffles | Done |
| 3 | FW-008 | P2 | Add a text submit callback | Proposed |
| 4 | FW-009 | P2 | Evaluate an editable picker API | Proposed |
| 5 | FW-011 | P2 | Benchmark and prototype opt-in lazy computed values | Investigation |
| Done | FW-012 | P2 | Reconcile stale roadmap and release claims | Done — November 15 evidence reconciliation |
| Done | FW-017 | P2 | Establish cohesive theme and visual acceptance | Done — scoped visual/error acceptance |
| Done | FW-024 | P2 | Publish alpha.7 | Done — publication history reconciled |
| Done | FW-019 | P2 | Validate independent sample adoption and upgrades | Done — published alpha.6 to alpha.7 |
| 12 | FW-013 | P3 | Evaluate additional reusable control APIs | Deferred |
| 13 | FW-014 | P3 | Opacity/translation first slice; evaluate broader expansion | First slice done locally; broader expansion deferred |
| Complete | FW-029 | P2 | Ship corrected embedded package README | Done November 20 — verified in public 1.1.0; 1.0.0 immutable |
| Later | FW-025 | P3 | Add contextual runtime failure diagnostics | Deferred — post-v1 |
| Later | FW-026 | P3 | Revalidate physical Narrator after peer-cache cleanup | Deferred — post-v1 follow-up |

P0 is reserved for release blockers. FW-003's startup bottleneck is resolved
locally; a new publication still needs exact committed-source CI and package
checks. Previously published alpha.5 evidence remains preserved. P1 addresses known correctness, measurement and
usability gaps; P2 improves scale and APIs; P3 needs a concrete use case before
implementation. Proposed and Investigation items do not commit to a new API.

## v1 scope and candidate preparation — updated November 15, 2026

Direction: a stable declarative C# UI layer for Windows
and WPF, with retained components, predictable state/bindings, workspace layout,
virtualized lists, coherent styling and reliable native-control integration.
The scoped implementation/acceptance items below are complete; this is not yet a
v1 release-readiness claim. Follow the [exact-candidate release sequence](releasing.md#next-release-candidate).

Candidate release requirements: explain and control performance variability
(FW-003/004/007), define editing and IME behavior during recycling (FW-005),
verify keyboard/UI Automation and assistive-technology behavior (FW-006),
and establish API, visual, lifetime and adoption contracts (FW-016–019).
Existing successful alpha.5 gates remain valid evidence for that preview;
they do not establish all of these broader guarantees.

Text submission (FW-008) is a strong candidate for a small v1 addition.
Editable pickers, lazy computed values, additional declarative controls,
general animation, hot reload and other backends require separate scope and
benefit decisions. They are not automatically v1 blockers. Keep native controls
available where specialized functionality already exists.

## v1 acceptance matrix

This matrix distinguishes completed scoped acceptance from future exact-RC gates. Owners below
are responsibility roles; a named assignee must be recorded before work starts.
Each row needs links to dated evidence for the release candidate. Unresolved
numeric performance/memory targets must be established from representative
measurements before claiming v1 acceptance; existing budgets remain enforced.

| Contract / backlog | Workload | Required outcome | Evidence / current gap | Owner role |
| --- | --- | --- | --- | --- |
| API and compatibility â€” FW-016 | Inventory public core/WPF APIs; compile and run a consumer upgrade | Explicit decision on direct View construction and supported extension points; approved API baseline; incompatible changes detected automatically | Reviewed preview contract and compatibility guards complete; published-preview binary upgrade acceptance complete; exact future RC/platform acceptance remains | Core/API maintainer |
| Performance â€” FW-003/004/007 | Same-binary controls and predecessor comparisons; full-list, virtualized, themed, editors, native scopes and keyed shuffles | Report latency, UI allocations and component work; explain reproducible regressions and measurement uncertainty within defined workloads | FW-003 identifies/avoids cold WPF DPI discovery; 32 local accepted-source gates pass; native-scope measurement complete. FW-007 keyed shuffles complete; FW-022 hosted source-first editor gate passes 32/32. Local timing failures retained; scoped memory acceptance complete; exact future RC validation remains | Performance maintainer |
| Virtualized editing â€” FW-005 | Edit while scrolling, recycling, reordering, filtering and removing rows; exercise IME composition | No write reaches a former item's binding; explicit tested focus, selection and IME commit/cancel/preservation policy | Provider completion before detachment and original-focus ownership implemented; real-window recycling, stale input and routed composition checks pass. User confirmed physical IME pass October 29; provider/build details not supplied | Codex / WPF/input maintainer |
| Accessibility â€” FW-006 | Keyboard-only workspace, virtual item discovery, screen reader, high contrast and scaling | Expected names, states, focus order and item access; actionable controls remain usable | Automated checks pass; user reported Narrator/scaling October 28 and contrast switching October 29 | Accessibility reviewer |
| Native ownership â€” FW-010/018 | Successful/failed creation, partial initialization, completion after removal, repeated mount/unmount | Explicit resource owner; detached controls cannot receive stale updates; cleanup and late completion obey documented ownership | FW-010 complete within documented standard-WebView2 scope: repeated interaction, lifetime/recovery and retention pass; permanent-stall policy defined. FW-018 scoped budgets pass; additional diagnostics deferred to FW-025 | WPF interop maintainer |
| Memory â€” FW-018 | Sustained scrolling through new rows, navigation, removal and workspace disposal | Distinguish bounded native realization, O(item-count) descriptions and retained visited-row state; demonstrate release of removed/disposed resources | Lifetime checks and all 12 frozen-budget native/browser runs pass November 15; [scoped evidence](fw018-memory-budgets-2026-11-15.md) | Runtime maintainer |
| Default appearance â€” FW-017 | Representative forms, compact toolbar, lists and split workspace under themes/scaling | Coherent typography/spacing and usable focus/disabled/error states; theme changes preserve input and compatible control identity | Complete within documented scope: eight inspected light/dark wide/compact renders, 44 focused tests, application-owned error contract; prior physical evidence retained ([acceptance](fw017-appearance-acceptance-2026-11-15.md)) | Theme/accessibility reviewer |
| Adoption and upgrades â€” FW-019 | Clean published-package starter, previous-version upgrade, independent workspace and shutdown | Documented setup succeeds; supported interactions and cleanup pass; deployed package provenance is verified | Independent NuGet-only alpha.6→alpha.7 source/binary acceptance passes; future exact v1 RC package acceptance remains ([evidence](fw019-package-adoption-2026-11-15.md)) | Release/sample maintainer |

November 15 user scope decision: FW-018 covers resource lifetime and scoped memory
budgets. Additional contextual error diagnostics (FW-025) and new physical Narrator
revalidation after peer-cache cleanup (FW-026) are separate post-v1 follow-ups, not
v1 blockers. Existing ownership, automated accessibility and prior manual evidence
remain required; no new physical Narrator pass is claimed. A full
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
October 29, 2026. Published in alpha.7 on November 13; publication status
reconciled November 15. [Release record](releases/0.1.0-alpha.7.md).

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

Completed November 11, 2026 within the [v1 native-provider and interaction contract](native-provider-stall-policy.md).
Final interactive acceptance passes **9/9** fresh-process cases: keyboard/three-theme
retention, airspace/resize, native/HTML pointer routing, scrolling and sampled
rectangular bounds. Full Release validation passes **172 tests, 15 visual checks,
21 stress assertions**, startup/editing/disposal and native-window lifetime checks.
Both knowledge graph checks pass. [Final acceptance and evidence](webview2-final-2026-11-11.md).

Permanent stalls have an explicit support limit: timeout reports and retains pending
ownership; close cannot be declared complete while provider work is pending. Hard
shutdown deadlines require app-owned process isolation, outside the v1 native-host
API. Standard WebView2 sibling-overlay/arbitrary-clip limitations are documented.
No framework API/runtime change was justified. GuidedGrade adoption and FW-018
budgets/diagnostics remain separate; this is not package/hosted release validation.

The dated notes below preserve the investigation history; the closure above is the
current acceptance status.

Resource-retention slice completed November 11: **six final runs pass**, fresh and
reused profiles, 270 parent/popup cycles including six negative controls, 540 owners
released exactly once, 1,350 captured process exits and 2,970 tracked references
collected. All 42 existing WebView2 cases also pass; two aggregate-error assertions
now reject unrelated extra cleanup errors. The initial cycle-count reporting
mismatch and raw runs are retained. Heap/handle trends are observations, not universal
budgets; FW-018 retains that work. Full Release validation passes 172 tests,
15 visual checks, 21 stress assertions and actual-window startup/disposal.
[Retention results and exception-test review](webview2-retention-2026-11-11.md).

November 11 continuation: **42/42 repeated WebView2 checks pass**, including 12 new
bounded-wait/late-success/late-fault and real script-close checks. The sample retains
pending ownership, reports deadline failures, rejects same-owner restart and drains
script-requested parent/child close exactly once. Controlled pending operations do
not prove recovery from a genuinely hung provider. Full Release validation passes 172 tests, visual checks, 21 stress assertions
and actual-window startup/disposal.
[Policy, scope and evidence](webview2-shutdown-2026-11-11.md).

Status: **Done**. Owner: Codex. Started October 29, 2026. Promoted to P1
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

Source review October 29: **GuidedGrade already has WebView2 integration**, pinned
to WebView2 1.0.4314.35 and framework alpha.7-local.1 in its dirty working tree.
[Reviewed implementation, source fingerprints and acceptance plan](webview2-review-2026-10-29.md).
App-side risks: pending initialization/popup tasks are not drained on close, popup
completion lacks closed-owner/error handling, and sequential cleanup can stop on
an exception. Child environment compatibility and event/resource release need
evidence. These are review findings, not reproduced framework defects.

Real-browser slice completed October 29: **15/15 cases** pass (three fresh-process
repetitions each) using local parent/child pages and isolated profiles, with WebView2
SDK 1.0.4314.35/runtime 156.0.4314.50. Tests cover environment-to-control application
boundary close, actual pending control initialization close, successful owned popup,
and parent/child close during actual pending child initialization. Deferrals complete,
late assignments are prevented, hosts release once and dispatcher cleanup completes.
Environment creation was synchronous here; its controlled boundary test is not
reported as a provider race. [Results and preserved evidence](webview2-acceptance-2026-10-29.md).

Interaction pilot October 29: forward/reverse WPF/browser Tab traversal, real text
input/Enter, and refresh/theme focus/selection retention pass once. Resize and
screen-pixel airspace checks also pass once: standard WebView2 covers WPF sibling
overlays; hiding it reveals the overlay, and a separate popup HWND appears above it.
[Interaction evidence and desktop-use limits](webview2-interaction-2026-10-29.md).
The initial foreground setup failure is retained. Further foreground tests stopped
when the user questioned desktop use; repetitions have not yet been run. Desktop
interaction was subsequently reauthorized for the recovery continuation. Interactive
cases still require opt-in and are excluded from the default runner. No framework
runtime or GuidedGrade code changed.

Failure/recovery slice completed October 29: **15/15 checks** pass (three profile
preflight checks plus twelve real missing-runtime/navigation/renderer/browser
failure-and-recovery cases). Original lifetime/popup cases also pass **15/15** on
the final sample. Crash cleanup now uses the cached subscribed core object; a known
invalid profile path is rejected before entering provider initialization. Earlier
invalid-profile timeout, DevTools command timeout and cleanup failures are retained.
[Detailed outcomes, changed probe and evidence](webview2-recovery-2026-10-29.md).
Generic stalled-provider policy remains unverified; no budget was relaxed. Full
framework regression passes 172 tests, 15 visual checks and 21 stress assertions.
User authorized desktop interaction for this continuation; these cases needed no
foreground input. Core/WPF runtime and GuidedGrade code are unchanged.

Final interaction and local-page resource acceptance are complete within the
linked policy. Broader budgets remain FW-018; forced provider termination is an
application/process-isolation responsibility, not a supported in-process promise.
GuidedGrade still needs application-side lifecycle hardening from the source review;
this standalone sample does not change or validate its current shutdown code.
No framework defect or new API requirement was reproduced. Framework runtime and
GuidedGrade are unchanged. The explicit browser command is not yet a CI release gate;
172 framework tests, 15 visual checks and 21 stress assertions pass during this work.
Existing hosted 32/32 performance evidence belongs to `64bd242`, not a browser benchmark.


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

Done November 15, 2026. Owner: Codex. Current README, release instructions,
roadmap, layout/native-interop/API/accessibility guides and backlog distinguish
published alpha.7, current uncommitted work and historical evidence. Recovered
alpha.7's release record plus 601 evidence files from `42d6617`; both exact-commit
GitHub runs and their 32/32 comparisons verified, and hosted/public DLL hashes
match the fresh FW-019 consumer. FW-024 closes on that verified history. Dated
failed experiments are retained, not rewritten as passes. New candidate and v1 RC
gates remain explicit; optional/post-v1 work is not promoted into blockers.
[Reconciliation audit](fw012-release-reconciliation-2026-11-15.md).
Documentation-only task; no runtime, baseline, version, publication or merge.

Original scope:

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

Status November 19, 2026: **First slice done locally; broader expansion deferred**.
Owner: Codex. Opacity/translation destinations, explicit timing/property masks,
lazy attached controller storage, native interruptible clocks, inherited motion
suppression and immediate lifecycle cleanup are implemented. All 196 Release
tests pass, including 16 animation regressions; API, documentation/reference and
both graph checks pass. Seven final alternating pairs pass all 24 local checks
against pre-feature HEAD. Full-list mount/update +3.11%/+4.99%, virtualized
mount/update +8.90%/-4.66%, themed +2.54%/+0.27%; update allocation +0.32–1.05%,
with identical component work. Initial full-list mount +19.56% failure is retained;
the focused revision removes duplicate Node storage and isolates setup from Patch.
Cross-campaign variation prevents attributing the timing change solely to that edit.
Seven isolated native-clock/immediate retarget pairs preserve raw cost: 32 frames,
240 updates, median 4.000/0.143 ms and 44.1/7.1 MB synchronous allocation.
Windows motion stayed disabled; the diagnostic explicitly bypasses policy only
inside its internal-controller workload. No FPS, physical acceptance, hosted
release or publication claim. Accepted baseline/budgets are unchanged.
[Contract](animation.md), [methods, results and preserved failures](animation-performance-2026-11-19.md).

November 19 skill follow-through: updated the portable skill with a focused
animation reference, source-versus-published API gating, destination-based usage,
lifecycle/reduced-motion rules and measured cost caveats. Synchronized the local
Codex installation after checking for local customizations. Skill validation,
generated-reference freshness, documentation and both graph checks pass. This
follow-through changes guidance only; it does not change runtime or performance
claims and does not require a new benchmark.

User authorized opacity/translation, lazy node controllers, native WPF clocks,
regression checks and disabled-path/rapid-retargeting measurements. Non-Windows
renderers and animation features beyond this slice remain deferred.
Further expansion requires a consumer need, measurement plan and explicit scope
decision. The user approved the first animation slice after the review below.
Existing screen-entry transitions are already implemented.

Earlier November 19, 2026: animation design review started at user request on
`codex/animations`; implementation was deferred pending the subsequent scope decision.
Reviewed core View descriptions, retained Node/Frame reconciliation, navigation
entry clocks, logical snapshots and virtual-row release. Proposed first slice:
explicit per-view opacity/translation targets with immutable duration/easing
metadata, WPF-owned interruptible clocks and reduced-motion policy. First mount
and virtual-row restoration settle at target; removal retains immediate cleanup.
Follow with opt-in entry effects; scope exit retention, layout motion, springs
and sequences separately. Candidate consumer: a retained Trace inspector detail
change, without animating its ingestion stream. Acceptance must cover retargeting,
unchanged refreshes, detach/disposal, recycling, motion suppression, focus/input
and native-host limits; measure disabled-path overhead and active frame behavior
alongside the existing full-list/virtualized performance baseline. This is a
proposal, not implemented behavior or measured performance evidence. Other
rendering backends remain deferred independently.

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

Status: **Done — scoped visual and error acceptance**, November 15, 2026.
Owner: Codex. [Acceptance and evidence](fw017-appearance-acceptance-2026-11-15.md).

- [x] Existing scoped states, compact sizing, wrapping, theme/local precedence,
  responsive/split behavior and FW-023 composition work credited.
- [x] Eight native renders individually reviewed: light/dark, 1040/760 DIP,
  normal/editor-focus appearance; form, compact toolbar, review list and split
  panes with long text, disabled action and explicit errors remain readable.
- [x] Validation/error contract documented as application-owned composition using
  existing text and accessible labels; invalid/valid transitions verified.
- [x] 222 fixture assertions and 44 focused styling/layout tests pass. Captures,
  source hashes, TRX and logs archived. Earlier compile failure retained.
- [x] Prior FW-006 physical contrast/scaling evidence retains its original scope.

The focus captures use an injected template state on a hidden HWND, not a new
physical focus campaign. No new Narrator, DPI matrix or hosted gate is claimed.
Core/WPF source hashes are unchanged; this task changes only acceptance tooling
and documentation, so no new performance campaign is required. FW-019 remains
sample adoption/upgrades; FW-025/026 are post-v1. The [earlier review](fw017-evidence-review-2026-11-15.md)
records the gaps before this continuation closed them.

Original scope retained for traceability:

Proposed October 23, 2026. Build on existing ThemeTokens, native style isolation
and corrected compact controls. Define a consistent typography/spacing/density
system with light/dark appearances and complete interaction states. Verify
representative forms, compact toolbars, lists and split workspaces with long
text, disabled/focused/validation states, high contrast and display scaling.
Review real-window captures alongside layout assertions; a template that fits
its glyph is not by itself a visually coherent application. Integrate with
FW-006 accessibility acceptance and retain theme/local-value precedence.

### FW-018 Verify resource lifetime and scoped memory budgets

Scope split November 15 by user decision: additional contextual diagnostics move
to FW-025; physical Narrator revalidation after peer-cache cleanup moves to FW-026.
Both are post-v1 follow-ups. Close FW-018 on scoped memory acceptance; the historical
entries below retain the original wider scope and its evidence.

Status: **Done — scoped lifetime and memory acceptance**, November 15, 2026.
All 12 fresh processes pass the numerical policy frozen before measurement:
six native runs at 500/5,000 rows (4,392 workspace cycles including warmup) and
six fresh/reused-profile browser runs (792 parent/popup cycles). Raw samples,
source/runtime agreement, ownership, reporting controls and shutdown validate.
Native input evidence is quiet. Native late heap growth is at most zero; browser
late heap growth at most 1,208 bytes. One browser run reaches the +16 late-handle
limit exactly; older calibration outliers remain flagged. No numerical limit,
existing performance baseline or production runtime source changed in this task.
Evaluator fixtures pass and are wired into CI; no hosted run is claimed.
[Budget contract](memory-budgets.md) · [Validation evidence](fw018-memory-budgets-2026-11-15.md).
FW-025/FW-026 retain the explicitly deferred post-v1 work.

#### Historical investigation record (original wider scope)

November 15 production peer-cache cleanup implemented and locally validated:
held row/list peers retain zero disposed editors (previously ten); a directly held
editor peer retains only its own editor, which collects after release. Three fresh
native runs pass 21 cases. All 176 tests pass, including four regressions that fail
against unchanged baseline binaries. Recycling/realization and key reuse remain
correct. The 15 visual checks, 21 stress assertions, real-window startup/disposal
and native-window shutdown also pass. Seven alternating pairs pass 32/32 performance checks versus pre-change
HEAD; allocations within 0.05%, component work unchanged, timing -3.79% to +4.91%.
Raw failures/prototypes are preserved. No accepted baseline/budget change or hosted
release claim. Physical Narrator revalidation and sustained external-client costs
remain unmeasured. FW-018 stays open for representative native/browser memory
budgets and contextual failure diagnostics. [Cleanup evidence](fw018-peer-cache-cleanup-2026-11-15.md).

November 15 plain-WPF comparison completed: all eight processes pass, covering
240 windows, 540 verified editor-focus acquisitions and 1,248 snapshots. Independent
plain WPF (no framework assembly loaded) and framework-native builds reproduce
identical fixed GDI deltas: 24 untouched / 30 focused during repeated window cycles,
14 untouched / 17 focused after UI-thread exit. No tracked survivors; no per-window
GDI accumulation in 30 cycles per process. The fixed +3 residual no longer provides
evidence of a framework-specific leak; exact identities remain unresolved. Preserve
raw counts and platform comparison; pursue deeper tracing if growth diverges from
plain WPF or violates representative budgets. Remaining FW-018 work: native/browser
budgets, production peer-cache acceptance and contextual failure diagnostics.
No runtime or baseline change. [Comparison](fw018-plain-wpf-focus-2026-11-15.md).

November 15 GuidedGrade-closed native tracing completed: four accepted campaigns validate
2,412 samples / 9,648 workspace cycles with zero tracked retained editors/resources.
The +6 GDI step reproduces without GuidedGrade. Identified objects include a font,
msctfui.dll text-input windows and a CoreMessaging helper. The font/input UI windows
disappear at dispatcher shutdown; CoreMessaging's window disappears at STA-thread
exit. Three aggregate GDI objects remain above the untouched reference after thread
exit and outside the partial shared-handle inventory. The fresh same-binary untouched
thread control passes with no input and GuidedGrade absent at every audit: final
10 GDI / 4 USER versus focused 13 GDI / 6 USER. Two excluded attempts (app at exit;
activation followed by user-requested stop) remain preserved and outside these totals.
No budget approval or production
lifecycle change. This preceded the plain-WPF comparison above; its fixed-residual
interpretation is superseded by that control. Peer-cache remediation and contextual
diagnostics remain separate. [Findings](fw018-guidedgrade-closed-resources-2026-11-15.md).

November 15 manual focus observation completed: user confirmed two clicks; the
first activation at batch 7 (before the prompt) includes pointer/client-area input
and reproduces +6 GDI/+9 USER. The second adds no GDI step. All 603 samples / 2,412
workspace cycles pass tracked ownership, reporting and shutdown checks. Final 100
batches plateau at 471 handles / 27 GDI / 25 USER; all editor survivors are zero.
Post-dispatcher counts remain above the shorter untouched reference. This supports
an activation/input initialization signature, not a proven title-bar-only cause or
fully attributed resource budget. Next: identify the actual native resources and
keep peer-cache retention remediation separate. [Findings](fw018-manual-focus-2026-11-15.md).

November 15 accessibility attribution: 21 controlled peer cases confirm a held
row/list/editor peer retains all 10 realized editors through cached children.
Releasing the root collects all; one-row cache refresh leaves nine and refreshing
all realized-row caches releases all. Native resources were already released.
Six alternating clean/UIA-warm soaks pass 1,098 raw samples / 4,392 workspace cycles.
Six historical native dumps have no editor/row-proxy instances. One warm run has
+6 GDI and a USER step exactly when activation/focus appears at batch 166, while
editor survivors remain zero. Activation/focus is now included in the summary's
interaction classification, with regression fixtures. A deliberate focus attempt
was blocked by Windows at batch 60; preserve it as failed setup, not a passing
focus check. Its matched control passes. All 172 framework tests and focused
negative/parser checks pass. No production cache fix or budget change. FW-018 stays
open for manual focus attribution, production cache-lifetime acceptance, budgets
and contextual diagnostics. [Findings](fw018-automation-retention-2026-11-15.md).

November 15 screenshot follow-up completed as one diagnostic pair: manual capture
and untouched control each pass 603 batches / 2,412 workspace cycles, with all
1,206 samples verified. Both finish -12 handles, flat GDI and -1 USER; no lasting
capture-related resource step reproduced. Median batch times 204.73 vs 196.92 ms
are not causal evidence from one sequential pair. Failed setup controls are
preserved: a sampled editor is rooted by a ref-counted accessibility ElementProxy
through cached virtual-row/textbox peers. Automation session reset preceded the
successful manual pair; external client identity remains unproven. Error diagnostics
now include survivor kinds and an opt-in failure checkpoint without relaxing checks.
Negative-retention, real-window and parser controls pass. FW-018 remains open for
interactive/accessibility retention, other growth, budgets and contextual errors.
[Manual screenshot comparison and evidence](fw018-snipping-comparison-2026-11-15.md).

November 15 continuation: tracing remaining handles using process-local creation
history in a separate diagnostic campaign; extending unpaused native/browser soaks
and row counts before proposing budgets. Sample runners now record requested
cycle counts and explicit case deadlines; defaults and accepted performance gates
are unchanged. Completion also requires a decision on actionable error diagnostics.
Creation-history controls validate handle/PID/open/close metadata, but Windows
returns empty stacks in this session. The browser's two persistent file handles
are identified as .NET 10.0.13 System.Private.Windows.Core.dll and
System.Windows.Primitives.dll, opened by the tiered-compilation worker. Native
added event/I/O-completion handles have runtime-worker creation IDs; exact helper
stacks remain unavailable. A broad file-query pilot timed out and is preserved;
the corrected probe queries only added disk files, never pipe state/data. Longer
unpaused checks have completed; no budget or leak-freedom claim yet. The first three
180-batch/500-row runs pass ownership checks; run 2 has a late +26 handle step
with additional GDI/USER objects. Preserve and investigate that outlier; it is not
an approved plateau. All three 5,000-row runs also pass tracked ownership and
visited/removal accounting (5,000 logical states, 10–11 native controls, then zero).
After the user reported hovering over the scrolling window, a bounded passive
window-input observer was added. It records counts/booleans only and never blocks
input or moves the pointer. One repeat records keyboard activity despite no mouse
entry; it is not classified as untouched. Input and activation evidence are kept
separate from ownership and memory-budget decisions.
The two repeat runs with no routed input have decreasing heap/handle counts and
no GDI/USER growth. All six 132-cycle browser runs pass ownership/shutdown;
whole-process handle changes remain +1 to +35 and need further attribution.
Combined evidence validates 15 runs and 2,442 spooled samples: 6,588 native
workspace cycles and 792 browser cycles. Full Release passes 172 tests, 15 visual
checks and 21 stress assertions; parser fixtures pass. FW-018 remains in progress:
controlled interactive resource growth, representative budgets and contextual
failure diagnostics are still outstanding. No release baseline or budgets changed.
[November 15 findings](fw018-long-memory-2026-11-15.md).

Heap/handle attribution continuation (November 13): opt-in start/end/quiet dumps
now separate runtime/reporting caches from ownership checks. Native heap decreases;
fresh browser's exact 49,152-byte array expansion is rooted by .NET's type-cast
cache. A reporting getter is rooted by System.Text.Json metadata. Browser worker
counts and a transient five-handle group rise/fall together. Both browser modes
retain two unnamed file handles and one Windows cache-file mapping; native retains
two events and one I/O-completion handle whose allocator is still unresolved.
All three handle-aware diagnostic workloads pass their ownership checks. The
standard-collector pilot lacks handle metadata and is retained as incomplete
evidence. Pauses perturb the process: no subtraction, budget or regression claim.
Next: creation-stack attribution for remaining handles, then longer unpaused
campaigns and additional row counts before setting budgets. See
[heap/handle findings](fw018-heap-handle-attribution-2026-11-13.md).
Normal Release revalidation passes 172 tests, 15 visual checks, 21 stress assertions,
reporting controls and window/shutdown checks; both analyzer fixture suites pass.
Both unpaused browser modes also pass (88 total cycles/reporting samples).

Bounded-reporting continuation (November 13): native and browser samples now spool
each sample to a fresh file, retaining only one weak probe between measurements.
Every previous sample must collect before the next is recorded; complete reports
are reconstructed only after measurement. The 128-sample control and short native
regression pass; three 60-batch native runs and all six browser runs pass. All 189
native soak samples and 264 browser samples verify collection; raw evidence matches
reconstructed reports. Native collected-heap changes range from -4,648 to +46,904
bytes; browser heap changes from +216 to +99,384 bytes and handles from +3 to +23.
Residual process trends need attribution before budgets; bounded reporting alone
is not whole-process leak proof. Protocol is
`bounded-sample-files-v1`; prior retained-reporting evidence remains separate.
[Bounded reporting findings](fw018-bounded-reporting-2026-11-13.md).
Final Release validation passes 172 tests, 15 visual checks, 21 stress assertions,
reporting controls, the one-batch regression and window/shutdown checks.

Reporting isolation continuation: implemented an end-of-run save/release/reload probe
shared by the native and browser samples. Empty, deliberately retained and fully
released controls pass. Final three 60-batch native runs release 31,808–31,832 reporting
bytes; all six browser runs collect their sample roots (38,016 bytes fresh profiles,
25,344 reused). The initial browser pilot detected one rooted sample; moving sample
construction out of the async loop resolves it. Failed pilot and measured source
are preserved. Samples are measured before report reload; no heap budgets or
corrected trends are claimed. Bounded reporting is now implemented above; residual
trend attribution and repeated budget calibration remain. [Reporting isolation evidence](fw018-reporting-retention-2026-11-13.md).
Final Release validation passes 172 tests, 15 visual checks, 21 stress assertions,
reporting controls and window/shutdown checks; direct fresh-directory memory smoke
also passes after a one-batch pilot exposed a remaining reporting root. Both callers
now reject surviving reporting roots; native construction is isolated too. Two final
browser guard checks pass. Raw failed/successful evidence and exact measured sources are archived.
Framework runtime and accepted performance budgets remain unchanged.

Updated November 13, 2026: resumed after FW-010. The offline memory analyzer now
checks all retention checkpoints for description counts, required ownership probes,
logical/native payload accounting, bounded native realization and complete removal.
Focused fixtures pass; reanalysis of both archived three-run campaigns passes (the
longer campaign alone includes retention checkpoints). This is existing-evidence
analysis, not a fresh runtime/performance run. The later bounded-reporting continuation
removes growing sample retention during measurements. Next: attribute residual
heap/handle trends, establish budgets, then design contextual failure diagnostics
and measure any instrumentation overhead. [Scope and findings](fw018-memory-diagnostics-2026-11-13.md).

Historical status before the November 15 scope split: **In progress**. Owner: Codex. Started October 29, 2026. Promoted to P1
and now first in the active queue after FW-010 closed November 11. Initial slice: weak-reference
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

Completed November 15, 2026. Owner: Codex. Standalone PackageWorkspace restores
only published packages in independent consumers and isolated caches. Exact
alpha.6 → alpha.7 source and unchanged-consumer binary upgrade pass in both
full-list and virtualized modes: 14 final processes, 56 workspace lifetimes,
168 collected host/editor probes, balanced ownership and completed shutdown.
Forms, direct list selection, native edits, resize and navigation are checked.
Source/loaded assembly hashes, lock files, captures and raw repeated diagnostics
are retained. Final update timing changes +5.54% full-list / +4.45% virtualized;
allocations and component work are effectively unchanged. No update-speedup,
universal memory budget or hosted-release claim. Windows 11 x64 / .NET 10.0.13
is the verified scope. [Adoption and upgrade evidence](fw019-package-adoption-2026-11-15.md).

The original pilot shutdown failure/stall and earlier campaign are retained.
Final harness uses the normal Application lifecycle. No framework runtime change.
NuGet alpha.7 availability is confirmed; FW-012 subsequently reconciled the
complete hosted/publication history and closed FW-024. Future v1 RC packages need exact-package
acceptance; this closes the reproducible sample/upgrade capability and current
published preview path, not a future unbuilt release.

Original scope:

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

### FW-023 Ship an agent skill and visual before/after

Status: **Done**. Owner: Codex. Started and completed November 13, 2026
at user request. Added the portable `skills/ui-framework` skill and composition
recipe, optional discovery metadata, and a local Codex installation. An isolated
capture fixture renders the same release workspace before/after at 1040 DIP and
at 640 DIP; a reproducible two-frame GIF holds each real native render with a
presentation caption. README and [usage/evidence](agent-skill.md) link the result.

Consulted layoutstyle, state/component and host ownership in the curated graph
and the layout guide's interactive neighborhood, then verified source factories,
tokens and capture behavior. Added the agent-skill concept and ownership links.
Validation: Release build/captures with .NET SDK 10.0.402, visual inspection of
wide before/after and narrow after, skill validator, GIF frame/timing checks,
`tools/Update-KnowledgeGraph.ps1 -Check`, generated graph `--check` and
`tools/Test-Documentation.ps1` pass. System SDK mismatch is documented; the
repository pin remains unchanged. No framework runtime/template or benchmark
changes; no performance improvement or release acceptance claim. Static captures
do not establish keyboard, IME or accessibility acceptance. Source, media and
documentation are included in this commit; no package published.

### FW-024 Publish alpha.7

Status: **Done — published November 13, record reconciled November 15, 2026**.
Owner: Codex. Exact release commit `c5d639bcff707ff1a75b23f2a60135d6be6cb7b2` passed
Validate 43409502188 and Publish NuGet 43410864931, including 32/32 performance
checks in each. Both libraries/symbols published; public DLL hashes match hosted
artifacts and fresh FW-019 installation. Complete source/evidence restored from
`42d6617` without merging unrelated code. [Release record](releases/0.1.0-alpha.7.md).
This closes historical publication only, not a release of current uncommitted work.

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
| FW-023 | November 13, 2026 | Portable C# UI skill, local installation and reproducible native before/after GIF; build, visual inspection, skill/media, docs and graph checks pass. Included in this commit. | [Usage and evidence](agent-skill.md) |


### FW-025 Add contextual runtime failure diagnostics

Status: **Deferred — post-v1**. Split from FW-018 November 15, 2026 by user decision.
Evaluate actionable component/key context for invalid views, rendering failures,
unexpected rebuilds and cleanup failures. Preserve existing exception behavior;
do not mask application errors. Any opt-in instrumentation needs measured enabled
and disabled overhead. A full rebuild inspector is not implied. This is not a v1
release blocker; resource-lifetime and memory acceptance remain in FW-018.

### FW-026 Revalidate physical Narrator after peer-cache cleanup

Status: **Deferred — post-v1 follow-up**. Split from FW-018 November 15, 2026 by
user decision. Exercise virtual-row discovery, recycling, focus and removed-key
reuse with physical Narrator against the production peer-cache cleanup; record
OS, build and observed behavior. Existing automated peer checks and October manual
acceptance remain evidence, but do not establish a new physical pass. Consider
sustained external-client retention separately if the workload warrants it. This
follow-up is not a v1 blocker under the agreed scope; any discovered correctness
regression must be triaged on its own evidence.


### FW-027 Package current source and validate against alpha.7

Done November 15, 2026. Owner: Codex. Built both libraries/symbols as
`0.1.0-alpha.8-local.1` from preserved dirty source. Full Release passes 176 tests,
reporting controls, 15 visual stress checks, 21 stress assertions, real-window
startup/native shutdown and the short memory check. API guard probes and fresh
package smoke checks pass. Seven alternating pairs against alpha.7 source
`c5d639b` pass all 32 metrics: timings −0.02% to −2.30%, UI allocations effectively
unchanged, identical component work. All 58 package/source manifest entries match.

Fourteen public-alpha.7 → exact-local-package source/binary upgrade processes
pass, with 56 lifetimes and 168 collected host/editor probes. Smaller workspace
diagnostics retain +7.40% full-list mount and +4.01% virtual update increases;
raw ranges and interpretation are recorded rather than described as universal
improvement. Existing 12-run memory budgets match 54 runtime source entries and
are reused, not newly rerun. All evidence/hashes preserved.
[Candidate and results](fw027-local-candidate-2026-11-15.md).

The accepted baseline and budgets remain unchanged. No commit, push, hosted run
or publication was performed. Exact committed-candidate hosted validation against
the accepted baseline remains required before release. The default repository
version and sample public dependency remain alpha.7; use the unique local version
explicitly to consume these new packages.

## FW-028 — Validate and publish v1

Done, November 15, 2026. Detailed acceptance commit a1d2ed6, upstream integration
e1aad81 and candidate cbd85b8 are pushed. Validate 43719989593 and Publish NuGet
43721319796 passed all correctness/package checks and 32/32 performance metrics
each against the unchanged accepted baseline. Local Release passed 176 tests;
local accepted-baseline diagnostics passed 32/32. Exact local and public package
campaigns each passed 14 source/binary upgrade processes. Both publicly installed
DLLs match hosted artifacts; the standalone sample pins 1.0.0 and restores locked.
Both libraries and symbols are published. Raw evidence/manifests and the detailed
beginning-to-end write-up are in [the release record](releases/1.0.0.md).
The post-upload README discrepancy is explicit in FW-029; it does not alter binaries.

## FW-029 — Ship corrected embedded package README

Done November 20, 2026. Public 1.1.0 README hashes match corrected hosted
artifacts; see [release evidence](releases/1.1.0.md). Post-upload public-page verification found the obsolete
Preview limitations paragraph still embedded in 1.0.0. Repository text is corrected
and the stable-version documentation guard rejects those old status markers.
The correction shipped in 1.1.0. Do not
overwrite 1.0.0 or describe its embedded README as corrected. Runtime gates passed;
this follow-up concerns documentation, not an observed runtime regression.
