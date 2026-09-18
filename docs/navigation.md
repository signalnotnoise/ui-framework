# Navigation and screen lifetime

`NavigationStack<TRoute>` owns observable, UI-thread-bound history in the platform-independent core. Routes should be immutable values. `UI.Navigation` describes the screens; WPF owns mounting, snapshots, and entry transitions.

```csharp
private readonly NavigationStack<string> history = new("overview");

public override View Body() => Navigation(history, route => route switch
{
    "overview" => Component<Overview>(),
    "board" => Component<Board>(),
    _ => Text("Unknown screen")
});

// From event handlers:
history.Push("board");
history.Back();
```

Keep history in a retained component field or application model. The screen factory should be pure and preferably return component descriptors: it runs for every history entry, but only the active screen's component body mounts. Do not create a new history inside Body. Use distinct view keys when replacing independent navigation boundaries.

## History

- Push creates a distinct identity even for an equal route.
- Back pops one entry and returns false at the root; it never empties the stack.
- PopToRoot discards entries above the original root and restores its logical state.
- Reset creates a fresh root identity and discards previous history and snapshots.
- Current, Entries, and CanGoBack participate in dependency tracking. Entries returns a snapshot. Reads and writes require the owning thread.

## Retention

Only the active screen has a native subtree and subscriptions. On departure, the renderer captures logical component instances, detaches subscriptions, calls unmount hooks, and disposes native controls. Back remounts saved instances with their state; mount hooks run again. Hooks must support repeated cycles and recreate resources released during unmount.

Snapshots include nested navigation and visited virtual-list rows, including recycled rows. Restoring a virtual list still mounts only the viewport and buffer. Removing history entries releases their snapshots; disposing the root releases active and saved trees. Application model references may independently keep state alive.

Retention is logical, not native: scroll offsets, text selection, keyboard focus, and IME composition are not restored by navigation. State that must outlive a popped screen belongs in the application model. Launchpad's Overview action pops Board; reopening it starts fresh local screen state while model-owned edits and filters remain.

## Transitions

FadeSlide animates the incoming native host from transparent and 10 pixels below its final position over 160 ms. The outgoing screen unmounts immediately. Rapid navigation cancels outgoing animations, and completed animations do not hold property values. Same-screen updates do not replay the animation.

NavigationTransition.None suppresses motion and cancels a running entry animation when applied. WPF also respects SystemParameters.ClientAreaAnimation when starting/updating screens. Launchpad exposes Reduce motion. This is an entry transition, not a general animation API, shared-element system, navigation guard, or URL router.

## Verification

All 33 Release tests passed on September 18, 2026. Coverage includes root behavior, entry identity, thread affinity, Back, hidden subscription release, fresh popped-screen state, nested navigation, virtual-row restoration, and repeated navigation with motion disabled.

`dotnet run --project samples/Counter -c Release -- --navigation-check` opens a real WPF window and runs 64 steps with 104 assertions: native edits, Back, retained Board preferences, resizing, interrupted transitions, and reduced-motion cancellation. Captures go to `artifacts/navigation-stress`. Closing this diagnostic window early fails the check. This does not establish FPS, complete keyboard navigation, or accessibility coverage.
