using UI_Framework;
using static UI_Framework.UI;

namespace SkillPreview;

public sealed class ReleaseScreen : Component
{
    public static ThemeTokens Tokens { get; } = new();
    private readonly State<string> release = new("Autumn release");
    private readonly State<bool> notes = new(true);
    private readonly State<string> status = new("All changes saved locally.");
    public bool Polished { get; set; }

    public override View Body() => Polished ? After() : Before();

    private View Before() => VStack(
        Text("SHIPROOM / RELEASE WORKSPACE"),
        Text("Make room for what is next."),
        Text("A focused home for your team's next release."),
        Text("3 work items    1 ready to ship    2 in progress"),
        Text("Release settings"), Text("Release name"),
        TextField(release).AccessibilityLabel("Release name"),
        Toggle("Include release notes", notes),
        Button("Save release", Save), Text(status.Value),
        Text("On the release board"),
        Text("Keyboard navigation / Maya Chen / Ready"),
        Text("Workspace search / Alex Rivera / Building"),
        Text("Export improvements / Sam Patel / Building")
    ).Spacing(6).Padding(16).Background("#FFFFFF").Foreground("#172D32");

    private View After() => VStack(
        Text("SHIPROOM / RELEASE WORKSPACE").FontSize(12).Foreground(Tokens.Accent),
        VStack(Text("Make room for what is next.").FontSize(34),
            Text("A focused home for your team's next release.").Foreground(Tokens.Muted)
        ).Spacing(8),
        AdaptiveGrid(180,
            Metric("IN SCOPE", "3", "work items"),
            Metric("READY", "1", "ready to ship"),
            Metric("IN MOTION", "2", "in progress")
        ).Spacing(16),
        AdaptiveGrid(340,
            VStack(Text("Release settings").FontSize(22),
                Text("Release name").FontSize(12).Foreground(Tokens.Muted),
                TextField(release).AccessibilityLabel("Release name"),
                Toggle("Include release notes", notes),
                Button("Save release", Save).ButtonStyle(ButtonStyleKind.Primary),
                Text(status.Value).FontSize(12).Foreground(Tokens.Muted)
            ).Spacing(16).Padding(24).Background(Tokens.Surface).CornerRadius(12),
            VStack(Text("On the release board").FontSize(22),
                Item("Keyboard navigation", "Maya Chen", "Ready"),
                Item("Workspace search", "Alex Rivera", "Building"),
                Item("Export improvements", "Sam Patel", "Building")
            ).Spacing(16).Padding(24).Background(Tokens.Surface).CornerRadius(12)
        ).Spacing(16)
    ).Spacing(24).Padding(32).Background(Tokens.Canvas).Foreground(Tokens.Ink);

    private static View Metric(string label, string value, string detail) => VStack(
        Text(label).FontSize(11).Foreground(Tokens.Muted),
        HStack(Text(value).FontSize(32), Text(detail).FontSize(14).Foreground(Tokens.Muted)).Spacing(12)
    ).Spacing(8).Padding(20).Background(Tokens.Surface).CornerRadius(12);

    private static View Item(string title, string owner, string state) => VStack(
        Text(title).FontSize(17),
        FlexRow(Text(owner).FontSize(12).Foreground(Tokens.Muted),
            Text(state).FontSize(12).Foreground(Tokens.Accent).Width(64))
    ).Spacing(6);

    private void Save() => status.Value = $"Saved: {release.Value}. Release notes {(notes.Value ? "included" : "excluded")}.";
}
