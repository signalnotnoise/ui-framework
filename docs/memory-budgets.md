# Scoped sustained-memory acceptance

The policy in [memory-budgets.json](../tools/memory-budgets.json) passed all 12
fresh native/browser runs on November 15, 2026. Numerical limits were frozen before
measurement; only the validation-status metadata changed afterward. See the
[dated evidence](fw018-memory-budgets-2026-11-15.md). A parser test alone does not
approve a budget.
This policy is separate from the existing rendering timing/allocation baseline.

## Workloads and ownership

Native acceptance covers three fresh processes each at 500 and 5,000 logical rows,
with three warmup batches and at least 180 measured batches of four workspace cycles.
The existing window, deliberate-retention and visited-state/removal checks must pass.
Every measured batch must release all tracked hosts, components, logical/native
payloads, editors and popups, with balanced release counts, zero owned native bytes
and no wrong-thread releases. Native interaction or missing activation evidence makes
the campaign ineligible for the quiet profile; it is never silently filtered out.

Browser acceptance covers three fresh-profile and three reused-profile processes,
each with four warmup plus at least 128 measured parent/popup cycles on the local
acceptance pages. Each cycle must release both owners exactly once, complete its
popup deferral, collect tracked references and observe every captured isolated
browser process exit. A deliberate retained owner must be detected and then collect
after its root is removed. This does not budget arbitrary websites, authenticated
sessions or a long-lived application's entire browser process tree. The browser
harness does not monitor native-HWND user input; leave its windows untouched.

Reports must match all spooled raw samples, prove bounded reporting and completed
dispatcher/cleanup shutdown, and use matching framework source hashes and SDK/.NET
versions across campaigns. Browser runtime versions must be consistent across all
browser cycles. No inferred cache size is subtracted from any observation.

## Limits

For each resource, evaluate the peak of every measured sample and two differences:
the median of the final 32 samples minus the first 32, and the final median minus
the 32-sample window beginning at the measured midpoint. The minimum run lengths
keep those windows distinct. Every process must pass; passing runs cannot average
away a failing process. Negative changes remain negative rather than being hidden.

Native post-cleanup collected heap has an 8 MiB ceiling, 128 KiB total-window growth
limit and 64 KiB late-window growth limit. Browser host collected heap uses the same
ceiling, 256 KiB total and 64 KiB late growth. These permit bounded initialization
steps while still testing continuing drift; they are finite-workload budgets, not
proof against every slower leak.

Native private bytes have a 512 MiB ceiling; the browser host has a 384 MiB ceiling.
Both allow at most 32 MiB total-window growth and 16 MiB late growth. Handle ceilings
are 768 in either host. Native handles may grow by 16 total / 8 late; browser-host
handles by 32 total / 16 late. GDI has a ceiling of 64 and growth limits of one;
USER has a ceiling of 96, total growth of four and late growth of two.

The browser's captured live process group must stay within 16 processes and 512 MiB
summed private bytes for the small local-page workload. Working sets are retained
as observations, not summed as exclusive memory: shared resident pages and external
memory pressure make such a sum misleading. Live process snapshots are sampled
before close; transient peaks between snapshots are outside this guarantee.

Calibration used preserved bounded-reporting native/browser runs. Whole-process
ceilings provide regression headroom above those observations, not recommended
application capacity. The tighter late-growth checks deliberately retain failures
for an older browser +20-handle late step and the older native GUI/input step.
Do not increase a limit merely because a fresh run fails. Investigate it, preserve
the policy and failed evidence, and justify any future policy revision explicitly.

## Running and evidence

Use the recorded SDK/runtime and a fresh directory on an idle interactive desktop:

```powershell
pwsh -NoProfile -File tools/Test-MemoryBudgets.ps1 -OutputDirectory artifacts/new-memory-acceptance
```

The driver copies the policy before measuring, runs the existing native/browser
workloads, then evaluates that frozen copy. It rejects diagnostic checkpoint/focus
overrides. Missing or malformed evidence and budget failures produce a nonzero exit;
the evaluator preserves its result in a new file. Never reuse the output directory.
Source manifests, raw samples, runtime versions, failed runs and policy hash belong
with the acceptance record. Profiles remain isolated and local.

CI runs the evaluator's sensitivity fixtures. The sustained desktop campaign is an
explicit local acceptance command; it is not claimed as a hosted release gate.
Additional contextual failure diagnostics (FW-025) and physical Narrator
revalidation after peer-cache cleanup (FW-026) are separate post-v1 follow-ups,
per the November 15 user scope decision. No new physical pass is claimed.
