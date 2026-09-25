# Workspace layout

These APIs are implemented in the working source after `d5a94b2`; the pinned `0.1.0-alpha.2-local.3` app packages do not contain them. No package or app migration is implied by this source change.

## Vertical fill

```csharp
FlexColumn(
    Text("Assignment review").Height(48),
    TextEditor(document).Id("editor"),
    Text("Ready").Height(28)
).Spacing(8);
```

`FlexColumn` uses finite remaining height in the same way `FlexRow` uses width. Explicit child Height takes precedence. Otherwise Flex(0) sizes to content, and positive weights share remaining space. Fixed gaps consume space before weighted rows. Under an unbounded vertical parent, rows measure to content; a vertical Scroll around this layout cannot provide a fill-height constraint. The host must receive a finite viewport. Fixed contents exceeding the viewport do not automatically shrink.

WPF's `FlexColumnPanel` owns row definitions and reuses them when the row count is unchanged. Ordinary reconciliation owns children and preserves keyed controls/selection on reorder. The core remains WPF-independent.

## Resizable panes

```csharp
var navigationWidth = new State<double>(280);
var hidden = new State<bool>(false);

SplitPane(
    navigation.Id("navigation"),
    editor.Id("editor"),
    navigationWidth,
    SplitAxis.Horizontal,
    minimumFirst: 120,
    minimumSecond: 240,
    firstCollapsed: hidden.Value
);
```

Horizontal means side by side; Vertical means stacked. The first extent is in pixels; the second pane fills the rest. The separator is six pixels. Native GridSplitter provides pointer resizing, arrow-key resizing in ten-pixel increments, focus and automation behavior. The size binding is updated when a drag completes or an arrow key is released. `Binding<double>` is also accepted, including bindings that normalize or reject changes. An unrelated parent update preserves an in-progress drag size. Pane content remains mounted while the first pane is collapsed; state subscriptions and application updates remain active, so collapse is not virtualization. The bound size is retained for restoration.

The WPF panel owns the separator independently of the two declarative children. Generic renderer reconciliation retains keyed panes, while teardown disables resize callbacks before releasing children. No child ViewHosts or application-owned native Grids are required to use the primitive. Ancestor navigation/virtualization snapshots use the normal logical child tree.

The viewport should fit the configured minimum extents plus the separator. Programmatic extents and explicit child sizes can exceed the available viewport; the panel clips overflow instead of silently changing the bound model. Applications should supply suitable minimums for compact windows. This first version collapses the first pane; nested panes can express larger workspaces. It does not provide detachable tabs, floating windows, persisted workspace arrangements or a docking manager.

## Edge docking

```csharp
Dock(
    center: SplitPane(navigation, editor, navigationWidth),
    top: toolbar.Height(48),
    bottom: status.Height(28),
    right: inspector.Width(320)
);
```

`Dock` composes FlexColumn and FlexRow. Top/bottom span the entire width; left/right surround the center in the middle row. Edges size to their content or explicit dimensions. Internal keyed regions preserve center identity when an edge is added or removed. This is fixed edge layout, not drag-and-drop docking.

## Validation and remaining gaps

`WorkspaceLayoutTests` covers allocation, weights, retained editor selection, docking edge removal, splitter bounds and mouse/keyboard event routing, collapse/orientation/reorder identity and release counts, and invalid configurations preserving the retained tree. Input checks use native routed events on a hidden WPF presentation source; they are not a manual accessibility/screen-reader campaign.

The final correctness suite passed 97 tests, including rejected size bindings, native Escape cancellation, and changing FlexRow widths/gaps across editor reorder and removal. The release suite also checks 15 visual assertions and 21 stress assertions. A seven-sample before/after run used `tools/Test-Performance.ps1 -BaselineRef 2b678989d70158ffb40d31bfb949e18969439717 -IncludeLayoutEditors`. The reference captures the pre-existing dirty renderer fixes without moving the accepted baseline or changing the user's work. Raw results and exact candidate source are retained under `artifacts/performance/workspace-layout-2026-09-23`; the raw reports and source manifest are also preserved in [performance evidence](performance-evidence/2026-09-23-workspace-layout/partial-summary.json).

### Performance: incomplete gate

Follow-up: [September 24 timeout investigation](editor-timeout-investigation.md) reproduced the text-services wait on the pre-layout revision and in standalone WPF. This narrows the failure but does not establish performance approval.

Seven alternating samples per side completed for the full list, virtualized list and themed full list, with one discarded warmup per side. SDK 10.0.401 and runtime 10.0.12 were unchanged. Medians follow; allocations are UI-thread bytes.

| Scenario | Mount ms before / after | Update ms before / after | Mount bytes before / after | Update bytes before / after |
| --- | ---: | ---: | ---: | ---: |
| Full list | 5,294.85 / 5,237.26 | 6,595.72 / 6,665.28 | 342,594,152 / 343,007,360 | 603,612,968 / 604,808,200 |
| Virtualized | 364.98 / 372.55 | 1,135.25 / 1,120.13 | 9,591,888 / 9,620,168 | 60,591,472 / 61,097,328 |
| Themed full list | 5,322.13 / 5,264.88 | 13,495.56 / 13,628.86 | 327,608,288 / 327,929,912 | 1,157,879,936 / 1,159,047,560 |

Full-list updates were 1.05% slower, virtualized updates 1.33% faster, and themed updates 0.99% slower. Update allocations rose 0.20%, 0.83%, and 0.10% respectively. These completed scenarios are within unchanged budgets; timing variation does not establish a speedup. The extra nullable descriptor on each View contributes object-size overhead. Component work was identical: full/themed mount builds 1,022, update builds 5,141, update mounts/unmounts 456/658; virtualized counts 27, 194, 98/95.

The **overall command failed**: the candidate layout-editor warmup exceeded the unchanged 120-second process limit and produced no report or error text. The baseline warmup completed in 2,613.33 ms mount plus 92,517.04 ms updates, allocating 126,224,376 mount bytes and 1,109,230,256 update bytes (1 mount build, 50 update builds). No repeated editor comparison completed, so neither a causal regression nor equivalence is established. The failure is preserved and blocks performance sign-off; no timeout or budget was relaxed.

These existing performance workloads assess general renderer overhead (including the extra optional layout field on View); they do not directly compare new workspace layouts against the app's adapters. App integration and a like-for-like populated workspace comparison remain necessary before claiming a performance benefit or replacing the installed package. Historical release-gate failures remain in force until a separate accepted-baseline run establishes release readiness.

Still outstanding: declarative application/context menus, horizontal scrolling, tree views, tooltips, and richer docking behavior. This increment closes the initial source-level vertical-fill, two-pane resizing and fixed edge-layout gaps only.
