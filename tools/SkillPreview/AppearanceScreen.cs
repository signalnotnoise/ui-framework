using UI_Framework;
using static UI_Framework.UI;

namespace SkillPreview;

public sealed class AppearanceScreen : Component
{
    public ThemeTokens Theme { get; set; } = new();
    private readonly State<string> name = new("");
    private readonly State<bool> notes = new(true);
    private readonly State<int> audience = new(0);
    private readonly State<double> pane = new(310);
    private readonly State<string> message = new("Error: Enter a release name before saving.");

    public override View Body() => Dock(
        top: VStack(
            Text("Release workspace").FontSize(28),
            Text("Review the details, resolve errors, then prepare your release.").Foreground(Theme.Muted),
            HStack(Button("Save draft", Save).ButtonStyle(ButtonStyleKind.Primary),
                Button("Preview", () => message.Value = "Preview is ready.").ButtonStyle(ButtonStyleKind.Quiet),
                Button("Publish", () => { }).IsEnabled(false)).Spacing(8)
        ).Spacing(10).Padding(8),
        center: SplitPane(
            VStack(Text("Release details").FontSize(22),
                Text("Release name (required)").FontSize(14),
                TextField(name).AccessibilityLabel("Release name. " + message.Value),
                Text(message.Value).FontSize(14),
                Text("Audience").FontSize(14),
                Picker(new[] { "Internal review", "Public preview" }, audience).AccessibilityLabel("Audience"),
                Toggle("Include release notes and a summary of changes for reviewers", notes),
                Text("Publication is unavailable until review is complete.").Foreground(Theme.Muted)
            ).Spacing(14).Padding(20).Background(Theme.Surface).CornerRadius(8),
            VStack(Text("Review queue").FontSize(22),
                Row("Keyboard navigation", "Ready", "Verify the final reading order and focus visibility."),
                Row("A longer workspace title that must remain readable at compact widths", "In review",
                    "Confirm that the editor and navigation panes keep their contents when resized."),
                Row("Package notes", "Blocked", "Error: Add an upgrade note before publishing this change."),
                Text("3 items · 1 ready · 1 blocked").FontSize(12).Foreground(Theme.Muted)
            ).Spacing(20).Padding(20).Background(Theme.Surface).CornerRadius(8),
            pane, minimumFirst: 260, minimumSecond: 260),
        bottom: Text("Draft only · Changes are stored for this session.").FontSize(12).Foreground(Theme.Muted).Padding(8)
    ).Padding(24).Background(Theme.Canvas).Foreground(Theme.Ink);

    private View Row(string title, string status, string description) => VStack(
        Text(title).FontSize(17), Text(status).FontSize(12).Foreground(Theme.Accent),
        Text(description).FontSize(14).Foreground(Theme.Ink)
    ).Spacing(6);

    private void Save() => message.Value = string.IsNullOrWhiteSpace(name.Value)
        ? "Error: Enter a release name before saving." : "Saved: " + name.Value;
}
