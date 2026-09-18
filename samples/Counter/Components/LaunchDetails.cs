using UI_Framework;
using static UI_Framework.UI;

namespace StressLab;

public sealed class LaunchDetails : Component
{
    public LaunchModel Model { get; set; } = null!;
    public int? ItemId { get; set; }

    public override View Body()
    {
        var item = Model.Items.FirstOrDefault(item => item.Id == ItemId);
        if (item is null) return VStack(Text("This card is no longer available.").FontSize(28), Button("Open board", Model.OpenBoard)).Spacing(20);
        return VStack(
            Label($"RELEASE 01  /  LP-{item.Id}  /  {item.Stage.Value.ToString().ToUpperInvariant()}"),
            Text(item.Title.Value).FontSize(34),
            Text("Give this idea a clear direction.").Foreground(LaunchTheme.Tokens.Muted),
            AdaptiveGrid(340,
                VStack(Text("The details").FontSize(24), Label("TITLE"), TextField(item.Title),
                    Label("OWNER"), TextField(item.Owner), Label("AREA"), TextField(item.Area),
                    Label("NOTES"), TextField(item.Notes), Toggle("Priority for this release", item.Priority)
                ).Spacing(14).Padding(28).Background(LaunchTheme.Tokens.Surface).CornerRadius(16),
                VStack(Text("Keep it moving.").FontSize(24),
                    Text("Choose the next stage. The overview and board will reflect your changes when you return.").Foreground(LaunchTheme.Tokens.Muted),
                    VStack(Enum.GetValues<LaunchStage>().Select(stage => Button(
                        item.Stage.Value == stage ? $"● {stage}" : $"Move to {stage}", () => Model.Move(item, stage))
                        .ButtonStyle(item.Stage.Value == stage ? ButtonStyleKind.Primary : ButtonStyleKind.Secondary)).ToArray()).Spacing(12),
                    Text("Changes are applied as you type and kept for this session.").FontSize(13).Foreground(LaunchTheme.Tokens.Muted),
                    Button("← Back to where I was", () => Model.Navigation.Back()).ButtonStyle(ButtonStyleKind.Quiet)
                ).Spacing(20).Padding(28).Background("#E3EFE7").CornerRadius(16).Align(ViewAlignment.Stretch, ViewAlignment.Start)
            ).Spacing(24)
        ).Spacing(20);
    }

    private static View Label(string text) => Text(text).FontSize(12).Foreground(LaunchTheme.Tokens.Muted);
}
