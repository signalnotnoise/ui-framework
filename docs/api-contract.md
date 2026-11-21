# Public API and compatibility contract

FW-016 decision, October 23, 2026; adopted for v1 on November 15.
This defines the supported stable contract. Publication status and exact-package
evidence are tracked in [the v1 release record](releases/1.0.0.md).

The v1 acceptance matrix is Windows 11 x64 / .NET 10, with local SDK 10.0.402
and runtime 10.0.13. Hosted Windows validation records its actual SDK/runtime
separately. Other operating systems, architectures and .NET 11 previews are not
part of this tested support claim.

## Supported surface

Source-only additions are recorded in `PublicAPI.Unshipped.txt` until released.
Version 1.3.0 / FW-040's [button tooltip contract](tooltips.md) is additive and not available in
published 1.2.0. Its text override does not change accessible names or native
tooltip presentation policy.

The core library remains platform independent; the Windows renderer targets WPF.
Both currently target .NET 10. Support is limited to Windows/WPF for rendering;
new runtime or platform targets require their own build, package and interaction
acceptance. A framework release does not extend the runtime vendor's support life.

Retain the published `UI_Framework` and `UI_Framework.Wpf` namespaces and package
IDs. Public types, including ViewSession and ObservableState extension methods,
are intentional API. Public record-generated members are included in the inventory.
Internal renderer nodes, snapshots and dependency tracking are implementation details.

Version 1.1.0 adds optional `View.Motion`, fluent opacity and
translation targets, core timing descriptions and the WPF inherited motion switch.
These additions are tracked in `PublicAPI.Shipped.txt`; they are not available
in published 1.0.0. See the [animation contract](animation.md) for interruption,
recycling, reduced-motion and input semantics.

Direct `View` construction and record `with` expressions remain supported. UI
factory methods are the recommended path. Public properties form a description
schema, not a plugin protocol for arbitrary native renderers. Callers must provide
valid kind/child/component combinations and stable, unique sibling keys. WPF
validates descriptions before patching; validation is not a compile-time guarantee.
`PlatformContent` is an opaque backend payload: use WpfUI.Native for WPF islands.
Applications must not mutate supplied child/options collections behind a description.
An intentional API redesign requires a separately reviewed migration proposal.

## Behavioral promises

| Area | Contract and verification |
| --- | --- |
| Identity | Same parent, sibling key or position, kind and component type retain compatible instances. Parent changes and removal/reinsertion remount. See [components](components.md), ComponentChecks and PanelReorderTests. |
| Lifecycle | Mount/unmount may repeat during navigation or virtualization. Keep durable state in models; detach subscriptions before cleanup. Native callbacks execute on the dispatcher; native removal detaches before the original release callback. See NativeHostTests. |
| State/threading | State, list, computed and session operations belong to their creating thread; WPF controls belong to their dispatcher. Background results require explicit dispatch. Notifications complete delivery before reporting subscriber failures. See StateNotificationTests and StateSessionTests. |
| Bindings/derived values | Bindings read accepted values back and filter projected equality. Computed reevaluates reads, including ordinary properties; caching is not implied. StateList enumeration is a snapshot; item fields are not deeply observed. See [bindings](bindings.md). |
| Sizing/styles | View modifiers affect the outer frame. Native local values and bindings retain WPF precedence. Automatic native button height retains its theme minimum; explicit height permits compact sizing. PreserveNative isolates common implicit control styles, not all resources. See [styling](layout-styling.md) and [interop](native-interop.md). |
| Virtualization | Bound native realization does not bound description work or visited-row logical state. Unmount creates fresh native controls on return and may restore logical components. Active native composition uses WPF's focus-loss path before detachment; IME sessions are not serialized. User-confirmed physical IME acceptance is recorded October 29, with build/provider details unspecified. See [virtualization](virtualization.md). |
| Keyboard wrappers (published since alpha.6) | ViewHost and internal virtual-row presenters default to non-focusable/non-tab-stop wrappers; their actionable descendants own keyboard focus. Explicit native local values retain WPF precedence. See [accessibility](accessibility.md). |
| Errors | Build/validation failures retain the previous tree; patch failures clear damaged hosts and permit explicit Refresh. No arbitrary mutation rollback. See [recovery](error-recovery.md), RendererFailureTests and ViewValidationTests. |
| Cleanup policy | Applications opt into one WpfComCleanupPolicy on their STA before creating controls and close it after hosts, before dispatcher shutdown. Hosts do not install it implicitly. See [lifecycle](application-cleanup-policy-2026-09-25.md). |

These are the supported v1 behavioral contracts. Provider-completion, focus-race and wrapper fixes
ship in alpha.6/alpha.7. [October 29 manual acceptance](manual-acceptance-2026-10-29.md)
closes FW-005/FW-006 within its recorded scope; missing provider/build/scale details
are not inferred. The automation peer-cache cleanup ships in 1.0.0,
with additional physical Narrator revalidation deferred to FW-026 after v1.

## Compatibility checks and change policy

Each library has PublicAPI.Shipped.txt (reviewed baseline) and
PublicAPI.Unshipped.txt (explicitly reviewed additions). Pinned, private build-time
Microsoft.CodeAnalysis.PublicApiAnalyzers checks both during ordinary builds.
RS0016 and RS0017 are errors: undeclared additions/changes and missing declarations
fail compilation. Release validation builds with warnings treated as errors too.
Nullable annotations, optional defaults and protected API are included. These
checks do not replace behavioral tests, downstream compilation or binary package
compatibility validation for a stable release.

The existing two Native overloads differ by a required style-scope parameter.
RS0026 is suppressed only in WpfUI.cs to retain their existing optional signatures;
future overload additions require explicit ambiguity/source compatibility review.

During previews, breaking changes require a fresh version, changelog and migration
example. For v1, preserve supported source/binary and documented behavioral contracts
within the major version. Prefer additive APIs; mark replacement guidance with
Obsolete before eventual removal in a major version. Do not silently alter default
threading, identity, computed evaluation or cleanup ownership in a patch release.
No fixed maintenance duration or additional supported OS/SDK matrix is promised yet.

For an intentional addition, review its contract and add the analyzer's declaration
to Unshipped. At a release, move approved additions into Shipped. Do not regenerate
or delete baseline entries merely to clear a build failure. A breaking baseline edit
requires justification, version/migration documentation and consumer verification.

Example: existing Native(create, update, release) keeps inherited styles. Opt into
Native(create, NativeStyleScope.PreserveNative, update, release) when needed; this
adds a style boundary without changing the old overload's meaning. Existing
`new View(ViewKind.Text) { Content = "Title" }` and `UI.Text("Title")` remain valid.

For every v1 release candidate, repeat the independent consumer upgrade on the exact RC packages (FW-019 already
verified the published alpha.6-to-alpha.7 source/binary path), select a concrete supported Windows/runtime test matrix, and
complete the broader [acceptance matrix](backlog.md). API inventory acceptance alone
does not close those release requirements.

Run `pwsh -NoProfile -File tools/Test-ApiCompatibility.ps1 -VerifyGuard` to prove both diagnostic guards fail and restore the baseline. The Windows CI job runs this probe; ordinary builds enforce the baseline without modifying it.

## Independent package acceptance — November 15, 2026

FW-019 now verifies the published alpha.6-to-alpha.7 source and binary upgrade on
Windows 11 x64 / .NET 10.0.13 using a standalone NuGet-only workspace. See the
[acceptance record](fw019-package-adoption-2026-11-15.md) for hashes, cleanup and
measurement limits. This preview path does not replace exact-package acceptance
for a future v1 release candidate or expand the supported platform matrix.

Version 1.2.0 adds immutable `VectorIcon` definitions, icon views and icon buttons,
plus spacing, typography, radius and icon groups on `ThemeTokens`. Existing defaults
and explicit sizes remain unchanged. See [icons](icons.md) and [shared tokens](design-tokens.md).
