# FW-018 manual focus observation - November 15, 2026

Status: manual observation complete; exact process-resource attribution remains open. No framework source or baseline changes.

The user requested a manual title-bar focus check after Windows rejected the
sample's automatic activation request. The existing diagnostic binary runs 500
rows, four workspace cycles per batch, three warmup and 600 measured batches.
No automation client inspects the window and no automatic focus flag is enabled.
Sources and binary hashes match the preceding focus-control build.

The user confirmed two clicks. Raw telemetry records the first activation at
batch 7, before the click prompt (issued after batch 38), with pointer movement
and one client-area mouse-down. A second activation appears at batch 44 after
the prompt. This is not a title-bar-only intervention or a pristine opening
baseline; it combines observed pointer activity, a client-area click and focus.
The first activation raises GDI 21 to 27, USER 18 to 27 and handles 456 to 484.
USER drops to 26 in the next batch. The second activation adds no GDI increase.
No key presses are recorded.

The preceding same-binary untouched control has 180 measured batches, versus 600
here. Use it as context for the resource signature, not as a matched timing or
allocation benchmark. Background activity is uncontrolled. A graph query was
running during the early portion and was stopped; no latency claim is made.

## Completed evidence

All 603 raw samples match the reconstructed report and pass the existing native
ownership, bounded-reporting and shutdown validator: 2,412 workspace cycles and
183,312 native mounts. Every recorded survivor count and owned native byte count
is zero. Dispatcher shutdown and cleanup-policy closure pass. No source changed,
so this turn validates the new evidence without rerunning the full framework suite.

Measured endpoints are handles 456 to 471 (+15), GDI 21 to 27 (+6), USER 18 to 25
(+7), collected heap 1,847,168 to 1,872,168 bytes (+25,000). Handles peak at 484
at the first activation, then decline. All four collected-heap quarter medians
are 1,872,168 bytes. Final 100 batches have exactly 471 handles, 27 GDI and 25 USER
objects; GDI remains 27 throughout the post-activation interval. USER briefly
reaches 28, so the resource history is preserved rather than reduced to one endpoint.

After dispatcher shutdown (before process exit), counts are 403 handles, 14 GDI
and 6 USER. The earlier untouched same-binary control reports 392, 10 and 3.
This is not proof all process resources returned to the untouched level. These
counters include runtime/provider/window resources outside tracked editor ownership;
the final managed-memory sample also includes reconstructed reporting data and is
not a comparable collected-heap checkpoint. The process exits successfully.

The resource signature reproduces the earlier +6 GDI step at activation, while
managed editor collectability remains intact. It strengthens focus/input-related
initialization as an explanation for that pattern, distinct from the accessibility
proxy's retained-editor graph. Because the first event also includes pointer and
client-area input, it does not isolate title-bar focus, IME or a particular allocator.
The second activation adds no GDI step. This is one manual run, not proof of an
unbounded leak or proof that every historical outlier has the same cause.

## Follow-up

FW-018 remains open. Identify the actual GDI/USER/handle resources created on first
activation and determine their ownership/cache lifetime before approving a budget.
Keep the production automation-peer cache-lifetime work separate: this experiment
changes no renderer behavior and shows no retained editors. No performance release
gate, accepted baseline or budget has changed.

[Evidence archive](verification-evidence/2026-11-15-manual-focus/README.md) preserves
raw samples, click request/receipt markers, analysis and exact source/binary manifests.
The earlier shorter untouched report is included for context.
