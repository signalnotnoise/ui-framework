# Changelog

## 1.3.0 — November 21, 2026

- FW-040: independent Button/IconButton tooltip text, null restoration and empty
  suppression, preserving accessible names and native WPF policy.
- FW-041: clarify frame versus native icon-button sizing with a compiled compact
  toolbar recipe, contrast/DPI checks and retained native feedback.
- Add regression/package checks, version-aware skill guidance and archived evidence.
  See [release evidence](docs/releases/1.3.0.md); both packages and symbols are published.

## 1.2.0 — November 20, 2026

- Add shared spacing, typography, radius and icon token scales; scoped themes
  now control icon-button label spacing. See [design tokens](docs/design-tokens.md).

- Add reusable vector icons, icon-and-text buttons and accessible icon-only buttons.
  See [the icon contract](docs/icons.md).

## 1.1.0 — November 20, 2026

- Add explicit opacity/translation destinations and retained-update animation
  specifications in core, with lazy WPF node controllers, native interruptible
  clocks, inherited motion suppression and immediate detach cleanup. See the
  [contract and diagnostic commands](docs/animation.md).
- Ship the corrected embedded package README and updated version-aware agent skill.

## 1.0.0 — November 15, 2026

Published both libraries and symbols from `cbd85b8` after both exact-commit hosted
workflows passed, including 32 performance checks each. Establish the supported v1 API and
behavioral compatibility contract for Windows 11 x64 / .NET 10. See the
[release record](docs/releases/1.0.0.md) for evidence and scope.

- Release disposed virtual-row editors held indirectly through cached automation
  children; direct editor-peer ownership remains explicit. This candidate fix
  is not in published alpha.7. See [verification](docs/fw018-peer-cache-cleanup-2026-11-15.md).
- Add bounded memory reporting, scoped lifetime budgets, light/dark appearance
  acceptance and the independent NuGet-only adoption/upgrade workspace. These
  source tools are not runtime features added to the alpha.7 package.


## 0.1.0-alpha.7 — November 13, 2026

Published both libraries and symbols from `c5d639b` after Validate and Publish
NuGet passed. Public installation hashes match hosted packages. See the
[release record](docs/releases/0.1.0-alpha.7.md).

- Use indexed keyed virtual-row storage for large reorders and reduce initial
  mount allocations while preserving active editors, logical state and cleanup.
  Retain individual collection notifications and the full-list comparison.
- Establish the exact-commit hosted performance release gate, with unchanged
  accepted baseline/budgets and source-first editor benchmark initialization.
- Add real-window/native lifetime and scoped WebView2 ownership acceptance
  evidence, including asynchronous close, popup ownership and failure recovery.
  Provider work and shutdown remain application-owned; no new interop API.
- Record user-confirmed physical IME and Windows contrast acceptance from
  October 29; these historical results do not imply every application is verified.
- Add the portable UI Framework coding-agent skill, composition recipe and a
  reproducible before/after GIF from native WPF renders. The skill and media
  live in the source repository; NuGet packages contain the core/WPF libraries.

## 0.1.0-alpha.6 — October 28, 2026

The pending manual checks mentioned in this dated entry were subsequently
confirmed October 29; see [manual acceptance](docs/manual-acceptance-2026-10-29.md).

Published libraries and symbols from `fd72534` after both hosted workflows
passed correctness, package and performance gates. See the
[release record](docs/releases/0.1.0-alpha.6.md).

- Follow Windows contrast-theme colors in scoped WPF control styles without
  replacing templates. Restore supplied tokens when contrast mode ends; use
  opaque disabled colors instead of fading controls. Local application overrides
  retain precedence. Physical contrast acceptance remains pending; all 32 local performance gates pass.

- Expose virtual-row automation names before realization through WPF item peers,
  allowing name-based discovery and realization of offscreen rows. Add retained
  state and keyboard traversal checks plus an accessibility acceptance window.
  Narrator and physical scaling were reported passing; manual system contrast acceptance remains pending.
- Keep ViewHost and virtual-row lifetime wrappers out of default keyboard focus
  traversal, so Tab reaches the actionable descendants instead of invisible wrappers.
- End focus on actively composing editors before a virtual row detaches, using
  WPF's TSF/IMM focus-loss path and source identity across native phase objects.
  Preserve the original binding during completion and finish row
  cleanup if the provider fails. Composition sessions are not serialized.
- Prevent regenerated rows from stealing focus selected in another editor before
  reattachment. Add visible-window checks for actual recycling, detached input,
  routed composition commit/cancel, reorder, removal and disposal. Physical IME
  acceptance remains pending; see [editing policy](docs/virtualization.md).

- Share frozen TextBox brushes in normal mode and batch theme resource publication
  to avoid repeated invalidation walks. Respect caller-owned initialization scopes.
  Local seven-pair comparison: editor mount allocations -3.72%, themed mount
  allocations +0.20%; unchanged budgets, workload and component work.
- Initialize native UIA before refreshing test peer caches; preserve all discovery
  and row-identity assertions. All 146 framework tests pass locally, alongside
  visual, stress, API guard and package smoke checks.
- Add offline knowledge-graph Git history views for visual code/history correlation.
  Preserve failed experiments and raw performance evidence for reproducibility.

## 0.1.0-alpha.5 — October 23, 2026

Published both libraries and symbol packages from `fbf5204` through Trusted
Publishing after all release gates passed. All 119 tests, 15 visual checks,
21 stress assertions and fresh package checks passed. Public installation
verified the compact-button fix and assembly hashes against hosted artifacts.
See [release evidence](docs/releases/0.1.0-alpha.5.md). The accepted performance
baseline and budgets are unchanged; FW-003 timing investigation remains open.

- Respect native button Padding in themed chrome, including live bindings and
  zero-padding close buttons. Retain the automatic 36-DIP minimum while allowing
  explicit native Height to size compact buttons; local MinHeight still wins.

## 0.1.0-alpha.4 — October 20, 2026

Published both libraries and symbol packages through GitHub Actions Trusted
Publishing from `039d3bb`, after all release gates passed. Fresh NuGet.org
installation verified both overloads, style-scope changes, compact input,
bindings, selection and cleanup; installed assemblies match the hosted artifacts.
See docs/releases/0.1.0-alpha.4.md for release evidence. Earlier direct-comparison
timing concerns remain tracked separately; no baseline or budgets changed.

- Add opt-in `NativeStyleScope.PreserveNative` to shield common native controls
  from ancestor implicit templates while retaining local styles and control identity.
- Remove duplicate themed input padding so compact query fields have a usable
  editing viewport; retain native padding, selection, bindings and multiline scrolling.

## 0.1.0-alpha.3 — September 26, 2026

Third experimental NuGet preview, published through GitHub Actions Trusted Publishing after the editor cleanup release gate and manual editor, IME, screen-reader, and lifecycle acceptance passed.

- Add `WpfComCleanupPolicy`, an explicit application-owned WPF lifecycle API that
  defers COM wrapper cleanup to idle boundaries and drains it after native hosts
  are disposed. The 1,000-editor release workload now completes without the
  reproduced late `TextBox.Text` stalls.
- Add `FlexColumn`, keyed `Dock` composition, and bound `SplitPane` layouts with retained children, native keyboard/mouse resizing, minimum extents and first-pane collapse.
- Reduce enum-validation overhead while retaining invalid-description checks, and cover layout sizing, keyed retention, splitter bindings and disposal with regression tests.
- Add isolated workspace and actual-consumer diagnostics, layout probes and timeout metadata; retain repeated runs, failed experiments and release limitations.
- Document the consumer file-tree optimization: model-backed recycled selection and active-editor retention, with a measured 20.60% update-time improvement in the final 1,000-file comparison. This is an application improvement, not a framework package speedup.

- Clear damaged host trees after patch failures and support explicit rebuild; preserve good trees when body construction or validation fails.
- Validate public view descriptions before patching, enforce notification thread ownership, and deliver state/computed notifications to all observers before reporting subscriber errors.
- Restore the specific visual focus path and TextBox selection on row regeneration; cancel stale focus callbacks and finish sibling cleanup when a row release throws.
- Bound STA tests, pin workflow actions to verified commits, and check documentation release versions.
- Guard unchanged editor properties, remove adaptive-layout LINQ, and add an optional themed layout/editor performance scenario. Performance evidence and remaining release gates are recorded separately.

## 0.1.0-alpha.2 — September 21, 2026

Second experimental NuGet preview, including native interop, accessibility labels, rich button content, and rendering optimizations.

- **Native WPF interop**: Embed retained native WPF controls with `WpfUI.Native<T>()` and `NativeControlHost`, supporting lifecycle hooks and isolated layout.
- **Accessibility labels**: Native contextual automation names via `View.AccessibilityLabel` (`AutomationProperties.SetName`) without altering visible control text.
- **Rich button content**: `ButtonContentPresenter` supports arbitrary native WPF elements, data templates, string format, template selectors, and text wrapping.
- **High-contrast toggle visuals**: Scoped `CheckBox` template with clear visual indicator, rounded checkmark geometry, and focused interaction states.
- **Rendering & allocation optimizations**:
  - Direct set reconciliation in `Dependencies.Reconcile` eliminating temporary LINQ HashSet allocations.
  - On-demand duplicate key validation in `ViewHost.Validate`.
  - Guarded dependency property writes in `ViewHost.Patch` avoiding layout churn and enum boxing.
  - Core list update allocations improved by 5.02% (~32 MB reduction) and virtualized update allocations by 5.84%.
- **Resilience**: Preserved subscriptions and rollback recovery on failed component builds and computed getters; transactional candidate dependency commit and exception-safe replacement node cleanup in `ViewHost`.

## 0.1.0-alpha.1 — September 18, 2026

First experimental NuGet preview, published through GitHub Actions Trusted Publishing from commit `78c88fb`.

- Packable `SignalNotNoise.UI` and `SignalNotNoise.UI.Wpf` libraries, with README, MIT license, repository metadata, and portable symbol packages.
- Isolated local-feed WPF consumer validation and CI package artifacts.
- Multiline/read-only editors, password fields, indexed pickers, and light/dark picker themes.
- Responsive layouts, scoped styling, and retained navigation with transitions.

- C# view descriptions and a Windows WPF renderer.
- Components with keyed identity, local state, lifecycle hooks and batched updates.
- Observable values/lists, projected bindings and derived values.
- Explicit component memoization.
- Virtualized lists with variable-height rows, saved component state and incremental collection updates.
- An interactive stress lab with full-list/virtualized modes and automated checks.
- MSTest regression coverage, architecture documentation and a maintained knowledge graph.

Known limitations include incomplete visible-window focus/IME and accessibility verification, quadratic work for some large shuffles, nontransactional rendering, and no general animation system or non-Windows backend. See docs/virtualization.md and docs/roadmap.md.
