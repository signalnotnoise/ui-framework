# Repeated WPF focus and window lifetime comparison

Two independently built executables share the window, focus, collection and resource
measurement code. `Plain` has no framework project reference and verifies that no
framework assembly loaded. `Framework` wraps the same ten ordinary WPF TextBoxes
in native views under `ViewHost` and `VStack`. Neither uses framework theme templates,
virtualization, an external UI Automation client or a COM cleanup policy.

Each process creates 30 test windows plus one persistent anchor window. Focused
cases alternate activation between the anchor and test window three times per
cycle, verifying the test window is active and the intended editor has keyboard
focus. Quiet cases reject any recorded window activation. Both edit identical text,
detach contents and close each test window; framework hosts are explicitly disposed.
After dispatcher pumping and forced collection, weak probes check all ten editors,
the content root and test window. Separate checkpoints cover dispatcher and STA-thread
exit while the process stays alive. Weak probes do not cover every WPF/OS allocation.

The driver runs one quiet case per implementation and three focused pairs in
plain/framework, framework/plain, plain/framework order. Run on an otherwise idle
interactive desktop with GuidedGrade closed; leave the test windows untouched.
Focus normally occurs automatically. If initial activation is unavailable, the
sample waits up to 90 seconds for title-bar activation and then fails explicitly.
The driver bounds each process at 150 seconds and preserves failed output.

```powershell
pwsh -NoProfile -File tools/Test-FocusComparison.ps1 -OutputDirectory artifacts/new-focus-comparison
python tools/Summarize-FocusComparison.py artifacts/new-focus-comparison artifacts/new-focus-summary.json
```

Select the same SDK/runtime for all builds and runs. The driver records binary
hashes, SDK and GuidedGrade process-name audits at each process start/exit. It does
not continuously monitor other apps. Snapshots spool to separate files; their
serializer/runtime overhead is shared by both cases. This is a resource attribution
experiment, not a latency benchmark or an approved memory budget. Absolute resource
counts need not match the larger NativeLifetime workload. Compare focused-minus-quiet
deltas and repeated-cycle trends, not object identity across processes.
