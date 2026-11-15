# FW-017 existing-evidence review — November 15, 2026

Subsequent same-day [acceptance](fw017-appearance-acceptance-2026-11-15.md) closes
both gaps below. This document preserves the earlier review rather than claiming
those new checks were already done at review time.

This review closes the verified implementation and example work within FW-017.
It does not start another redesign or claim a new test, benchmark or physical
acceptance run. The remaining scope is two specific acceptance decisions below.

## Completed portions

- [x] **Scoped appearance and interaction implementation.** Core
  [ThemeTokens](../UI%20Framework/Styling/ThemeTokens.cs) supplies colors, radius
  and control padding. WPF's [ThemeStyles](../UI%20Framework.Wpf/Styling/ThemeStyles.cs),
  TextBoxStyles, PickerStyles and ToggleStyles implement the supported native
  controls' hover, pressed where applicable, focus and disabled feedback.
  [LayoutStyleTests](../tests/UI%20Framework.Checks/LayoutStyleTests.cs),
  [ToggleStyleTests](../tests/UI%20Framework.Checks/ToggleStyleTests.cs) and
  [ContrastThemeTests](../tests/UI%20Framework.Checks/ContrastThemeTests.cs)
  cover scoped replacement, light/dark toggle rendering, disabled colors,
  editing/focus/selection retention and local-value precedence. This is not a
  claim that every state of every control has a visual snapshot.
- [x] **Compact controls and content sizing.**
  [Compact button evidence](compact-button-validation.md), CompactButtonTests
  and ButtonContentTests cover native padding/height/minimum precedence, retained
  content/templates and generated-label wrapping. ToggleStyleTests covers short
  and long labels. These are completed fixes, not outstanding FW-017 implementation.
- [x] **Responsive and split-workspace behavior.**
  [LayoutStyleTests](../tests/UI%20Framework.Checks/LayoutStyleTests.cs) verifies
  reflow without replacing the editor or losing selection;
  [WorkspaceLayoutTests](../tests/UI%20Framework.Checks/WorkspaceLayoutTests.cs)
  covers fill, docking, retained split panes and mouse/keyboard resizing.
  [Launchpad](launchpad.md) already supplies overview, board and editable details
  compositions. Layout correctness does not by itself establish aesthetic approval.
- [x] **A documented composition approach and polished reference render.**
  [The shipped skill](../skills/ui-framework/SKILL.md) gives type-scale, spacing,
  hierarchy and density guidance; these are composition recommendations, not a
  new mandatory public token API. FW-023, commit `0edea41`, implements the
  [release workspace](../tools/SkillPreview/ReleaseScreen.cs) and records inspected
  1040-DIP and 640-DIP native captures. See [verification](agent-skill.md) and the
  [narrow capture](images/ui-framework-after-narrow.png), inspected again during
  this review. The default-theme form and board reflow is already delivered.
- [x] **Existing contrast and physical scaling acceptance.**
  [October 29 manual acceptance](manual-acceptance-2026-10-29.md) records the
  user's contrast-switching pass and the earlier October 28 scaling/Narrator
  reports. Keep that credit under FW-006; do not ask to repeat it merely because
  FW-017 also mentions contrast/scaling. Exact build, OS/provider and scale values
  were not supplied. It is not an exact-current-candidate physical pass.

## Evidence chronology and boundaries

The initial October 28 contrast investigation recorded two discovery failures
and performance regressions. Those failures remain historical evidence; they
must not be described as a passing campaign. The subsequent
[alpha.6 release record](releases/0.1.0-alpha.6.md) records their resolved state:
146 passing tests and both hosted workflows passing all 32 performance gates.
Its then-open manual contrast check was subsequently closed by the October 29
confirmation. No live GitHub status was queried in this documentation review.

The 15 checks in [VisualStressChecks](../samples/Counter/VisualStressChecks.cs)
exercise workload progression, stop/restart, state and cleanup. Rendering a
snapshot inside that suite does not turn these into 15 visual-design approvals.
Likewise FW-023's captures do not exercise keyboard input, contrast switching,
physical scaling or error states. Existing dedicated tests and manual reports
carry those separate claims.

ThemeTokens has no shared typography/spacing/density scale or error-state token.
The composition recipe already supplies guidance, so absence of a new API is
not itself a v1 defect. Menus, scrollbars, application-owned native controls and
local color overrides remain within the application's documented responsibility;
FW-017 does not silently add them to the framework's styling promise.

## Remaining acceptance scope

- [ ] **Combined visual acceptance record.** Reuse the existing examples and
  captures to review representative forms, a compact toolbar, lists and a split
  workspace in light/dark appearances with long content and the supported
  focus/disabled states. Record which surfaces and states were actually inspected.
  Current evidence covers pieces, but no linked record establishes that combined
  review. Fix only a concrete defect found; no wholesale restyling is prescribed.
- [ ] **Validation/error appearance decision.** State whether v1's error content
  remains application-owned composition or whether FW-017 promises a reusable
  framework appearance. The original requirement names validation states, but
  the reviewed implementation/evidence does not establish a shared contract or
  its visual acceptance. Resolve and document that scope before calling the
  original requirement fully complete; a new API is not automatically required.

FW-017 therefore remains **implementation/example portions closed; acceptance
follow-up only**. This review neither silently drops those two criteria nor
turns new typography APIs, another contrast campaign or a redesign into blockers.
FW-019 still owns independent package adoption and upgrade acceptance. FW-025/026
remain the separately agreed post-v1 diagnostics/Narrator follow-ups.
