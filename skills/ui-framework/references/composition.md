# A retained, editable component

Put this type in `Components/ReleaseEditor.cs`. The component owns local title
state; its caller owns saving. Imports and signatures match the source kit;
confirm them against the consumer's installed version.

```csharp
using UI_Framework;
using static UI_Framework.UI;

public sealed class ReleaseEditor : Component
{
    private readonly State<string> title = new("Autumn release");
    public Action<string> Save { get; set; } = _ => { };

    public override View Body() => VStack(
        Text("Release details").FontSize(24),
        Text("Give your team a clear next milestone."),
        Text("Release name").FontSize(12),
        TextField(title).AccessibilityLabel("Release name"),
        Button("Save release", () => Save(title.Value))
            .ButtonStyle(ButtonStyleKind.Primary)
    ).Spacing(12).Padding(24);
}
```

At the application's existing WPF window boundary:

```csharp
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

var tokens = new ThemeTokens();
var host = new ViewHost(() =>
    Component<ReleaseEditor>(editor => editor.Save = SaveRelease)
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

For a dashboard, compose a header, `AdaptiveGrid` of meaningful summaries and
an editable/list workspace. A card can be `VStack(...).Spacing(12).Padding(24)
.Background(tokens.Surface).CornerRadius(12)`. Keep palette values in one place;
use `tokens.Muted` for secondary text only when contrast is sufficient. Default
tokens supply native hover, pressed, disabled and focus states. A custom palette
must consider all those states, not only Accent and Canvas.
