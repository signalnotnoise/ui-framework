# Native style boundary and compact input validation — October 11, 2026

This records the October 11 candidate. The fixes later shipped in
`0.1.0-alpha.4` on October 20; see the [release record](releases/0.1.0-alpha.4.md).

The Unreleased WPF changes add an opt-in `NativeStyleScope.PreserveNative` boundary
for common control implicit styles and remove duplicate padding from the themed
TextBox/PasswordBox border. Native editing already applies its own padding.
The original `WpfUI.Native` overload still inherits host styles and retains its
public signature.

The compact field reproduction uses a 36-pixel TextField, 16-pixel font and a
60-pixel padded toolbar. Before the fix, its 21.28-pixel character line exceeded
the 2-pixel editing viewport. The regression now passes with native padding,
binding, selection and multiline scrolling preserved. [Rendered field](performance-evidence/2026-10-11-native-styles/search.png).

All 106 correctness tests pass. New checks cover conflicting host templates,
visible native input/button content, changing the scope without recreation,
local style precedence, automation names, click routing and one-time release.
These checks use offscreen WPF presentation and do not establish live IME or
screen-reader acceptance of a third-party editor.
The Release build has zero warnings/errors; 15 visual checks and all 21 stress
assertions also pass.

## Accepted baseline comparison

Seven alternating measured processes per side plus discarded warmups compared
against accepted revision `78c88fb901c202e3c2e49b6de300d1ce2369e00b` on this machine,
using the identical candidate harness, SDK and runtime (10.0.12), 1,000 rows and
50 mixed operations. Editor runs use the same application-owned COM cleanup
policy on both sides. All 32 existing gates pass; no budgets or baseline changed.

| Scenario | Mount time | Update time | Mount UI allocation | Update UI allocation |
| --- | ---: | ---: | ---: | ---: |
| Full list | +0.64% | −36.39% | −1.01% | −25.56% |
| Virtualized | +3.69% | −0.11% | −1.61% | −5.11% |
| Themed full list | −2.85% | −58.87% | −2.72% | −57.08% |
| Layout/editors | +0.20% | −4.91% | +0.13% | −3.29% |

Component builds and mount/unmount counts are unchanged in every scenario.
Positive changes are measured increases within existing budgets, not speedups.
The large improvements include earlier renderer work and cannot be attributed
to this change. [Summary and raw repeated runs](performance-evidence/2026-10-11-native-styles/accepted-baseline/summary.md)
include source hashes and min/max timing ranges in the JSON summary.

## Immediate predecessor comparison

An initial three-sample alternating comparison against `533fbe1` passed all
32 budgets but showed wide timing variation. Full-list update time increased
7.01%, virtualized update time 8.85% and themed mount time 8.31%; themed update
time decreased 4.04%. Allocations changed by at most +0.58% and component work
was identical. The full-list candidate update range was 4,063.85–5,029.13 ms
against 4,100.53–4,273.17 ms. This is not evidence of a universal speedup.
[Initial direct summary and raw runs](performance-evidence/2026-10-11-native-styles/direct-initial/summary.md)
are retained. A seven-sample alternating repeat also passed all 32 budgets:

| Scenario | Mount time | Update time | Mount UI allocation | Update UI allocation |
| --- | ---: | ---: | ---: | ---: |
| Full list | −2.85% | −7.59% | +0.003% | +0.06% |
| Virtualized | +3.93% | +7.66% | −0.004% | +0.003% |
| Themed full list | +4.13% | +6.55% | −0.08% | −0.03% |
| Layout/editors | −3.79% | −0.44% | −0.001% | −0.0001% |

Component work stayed identical. The full-list and themed timing direction
changed between comparisons; virtualization stayed slower in both direct
comparisons. The repeated virtualized update ranges overlap (baseline
1,077.56–1,365.73 ms; candidate 977.36–1,277.44 ms), as do themed updates
(4,263.45–6,637.07 versus 4,440.17–5,384.57 ms). These are measured increases
within budget and an unresolved timing concern, not proof of zero regression.
The virtualized harness does not apply ThemeStyles and neither list harness
constructs `WpfUI.Native`, so the changed template and opt-in scope are not
executed in that virtualized workload. No focused runtime optimization is
justified by this evidence alone; no unrelated hot path was changed to chase
the noisy timing. [Repeated direct summary and raw runs](performance-evidence/2026-10-11-native-styles/direct-repeat/summary.md).

The standard benchmark does not exercise a specialist native island with
`PreserveNative` enabled. It establishes the existing workload gates, not the
cost of theme replacement for an arbitrary third-party editor. This feature is
Unreleased at the time of this comparison; no package version or consumer pin
changed in this validation. Later publication is recorded separately above.
