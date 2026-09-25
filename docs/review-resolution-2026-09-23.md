# Review resolution — September 23, 2026

This addresses the static review of f381269958aa7113c63d3b47c467781a05d8898f. The hardening changes were committed and pushed as d5a94b2; they are not a published package. The existing sample diagnostic work was retained. See the subsequent [validation and notification cost investigation](validation-costs-2026-09-23.md) for the focused follow-up.

| Review finding | Resolution |
| --- | --- |
| 1. Partial rendering transactions | Defined two failure boundaries: build/validation retain the good tree; patch failures clear and dispose the affected tree and permit explicit rebuild. Added child-body, partial sibling mutation, native update, replacement/removal cleanup, stale callback and lifecycle-error checks. Arbitrary application side effects are not rolled back. |
| 2. Unbounded STA tests | Named background STA workers have a 60-second limit and fail the isolated test host with thread/state/elapsed diagnostics. Release validation also retains test-runner hang diagnostics. |
| 3. Mutable workflow actions | Resolved the existing action tags to commit SHAs, retained version comments and added weekly Dependabot updates. Documentation validation rejects mutable references. |
| 4. Adaptive layout LINQ | Replaced per-row Max enumeration with a loop. Added an optional themed layout/editor workload without removing the full-list comparison. |
| 5. StateList snapshots | Retained and documented stable snapshot semantics; regression-tested mutation during enumeration. No incompatible fail-fast enumerator or span over mutable storage was introduced. |
| 6. Editor writes | Guarded unchanged read-only, max-length, undo-enabled, selected-index and caret assignments; retained binding correction and initial local-value precedence over later styles. |
| 7–8. Malformed public views | Validate dimensions, editor limits, enums, collections, child structure and component/platform metadata before patching. Retained the public record API. |
| 9. Notification affinity | NotifyChanged verifies the creating thread, including custom subclasses. |
| 10. Subscriber exceptions | State and Computed notify all captured subscribers, then rethrow one error or aggregate multiple errors. No invocation-list array is allocated on successful notification. |
| 11. Virtual row ownership | Ensure capture/release failures do not skip cleanup; finish cleanup for every realized row. Added native recycling, throwing release and focus-queue checks; existing removed-key/fresh-state checks remain. |
| 12. Focus restoration | Capture framework keys and visual paths, restore the specific editor and TextBox selection, cancel pending callbacks on release, and respect focus moved outside a row. Native/unkeyed segments require stable structure. |
| 13. Native ownership | Existing XML/API ownership contract already explicitly specifies release and no implicit IDisposable. Updated failure-time release behavior in the guide. |
| 14. Documentation drift | Refresh index and graph, distinguish historical evidence, and check current README/changelog/graph versions against package metadata. |
| 15. Template placeholder | Removed Class1.cs and its compilation/archive exceptions. |
| 16. Test dependency management | Kept explicit versions in the only test project; documented paired adapter/framework upgrades and Windows/.NET 10 validation. Central package management remains unnecessary at this project count. |

See [error recovery](error-recovery.md), [virtualization](virtualization.md), and [release gates](releasing.md) for the supported contracts and remaining limitations.

## Correctness evidence

Test-Release passed: Release build with no warnings/errors, 87 MSTest tests, graph/version/action-pin validation, 15 visual checks and 21 full-stress assertions. The stress campaign includes 1,000 rows and 200 mixed operations. Real-window tests exercise keyed editor reorder, selection restoration, external focus movement and themed automation peer patterns.

Test-Packages -NoBuild passed using a fresh package-only consumer: transitive dependencies, rendering, events, retained controls, themed rich button content and 1,000 native-island updates. Nothing was published. Documentation-check negative probes rejected both a mutable action reference and a mismatched README version.

A deliberately hung child process linked the actual StaTestRunner implementation. The 60-second timeout emitted the named test/thread diagnostic and terminated the process; the parent observed exit after 65.00 seconds with nonzero exit code -2146232797. Evidence is in artifacts/sta-timeout-probe/result.json and result.log. Both final performance source manifests matched the working tree with zero source changes after their builds.

Local raw correctness evidence: artifacts/release-validation and artifacts/package-validation/744faae057a84365b30e07430414b044.

## Performance evidence

The accepted-baseline comparison passed all existing budgets. The reference remains 78c88fb901c202e3c2e49b6de300d1ce2369e00b; tools/performance-baseline.json was not changed. Each scenario used seven measured fresh processes per side, alternating order, plus discarded warmups; both revisions used the same current harness, SDK 10.0.401, runtime 10.0.12, machine, 1,000 rows and 50 operations. No correctness tests ran concurrently with the final comparisons.

Final candidate medians and changes against the accepted baseline:

| Scenario | Mount ms | Update ms | Mount UI-thread bytes | Update UI-thread bytes |
| --- | ---: | ---: | ---: | ---: |
| Full list | 5,290.42 (-1.63%) | 6,378.85 (-1.14%) | 342,491,368 (-1.12%) | 603,632,216 (-5.16%) |
| Virtualized | 376.49 (-0.61%) | 1,117.20 (-4.43%) | 9,593,720 (-1.88%) | 60,601,216 (-5.89%) |
| Themed full list | 4,964.88 (-5.68%) | 12,947.85 (+11.42%) | 327,847,040 (-2.86%) | 1,157,970,896 (+6.36%) |

The themed update regression remains real relative to the accepted reference and is close to its pre-existing 12% time / 7% allocation allowances. Passing those allowances is not a speedup. The initial before-change campaign already exceeded the themed time allowance (+14.43%); that failed evidence is retained. Timing is variable, so small median changes should not be interpreted as universal latency improvements.

Component work is identical on both sides: full/themed lists have 1,022 mount body builds, 5,141 update body builds, 456 update mounts and 658 update unmounts; virtualized lists have 27, 194, 98 and 95 respectively. Lower work counts were not substituted for timing evidence.

Raw evidence and source snapshots are in artifacts/performance/review-complete-accepted. Initial and intermediate evidence remains in review-before-verified, review-after and review-final-accepted; the last two include interrupted campaigns and must not be presented as final gate results.

### Comparison against the reviewed commit

A separate seven-sample campaign against f381269958aa7113c63d3b47c467781a05d8898f also passed the configured budgets. Raw runs, min/max ranges, summaries and the final candidate source snapshot are in artifacts/performance/review-complete-previous.

| Scenario | Mount ms | Update ms | Mount UI-thread bytes | Update UI-thread bytes |
| --- | ---: | ---: | ---: | ---: |
| Full list | 5,366.55 (-1.23%) | 6,720.72 (+0.76%) | 342,564,448 (-0.34%) | 603,469,696 (-0.23%) |
| Virtualized | 384.28 (+0.39%) | 1,179.70 (+4.03%) | 9,593,720 (-0.07%) | 60,597,488 (-0.12%) |
| Themed full list | 4,980.99 (-6.86%) | 12,186.39 (-0.03%) | 327,683,760 (-0.38%) | 1,157,957,464 (-0.12%) |

Component-work counts remained identical. These changes primarily harden correctness: allocation reductions against the immediate predecessor are modest, and the virtualized update median regressed by 4.03%. Inspection of paired samples found six of seven candidate updates slower, with paired changes ranging from -7.73% to +9.82%; the slowdown is retained as an observed regression rather than dismissed or hidden by the older baseline comparison. Guarded editor/caret writes and the allocation-free layout loop are the focused optimization attempts retained here. They do not establish a universal latency improvement, and no baseline or budget was advanced to accept the result.

### Layout/editor diagnostic limitation

The optional layout/editor workload retains 1,000 themed multiline editors and 50 updates/resizes. Baseline and candidate probes stalled in WPF TextStore lock handling, including after adding a native window source and a normal Application dispatcher loop. Stack traces and the initial headless source are preserved under artifacts/performance/review-after. The root cause is not established, and no valid paired layout/editor latency comparison is available.

An isolated candidate experiment that avoided redundant caret writes completed the unchanged workload: 2,142.76 ms mount, 77,510.60 ms updates, 126,223,288 mount bytes and 1,109,226,344 update bytes, with 1/50 body builds and no component lifecycle work. This is a single diagnostic run, not proof of a general IME fix or a measured speedup. The caret guard was retained with regression checks for caret clamping, binding feedback and editor selection. Input methods were not disabled, and the workload was not reduced. Performance processes now fail after 120 seconds instead of hanging a campaign indefinitely.

## Remaining production gates

Automated UI Automation peer checks do not establish screen-reader announcements. Manual IME composition, assistive-technology use, and full-application scrolling/frame behavior remain required before a production claim. Focus restoration does not snapshot native IME state or undo arbitrary application callbacks. Public child/options collections must remain stable during rendering.
