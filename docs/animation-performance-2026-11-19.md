# Animation first-slice verification — November 19, 2026

Scope: optional opacity/translation destinations, lazy per-frame controller,
native WPF clocks and immediate lifecycle cleanup. See [the contract](animation.md).
This is local implementation evidence, not publication or hosted release acceptance.

## Environment and method

Windows 11 x64 (reported build 26200), SDK 10.0.402, runtime 10.0.13. The system
dotnet resolved SDK 11; installed the official SDK 10.0.402 into
`C:/Users/iotero/.local/share/dotnet-10` and selected it through process-local
PATH/DOTNET_ROOT. Repository SDK/version pins were not changed.

Ordinary rendering compares pre-animation HEAD
`dc570c709807cecacd0ed0ef341fc0c6aef615e7` with archived dirty candidate sources.
Both revisions execute the same candidate Counter harness. Seven fresh-process
samples per side follow one discarded warmup per side, with alternating order.
Each campaign retains full-list, virtualized and themed-full-list scenarios at
1,000 rows / 50 mixed operations. Builds, tests and the active-clock campaign do
not run concurrently with measured ordinary rendering processes.

The accepted baseline `78c88fb901c202e3c2e49b6de300d1ce2369e00b` and all budgets
remain unchanged. These comparisons use pre-feature HEAD for attribution, not
the accepted release baseline. No hosted workflow or package publication ran.

## Initial ordinary-view result and focused change

The initial implementation added a nullable controller field to each Node as
well as optional controller storage on its frame, with setup inline in Patch.
Full-list median mount time rose from 5,230.78 to 6,253.82 ms (+19.56%), exceeding
the unchanged 10% timing budget. Full-list updates rose 2.00%. Virtualized
mount/update changes were -3.57% / +0.44%; themed mount/update were -2.83% / -0.80%.
Allocation increases were 0.13–0.31% for mount and 0.63–1.06% for updates.
Component work was unchanged. The full-list mount ranges overlapped substantially
(baseline 4,851–7,627 ms; candidate 5,626–7,582 ms); this is not causal proof of a
19.56% per-node cost, but it is a failed measured budget and was investigated.

The focused revision removes duplicate Node controller storage and isolates
clock/policy setup in a non-inlined helper. Default reconciliation checks only
current/previous optional motion metadata. Cleanup looks up the optional frame
controller. This reduces per-node storage and keeps setup out of the large Patch
method; performance impact must be judged from the separate final campaign.
The initial failed comparison and original changed runtime files are retained.

## Final ordinary-view comparison

All 24 local comparisons pass the unchanged budgets against pre-feature HEAD.
This is bounded overhead, not a zero-cost or universal-speedup claim:

- Full-list mount: 3,816.90 → 3,935.52 ms (+3.11%); 50 mixed updates:
  3,210.04 → 3,370.23 ms (+4.99%). Mount allocation +0.10%; update allocation
  +0.34% (466,487,000 → 468,058,216 bytes).
- Virtualized mount: 409.88 → 446.38 ms (+8.90%); updates:
  443.18 → 422.52 ms (-4.66%). Mount allocation +0.29%; update allocation
  +1.05% (47,885,296 → 48,389,864 bytes).
- Themed full-list mount: 3,954.96 → 4,055.52 ms (+2.54%); updates:
  4,021.16 → 4,032.07 ms (+0.27%). Mount allocation -0.02%; update allocation
  +0.32% (471,119,016 → 472,626,504 bytes).
- Component work is identical on both sides: full/themed mount/update Body
  counts 1,022 / 5,141; virtualized 27 / 165. Mount/unmount counts also match.

Final baseline absolute timings differ substantially from the initial campaign.
The fresh paired comparison establishes the reported final overhead; it does
not isolate how much of the cross-campaign timing change came from the storage
optimization versus machine/cache variation. The initial regression remains
evidence. In particular, virtualized mount is close to the 10% limit and is not
described as improved. All 64 current core/WPF source and contract files match
the final campaign's archived candidate manifest.

## Rapid-retargeting result

Seven alternating pairs plus discarded warmups compare native-clock replacement
with immediate targets on the final controller. Each process mounts 32 retained
text frames, warms up 30 updates, then performs 240 updates with 160 ms timelines
and a requested 8 ms timer interval. All three channels change together. This
is deliberately stressful interruption, not a typical occasional feedback effect.

Windows motion is disabled on this machine. The initial public-path pilot
correctly refused to claim active-clock measurements. The final diagnostic
explicitly exercises the internal controller, preserving the OS setting. Public
policy suppression is separately covered by regression tests. This distinction
is recorded in every report. The isolated workload is not an end-to-end public
animation-policy benchmark and is not compared against a nonexistent old feature.

Medians of seven process-level results:

- Synchronous update median: 0.143 ms immediate, 4.000 ms animated. Median of
  per-run p95 update latency: 0.421 ms immediate, 8.927 ms animated.
- Total synchronous update time over 240 operations: 43.54 ms immediate,
  986.24 ms animated.
- Synchronous update allocation: 7,101,616 bytes immediate, 44,054,728 animated.
  Total measured UI-thread allocation including dispatcher/render work:
  7,582,832 bytes immediate, 57,587,824 animated.
- Exactly 240 root Body builds in both modes; no component bodies are used.
  Every animated run confirms 32 frames with active opacity clocks at its last
  update; every immediate run confirms zero.
- Host construction: 106.32 / 104.89 ms and 854,608 bytes in each mode. This
  excludes subsequent controller setup. Controller setup medians: 0.221 / 0.506
  ms and 18,824 / 19,504 bytes, immediate / animated respectively.
- Median of per-run p95 rendering-callback gaps: 17.62 / 17.59 ms. These are
  callback intervals from an offscreen HWND, not presented-frame FPS or proof
  of visual smoothness.

Native-clock replacement carries substantial allocation and CPU cost. The
implementation avoids application state writes and per-frame component builds,
but that is not evidence of free animation or universally lower latency.
No clock-cost budget or baseline was invented after measurement.

## Correctness and evidence

All 196 Release regression tests pass, including 16 animation checks. The
initial 12-test run had one test-fixture failure: visual traversal occurred before
layout in the native-transform test. Adding layout fixed the fixture; no runtime
behavior was changed for it. The failed TRX is retained alongside the final suite.
The final suite ran after the controller-storage optimization. Public API analyzer
checks pass; new declarations are in Unshipped, with shipped signatures unchanged.
Curated/generated graph validation, documentation validation and generated shipped
skill-reference freshness checks pass. The new API is documented separately as
unreleased; the published-package reference remains unchanged.

Raw reports and manifests live in
[`performance-evidence/2026-11-19-animation`](performance-evidence/2026-11-19-animation/README.md).
Full local source/binary snapshots, baseline archive, build logs and pilots remain
under `artifacts/animation-2026-11-19/`. The first public-path rapid pilot failed
with `Windows motion is disabled; active-clock diagnostics cannot run`; it made
no performance claim and did not change OS preferences.

No new physical keyboard, screen-reader, HWND composition or visual smoothness
acceptance is claimed. Native WPF child transforms and input eligibility,
measured layout size, clock continuity/cleanup, recycling and reduced-motion
suppression are automated checks. Existing navigation behavior remains separate.
