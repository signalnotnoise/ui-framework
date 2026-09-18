# Component contracts

## Identity and ownership

`UI.Component<T>()` creates a description containing a type and factory, not an instance. `T` needs a public parameterless constructor. A renderer node creates the instance on its first mount and gives it a separate `ViewHost` and `ViewSession`.

A match requires the same parent, sibling key (or unkeyed position), view kind, and component type. Matching reuses the component instance, its state fields, and compatible WPF controls. Keys are local to siblings, not global. Moving between different parents remounts. Removing and later reintroducing a key remounts. Ordinary function helpers do not create an observation boundary: use a component when local state needs its own lifetime and update scope.

## Props and state

Configure callbacks run on the retained instance before every body build. Assign ordinary properties for props. Both configure and Body reads are collected by the component's own session. If a parent reads state to capture a prop value first, that parent also becomes a subscriber. Shared raw state can intentionally be read by multiple components; all of those readers update. Bindings and selectors notify readers only when their projected result changes.

Keep state fields on the instance. Do not create state inside Body, since it would be replaced on each call. Do not mutate observable state during configure or Body. Use event callbacks or mount hooks for updates. State and StateList belong to the thread that created them; background workers must dispatch results to the UI thread.

StateList tracks structural changes, including item replacement and reorder. Enumeration returns a snapshot. It does not observe arbitrary mutable fields inside each item. Read Count, index, or enumerate within Body/configure to subscribe.

## Scheduling

Changes invalidate only the sessions that read the changed observable. Each host queues at most one pending render at WPF DataBind priority. A local update does not call parent or sibling bodies. A parent update configures and rebuilds retained descendants by default. A component description with `.Memo(inputs)` skips that work when old/new inputs compare equal and the child has no pending local invalidation. Refresh performs a synchronous build and consumes pending work for that host; descendant Memo boundaries still apply.

Memo compares inputs using object equality; immutable records and tuples provide value comparison. Include all changing props and callback dependencies. Observable objects can be passed by stable reference when the child reads them reactively; ordinary mutable objects need an immutable value snapshot or revision input. Keys control identity and local-state lifetime; Memo controls whether a retained instance needs a parent-driven build. Do not use keys as a substitute for input comparison.

If Body throws, its previous successful dependency subscriptions remain intact and the temporary tracking context is restored. Renderer changes are not transactional; application-level error boundaries and rollback are future work.

## Lifetime

1. Construct the component.
2. Configure and render its initial Body, establishing subscriptions.
3. Call OnMounted once. Changes here can schedule a subsequent render.
4. Reconfigure and render on prop or observed-state updates.
5. On removal/replacement/host disposal, first detach subscriptions and suppress queued renders throughout the removed subtree.
6. Dispose descendants and call OnUnmounted once, child before parent.

Hooks describe renderer lifetime. OnMounted does not mean WPF Loaded, layout completion, or keyboard focus. Visual-tree unloads do not themselves unmount retained components. The application owns the root ViewHost and must dispose it, normally from Window.Closed. Release timers, subscriptions, and cancellation resources acquired in OnMounted from OnUnmounted. Cleanup must be safe on the UI thread; asynchronous cleanup hooks are not provided.

## Verified behavior

Navigation and virtual-list restoration can remount a saved logical component instance. OnMounted/OnUnmounted therefore run once per mount cycle, not necessarily once per object. Hidden navigation screens release subscriptions and controls while preserving component fields for Back. See [navigation lifetime](navigation.md).

The executable checks cover local update isolation and batching, keyed reorder and local text retention, selection retention, prop refresh, type/key replacement, removal and remount, cleanup before queued renders, list mutations/no-ops/invalid moves, cross-thread rejection, failed-build dependency handling, and state reads in configure callbacks. These checks run WPF controls and a dispatcher without a visible window. They do not establish uninterrupted keyboard focus, accessibility behavior, or IME correctness.
