# Standalone package workspace

A small Windows/WPF workspace installed only from published SignalNotNoise.UI.Wpf
and its transitive SignalNotNoise.UI package. Copy this directory anywhere; it has
no repository project references or dependency on repository build properties.

Requirements: Windows 11 x64, a stable .NET 10 SDK with WPF desktop runtime and
NuGet.org access for the first restore. The recorded acceptance environment uses
SDK 10.0.402 / runtime 10.0.13. Other Windows/runtime combinations are not covered
by this sample's acceptance record.

```powershell
dotnet restore --locked-mode
dotnet run -c Release --no-restore
```

Choose a work item, edit the release name and native notes, resize the split panes,
then visit Review and Back. Edits live in the application model across navigation.
Data is session-only: this sample has no persistence, browser or grading workflow.
The notes editor deliberately preserves native WPF styling to demonstrate interop.

`Program` owns one Application, STA cleanup policy, ViewHost and window. Closing
detaches/disposes the host, closes the policy, then shuts down the application.
The model owns durable edits and native event subscriptions. Its native release
callback checks detachment and unsubscribes exactly once. Navigation recreates
native controls while restoring model data; it does not serialize native selection.

## Upgrade and repeatable checks

The default exact dependency and lock file target alpha.7. To test alpha.6 in a
separate copy, restore with `-p:FrameworkPackageVersion=0.1.0-alpha.6 --force-evaluate`
and pass that property to the subsequent build. Restore/build alpha.7 again to
perform a source upgrade. Never overwrite a published package or reuse caches
as proof of a clean installation.

```powershell
dotnet run -c Release --no-restore -- --check C:\Temp\new-workspace-check
```

The check uses real windows positioned offscreen and routed control actions; it
does not take desktop focus or claim physical keyboard/IME acceptance. It exercises
forms, list selection, resize, retained native editing, navigation and disposal;
requires zero tracked survivors after collection and completed dispatcher shutdown;
and records timing, UI-thread allocations, component work and post-shutdown memory.
Add `--full-list` for the unvirtualized control workload. Use a fresh output path.

In the framework checkout, `tools/Test-WorkspaceAdoption.ps1` automates isolated
NuGet-only restores, alternating alpha.6/alpha.7 checks and binary compatibility:
it keeps the alpha.6 consumer assembly/deps intact while replacing only the two
framework DLLs with the published alpha.7 DLLs. `tools/Summarize-WorkspaceAdoption.py`
verifies loaded hashes, ownership and measurements. These local diagnostics are
separate from the framework's exact-candidate hosted performance release gate.

For a uniquely versioned local candidate, keep the baseline public and pass
`-CandidatePackageDirectory <local-feed> -BaselineVersion 0.1.0-alpha.7
-CandidateVersion <unique-version>` to the adoption driver. It uses a fresh
candidate cache and verifies installed `.nupkg` hashes against the supplied files.
Without this option, both versions must be publicly indexed. A local candidate
does not change the sample's default public dependency or authorize publication.
