using UI_Framework;
using static UI_Framework.UI;

namespace StressLab;

public sealed class Launchpad : Component
{
    public LaunchModel Model { get; set; } = null!;
    private static string Ink => LaunchTheme.Tokens.Ink;
    private static string Muted => LaunchTheme.Tokens.Muted;
    private static string Green => LaunchTheme.Tokens.Accent;

    public override View Body()
    {
        var shipped = Model.Items.Count(item => item.Stage.Value == LaunchStage.Shipped);
        var building = Model.Items.Count(item => item.Stage.Value == LaunchStage.Building);
        var visible = Model.VisibleItems().ToArray();
        return VStack(
            FlexRow(
                Text("◈  LAUNCHPAD").FontSize(20),
                Text("STUDIO NORTH  /  RELEASE 01").FontSize(12).Foreground(Muted).Align(ViewAlignment.Center),
                Text("●  Live session").FontSize(12).Foreground(Green).Align(ViewAlignment.End)
            ).Spacing(12).Padding(20).Background("#FFFFFF").CornerRadius(14),
            FlexRow(
                VStack(Label("MAKE SOMETHING WORTH SHIPPING"), Text("Small team. Big next chapter.").FontSize(34),
                    Text("A shared space for the ideas, details, and decisions behind your next release.").Foreground(Muted)
                ).Spacing(10).Flex(3),
                VStack(Label("RELEASE PROGRESS"), Text($"{shipped} / {Model.Items.Count} shipped").FontSize(26).Foreground(Green),
                    HStack(Enumerable.Range(0, 16).Select(i => Text("").Width(12).Height(6)
                        .Background(i < (Model.Items.Count == 0 ? 0 : (int)Math.Round(16.0 * shipped / Model.Items.Count)) ? Green : "#DCE6DF").CornerRadius(3)).ToArray()).Spacing(3)
                ).Spacing(10).Width(250)
            ).Spacing(20).Padding(10),
            AdaptiveGrid(230, Metric("01 / THE PIPELINE", $"{Model.Items.Count - shipped} ideas in motion", "From the first sketch to the final detail."),
                Metric("02 / RIGHT NOW", $"{building} being built", "Keep the work moving, one card at a time."),
                Metric("03 / THE FINISH LINE", $"{shipped} shipped", "Progress you can see and celebrate.")
            ).Spacing(16),
            FlexRow(VStack(
                Text("Release board").FontSize(23),
                Label("SEARCH TITLE, OWNER OR AREA"),
                FlexRow(TextField(Model.Query), Toggle("Priority only", Model.PriorityOnly).Width(120)).Spacing(12),
                Label("HAVE AN IDEA? GIVE IT A TITLE"),
                FlexRow(TextField(Model.Draft), Button("+ Add an idea", Model.Add).ButtonStyle(ButtonStyleKind.Primary)
                    .IsEnabled(!string.IsNullOrWhiteSpace(Model.Draft.Value)).Width(140)).Spacing(12),
                AdaptiveGrid(240, Column(LaunchStage.Planned, visible, "#EDF1ED"), Column(LaunchStage.Building, visible, "#E8F0ED"),
                    Column(LaunchStage.Shipped, visible, "#F1EDE2")).Spacing(14),
                Text($"{visible.Length} of {Model.Items.Count} cards shown  ·  Select a card to shape the details.").FontSize(12).Foreground(Muted)
            ).Spacing(16).Flex(3),
                Editor().Width(280).Align(ViewAlignment.Stretch, ViewAlignment.Start)
            ).Spacing(20),
            FlexRow(Text(Model.Activity.Value).FontSize(13), Label("DEMO WORKSPACE · CHANGES LAST THIS SESSION").Align(ViewAlignment.End)).Spacing(12)
        ).Spacing(24).Padding(28).Background(LaunchTheme.Tokens.Canvas).Foreground(Ink);
    }

    private static View Label(string text) => Text(text).FontSize(11).Foreground(Muted);
    private static View Metric(string label, string value, string detail) => VStack(Label(label), Text(value).FontSize(25),
        Text(detail).FontSize(13).Foreground(Muted)).Spacing(10).Padding(20).Background(LaunchTheme.Tokens.Surface).CornerRadius(14);

    private View Column(LaunchStage stage, LaunchItem[] visible, string color)
    {
        var items = visible.Where(item => item.Stage.Value == stage).ToArray();
        return VStack(
            FlexRow(Text(stage.ToString()).FontSize(17), Text(items.Length.ToString()).FontSize(17).Align(ViewAlignment.End)).Spacing(8),
            Scroll(VStack(items.Length == 0
                ? [VStack(Text("Room for what's next").FontSize(16), Text("Add an idea or adjust your filters.").FontSize(12).Foreground(Muted)).Spacing(8).Padding(14)]
                : items.Select(Card).ToArray()).Spacing(12)).Height(390)
        ).Spacing(16).Padding(12).Background(color).CornerRadius(14).Id(stage.ToString());
    }

    private View Card(LaunchItem item) => VStack(
        FlexRow(Label($"LP-{item.Id} · {item.Area.Value}"), Text(item.Priority.Value ? "★" : "").Foreground("#B7791F").Width(18)).Spacing(8),
        Button(item.Title.Value, () => Model.Selected.Value = item).FontSize(16).ButtonStyle(ButtonStyleKind.Quiet),
        Text(item.Owner.Value).FontSize(13).Foreground(Muted),
        item.Stage.Value == LaunchStage.Shipped ? Text("✓ Ready for the world").FontSize(12).Foreground(Green)
            : Button(item.Stage.Value == LaunchStage.Planned ? "Start building →" : "Ship it ↗",
                () => Model.Move(item, item.Stage.Value == LaunchStage.Planned ? LaunchStage.Building : LaunchStage.Shipped)).FontSize(12)
                .ButtonStyle(item.Stage.Value == LaunchStage.Building ? ButtonStyleKind.Primary : ButtonStyleKind.Secondary)
    ).Spacing(12).Padding(14).Background(ReferenceEquals(Model.Selected.Value, item) ? "#DBEEE4" : "#FFFFFF").CornerRadius(10).Id(item.Id.ToString());

    private View Editor()
    {
        var item = Model.Selected.Value;
        if (item is null) return Text("Select a card to get started.").Padding(20);
        return VStack(Label($"IN THE DETAILS / LP-{item.Id}"), Text("Make it yours.").FontSize(25),
            Text("Edits appear on the board as you type.").FontSize(12).Foreground(Muted),
            Label("TITLE"), TextField(item.Title),
            Label("OWNER"), TextField(item.Owner),
            Label("AREA"), TextField(item.Area),
            Toggle("Priority for this release", item.Priority),
            Label("NOTES"), TextField(item.Notes),
            Label("MOVE TO"),
            VStack(Enum.GetValues<LaunchStage>().Select(stage => Button(
                item.Stage.Value == stage ? $"● {stage}" : stage.ToString(), () => Model.Move(item, stage))
                .ButtonStyle(item.Stage.Value == stage ? ButtonStyleKind.Primary : ButtonStyleKind.Secondary)).ToArray()).Spacing(6),
            Text("The best work starts with a clear next step.").FontSize(13).Foreground(Green)
        ).Spacing(12).Padding(20).Background("#FFFFFF").CornerRadius(14).Id(item.Id.ToString());
    }
}
