# Reactivity performance investigation

Source review, September 20, 2026. This is an investigation plan, not an implemented lazy-computed API or a performance-parity claim.

## Current behavior

- `State<T>` suppresses equal writes. `ObservableState` synchronously invokes subscribers.
- `Computed<T>.Value` evaluates its getter on every read, including while observed. An observed computed also evaluates on dependency notifications, then compares its result before notifying readers. Unobserved computed values detach subscriptions.
- General computed evaluations capture a new dependency HashSet and reconcile subscriptions. Fixed-source projections avoid repeated general dependency capture.
- `ViewSession.Build` captures and reconciles its dependency set. WPF batches scheduled view refreshes, but this does not batch all upstream computed evaluations.
- Getter re-evaluation intentionally supports non-observable component properties. Replacing this behavior with unconditional caching would change the current contract.

## Comparison with Alien Signals

The [upstream implementation](https://github.com/stackblitz/alien-signals/blob/master/src/index.ts) caches computed values, tracks dirty/pending state, checks upstream changes when needed, and queues effects with batching support. Its [graph algorithm](https://github.com/stackblitz/alien-signals/blob/master/src/system.ts) uses linked dependency/subscriber records. These are meaningful architectural differences; its published JavaScript benchmark results do not establish performance relative to this .NET/WPF framework.

An upstream-linked [C# port](https://github.com/CTRL-Neo-Studios/csharp-alien-signals) is a possible comparison implementation, not an audited or adopted dependency. Pin source revisions and inspect licensing, correctness and supported semantics before adding it to any harness. A same-runtime comparison would assess that port, not automatically prove parity with JavaScript Alien Signals.

## Bounded next experiment

1. Establish core-only elapsed-time, allocation and getter/effect-count baselines for repeated reads, writes before a read, unchanged derived results, long chains, diamond dependencies, broad fan-out, conditional dependency switching and attach/detach churn. Preserve semantic expectations and raw repeated runs.
2. Prototype opt-in cached computed values whose inputs must be observable, or explicitly invalidate ordinary component inputs. Preserve existing Computed semantics until compatibility is demonstrated.
3. Separate invalidation from evaluation: mark potentially stale nodes, recompute only as required by a reader or scheduled observer, and suppress downstream work when the value is unchanged. An observed effect can still require immediate computation outside a batch; lazy does not mean every computation waits indefinitely.
4. Test batching, reads during a batch, diamond consistency, cycles, exceptions, dynamic dependencies, observer disposal and thread ownership before integration. Consider dependency-link reuse after measurement shows the graph bookkeeping matters.
5. Run the existing unchanged full-list, virtualized and themed performance comparison. Report rendering time and UI-thread allocations as well as graph work; fewer getter executions alone do not demonstrate faster rendering.

The ContentPresenter fix is measured separately. No lazy graph rewrite is included in that package, and no release claim should imply this investigation has already produced a speedup.
