# Opacity and translation (unreleased)

This first slice adds visual targets and retained-update animation. It is not in
the published 1.0.0 packages. Core descriptions contain no WPF types.

```csharp
private static readonly AnimationSpec feedbackMotion =
    new(TimeSpan.FromMilliseconds(160), AnimationEasing.EaseOut);

public override View Body() => Text("Saved")
    .Opacity(saved.Value ? 1 : 0)
    .Translate(0, saved.Value ? 0 : 6)
    .Animate(AnimationProperties.Opacity | AnimationProperties.Translation,
        feedbackMotion);
```

`saved` is retained application/component state. State stores the destination;
WPF clocks produce intermediate values without state notifications or Body calls.
The property mask is explicit. Easing supports Linear and quadratic EaseOut.
Durations must be non-negative; zero settles immediately. Opacity must be finite
and in [0,1]; X/Y must be finite and may be negative. Modifiers and renderer
validation reject invalid values, including direct record construction.

## Ownership and behavior

- `View.Motion` is optional immutable metadata. Ordinary views allocate no motion
  record, controller, transform, clock or event subscription. View carries one
  additional reference field; controller storage uses an optional attached value
  on the existing frame, with no additional Node field.
- WPF lazily creates one `NodeAnimation` for a view using motion. It animates the
  existing outer frame. Translation allocates one reusable TranslateTransform
  only when a nonzero translation is requested. Native child transforms remain
  application-owned. The controller may stay allocated until node disposal.
- Mount, restoration and unloaded updates apply targets immediately. There is
  no automatic entry animation. Translation changes visual placement in DIPs,
  not measured size, sibling layout or reserved space; ancestors may clip it.
- A retained target change starts from its currently displayed value. An
  unchanged target does not restart, even when the specification is reconstructed
  or duration/easing changes. New timing applies to the next target change.
  Interruption preserves position, not velocity.
- Each property has at most one owned clock. Replacement captures the effective
  value, removes its previous clock, commits the base destination and applies a
  new native clock using SnapshotAndReplace and FillBehavior.Stop. Completion
  removes clocks and handlers; the base destination remains.
- Removing a property from the mask, passing a null specification, or setting
  zero duration cancels affected clocks and settles targets. Setting Motion to
  null restores opacity 1 and translation (0,0).
- Detach cancels clocks before user cleanup callbacks. Unload also settles;
  reload does not replay. Keyed reuse follows existing node identity rules, but
  a move that causes native unload settles rather than preserving motion.
- Virtual-row recycling discards native motion. Re-realization uses the latest
  logical target. Navigation and row snapshots never retain clocks/transforms.
- Opacity zero does not hide, disable, remove from automation or change Tab
  eligibility. Applications must manage actual visibility/removal and input
  behavior explicitly. Translation is not a layout transition.

## Reduced motion

```csharp
AnimationSettings.SetIsEnabled(host, false);
```

This WPF attached property inherits to descendants. Disabling it settles active
property animations immediately, without a rebuild. Re-enabling allows future
target changes and does not replay. Windows `SystemParameters.ClientAreaAnimation`
also gates public animation. Controllers subscribe to its changes only while
clocks are active and unsubscribe on completion, cancellation or disposal.

Existing navigation entry transitions retain their separate
`NavigationTransition.None` switch and Windows preference check. This slice
does not change navigation timing or add outgoing-screen retention.

## Limits and verification

No width/height animation, layout transitions, exit retention, springs, loops,
sequencing, completion callbacks or arbitrary dependency-property targets are
introduced. HWND-backed native islands retain their existing airspace/composition
limitations; frame opacity/translation is not a promise to animate embedded HWND
pixels. Ordinary WPF child transform ownership is regression-tested.

`AnimationTests` covers sparse defaults, initial/unloaded targets, interruption,
unchanged/timing-only updates, selective cancellation, zero duration, inherited
motion suppression, unload, completion cleanup, rapid retargeting, invalid direct
descriptions, native transforms/input eligibility, collectability with an old
clock retained, row regeneration and public loaded-host removal.

Run the ordinary before/after comparison with the same harness and SDK:

```powershell
tools/Test-Performance.ps1 -BaselineRef <pre-animation-commit> -Samples 7 `
    -OutputDirectory artifacts/animation-disabled-path -ReportOnly
tools/Test-AnimationPerformance.ps1 -Samples 7 `
    -OutputDirectory artifacts/animation-retarget
```

The first command retains full-list, virtualized and themed-full-list workloads.
The second alternates fresh enabled/disabled processes after discarded warmups:
32 retained text frames, 240 retargets, 160 ms duration, requested 8 ms interval.
It deliberately invokes the internal controller to isolate native-clock cost
even if Windows motion is disabled, without changing the user's preference.
It is not an end-to-end public-policy benchmark or a pre-feature speedup claim.
Both modes perform identical host refreshes and target updates. Raw update
latencies, UI-thread allocations, Body counts and rendering callback intervals
are preserved. Rendering callbacks from an offscreen HWND do not establish
presented-frame FPS, visual smoothness or physical accessibility acceptance.
No component bodies are used in this isolated workload; the ordinary comparison
retains its existing component-work measurements.

See [November 19 implementation and measurement evidence](animation-performance-2026-11-19.md)
for the retained initial regression, focused optimization and rapid-retargeting costs.
