# FW-018 heap and handle attribution — November 13, 2026

FW-018 remains in progress. No memory budgets or performance baselines changed.
The remaining process growth is now partially attributed; it is not evidence of
accumulating framework hosts, nor is it proof of whole-process leak freedom.

## Method and boundaries

`tools/Test-MemoryAttribution.ps1` launches only its own NativeLifetime or
WebViewAcceptance process. The opt-in `UI_FRAMEWORK_MEMORY_CHECKPOINTS` rendezvous
pauses after existing collection/sample-release checks at the first measured
iteration, final iteration and a final five-second quiet period. Normal runs do
not pause. Native uses three warmup plus 60 measured batches, four cycles per
batch and 500 rows. Each browser mode uses four warmup plus 40 measured cycles,
local pages and isolated profiles, with its existing deliberate-root control.

A separate Windows DbgHelp collector requests full memory, handle metadata and
thread information (`0x1006`). Offline SOS analysis records heap types, GC handles,
threads and thread-pool workers. The parser explicitly reports missing handle
metadata as unavailable. Its fixtures cover both descriptor layouts, unknown
types, truncated evidence and the boundary between heap and GC-handle statistics.
The initial native pilot using the standard dotnet-dump collector lacked handle
metadata and remains preserved as incomplete evidence for that question.

These are `memory-attribution-paused-v1` diagnostic runs, **not** comparable
performance or budget runs. Dump pauses, polling, diagnostic attachment and
serialization can affect warmup, cache eviction and worker counts. Heap totals
below exclude `Free`, but may still include unrooted objects. Root attribution is
reported separately. No bytes are subtracted from the normal telemetry reports.

## Heap findings

The handle-aware native run has non-free heap totals of 2,086,109 → 2,056,871 →
2,053,651 bytes (start/end/quiet). The earlier pilot also decreases. Native owned
buffer and managed collectability checks pass. WPF text-input objects and runtime
reflection/JSON metadata decrease; this experiment does not reproduce sustained
native heap growth.

Fresh browser totals are 1,361,381 → 1,390,528 → 1,435,754 bytes. Reused-profile
totals are 1,361,225 → 1,387,668 → 1,386,242 bytes.

The fresh run's large `Int32[]` is conclusively rooted by
`System.Runtime.CompilerServices.CastHelpers.s_table` in the installed .NET
10.0.13 runtime. Its size is 49,200 bytes at start/end and 98,352 at quiet: an
exact 49,152-byte expansion of the runtime type-cast cache. The static field and
array addresses match in the dump. This explains a step in this diagnostic run;
it does not retrospectively prove every earlier 49 KB step has the same cause.
The expansion occurring after the measured workload also demonstrates why a dump
campaign cannot be treated as an unperturbed memory budget.

Both browser runs gain 36 `DynamicMethod` objects (4,608 bytes), corresponding
IL-generation objects and small arrays. A selected fresh-run method is named
`CollectedGetter` and its root chain is the static System.Text.Json
`ReflectionEmitCachingMemberAccessor` cache → dictionary entry → delegate →
dynamic method. This directly attributes that object to reporting metadata.
It does not attribute every byte-array or dynamic-method instance, or demonstrate
that those caches remain bounded indefinitely. The suspected process-enumeration
buffer was checked and was **not** the owner of the large array.

## Handle findings

Native handles are 476 → 479 → 479: two additional events and one I/O-completion
handle. Worker count stays two. File, section and thread handle counts are stable.
The three added handles are not yet attributed to a particular allocator.

Fresh browser handles are 515 → 529 → 524. Worker counts are 3 → 5 → 4. Between
end and quiet, one thread handle, three events and one I/O-completion handle go
away. Reused browser handles are 515 → 523 → 518, with workers 3 → 4 → 3; the same
five-handle group goes away. The matching changes support a worker-resource
explanation for part of the transient growth, not ownership proof for every event.

Both browser modes retain two unnamed file handles and one section named for
`ProgramData/Microsoft/Windows/Caches/cversions.2.ro`. The section's name identifies
the mapped Windows cache file, not the call stack that opened it. The unnamed file
handles still require tracing. Unknown-type handles remain constant at 149 in the
native run and 156 in each browser run; unknown does not mean absent.

All diagnostic workloads complete their original ownership, collection and
browser-process-exit checks. Browser runs each verify all 44 reporting samples
collected; native runs each verify all 63. No sample histories remain rooted
during measurement. This remains separate from OS/runtime cache observations.

## Next evidence before budgets

1. Trace the two unnamed browser file handles and native event/I/O-completion
   handles to their creation stacks, using an explicitly separate diagnostic run.
2. Repeat the normal, unpaused protocol with longer durations and additional row
   counts to distinguish startup/cache plateaus from sustained growth. Keep fresh
   and reused profiles separate and retain the full-list/virtualization context.
3. If reporting metadata materially obscures that calibration, evaluate a fixed
   serialization contract as a measurement change, preserving the current evidence.
   Do not remove workload, force worker limits or subtract inferred cache sizes.
4. Set representative budgets only from the normal repeated campaign. Contextual
   error diagnostics and instrumentation overhead remain separate FW-018 work.

## Validation

Normal Release validation passes 172 framework tests, 15 visual checks, 21 stress
assertions, reporting retention controls, the one-batch memory regression and
real-window/shutdown checks with checkpoint opt-in absent. Native-memory analyzer
and new minidump-parser fixtures pass. Both unpaused browser modes also pass one
44-cycle run each, with all 88 reporting samples collected and normal process exit.
Diagnostic workloads and normal validation
are distinct evidence; stress timings here are not paired performance results.

## Reproduction and evidence

Use PowerShell 7, the recorded SDK/runtime and a locally installed dotnet-dump:

```powershell
./tools/Test-MemoryAttribution.ps1 -Mode native -OutputDirectory ./artifacts/attribution-native-new
./tools/Test-MemoryAttribution.ps1 -Mode fresh -OutputDirectory ./artifacts/attribution-fresh-new
./tools/Test-MemoryAttribution.ps1 -Mode reused -OutputDirectory ./artifacts/attribution-reused-new
python tools/Summarize-MemoryAttribution.py ./artifacts/attribution-native-new --output ./artifacts/attribution-native-new/attribution.json
python scripts/test-memory-attribution.py
```

Each output directory must be new. Dumps remain local under ignored `artifacts`;
they can contain process memory and are not included in repository evidence.
The [evidence archive](verification-evidence/2026-11-13-heap-handle-attribution/README.md)
preserves measured-source snapshots, manifests, summaries, analysis logs and dump
hashes. Framework libraries and accepted latency baseline are unchanged; no
paired latency or hosted release gate is claimed for diagnostic tooling.

Primary structure references: [MiniDumpWriteDump](https://learn.microsoft.com/en-us/windows/win32/api/minidumpapiset/nf-minidumpapiset-minidumpwritedump),
[handle descriptors](https://learn.microsoft.com/en-us/windows/win32/api/minidumpapiset/ns-minidumpapiset-minidump_handle_descriptor),
[dump analysis](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-dump),
and [.NET CastHelpers](https://github.com/dotnet/runtime/blob/v10.0.0/src/coreclr/System.Private.CoreLib/src/System/Runtime/CompilerServices/CastHelpers.cs).
The installed-runtime dump's static-field value is the evidence for cache ownership;
the linked source provides context, not a claim of an exact 10.0.13 source match.
