# Retained WPF controls

See [validation status](native-interop-validation.md) for historical integration evidence. Native interop ships in the published alpha previews; the style-scope API below ships in `0.1.0-alpha.4`.

`WpfUI.Native<T>(Func<T> create, Action<T>? update = null, Action<T>? release = null)` embeds an app-owned native WPF island in a declarative view. `T` must derive from `FrameworkElement`. This supports existing editors, trees, context menus, splitter grids, docking libraries and console controls without recreating their functionality in the framework.

```csharp
// Inside a component body: read reactive state here, before the update callback.
var content = document.Value;
return WpfUI.Native(
    () => new TextBox { AcceptsReturn = true },
    editor => { if (editor.Text != content) editor.Text = content; },
    editor => { /* detach application subscriptions; dispose resources you own */ })
    .Id("document-editor")
    .AccessibilityLabel("Document editor");
```

The factory runs once per mount. The same key, sibling position/order rules, and **declared generic type** retain the element; changing factory delegates does not recreate it. Changing the key or declared type, removing the view, or unmounting its parent releases it. `update` runs on initial mount and each patch using the newest callback. Read reactive values in the component body: reads performed later inside `update` are not dependency subscriptions. Application code owns event feedback suppression, document state and native property updates. The renderer does not replace a native TextBox's text or reconcile a native Grid's children.

The factory must return an unparented element on the host's UI thread. Parented controls, presentation roots, windows and null results are rejected without stealing ownership. Once accepted, the element is detached before the **original mount's** `release` callback runs exactly once. The framework never implicitly calls `IDisposable.Dispose` on the native element. Use `release` to unsubscribe or dispose app-owned resources. If initial update fails, the accepted element is released; an update failure on a retained element now clears and releases its affected host tree; an explicit successful host Refresh creates fresh elements (see error-recovery.md). There is no rollback of arbitrary application mutations.

Creation/update/release execute on the WPF dispatcher. Removal and explicit host disposal drive cleanup; temporary WPF `Unloaded` events do not define framework lifetime. Navigation and virtualization can unmount native islands and create fresh controls later, so durable editor/document state belongs in application models. Native controls are not saved in logical snapshots.

Width, height, padding, alignment, background and enabled modifiers apply through the outer framework frame. Existing implicit WPF styles may inherit into the island. `AccessibilityLabel` targets the island root: without a label it preserves a factory-supplied name; setting then removing a label clears the local name override. Native names on descendants remain application-owned. A native island takes no declarative children; compose its native children in its factory/update callbacks.

Core carries only `ViewKind.Platform` and an opaque `PlatformContent` descriptor. WPF's `Interop` layer owns creation, identity and cleanup. The common renderer owns outer layout and reconciliation.

## Asynchronous ownership verification

[FW-010/FW-018 controlled lifetime checks](native-lifetime-2026-10-29.md) exercise
late initialization, faults, cancellation, remount and collectability using the
existing callbacks. Keep asynchronous initialization in an application-owned
adapter with an observed task; cancellation alone is insufficient when a provider
can complete after removal. Check disposal on the UI dispatcher before applying
results, release late-acquired resources, and report failures explicitly.
A factory must clean its own partial resources if it throws before returning.

The dispatcher must remain available for the adapter's completion/cleanup work;
this initial fixture does not establish dispatcher-shutdown or third-party popup,
airspace or provider guarantees. No async-void create/update/release contract is
introduced. Further integration and memory acceptance remains in the backlog.

## Preserving native styles

Specialist controls can depend on their own templates. Host or application
implicit Button/TextBox styles may otherwise replace those templates and hide
query text, navigation glyphs, or other native content. Use the opt-in overload:

```csharp
WpfUI.Native(
    () => CreateSpecialistEditor(),
    NativeStyleScope.PreserveNative,
    update: editor => UpdateDocument(editor),
    release: editor => DisposeEditor(editor)).Id("specialist-editor");
```

The original three-argument overload continues to inherit styles. The new
scope installs empty implicit styles on the island wrapper for common WPF
input, button, list/tree, menu, scrolling, label, slider, progress and tab
controls. These stop ancestor implicit-style lookup while retaining WPF's
theme templates. Explicit local styles and implicit styles inside the native
island take precedence. See [the exact protected types](../UI%20Framework.Wpf/Interop/NativeStyleResources.cs).

This is an implicit-control-style boundary, not a separate resource or theme
universe: named resources, data templates, inherited properties, custom control
types and explicit application style assignments are not masked. Applications
remain responsible for the specialist control's colors and accessibility.
Changing the scope with the same key/type retains the native instance, text,
selection and lifetime callbacks. Reapplying the outer theme does not replace
the island's styles. The core remains independent of WPF.

`NativeStyleTests` verifies conflicting templates, scope changes, native query
and button content, local styles, automation naming, click routing and one-time
release. These hidden presentation checks do not establish live IME or
screen-reader acceptance for a particular third-party editor.
See [validation and performance evidence](native-style-validation.md).

## Migration follow-ups

### Historical browser proposal — October 20, 2026

GuidedGrade reported a request to embed the Full Sail Online grading workspace
using an app-owned WebView2 adapter. Its reported package pin is
`SignalNotNoise.UI.Wpf` `0.1.0-alpha.3-local.2`; the reporting app had not added
`Microsoft.Web.WebView2`. These are consumer-reported details, not a verified
browser integration. No browser primitive or WebView2 validation is supplied
by the native-host contract.

The initial integration should evaluate the existing `WpfUI.Native` factory,
update and release callbacks. The framework retains the native root while its
key, position and declared type remain stable, and detaches it before release;
it does not automatically dispose native controls. The app adapter must own
asynchronous initialization, error reporting, event subscriptions and explicit
cleanup, including initialization that completes after unmount. Keep browser
dependencies and application navigation outside the platform-independent core.

Before claiming browser support, validate stable identity across updates,
removal/remount and one-time cleanup; initialization failure and unmount races;
keyboard navigation and focus restoration; popup/new-window ownership; and
resizing, clipping, overlays and native airspace behavior in the real grading
layout. Login/SSO compatibility and browser lifetime remain unverified. This
tracking note changes no package, consumer pin or runtime behavior. A framework
API change should follow a reproduced limitation of the existing host contract.

Reported by Lab-Feedback-WPF on September 20, 2026:

- Editable picker/custom text entry: currently use TextField plus Picker or a native editable ComboBox.
- Text-field submit/Enter callback: currently use native keyboard handling through an island.
- Declarative tree, context menu, splitter/docking and progress controls: currently retain WPF islands.

These are recorded gaps, not implemented declarative APIs. A local prerelease is for migration validation; it does not publish or replace the existing NuGet release.

Build and verify an isolated local feed with a distinct version:

```powershell
./tools/Test-Packages.ps1 -Version 0.1.0-alpha.2-local.2 -OutputDirectory artifacts/local-packages/native-interop
```

Both packages receive the same version override; the script verifies a fresh package-only consumer and 1,000 retained native updates. Existing custom package versions are not overwritten. Use the feed with a matching exact WPF package version and retain its core dependency, source revision, license and checksums. Do not republish `0.1.0-alpha.1` or reuse a local version for changed binaries.

## Controlled real-window shutdown evidence — October 29, 2026

The [NativeLifetime sample](../samples/NativeLifetime/NativeWindowSession.cs) defers
window close while the application adapter cancels and drains initialization. It
disposes/detaches the host before accepting late results, releases late resources
on the UI thread, then closes the window before dispatcher shutdown. A timeout
leaves the dispatcher/window alive for retry; observed cleanup errors are surfaced.
This is application-owned policy using existing callbacks, not a new framework API.

Real-window focus, selection, WPF popup HWND release, resize, managed overlay and
five shutdown cases pass. Repeated memory checks distinguish bounded native
realization from retained visited-key state. See the [measurements and limitations](native-window-memory-2026-10-29.md).
These checks do not establish HwndHost/native-airspace or browser-provider support.

## Verified GuidedGrade source review — October 29, 2026

The historical proposal above is superseded by the [current source review](webview2-review-2026-10-29.md).
GuidedGrade already hosts WebView2 through a keyed native DockPanel, owns browser
initialization/navigation/popups/disposal and pins WebView2 1.0.4314.35 with framework
alpha.7-local.1 in its reviewed working tree. Browser-specific acceptance remains
missing: pending initialization/child requests at close, cleanup failure isolation,
child environment compatibility, native airspace and repeated process/resource
measurements. No framework API limitation has been reproduced. The application
window currently owns disposal; the native view supplies no release callback, so
framework detachment does not by itself dispose the browser.

## Real WebView2 acceptance slice — October 29, 2026

The [real-browser sample](../samples/WebViewAcceptance/README.md) passes 15/15
local-page/isolated-profile initialization and owned-popup close cases with the
existing host. It demonstrates app-owned draining before detach/release, not forced
provider cancellation or acceptance of GuidedGrade's current synchronous cleanup.
See [case outcomes and limits](webview2-acceptance-2026-10-29.md). Focus/airspace,
failure/recovery and browser-process memory still need acceptance.

## Preliminary keyboard and airspace evidence — October 29, 2026

[Interaction checks](webview2-interaction-2026-10-29.md) pass once per mode with
real keyboard input and screen pixels. Standard WPF WebView2 covers WPF sibling
overlays despite their Z-order; a separate popup HWND can appear above it. This is
a provider/platform boundary, not a demonstrated framework defect. Forward/reverse
Tab, text/Enter and refresh/theme retention passed. Repetitions require explicit
interactive approval; default test runs exclude foreground interaction cases.

## Failure and recovery evidence — October 29, 2026

The [recovery slice](webview2-recovery-2026-10-29.md) passes profile validation, real
missing-runtime/navigation failure, renderer recovery on the same controller, and
browser recreation after an observed process exit. Application cleanup must avoid
querying CoreWebView2 through an already crashed control; retain the subscribed
core for handler removal and attempt all cleanup steps while surfacing errors.
The sample rejects file/profile conflicts before provider startup after one
unguarded attempt timed out. Generic provider hangs remain unverified. These are
sample ownership policies, not automatic framework recovery or GuidedGrade fixes.


## November 11 bounded waits and script-close

The WebView2 sample now verifies bounded initialization/close waits without abandoning
pending provider ownership, exact late-fault propagation, fresh-owner retry and real
parent/child script-close. All 42 repeated acceptance cases pass. A timeout does not
cancel WebView2 or authorize dispatcher shutdown before drain. Permanently hung
provider termination and browser resource budgets remain outside this evidence.
See [policy and validation](webview2-shutdown-2026-11-11.md).


## November 11 browser resource retention

Six fresh-process retention runs pass for fresh/reused isolated profiles: 270 normal
parent/popup pairs including negative controls, 540 one-time owner releases, 1,350
captured process exits and 2,970 weak references collected. The deliberately rooted
owners remain visible to the probe until released. No process is killed by these
retention modes. Heap/handle observations remain separate from strict release checks
and do not establish arbitrary-site budgets. See [results and reviewed exception
coverage](webview2-retention-2026-11-11.md).


## V1 acceptance boundary

The final interactive campaign passes 9/9 repeated cases. See the
[final acceptance](webview2-final-2026-11-11.md) and [permanent-stall/interaction
contract](native-provider-stall-policy.md). Timeout retains pending ownership; hard
shutdown deadlines require app-owned process isolation. Standard browser HWND
overlay/arbitrary-clip limitations are explicit. FW-018 budgets and GuidedGrade
adoption remain separate.
