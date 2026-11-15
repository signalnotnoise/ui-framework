using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

namespace PackageWorkspace;

public sealed class WorkspaceScreen : Component
{
    public WorkspaceModel Model { get; set; } = null!;
    public override void OnMounted() => Model.Mounts++;
    public override void OnUnmounted() => Model.Unmounts++;

    public override View Body()
    {
        Model.Builds++;
        var rows = Enumerable.Range(0, 100).Select(i =>
            Button($"Work item {i + 1:000}", () => Model.Selected.Value = i)
                .ButtonStyle(i == Model.Selected.Value ? ButtonStyleKind.Primary : ButtonStyleKind.Quiet)
                .Id("item-" + i)).ToArray();
        return Dock(
            top: VStack(Text("Release workspace").FontSize(28),
                Text("A standalone sample installed from NuGet").FontSize(14),
                HStack(Button("Review", () => Model.History.Push("review")),
                    Button("Next item", () => Model.Selected.Value = (Model.Selected.Value + 1) % 100)).Spacing(8)
            ).Spacing(10).Padding(12),
            center: SplitPane(
                VStack(Text("Work items").FontSize(20),
                    Model.Virtualized ? VirtualList(rows, 350) : Scroll(VStack(rows)).Height(350)).Spacing(12).Padding(16),
                VStack(Text("Release name").FontSize(14), TextField(Model.Title).AccessibilityLabel("Release name"),
                    Text($"Selected work item: {Model.Selected.Value + 1:000}").FontSize(20),
                    Text("Native notes").FontSize(14),
                    WpfUI.Native(Model.CreateEditor, NativeStyleScope.PreserveNative, Model.UpdateEditor, Model.ReleaseEditor)
                        .AccessibilityLabel("Native release notes").Height(170),
                    Text("Edits remain in the model when you visit Review and return.").FontSize(14)
                ).Spacing(14).Padding(16), Model.Pane, minimumFirst: 220, minimumSecond: 280),
            bottom: Text("Session-only data · Close the window to release the workspace.").FontSize(12).Padding(12)
        ).Padding(16).Background("#F4F6F8").Foreground("#172D32");
    }
}
