# Themed button content validation — September 20, 2026

The previous scoped Button template bound `Content` directly to `TextBlock.Text`. A native file-tab button containing a panel with a filename and close button therefore lost its visual content. The WPF template now uses a `ContentPresenter` with native content, template, selector and string-format bindings. A small WPF-only presenter preserves wrapping for ordinary generated string labels; application visuals and data templates retain their own layout policy. The renderer also skips assigning an unchanged string label to Button.Content, avoiding unnecessary presenter invalidation.

The four new regression tests cover visible/interactable rich content, content replacement and theme updates, automation names for plain labels, wrapped labels and inherited foreground, explicit/implicit/selected templates, native TextBlock wrapping, null content, and formatting. All 61 framework tests, 15 visual assertions and 21 full-list stress assertions passed. These are automated offscreen checks, not a visible-window screen-reader campaign.

## Initial measurement

Seven alternating measured processes per side/scenario plus discarded warmups compared the initial presenter against pre-change `8b8f4f2`. The workload remained 1,000 logical rows and 50 updates, including full-list, virtualized and themed-full-list cases. The initial presenter passed the comparison's configured budgets but added themed update latency (+7.01%) and allocations (+2.02%); mount time was +2.65%, mount allocations +1.42%. Component builds and mount/unmount counts were unchanged. This is a measured regression, not a speedup.

The allocation exception in that comparison is still the existing 6% themed-update limit; passing against the immediate predecessor does not establish compliance against the accepted published baseline. The renderer's redundant-label guard and reuse of the string-template resource key were added after this measurement. The final comparison uses the accepted published reference without changing budgets or workloads.

[Initial evidence](performance-evidence/2026-09-20-button-content-initial.json). Raw runs: `artifacts/performance/button-content-before-after`.

## Final measurement and release status

The final implementation (including the label guard and shared resource key) was measured with seven alternating processes per side/scenario against accepted published baseline `78c88fb`, on the same machine, SDK and workloads. The performance gate **failed** themed update allocations. No budget or baseline was changed.

| Scenario | Mount time | Update time | Mount allocations | Update allocations |
| --- | ---: | ---: | ---: | ---: |
| Full list | +0.77% | +0.95% | −0.25% | −2.59% |
| Virtualized | −0.32% | −0.37% | +0.16% | +0.92% |
| Themed full list | −3.54% | +9.87% | −2.08% | **+7.81% — failed 6% budget** |

Component builds and mount/unmount counts matched in every scenario. Themed updates took 13,003.72 ms versus 11,836.06 ms and allocated 1,174,129,528 versus 1,089,052,272 UI-thread bytes. The initial and final comparisons use different reference revisions, so they do not isolate the optimization's contribution. The final result does not establish a useful speedup from the label guard; it did not bring allocations within budget. App builds/tests were paused throughout measurement.

[Final evidence](performance-evidence/2026-09-20-button-content-final.json). Raw runs: `artifacts/performance/button-content-final`. Both comparisons were made from an uncommitted candidate; the final source accompanies this report, while the initial candidate omitted the redundant-label guard and allocated its resource key per presenter.

Local package `0.1.0-alpha.2-local.3` is for migration integration testing only. Public NuGet publication remains blocked by this performance result. The package smoke check now covers a native file-tab label and interactive close button under the framework theme, in addition to state updates and retained native interop. See the local feed manifest for actual package verification and hashes. [Lazy computed investigation](reactivity-performance-investigation.md) records a separate possible optimization; it is not implemented in these binaries.
