# Bounded reporting evidence — November 13, 2026

See [protocol, findings and limitations](../../fw018-bounded-reporting-2026-11-13.md).

`validation.zip` preserves three 60-batch native runs and six 44-cycle browser runs,
their complete spooled sample files, reconstructed reports, browser per-cycle files,
logs and exact measured sources. It also includes reporting controls, the short-run
regression, analyzer fixtures, final Release log/TRX/stress evidence and tooling.
Browser profiles remain local and are excluded. All 1,049 archive entries were
verified against the byte counts and SHA-256 values in `manifest.json`.

Every native soak sample (189 total) and browser sample (264 total) was verified
collected before another sample was recorded, with the final sample checked before
report reconstruction. Including the native deliberate-root case, all 190 native
raw sample files match reconstructed reports; all browser raw/per-cycle files match
as well. Existing ownership, process-exit and shutdown checks pass. Framework source
hashes match the prior retained-reporting native campaign.

Release validation passes 172 tests, 15 visual checks, 21 stress assertions,
reporting controls, one-batch regression and window/shutdown checks. No paired
latency or hosted performance gate was rerun; this is sample reporting tooling,
not a framework runtime optimization. No memory budget is approved. Residual heap
and handle growth is recorded in the findings and remains open in FW-018.

Previous retained-reporting measurements and failed isolation pilots remain in
the [separate archive](../2026-11-13-reporting-retention/README.md).
