# TextBox conditional contrast prototype — October 28, 2026

Status: retained locally; FW-006 remains open. No package publication.

## Implementation

TextBox normal colors are frozen brushes shared across instances. TextBoxStyles
uses conditional template triggers for Windows system resources only while its
inherited contrast flag is active. The palette weakly references its theme root,
sets that scope flag on system notifications, and ignores notifications after
another palette replaces it. Templates and native editing controls stay in place.
System-color resource references remain live while contrast is enabled; switching
off restores the original frozen brushes. No public API changes.

PasswordBox, buttons, toggles and pickers still use the existing bound palette.
This change does not claim to remove their clone costs. Native/app-owned local
styles and colors retain precedence. Physical system switching and IME acceptance
remain separate requirements.

## Correctness

Regression coverage checks shared frozen TextBox backgrounds, repeated transitions,
selection/caret colors, selected text and template/content-host identity, active
resource replacement, disabled colors, local overrides and nested theme boundaries.
Existing real-window focus/popup and unused-palette collectability checks remain.
The final suite is 142/144 passing; only the two previously reproduced virtual-row discovery failures remain. All five contrast tests pass. The 15 visual checks, 21 stress assertions and actual-window editing/disposal check also pass.

## Performance

Seven measured alternating process pairs plus one discarded warm-up per side,
same Counter harness, SDK 10.0.402/runtime 10.0.13 and unchanged accepted baseline
78c88fb. Full-list, virtualization, themed full-list and 1,000-editor workloads.
The run passes 30/32 budgets; component work is unchanged.

| Remaining failed metric | Baseline median | Candidate median | Change | Budget |
| --- | ---: | ---: | ---: | ---: |
| Themed mount allocations | 327,667,256 B | 344,005,536 B | +4.99% | +2% |
| Editor mount allocations | 126,205,384 B | 128,974,624 B | +2.19% | +2% |

Editor mount time is -20.00%, update time -4.61% and update allocations -3.30%
versus the accepted baseline in this run. Earlier mount timing varied substantially;
these numbers do not establish that this code change alone caused a 20% speedup.
Compared descriptively with the previous bound-palette run, editor mount allocations
fall from 134,886,120 to 128,974,624 bytes (5.91 MB, about 4.38%). Themed mount
allocations fall from 356,053,936 to 344,005,536 bytes (12.05 MB, about 3.38%).
Those cross-run deltas are distinct from the paired accepted-baseline comparisons.

The editor allocation result exceeds its unchanged limit by about 245 KB. Do not
round it into a pass or relax the budget. Next: isolate the remaining editor
allocation cost, then evaluate this conditional-color pattern for other controls.

Evidence is retained under
[the archive](performance-evidence/2026-10-28-textbox-contrast/manifest.json).
