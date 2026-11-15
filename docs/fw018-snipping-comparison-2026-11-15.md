# FW-018 manual Snipping Tool comparison - November 15, 2026

Status: one manual matched pair completed; FW-018 remains open. No budgets or baseline changed.

## Setup interference

The initial 600-batch control failed after batch 19 while Snipping Tool was
being prepared through Windows automation. Further controls failed before the
first recorded batch. One attempted close failed with unavailable input geometry;
the file named clean-closed is not evidence that Snipping Tool was closed.
A later close shortcut was sent, but closing was not verified before the next
failed control. These are preserved failed pilots, not clean controls.

The sample now includes surviving-object counts in its collectability error and
an opt-in collectability-failure rendezvous through the existing diagnostic
checkpoint mechanism. It still throws the original acceptance failure. Normal
success criteria and framework code are unchanged.

A separate failure dump identifies 21 surviving editors, associated payloads and
popups, while hosts, components and logical payloads are collected. A sampled
editor is rooted through a ref-counted MS.Internal.Automation.ElementProxy,
VirtualRowAutomationPeer, VirtualListAutomationPeer, cached row peers and a
TextBoxAutomationPeer. This proves an accessibility root for that sampled editor;
it does not identify the external client or prove every survivor has that root.
The Windows automation session was reset before the manual comparison. No further
window enumeration, accessibility inspection or automated input was performed.

## Manual experiment

Both conditions use identical Release binaries and 500 rows, four workspace cycles
per batch, three warmup batches and 600 measured batches. The capture condition
runs first. The user supplied a screenshot showing the normal detail-screen
navigation step. Request/receipt markers bound the capture to batches 47-91;
the exact capture instant is not instrumented. Both conditions completed. Earlier
failures are not discarded. The conservative interval includes the batch immediately
after the receipt marker (whose last completed batch is 90).

Batch timings describe scripted workspace cycles, not frame cadence. A frozen
Snipping Tool overlay does not by itself demonstrate a stopped dispatcher. The
one-pair experiment is diagnostic evidence, not a hosted performance gate or a
representative memory budget. Background applications remain uncontrolled.

## Results and interpretation

Both runs pass all ownership assertions, bounded-reporting validation and dispatcher
shutdown: 603 batches / 2,412 workspace cycles each, 4,824 cycles total. All 1,206
raw samples match reconstructed reports. The final binaries and measured-source
snapshots are preserved. Focused negative-retention and real-window checks pass,
as do native-memory and dump-parser fixtures. Framework libraries are unchanged;
this turn did not rerun the full release suite or hosted performance gate.

Both runs start at 453 handles and end at 441 (-12), retain 20 GDI objects, and
move from 17 to 16 USER objects. The capture run's collected heap changes +2,840
bytes; its four quarter medians are identical. The clean run changes +43,408 bytes,
with stable medians in the final three quarters. The latter step is not root-attributed
here and is not subtracted as an assumed cache size.

Median batch times are 204.73 ms with capture and 196.92 ms without (approximately
4% higher in the capture run). Median UI-thread allocations are 50,692,944 and
50,946,752 bytes per four-cycle batch respectively. Both runs perform 183,312 native
mounts. These are workload observations, not isolated mount/update benchmarks.
The single pair is capture-first and background load is uncontrolled: neither the
time difference nor allocation difference is evidence of a capture-caused regression
or optimization. Accepted performance workloads and budgets remain unchanged.

During the conservative capture interval (batches 47-91), median batch duration is
226.03 ms, maximum 490.91 ms; handles and GDI/USER counts are flat across its endpoints.
The equivalent clean interval median is 198.44 ms. Progress continues between the
request and screenshot receipt; no multi-second batch stall or persistent resource
step is reproduced. Frame presentation was not measured. A visually frozen capture
overlay is consistent with continued test execution, but the exact original pause
cannot be reconstructed from this pair.

The capture run records 28 pointer moves, no buttons/keys/activation. The clean run
records no observed input or activation. Resetting the automation session preceded
the successful pair, but the dump identifies a root mechanism, not the external
client's identity or a complete explanation for previous handle growth.

Conclusion: this manual capture did not reproduce lasting handle/GDI/USER growth.
Do not attribute the earlier resource outlier to Snipping Tool or close FW-018.
Remaining work includes external accessibility-client retention characterization,
other interactive resource steps, representative budgets and contextual errors.

[Preserved evidence](verification-evidence/2026-11-15-snipping-comparison/README.md)
includes all failed pilots and the sampled accessibility root, without raw dumps
or redistribution of the user's screenshot.
