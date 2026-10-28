# FW-005 virtualized editing — October 24, 2026

Implementation and automated verification cover the framework's row lifetime;
physical IME acceptance remains open. The predecessor is `be6e1d1`, committed
and pushed before starting this item. No package was published for this work.

## Behavior and ownership

The nearest `VirtualRowPresenter` owns routed WPF composition lifetime for its row.
Class handlers observe composition without three per-presenter subscriptions.
Native events are matched by editor source, not composition-object identity.
Before detachment, active editing loses keyboard focus while the original
control and binding remain attached, allowing WPF's normal TSF/IMM finalization.
A generic provider receives a virtual `Complete()` fallback if focus loss did
not end it. Stored native `FrameworkTextComposition` snapshots are not completed
later. Completion failures are reported after cleanup. The framework does not
synthesize candidate text or serialize an IME session.
Containing virtual lists do not also own the inner editor's session. An added
nested-list regression exposed two completion attempts in the first implementation
when the provider threw; nearest-row ownership fixes that case and inner-only
removal while the outer row remains mounted. The pre-fix failure is preserved.

WPF can keep a focused container outside the viewport. The framework leaves
that editor and its composition attached. Once a container is released,
logical component state and the resulting selection can restore into a new
native editor. Removed keys discard snapshots. HWND-based islands whose input
does not route through WPF retain their own native input ownership.

The investigation also reproduced a focus race: another editor selected after
container release but before reattachment was mistaken for permissible previous
focus. The new regression failed against the existing implementation. A snapshot
now weakly identifies the original focused element instead of treating whatever
is focused during reattachment as its owner. Restoration respects a newer editor
both before and after the callback is queued. Presenter release still cancels
queued work, including when a key is removed/reinserted.

Core state, public APIs, binding semantics, virtualization size, benchmark
operations and accepted budgets have not changed. Existing detached-node input
guards are exercised explicitly, rather than introducing a second binding path.

## Verification scope

Real-window tests exercise actual viewport recycling, bounded realization,
forced regeneration, keyed reorder, removal/reinsertion, stale native text and
routed input, selection and focus restoration, composition start/updates/commit,
empty-result cancellation, disposal and provider failure. The composition probe
subclasses public `TextComposition` to verify virtual provider dispatch. It is
not a TSF implementation or a physical IME.

The initial performance comparison uses `tools/Test-Performance.ps1` against `be6e1d1`,
the same candidate harness on both sides, SDK 10.0.402/runtime 10.0.13, 1,000 rows
and 50 mixed operations. Each of four scenarios has a discarded warmup per side
and seven alternating measured pairs. Full-list, virtualized, themed full-list
and layout editors are all retained. No builds or profiling run alongside the
timed processes.

### Correctness evidence

The final Release build has zero warnings/errors. All 134 regression tests, 15 visual
checks, 21 stress assertions, documentation/curated-graph checks and the actual
window startup/editing/disposal check pass. The focused virtualization/editing
selection has 29 passing cases. The manual acceptance executable also builds
with warnings treated as errors and passes its setup smoke, including real
focus, bounded/unclipped realization, detached-input rejection and scrolling
to row 999. This is not physical IME acceptance.

Running the current regression tests against archived, unchanged `be6e1d1`
framework sources produces seven failures out of thirteen selected cases: the
before-reattachment focus race, missing provider completion on regeneration,
removal and disposal, missing provider-failure propagation, and the two nested
composition cases. The other six
cases pass. This is deliberately failing regression evidence, not a failed
candidate validation or a baseline source modification.

Source review then found two native-specific assumptions in the initial mock
approach: WPF allocates a fresh composition object per phase, and its native
`Complete()` only flags the currently handled event. The installed WPF 10.0.13
assembly confirms that behavior. [TextStore's phase replay and focus-loss path](https://raw.githubusercontent.com/dotnet/wpf/v10.0.13/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Documents/TextStore.cs),
[TextEditor keyboard-focus handling](https://raw.githubusercontent.com/dotnet/wpf/v10.0.13/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Documents/TextEditor.cs)
and [FrameworkTextComposition](https://source.dot.net/PresentationFramework/System/windows/Documents/FrameworkTextComposition.cs.html)
support the corrected source tracking and focus-driven finalization. Three added
tests fail against the prior implementation and pass after correction, including
flat/nested final-selection capture. They still do not exercise a physical IME.

### First performance comparison

These three predecessor/control comparisons use the first implementation before
the nested ownership and native focus-loss fixes. Their original source and
binary manifests remain separate from final-code validation.

| Scenario | Mount time | Update time | Mount allocation | Update allocation |
| --- | ---: | ---: | ---: | ---: |
| Full list | +14.75% | -3.81% | -0.01% | -0.02% |
| Virtualized | -3.15% | -7.01% | +0.00% | +0.08% |
| Themed full list | -1.71% | -1.02% | +0.03% | +0.00% |
| Layout editors | +1.02% | -0.31% | -0.00% | +0.00% |

Component work matches in every scenario. **This comparison fails** the unchanged
10% full-list mount-time budget; the other 31 checks pass. Full-list mount pairs
range from -8.78% to +51.18%; their paired-change median is +7.27%, distinct from
the +14.75% ratio of medians used by the gate. Neither statistic replaces the
other. A timing-only failure in the nonvirtual workload needs investigation;
the measured virtualized improvement is not proof that this change speeds up
the framework. No accepted baseline or budget is advanced.

### Identical-binary timing control

Seven alternating full-list pairs run the exact same candidate payload on both
sides, with script-verified SHA-256 equality. All eight descriptive checks pass:
mount time -2.71%, update time +2.00%, mount allocation -0.01% and update
allocation +0.05%; component work matches. Mount pairs range from -17.28% to
+7.87%, with a -0.99% paired-change median. This demonstrates process timing
variation without a code difference, but does not attribute the first comparison's
+14.75% result or convert it into a pass. The control is never release eligible.

### Focused predecessor repeat

A further seven alternating full-list pairs, with unchanged code, harness,
data, operations and budgets, pass all eight checks: mount time +4.38%, update
time +4.37%, mount allocation +0.01%, update allocation -0.06%, identical
component work. Mount pairs range from -0.94% to +23.90%; their paired-change
median is +1.56%.

The first over-budget mount median did not reproduce. This repeat does not
erase the initial failure, establish a causal explanation for its exact size,
or turn the original 31/32 result into a clean full pass. The measurements show
startup timing uncertainty and no allocation/component-work regression beyond
the unchanged budgets. They do not prove a framework speedup or native IME
latency. Any publication still needs exact committed-source validation against
the accepted source, separately from these predecessor diagnostics.

Two accepted-source runs were deliberately stopped to correct the nested
ownership issue and then the native source/focus assumptions. Partial reports
and interruption records remain in `performance-accepted-source` and
`performance-accepted-final`; neither has a completed gate result. Final-code
accepted-source validation is collected separately as `performance-native-focus-final`.

### Final code against the accepted source

All **32 checks pass** against unchanged accepted revision
`78c88fb901c202e3c2e49b6de300d1ce2369e00b`, with the same protocol, SDK/runtime,
1,000 rows, 50 operations, discarded warmups and seven alternating pairs.

| Scenario | Mount time | Update time | Mount allocation | Update allocation |
| --- | ---: | ---: | ---: | ---: |
| Full list | -7.24% | -31.01% | -1.06% | -25.22% |
| Virtualized | -0.22% | -1.21% | -1.41% | -4.63% |
| Themed full list | -0.30% | -68.98% | -2.90% | -57.09% |
| Layout editors | +3.57% | -16.14% | +0.14% | -3.78% |

Component work is identical in all scenarios. The measured editor mount
increase is retained, within the unchanged 10% time/2% allocation budgets;
passing budgets is not a claim of zero latency increases. These comparisons
include the preceding framework milestone and do not isolate FW-005's cost or
benefit. They measure neither native IME latency nor whole-application frame rate.
The dirty candidate is identified by frozen source and binary manifests. Exact
committed-source CI/package validation is still required for any publication.

### Preserved evidence

[Evidence directory](performance-evidence/2026-10-24-virtualized-editing/)
contains each comparison's raw warmup/measured reports, process timing, logs,
summary, paired analysis, source/binary manifests, frozen candidate-source
archives, verification sources, and correctness TRX/logs. The root source archive
belongs to the first iteration; later interrupted/final runs have their own
archives. `archive-manifest-before-final.json` retains the first packaging record;
`archive-manifest.json` hashes the expanded evidence set. Final measured source
and runtime-binary hashes are checked against the current tree before packaging.
No failed or interrupted report is overwritten or reclassified.

## Remaining physical IME acceptance

The current user's language list reports only `en-US`. Actual Japanese/Chinese candidate
selection, TSF commit/cancel and candidate-window lifetime were not exercised.
Follow the [visible-window checklist](virtualization.md#physical-ime-acceptance-still-required)
on the intended release candidate. Record input language, Windows/runtime/package
versions, resulting text ownership, selection/focus and shutdown behavior.

FW-005 must retain this pending acceptance explicitly; routed event tests do not
justify a native IME readiness claim. No additional backlog item is started by
this change.
