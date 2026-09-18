using UI_Framework;
using static UI_Framework.UI;

namespace StressLab;

public sealed class Launchpad : Component
{
    public LaunchModel Model { get; set; } = null!;

    public override View Body() => VStack(
        FlexRow(Text("◈  LAUNCHPAD").FontSize(20),
            Text("STUDIO NORTH  /  RELEASE 01").FontSize(12).Foreground(LaunchTheme.Tokens.Muted).Align(ViewAlignment.Center),
            Text("●  Live session").FontSize(12).Foreground(LaunchTheme.Tokens.Accent).Align(ViewAlignment.End)
        ).Spacing(12).Padding(20).Background(LaunchTheme.Tokens.Surface).CornerRadius(14),
        FlexRow(
            HStack(Button("← Back", () => Model.Navigation.Back()).IsEnabled(Model.Navigation.CanGoBack),
                Button("Overview", Model.Navigation.PopToRoot).ButtonStyle(Model.Navigation.Current.Screen == LaunchScreen.Overview ? ButtonStyleKind.Primary : ButtonStyleKind.Quiet),
                Button("Board", Model.OpenBoard).ButtonStyle(Model.Navigation.Current.Screen == LaunchScreen.Board ? ButtonStyleKind.Primary : ButtonStyleKind.Quiet)).Spacing(8),
            Toggle("Reduce motion", Model.ReduceMotion).Width(150)
        ).Spacing(16),
        Navigation(Model.Navigation, Screen, Model.ReduceMotion.Value ? NavigationTransition.None : NavigationTransition.FadeSlide),
        FlexRow(Text(Model.Activity.Value).FontSize(13), Text("DEMO WORKSPACE · CHANGES LAST THIS SESSION").FontSize(11)
            .Foreground(LaunchTheme.Tokens.Muted).Align(ViewAlignment.End)).Spacing(12)
    ).Spacing(24).Padding(28).Background(LaunchTheme.Tokens.Canvas).Foreground(LaunchTheme.Tokens.Ink);

    private View Screen(LaunchRoute route) => route.Screen switch
    {
        LaunchScreen.Overview => Component<LaunchOverview>(screen => screen.Model = Model),
        LaunchScreen.Board => Component<LaunchBoard>(screen => screen.Model = Model),
        LaunchScreen.Details => Component<LaunchDetails>(screen => { screen.Model = Model; screen.ItemId = route.ItemId; }),
        _ => throw new ArgumentOutOfRangeException(nameof(route))
    };
}
