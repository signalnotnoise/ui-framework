# FW-041: compact icon-button sizing

Completed November 21, 2026 as a documentation/example clarification against
the existing 1.2.0 implementation. No runtime, public API, package, performance
baseline or benchmark-harness changes. FW-040 independent tooltips remains separate.

## Findings

ViewHost assigns DesiredWidth/DesiredHeight to Node.Frame. Node creates a native
Button with local padding and left alignment. WPF local values outrank ordinary
style setters, so a padding Setter alone does not remove that space. Clearing
the local value lets the style own padding but leaves native content sizing intact.

Two executable reproductions measured a 32-DIP outer frame. After clearing padding,
the native button measured 18 DIP with the framework border and 16 DIP with a
minimal reconstructed borderless template. Explicit native Width restored the
32-DIP target. The logs preserve the measured sizes. This explains a mechanism
consistent with GuidedGrade's report; its actual custom template and screenshots
were not used as independently verified framework evidence.

## Recipe and ownership

[SkillIconToolbarExample](../tests/UI%20Framework.Checks/SkillExamples/SkillIconToolbarExample.cs)
is one WPF example type. It retains reusable VectorIcon geometry and one named,
application-owned hit-target size, uses existing Icons/Spacing tokens for display
size and gaps, and bases its toolbar-only Button style on the framework style.
Native width/height and style-owned zero padding are explicit. A Loaded handler
clears renderer-local padding only in this deliberately owned scope. No visual-tree
walk, new framework modifier, global style or general-purpose adapter was added.

The application configures the host before attaching it, disposes it on removal,
and reapplies the recipe after theme/contrast changes. It supplies the current
system contrast flag; the recipe also updates the application canvas/text colors.
Keeping the base template and Quiet appearance retains transparent normal chrome
and native focus/hover/pressed/disabled behavior. An arbitrary replacement template
still owns its interaction states. Reload deliberately repeats the padding handoff;
the recipe must not be applied to foreign controls with their own local padding bindings.

Initial visual inspection exposed black system-colored icons on the fixed dark
canvas during simulated contrast. The final recipe explicitly changes the owning
surface colors, and a focused assertion now guards that surface/foreground pairing.
This correction is in the example, not in framework theme behavior.

## Verification and limits

- 43 focused cases pass: CompactIconButtonTests, CompactButtonTests, IconTests,
  SkillContractTests and DesignTokenTests. The new class adds two sizing cases
  and eight recipe cases across 320/800 DIP, simulated 100%/200% DPI and light/dark.
- Checks cover native target/icon containment, padding precedence, accessible
  names, tab-stop/focusability, UI Automation invoke, disabled state, retained
  button/icon identity through theme changes and callback detachment on disposal.
  Existing compact/icon checks provide native focus coverage. No new physical
  keyboard, IME or screen-reader campaign is claimed.
- Hidden presentation sources and injected read-only focus/hover states verify
  template feedback. Forced palette contrast uses this machine's SystemColors;
  it is not a physical Windows contrast-theme switch or a custom-color matrix.
- 24 final PNGs preserve quiet, focus and contrast-focus states for all eight
  configurations. Representative narrow/wide, light/dark and both-DPI images were
  inspected, including corrected contrast renders. Full captures are retained;
  the claim is not individual manual inspection of every saved image.
- Three skill-reference generator fixtures, generated-reference freshness and
  documentation checks pass. The repository skill and unchanged installed copies
  were synchronized. Each new C# file declares one type.

No runtime performance comparison was repeated: library sources, harness and
accepted budgets are unchanged. Geometry and screenshot acceptance are not speed
or allocation evidence, and no hosted release or publication is claimed.

[Raw test results, renders and hashes](verification-evidence/2026-11-21-icon-button-sizing/manifest.json)
describe the [acceptance archive](verification-evidence/2026-11-21-icon-button-sizing/acceptance.zip).
The initial focused results are retained alongside the final contrast-surface run.

Reproduce using the repository's stable .NET 10 SDK:

```powershell
dotnet test "tests/UI Framework.Checks/UI Framework.Checks.csproj" -c Release --filter "FullyQualifiedName~CompactIconButtonTests|FullyQualifiedName~CompactButtonTests|FullyQualifiedName~IconTests|FullyQualifiedName~SkillContractTests|FullyQualifiedName~DesignTokenTests"
python tools/Update-SkillReference.py --check
python scripts/test-skill-reference.py
```
