# Retained WPF controls

See [validation status](native-interop-validation.md): functional checks and a seven-sample performance repeat pass; earlier timing failures remain documented. Local packages are for integration testing, and no public publication has been performed.

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

The factory must return an unparented element on the host's UI thread. Parented controls, presentation roots, windows and null results are rejected without stealing ownership. Once accepted, the element is detached before the **original mount's** `release` callback runs exactly once. The framework never implicitly calls `IDisposable.Dispose` on the native element. Use `release` to unsubscribe or dispose app-owned resources. If initial update fails, the accepted element is released; an update failure on a retained element leaves it owned until removal/disposal. There is no rollback of arbitrary application mutations.

Creation/update/release execute on the WPF dispatcher. Removal and explicit host disposal drive cleanup; temporary WPF `Unloaded` events do not define framework lifetime. Navigation and virtualization can unmount native islands and create fresh controls later, so durable editor/document state belongs in application models. Native controls are not saved in logical snapshots.

Width, height, padding, alignment, background and enabled modifiers apply through the outer framework frame. Existing implicit WPF styles may inherit into the island. `AccessibilityLabel` targets the island root: without a label it preserves a factory-supplied name; setting then removing a label clears the local name override. Native names on descendants remain application-owned. A native island takes no declarative children; compose its native children in its factory/update callbacks.

Core carries only `ViewKind.Platform` and an opaque `PlatformContent` descriptor. WPF's `Interop` layer owns creation, identity and cleanup. The common renderer owns outer layout and reconciliation.

## Migration follow-ups

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
