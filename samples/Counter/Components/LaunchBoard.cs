using UI_Framework;
using static UI_Framework.UI;

namespace StressLab;

public sealed class LaunchBoard : Component
{
    public LaunchModel Model { get; set; } = null!;
    private readonly State<bool> showShipped = new(true);

    public override View Body()
    {
        var visible = Model.VisibleItems().Where(item => showShipped.Value || item.Stage.Value != LaunchStage.Shipped).ToArray();
        return VStack(
            Text("A little momentum, every day.").FontSize(32),
            Text("Move the work forward. Open a card when it needs your full attention.").Foreground(LaunchTheme.Tokens.Muted),
            AdaptiveGrid(300,
                VStack(Label("SEARCH TITLE, OWNER OR AREA"), TextField(Model.Query)).Spacing(8),
                HStack(Toggle("Priority only", Model.PriorityOnly), Toggle("Show shipped", showShipped)).Spacing(20)
            ).Spacing(16),
            Label("HAVE AN IDEA? GIVE IT A TITLE"),
            FlexRow(TextField(Model.Draft), Button("+ Add an idea", Model.Add).ButtonStyle(ButtonStyleKind.Primary)
                .IsEnabled(!string.IsNullOrWhiteSpace(Model.Draft.Value)).Width(160)).Spacing(12),
            AdaptiveGrid(260, (showShipped.Value ? Enum.GetValues<LaunchStage>() : [LaunchStage.Planned, LaunchStage.Building])
                .Select(stage => Column(stage, visible)).ToArray()).Spacing(16),
            Label($"{visible.Length} of {Model.Items.Count} cards shown · Filters and your Show shipped preference survive Back navigation.")
        ).Spacing(16);
    }

    private static View Label(string text) => Text(text).FontSize(12).Foreground(LaunchTheme.Tokens.Muted);

    private View Column(LaunchStage stage, LaunchItem[] visible)
    {
        var items = visible.Where(item => item.Stage.Value == stage).ToArray();
        return VStack(
            FlexRow(Text(stage.ToString()).FontSize(19), Text(items.Length.ToString()).Align(ViewAlignment.End)).Spacing(8),
            Scroll(VStack(items.Length == 0 ? [Text("Room for what's next").Padding(18).Foreground(LaunchTheme.Tokens.Muted)]
                : items.Select(Card).ToArray()).Spacing(12)).Height(430)
        ).Spacing(16).Padding(14).Background(stage == LaunchStage.Shipped ? "#F1EDE2" : "#E8F0ED").CornerRadius(14).Id(stage.ToString());
    }

    private View Card(LaunchItem item) => VStack(
        FlexRow(Label($"LP-{item.Id} · {item.Area.Value}"), Text(item.Priority.Value ? "★" : "").Foreground("#B7791F").Width(18)).Spacing(8),
        Button(item.Title.Value, () => Model.OpenDetails(item)).FontSize(18).ButtonStyle(ButtonStyleKind.Quiet),
        Label(item.Owner.Value),
        item.Stage.Value == LaunchStage.Shipped ? Text("✓ Ready for the world").FontSize(12).Foreground(LaunchTheme.Tokens.Accent)
            : Button(item.Stage.Value == LaunchStage.Planned ? "Start building →" : "Ship it ↗",
                () => Model.Move(item, item.Stage.Value == LaunchStage.Planned ? LaunchStage.Building : LaunchStage.Shipped))
                .ButtonStyle(item.Stage.Value == LaunchStage.Building ? ButtonStyleKind.Primary : ButtonStyleKind.Secondary)
    ).Spacing(14).Padding(16).Background(LaunchTheme.Tokens.Surface).CornerRadius(12).Id(item.Id.ToString());
}
