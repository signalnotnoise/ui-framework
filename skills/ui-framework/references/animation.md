# Opacity and translation

## Check availability first

As of November 19, 2026, this API is implemented in the source checkout but is
**not in published 1.0.0**. The generated shipped API reference intentionally
excludes it. Verify project references and the actual consumer assembly before
using `View.Opacity`, `View.Translate`, `View.Animate`, `ViewMotion`,
`AnimationProperties`, `AnimationSpec`, `AnimationEasing` or WPF
`AnimationSettings`. Do not upgrade a package just to use this example. With
1.0.0, use immediate existing UI updates or its existing navigation transition.

In a checkout, inspect `UI Framework/PublicAPI.Unshipped.txt`,
`UI Framework.Wpf/PublicAPI.Unshipped.txt` and `docs/animation.md`. The timing
evidence is in `docs/animation-performance-2026-11-19.md`. For a newer package,
inspect its metadata; this date is not a claim about that package's availability.

## Describe destinations

For a verified supporting consumer, a component can keep this specification in
a static readonly field and call this helper from Body using retained state:

```csharp
private static readonly AnimationSpec feedbackMotion =
    new(TimeSpan.FromMilliseconds(160), AnimationEasing.EaseOut);

private static View Feedback(bool saved) => UI.Text("Saved")
    .Opacity(saved ? 1 : 0)
    .Translate(0, saved ? 0 : 6)
    .Animate(AnimationProperties.Opacity | AnimationProperties.Translation,
        feedbackMotion);
```

These types are in `UI_Framework`. State contains the destination. Do not create
a timer that repeatedly writes interpolated values to State or rebuilds Body.
WPF owns native clocks; the core stores immutable descriptions. Avoid animating
every incoming row in a high-frequency stream without a measured consumer need.

The mask is explicit; only selected properties interpolate. Opacity is finite
and in [0,1]; translation is finite, signed DIPs. Duration is non-negative;
easing is Linear or quadratic EaseOut. Zero duration, a null animation spec or
removing a property from the mask settles affected properties immediately.

## Preserve behavior and ownership

- First mount, unloaded updates and virtual-row restoration apply destinations
  immediately. There is no entry replay. Removal disposes immediately; no exit
  retention, springs, loops, sequences or completion callbacks are supplied.
- Unchanged targets do not restart clocks, including when duration/easing changes.
  New timing applies to the next target change. Interruption starts at the current
  displayed value, preserving position rather than velocity.
- Keep stable keys. Native unload settles running motion, including a keyed move
  that unloads a frame. Recycling retains logical state, never native clocks.
- Opacity zero does not disable input, remove Tab stops or hide automation peers.
  Use application-owned removal/input decisions when content must be unavailable.
- Translation is a render transform: sibling layout and measured size stay fixed,
  and ancestors may clip it. It is not width/height or automatic layout animation.
- Effects belong to the framework frame, preserving ordinary native WPF child
  transforms. Do not promise opacity/translation of embedded HWND pixels; native
  islands retain their airspace/composition limits.

At the application's WPF boundary, `AnimationSettings.SetIsEnabled(host, false)`
inherits through descendants and immediately settles active property animations.
Re-enabling permits future changes, without replay. The public renderer also
honors Windows `SystemParameters.ClientAreaAnimation`; do not change the user's
OS preference to force an effect. Existing navigation still has its separate
`NavigationTransition.None` switch and Windows preference check.

## Measure the actual workload

No controller, transform or clock is allocated for ordinary views without motion
metadata. Opted-in frames lazily own controllers and reusable translation
transforms. Native-clock replacement still has a real CPU/allocation cost.

The November 19 diagnostic measured 32 frames / 240 rapid retargets with 160 ms
timelines: median synchronous update 4.000 ms animated versus 0.143 ms immediate,
and 44.1 MB versus 7.1 MB synchronous allocation over the run. Body-build counts
were identical. These are workload-specific observations, not budgets or a
prediction for a consumer. The diagnostic invoked the internal controller with
Windows motion disabled; that bypass belongs only to the diagnostic, not apps.
Rendering-callback intervals from its offscreen window are not presented FPS.

For framework changes, retain ordinary full-list and virtualized comparisons
using `tools/Test-Performance.ps1`; `tools/Test-AnimationPerformance.ps1` isolates
rapid native-clock cost. For consumers, verify rapid reversal, same-target
refreshes, removal/recycling, reduced motion, input/focus and actual native
appearance. Existing `AnimationTests` cover 16 regression cases; fewer Body
builds alone do not establish lower latency or smoothness.
