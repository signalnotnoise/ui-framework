# Binding stress lab

The existing Counter application is now a project-workspace stress harness. It uses the framework's composition API throughout the dashboard; WPF supplies the application window, theme defaults, timers, and native controls.

## Run interactively

```powershell
dotnet run --project samples/Counter -c Release
```

Start with 50 rows. Each work item owns an immutable record, nested details, and three checklist items. Rows and the inspector share projected bindings. Project preferences use chained bindings into another nested record. Row expansion is component-local state; data edits live in the workspace and survive filtering and unmounting.

| Action | What it exercises |
| --- | --- |
| Edit title/owner/notes in row and inspector | Shared two-way bindings and nested immutable record projection. |
| More / checklist edits | Conditional views, component-local state, nested keyed lists. |
| Shuffle / move first to last | Keyed reconciliation and retention of controls/local state. |
| Filter / only open | Conditional subscriptions and removal/remount of row components. |
| Mount board | Subtree cleanup; remount retains external data but resets local expansion. |
| Compact rows / checklist visibility | Broad dependency fan-out from shared projected settings. |
| 250 / 1,000 rows | Load the chosen size; full-list mode mounts all rows. |
| Virtualized list | Mount viewport rows and a buffer; preserve offscreen local state until the key leaves the list. See [virtualization](virtualization.md). |
| 10,000 writes | One event mutates one projected value repeatedly; rendering should batch. |
| ▶ Visual stress / Stop | 60 paced visible steps: edits, toggles, selection, keyed moves, layout changes, nested edits, batched updates, and filters. |
| Run 200 steps / Stop | Timer-paced mixed edits, moves, selections, removals/additions, preferences, and filters. |
| Recursive tree + / − | Retained nested components up to depth 24. |

The mixed campaign uses a fixed initial random seed and deterministic operation types. Its sequence also depends on previous shuffle/campaign actions within that workspace instance. Stop takes effect between UI-thread steps. Loading, shuffling, and rendering large trees are synchronous; the app may pause during an individual operation. The size is deliberately capped at 1,000 rows. Live diagnostics show body builds, mount/unmount totals, active components, managed heap size, and process working set. Those counters include the diagnostics component; rising memory alone is not proof of a leak.

## Repeatable automated workload

The **Visual stress** button makes an immediate edit and then advances every 350 ms, subject to UI-thread rendering time. An action strip and progress bar show the current phase. The run uses the current data size, clears search filters, mounts the board, and temporarily opens a focused row's checklist. It changes demo data. Stop cancels future steps and releases temporary expansion; completion also clears the open-only filter. Choose 50 rows to watch the sequence clearly or 1,000 rows to increase the load.

The button, real timer, all 60 steps, cancellation, restart, empty-board handling, and disposal can be checked with `dotnet run --project samples/Counter -c Release -- --visual-check`. This also saves an offscreen running-state image under artifacts/visual-stress.

From the solution directory:

```powershell
dotnet run --project samples/Counter -c Release -- --stress --report artifacts/stress/latest.json --snapshot artifacts/stress/dashboard.png
```

This uses actual WPF controls, native TextBox change events, dispatcher updates, and offscreen layout. It verifies shared editor synchronization, mounts 1,000 rows, retains a TextBox across shuffle, performs 10,000 projected writes, executes 200 mixed operations with a render/layout after each, filters to zero, unmounts/remounts the board, expands to depth 24, and checks that root disposal balances component mounts and stops state-driven renders.

The report records per-scenario elapsed time, body builds, mounts/unmounts, and allocations on the UI thread. It includes a pass/fail flag and assertion count. The PNG is an offscreen render of the real dashboard after resetting to 50 rows. A failing run returns a nonzero exit code and writes the error to its report.

Timings depend on the machine, build configuration, runtime warm-up, and workload. Initial mount includes startup costs. These are not FPS measurements or production performance guarantees. The automated workload does not verify actual keyboard focus, scrolling responsiveness, IME, or screen-reader behavior; those need visible-window testing. Stable mount/unmount counts establish lifecycle balance, not a complete garbage-collection leak proof.

## Back-to-back rendering comparison

```powershell
dotnet run --project samples/Counter -c Release -- --compare --reference --report artifacts/stress/reference-comparison.json
dotnet run --project samples/Counter -c Release -- --compare --report artifacts/stress/optimized-comparison.json
```

Each fresh process mounts the same 1,000-row tree outside the timed interval, then executes 50 identical deterministic mixed operations with a render/layout after each. The reference mode disables selected-value filtering and ignores Memo inputs through diagnostic AppContext switches, reproducing broad observation and parent-driven child rebuilding in the current executable. It is a reference mode, not a historical binary. Default mode enables the optimizations. Both verify row count and full component disposal. Reports record elapsed time, body builds, and UI-thread allocations; run them on an otherwise idle machine for a cleaner comparison.

The switches `UI_Framework.UnfilteredObservation` and `UI_Framework.IgnoreMemo` are diagnostic and must be set before framework types initialize. The app's `--reference` flag does that. Normal app runs leave both disabled. Production code should not depend on these switches.
