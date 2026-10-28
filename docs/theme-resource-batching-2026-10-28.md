# Theme resource batching — October 28, 2026



## Cause and controlled experiments



The residual editor allocation cost is concentrated in theme application. Isolated

copies of accepted/current source used identical allocation checkpoints around

host construction, native attachment, theme application and layout. One excluded

warm-up and three measured alternating samples retained the same 1,000 editors

and 50 mixed operations; component work matched. Diagnostic sources and reports

are preserved separately from the unmodified release benchmark harness.



Theme application allocated 11,587,728 bytes with the accepted source and

15,008,424 with the TextBox prototype. Host construction was cheaper in the

prototype; attachment and layout were nearly unchanged. Removing the redundant

disabled-background trigger saved only hundreds of bytes during theme application,

so that change was not retained.



A second controlled comparison batched resource publication using BeginInit/EndInit.

Theme allocation fell from 15,008,424 to 7,529,240 bytes. Total mount allocation

fell from 128,968,632 to 121,505,040 bytes: about 7.46 MB, with identical component

work. Diagnostic mount time increased slightly (about 1.4%); do not interpret the

allocation saving as a proven latency improvement.



WPF's [ResourceDictionary implementation](https://source.dot.net/PresentationFramework/System/Windows/ResourceDictionary.cs.html)

invalidates owners on initialized resource updates and defers those invalidations

between BeginInit and EndInit. Publishing palette/styles individually causes

repeated resource-change tree walks. Batching removes that repeated work without

removing controls, operations, styles or live contrast support.



## Implementation and ownership



ThemeStyles.Apply batches its palette and five native styles in the existing

resource dictionary. Local and merged resources are preserved. WPF rejects nested

BeginInit; the method catches that rejection only around BeginInit and joins a

caller-owned initialization without ending it. A finally block closes only a

scope opened by ThemeStyles. Failed style construction does not leave that scope

open. No public API, benchmark workload, budget or accepted baseline changed.



New regressions verify caller-owned initialization, unrelated local/merged

resources, and cleanup after invalid color construction. Existing contrast,

editing, selection, focus, popup and lifecycle tests remain applicable.



The full correctness suite is 144/146 passing: only the two previously reproduced

virtual-row discovery failures remain. All five contrast and two resource-update

checks pass. FW-006 remains open for those failures and physical contrast/IME

acceptance; performance results follow below.



## Seven-pair validation

The unmodified Test-Performance harness passes **32/32 gates**, using SDK 10.0.402,
runtime 10.0.13, seven alternating fresh-process pairs and one discarded warm-up
per side. Full-list, virtualization, themed full-list and editor workloads remain
unchanged; accepted baseline 78c88fb and all budgets are unchanged.

| Metric | Accepted median | Candidate median | Change |
| --- | ---: | ---: | ---: |
| Themed mount allocations | 327,693,704 B | 328,357,512 B | +0.20% |
| Themed mount time | 3,682.37 ms | 3,702.80 ms | +0.55% |
| Editor mount allocations | 126,193,944 B | 121,501,184 B | -3.72% |
| Editor mount time | 2,058.00 ms | 2,055.20 ms | -0.14% |
| Editor update allocations | 1,161,579,664 B | 1,123,291,632 B | -3.30% |
| Editor update time | 8,905.20 ms | 8,448.94 ms | -5.12% |

Component build/lifecycle counts match. Full-list mount time is +4.02%, within
its unchanged budget; report this alongside improvements. Prior timing variation
and failed experiments remain preserved. This local result does not close the
two correctness failures or physical acceptance requirements, and no publication
was performed.

The [evidence archive](performance-evidence/2026-10-28-theme-batching/manifest.json)
contains phase experiments, ineffective trigger removal, batched diagnostics,
source snapshots, raw paired runs, TRX results, logs and the unchanged budget policy.


Final checks: solution build has zero warnings/errors; 15 visual checks and
21 stress assertions pass. Actual-window startup/editing succeeds and active
work after disposal is zero. The full 146-test result remains 144 passing and
two known discovery failures; no tests or budgets were weakened.
