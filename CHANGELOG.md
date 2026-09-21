# Changelog

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
