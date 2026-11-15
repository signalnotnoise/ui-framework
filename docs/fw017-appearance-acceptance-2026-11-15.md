# FW-017 visual acceptance — November 15, 2026

FW-017 is complete within the framework's documented Windows/WPF appearance
scope. This closes the two gaps in the [existing-evidence review](fw017-evidence-review-2026-11-15.md)
without changing framework runtime code or adding a validation API.

## Combined visual review

The existing SkillPreview tool now has an isolated appearance-acceptance mode.
It composes existing declarative controls into a form, compact toolbar, three-row
review list and a resizable split workspace. It includes long titles/descriptions,
a multiline toggle label, a disabled Publish action with an explanation and
application-owned errors beside the field and in a list row.

Eight real WPF renders were generated and individually inspected: light/dark ×
1040/760 DIP width × normal/editor-focus state, all 820 DIP high at 96 DPI.
The headings and control spacing remain consistent, long content wraps without
cut-off text or overlapping rows, and both panes fit the compact viewport.
The disabled action remains readable and visibly subdued; focus is distinguished
from the normal field border. Error wording is readable in both palettes and
does not rely on red or any other color alone. The splitter uses the existing
native appearance. No renderer or template defect was found in these cases.

The captures use a hidden HWND. Focus appearance is deliberately driven through
the WPF template-state property, following the existing ToggleStyleTests pattern;
it is **not** a new physical keyboard-focus test. These are native renders, not
screenshots of the user's desktop. Hover/pressed implementation and prior
keyboard/contrast/scaling evidence retain their existing scope; this task does
not claim a new exhaustive state/OS-theme/DPI matrix or physical Narrator pass.
The [October manual record](manual-acceptance-2026-10-29.md) remains the physical
acceptance evidence with its documented missing build/scale metadata.

## Validation/error decision

The [v1 contract](layout-styling.md#validation-and-error-appearance-v1-contract)
is application-owned composition: rules, timing, error text and action eligibility
belong to the application. Existing Text and AccessibilityLabel APIs supply visible
and accessible context. The framework does not add error colors, adorners or an
automatic announcement promise. This resolves the original scope question; it
does not defer an unspecified validation subsystem to release time.

The fixture verifies invalid → valid → invalid transitions. Successful correction
replaces the error with confirmation, updates the field's accessible description,
and subsequent invalid input restores explicit error wording. Theme replacement
retains the actual editor. Both palettes use normal readable text for errors.

## Verification and reproduction

- Release fixture: **222 assertions pass**, including repeated bounds/state checks;
  these are not 222 independent scenarios or design approvals. Eight captures pass
  the separate visual inspection described above.
- **44/44 focused tests pass**, zero skipped, covering LayoutStyleTests,
  WorkspaceLayoutTests, CompactButtonTests, ButtonContentTests, ToggleStyleTests,
  ContrastThemeTests and ThemeResourceUpdateTests.
- SDK 10.0.402, .NET 10.0.13. The initial fixture compile failed on unsupported
  four-argument Padding and a Button identifier collision; the fixture was corrected
  to use existing APIs. Both logs are retained; no framework change was needed.
- Core/WPF source hashes match the prior memory-budget source manifest. No fresh
  performance comparison is needed for this fixture/documentation-only change;
  existing performance results are not presented as a new release gate.

```powershell
dotnet run --project tools/SkillPreview -c Release -- --appearance-acceptance artifacts/new-appearance-run
```

Use the repository's compatible SDK and a fresh output directory. The older
before/after capture mode is unchanged. [Archived captures, TRX, report, logs and
source provenance](verification-evidence/2026-11-15-appearance/README.md) make this
review reproducible. FW-019 still owns package-based sample adoption/upgrades;
FW-025/026 remain the agreed post-v1 diagnostics/Narrator follow-ups.
