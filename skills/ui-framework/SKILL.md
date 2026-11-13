---
name: ui-framework
description: Build and refine clean native C# interfaces with SignalNotNoise.UI and its WPF renderer. Use for UI Framework views, components, bindings, themes, and workspace layouts; not unrelated .NET UI stacks or web interfaces.
---

# UI Framework

Build a deliberate native interface using the kit's declarative C# views. Preserve the user's workflow and data while improving hierarchy, spacing, interaction and code ownership. This kit currently targets Windows/WPF on .NET 10; the core has no WPF dependency. Do not promise cross-platform rendering.

## Establish the contract

Inspect the application's project references and resolved package version before writing code. Package IDs are `SignalNotNoise.UI` and `SignalNotNoise.UI.Wpf`; namespaces are `UI_Framework` and `UI_Framework.Wpf`. Do not replace the user's UI stack or upgrade packages merely to use an example.

In a framework checkout, consult `docs/knowledge-graph.json`, bounded graph neighborhoods, and the relevant guide before changing a subsystem. Verify signatures against `UI Framework/UI.cs`, `View.cs`, and the consumer's actual assembly version; source docs can describe APIs newer than its package. The source repository is https://github.com/signalnotnoise/ui-framework. For a consumer without the checkout, inspect its installed package/API metadata and fetch only needed upstream guides if access is available. Report unavailable evidence rather than inventing APIs.

Read [the composition recipe](references/composition.md) for a concrete component and host pattern. In the source tree, route by task:

- State, identity, props: `docs/components.md`, `docs/bindings.md`.
- Proportional layout, themes: `docs/layout-styling.md`, `docs/workspace-layout.md`.
- Large editable lists: `docs/virtualization.md`, `docs/performance.md`.
- Native controls and shutdown: `docs/native-interop.md`, `docs/application-cleanup-policy-2026-09-25.md`.
- Navigation: `docs/navigation.md`. Accessibility: `docs/accessibility.md`.

## Design the screen

Start from its primary task, main action and reading order. Reuse the application's visual language. For a new surface, choose a small type scale (for example 12/16/24/32 DIP), a spacing rhythm (4/8/12/16/24/32), restrained corner radii, neutral surfaces and one accent. These are starting values, not framework requirements.

Use a clear title, short supporting copy and a dominant next action. Group related controls; use whitespace before adding borders or nesting more cards. Keep dense tools compact and reading surfaces comfortable. Show status in words as well as color. Use realistic long labels and empty/loading/error content when relevant to the requested workflow. Avoid ornamental metrics and inert controls added only to make a screenshot look complete.

- `VStack`/`HStack` size to content. They are not CSS flexbox.
- `FlexRow`/`FlexColumn` distribute finite available space. Explicit dimensions override weights; `Flex(0)` uses content size. Use `Dock` for fixed edges and a filling center, `SplitPane` for user-resizable workspaces.
- `AdaptiveGrid(minimumColumnWidth, children)` reflows mounted children; it does not virtualize them. Use `VirtualList(rows, height)` with stable keys and a finite viewport for large lists.
- Text wraps only when measured with finite width. Prefer a constrained `FlexRow` over a long description inside an unbounded `HStack`. Do not hard-code the whole screen to the screenshot dimensions.
- Use immutable `ThemeTokens` and `ThemeStyles.Apply(host, tokens)` at the WPF boundary. Set root `.Foreground(tokens.Ink).Background(tokens.Canvas)` explicitly: applying styles does not color standalone text or the root surface.
- `.Padding` and `.Height` describe the outer view frame, not native button content padding. Allow room for native chrome. Do not invent CSS properties, rich-button factories, arbitrary animation APIs or responsive visibility modifiers.

## Keep behavior and ownership clean

Use one declared C# type per file, named after its type. Group components, models and platform hosting by responsibility. Keep WPF types and application lifetime out of core components.

Keep local `State<T>` in retained component fields, never recreate it in `Body()`. Use ordinary properties for parent inputs and `Binding<T>` for child edits. Describe children with `Component<T>(configure)` so the renderer owns instances. Build/configure functions must not mutate observable state.

Use `.Id(stableDomainKey)` for dynamic siblings; keys must be unique within the parent. Indices and random keys destroy the identity contract when lists reorder. Removal releases state; re-adding the same key creates fresh state. Virtualized offscreen logical state has a distinct retention policy: read its guide before relying on it.

`StateList<T>` observes structural changes, not arbitrary item mutations. Use observable item fields or replace items. Create/read/write state on its owning UI thread; marshal async results through the application's dispatcher. Pair external subscriptions with unmount cleanup. Keep async cancellation, native initialization and shutdown application-owned.

Add `.Memo(immutableInputs)` only with a demonstrated need; include every changing prop and callback input. Fewer body builds are not evidence of lower latency.

Dispose `ViewHost` when its owner closes. For substantial WPF editing, follow the application's single-STA `WpfComCleanupPolicy` lifecycle; this is an application-wide decision, not a component-local helper. Configure DPI policy before creating windows; large initial trees benefit from `WindowInteropHelper(window).EnsureHandle()` before host construction.

## Verify the result

Build against the real consumer references. Check edits, normalized binding readback, retained state after reorder/navigation, and cleanup for behaviors you changed. Inspect actual native renders at wide and narrow widths; exercise Tab order, focus visibility, disabled controls and readable contrast. Label automation names on inputs with `.AccessibilityLabel(...)`; keep visible labels too. Static captures do not prove keyboard, screen-reader or IME acceptance.

For a visual comparison, keep content, viewport and DPI constant. Capture actual WPF before/after frames and retain the source and generation command. Describe the GIF as a presentation sequence, not a runtime animation or a performance result.

When changing the framework itself, follow its backlog, graph and regression workflow. Runtime/rendering/layout/style/virtualization changes require `tools/Test-Performance.ps1` before/after with repeated alternating runs, full-list comparison, themed controls where relevant, and unchanged accepted budgets. Consumer-only visual edits should use checks appropriate to that application; do not claim a framework release gate passed from a screenshot. Report build/test evidence and any manual acceptance still outstanding.
