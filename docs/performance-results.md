# First rendering optimization results

Recorded September 17, 2026, on .NET 10.0.12 in Release. These are individual development-machine measurements, not statistical benchmarks or FPS results.

## Matched reference / optimized comparison

Two consecutive fresh processes ran the same current executable, each mounting 1,000 rows before timing 50 deterministic mixed operations with WPF dispatcher processing and offscreen layout after each operation. Reference mode disables result filtering and ignores Memo inputs; optimized mode enables both. The data size and operation sequence are identical.

| Measurement | Reference mode | Optimized mode | Reduction |
| --- | ---: | ---: | ---: |
| Elapsed time | 18.651 s | 16.798 s | 9.9% |
| Component body builds | 32,350 | 5,141 | 84.1% |
| Allocated bytes on UI thread | 870,091,312 | 603,782,312 | 30.6% |

This comparison excludes initial mount timing and is not a comparison of two historical binaries. Native control/layout work remains significant; the body-build reduction does not translate into an equivalent elapsed-time reduction. Reference ran first, so runtime/system conditions and order can affect timing. Reproduce with the commands in [stress-lab.md](stress-lab.md).

## Full workload validation

The default 200-operation workload retained the same 1,000-row stress load and passed all 21 assertions. It produced 20,559 body builds for the mixed phase, compared with 132,570 in the saved pre-change run. A keyed shuffle required 2 body builds; a 10,000-write burst required 1. State/control retention and complete component disposal passed. The framework regression suite passed 90 checks.

Elapsed time for the separate full workload was 66.152 s versus 47.237 s in the earlier saved run. Those runs were not an interleaved controlled comparison; the saved baseline alone does **not** demonstrate an overall wall-time improvement. Both this result and the matched comparison are retained so fewer body builds are not presented as proof of a proportionate speedup.

## Implementation

- State-backed bindings/selectors compare projected results and flatten nested record projections onto one source.
- General Computed values track dynamic dependencies and detach when no longer observed.
- Memo inputs skip unchanged parent-driven component bodies without blocking local invalidation.
- Selection is a boolean selector per row; only the old and new selection notify their row readers.
- Retained subscriptions stay attached across builds, and existing stack margins are no longer reset before being restored.

## Next bottleneck

All 1,000 rows still exist as native controls. Compact-mode changes affect every mounted row, and filters remove/remount components. Viewport virtualization and native container reuse are the next substantial avenues for improving large-list latency. Visible-window focus, IME, and frame-rate behavior still need manual/instrumented UI testing.
