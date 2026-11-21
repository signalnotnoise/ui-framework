# A retained, editable component

Put this type in `Components/ReleaseEditor.cs`. The component owns local title
state; its caller owns saving. Imports and signatures match the source kit;
confirm them against the consumer's installed version. This recipe uses the
shared token scales introduced in 1.2.0, which are absent from NuGet 1.1.0. For older
consumers, keep equivalent application-owned design values in one shared theme
object; do not invent these framework properties or repeat literals per view.

```csharp
using UI_Framework;
using static UI_Framework.UI;

public sealed class ReleaseEditor : Component
{
    public ThemeTokens Theme { get; set; } = new();
    private readonly State<string> title = new("Autumn release");
    public Action<string> Save { get; set; } = _ => { };

    public override View Body() => VStack(
        Text("Release details").FontSize(Theme.Typography.Heading),
        Text("Give your team a clear next milestone.").FontSize(Theme.Typography.Body),
        Text("Release name").FontSize(Theme.Typography.Caption),
        TextField(title).FontSize(Theme.Typography.Body).AccessibilityLabel("Release name"),
        Button("Save release", () => Save(title.Value))
            .FontSize(Theme.Typography.Body).ButtonStyle(ButtonStyleKind.Primary)
    ).Spacing(Theme.Spacing.Medium).Padding(Theme.Spacing.ExtraLarge);
}
```

At the application's existing WPF window boundary:

```csharp
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

var tokens = new ThemeTokens();
var host = new ViewHost(() =>
    Component<ReleaseEditor>(editor => { editor.Save = SaveRelease; editor.Theme = tokens; })
        .Id("release-editor")
        .Foreground(tokens.Ink).Background(tokens.Canvas));
ThemeStyles.Apply(host, tokens);
window.Content = host;
window.Closed += (_, _) => host.Dispose();
```

`window` and `SaveRelease(string)` are supplied by the application. Hook disposal
into its established shutdown path rather than adding a second owner. If title
is shared with other editors, make it a parent-owned binding prop instead of
copying that prop into local state on every build.

Pass the same theme instance through component props and to `ThemeStyles.Apply`.
To change it, replace immutable groups with `with`, apply the new native styles
and refresh the host so components reselect their numeric token values. Style
application alone does not rewrite existing `.FontSize`, `.Spacing` or `.Padding`
values. Keep app-specific dimensions in the application's shared design settings.

For a dashboard, compose a header, `AdaptiveGrid` of meaningful summaries and
an editable/list workspace. Use the application's named spacing, padding and corner-radius tokens for cards. Keep palette values in one place;
use `tokens.Muted` for secondary text only when contrast is sufficient. Default
tokens supply native hover, pressed, disabled and focus states. A custom palette
must consider all those states, not only Accent and Canvas.
