using UI_Framework;
using static UI_Framework.UI;

namespace StressLab;

public sealed class LaunchOverview : Component
{
    public LaunchModel Model { get; set; } = null!;
    public override View Body()
    {
        var shipped = Model.Items.Count(item => item.Stage.Value == LaunchStage.Shipped);
        var building = Model.Items.Count(item => item.Stage.Value == LaunchStage.Building);
        var priorities = Model.Items.Where(item => item.Priority.Value && item.Stage.Value != LaunchStage.Shipped).ToArray();
        return VStack(
            VStack(Text("YOUR NEXT CHAPTER").FontSize(12).Foreground(LaunchTheme.Tokens.Accent),
                Text("Good ideas deserve a launch.").FontSize(38),
                Text("See what matters, pick the next move, and give your team something to celebrate.").Foreground(LaunchTheme.Tokens.Muted),
                Button("Open release board →", Model.OpenBoard).ButtonStyle(ButtonStyleKind.Primary)
            ).Spacing(18).Padding(32).Background("#E3EFE7").CornerRadius(20),
            AdaptiveGrid(230,
                Metric("THE PIPELINE", $"{Model.Items.Count - shipped} ideas in motion", "From the first sketch to the final detail."),
                Metric("RIGHT NOW", $"{building} being built", "A clear next step for every card."),
                Metric("THE FINISH LINE", $"{shipped} / {Model.Items.Count} shipped", "Progress worth celebrating.")
            ).Spacing(16),
            Text("Worth your attention").FontSize(24),
            AdaptiveGrid(280, priorities.Length == 0 ? [Text("No outstanding priorities. Explore the board for your next idea.")]
                : priorities.Select(item => VStack(
                    Text($"LP-{item.Id}  /  {item.Area.Value}").FontSize(12).Foreground(LaunchTheme.Tokens.Muted),
                    Text(item.Title.Value).FontSize(22), Text($"{item.Owner.Value} · {item.Stage.Value}"),
                    Button("Shape the details →", () => Model.OpenDetails(item)).ButtonStyle(ButtonStyleKind.Quiet)
                ).Spacing(14).Padding(24).Background(LaunchTheme.Tokens.Surface).CornerRadius(14).Id(item.Id.ToString())).ToArray()).Spacing(16),
            Text("Open a card, make a change, then go Back. Your work follows you.").FontSize(13).Foreground(LaunchTheme.Tokens.Muted)
        ).Spacing(24);
    }

    private static View Metric(string label, string value, string detail) => VStack(
        Text(label).FontSize(11).Foreground(LaunchTheme.Tokens.Muted), Text(value).FontSize(25),
        Text(detail).FontSize(13).Foreground(LaunchTheme.Tokens.Muted)
    ).Spacing(10).Padding(24).Background(LaunchTheme.Tokens.Surface).CornerRadius(14);
}
