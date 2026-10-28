# Contrast mount-allocation investigation — October 28, 2026

## Finding

Live bindings on `SolidColorBrush.Color` prevent freezing. WPF style/template
instantiation then creates per-control brush clones, including their binding
expressions and instance storage. The palette is logically shared, but its
brush instances are not shared by the mounted controls.

The [WPF StyleHelper implementation](https://source.dot.net/PresentationFramework/System/Windows/StyleHelper.cs.html)
uses per-instance storage and cloning for non-freezable style values. The local
.NET 10.0.13 probe verifies this behavior in the installed runtime: 1,000 editors
have 1,000 distinct bound background brushes in the live arm, versus one shared
frozen background brush in the diagnostic arm. These counts cover background
brushes only; controls also consume foreground/border and template brushes.

## Controlled experiment

Two isolated copies of the exact retained runtime and unchanged Counter harness.
The only source difference is ThemePalette's brush construction: the diagnostic
arm removes the Color binding and freezes the brush. It deliberately disables
live contrast updates. Production files, baseline and budgets were not changed.
Same SDK 10.0.402/runtime 10.0.13, same machine, alternating fresh processes, one
excluded warm-up plus three measured pairs per workload. Full-list is the
unthemed negative control. This is causal diagnostic evidence, not a release gate.

| Mount allocation median | Live bindings | Frozen diagnostic | Saved |
| --- | ---: | ---: | ---: |
| Unthemed full-list | 333.19 MB | 333.08 MB | 0.11 MB / 0.03% |
| Themed full-list | 356.12 MB | 321.56 MB | 34.56 MB / 9.70% |
| 1,000 editors | 134.88 MB | 128.95 MB | 5.93 MB / 4.40% |

MB means decimal megabytes. Rows, operation counts, mount/update body builds and
lifecycle work match across arms. The independent direct-control probe shows
approximately 5.83 MB saved mounting 1,000 editors. Warm palette/style setup is
roughly 53–96 KB and does not scale with item count; it cannot explain the large
mount regression. The reflection loop is not the primary optimization target.

This experiment does not establish lower mount latency: themed mount time was
2.57% higher in the frozen arm, while editor mount time was 3.65% lower. Retain
this uncertainty separately from the reproducible allocation reduction. The
frozen editor result still does not explain all overhead against the historical
accepted baseline; investigate that residual after removing the clone cost.

## Next implementation direction

Preserve live system-color updates while avoiding a bound Freezable in every
style/template instance. Prototype a shared palette representation or targeted
updates of existing controls, then verify focus, selection, popup and composition
ownership plus palette cleanup. Simply freezing production brushes would disable
the requested contrast support. The previous per-property dynamic-resource
attempt was more expensive and is not an accepted solution.

FW-006 remains open. No production runtime change was made in this investigation.
Source copies, probe, driver, builds, all raw runs and computed medians are in the
[evidence archive](performance-evidence/2026-10-28-allocation-cause/manifest.json).
