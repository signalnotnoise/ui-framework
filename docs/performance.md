# Rendering performance

The full-list measurements below predate virtualization. The stress lab now also supports [virtualized lists](virtualization.md), with separately documented verification and limitations.

See [measured results and their limits](performance-results.md) for the first optimization pass.

## What is being optimized

A body build is a call to a component's Body method. It regenerates view descriptions; it does not necessarily recreate controls or paint a frame. Expensive updates can come from unnecessary bodies, description allocations, WPF reconciliation/layout, creation/removal of native controls, and broad changes that genuinely affect many rows.

## Projected observation

```csharp
var title = item.Binding(x => x.Title, (x, value) => x with { Title = value });
var selected = selectedId.Select(id => id == myId);
```

Readers of title are not invalidated when another field changes. A selection change among 1,000 row selectors still evaluates the subscribed selectors, but only the two whose boolean result changed notify their render sessions. This trades cheap value comparisons for avoiding expensive UI body work.

Bindings and selectors with a known source observe that source directly. Chained record projections are flattened rather than building nested subscription graphs. These projection functions must be pure and should depend only on their supplied source and stable ordinary inputs. For multiple/dynamic observable dependencies use:

```csharp
var total = new Computed<decimal>(() => price.Value * quantity.Value);
```

Computed captures dynamic dependencies, switches subscriptions when branches change, and uses EqualityComparer by default (an optional custom comparer is supported). It attaches subscriptions when its first reader subscribes and removes them when the last reader leaves. Plain, non-observable props are refreshed when a getter is explicitly read; they cannot independently trigger updates. Thread ownership applies to state and derived observers.

## Skip unchanged child inputs

```csharp
Component<WorkRow>(row => { row.Model = workspace; row.Item = item; })
    .Id(item.Id.ToString())
    .Memo((workspace, item));
```

The key retains identity across reordering. The Memo tuple declares the inputs needed to configure the component. If the inputs compare equal, the renderer skips configure/Body for that parent-driven pass. Independently observed changes and pending local renders still run; dirty child work is consumed once if its parent also updates.

Memo is opt-in. Include changing labels, scalar props, callbacks, and any callback dependencies in its inputs. Prefer immutable values/records/tuples; comparing the same mutated reference does not detect its ordinary property changes. Excluding an input can leave stale UI or callbacks. State-backed objects can be passed by stable reference because the child observes their values directly.

## Stress workload and remaining limits

The stress board uses projected bindings, per-row selected-state selectors, and explicit Memo inputs on retained child components. All 1,000 rows still mount; no virtualization or reduction in work-item count is used to improve the comparison. The renderer also avoids resetting a retained element's margin to zero immediately before restoring its stack spacing.

Use the workload in [stress-lab.md](stress-lab.md) and compare body builds, UI-thread allocations, and elapsed time together. Lower body counts alone are not proof of a speed improvement. Timing includes native control work and varies by machine and runtime warm-up.

Changing compact mode affects all visible/mounted rows. Filtering removes components and remounts them when they return. Those operations still require work. Optional [virtualized lists](virtualization.md) now reduce native control counts; large-shuffle reconciliation and more targeted layout changes remain opportunities. Full keyboard/IME/focus and frame-rate testing still requires a visible window.
