using Lab_Feedback_WPF.Presentation;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Lab_Feedback_WPF;

public partial class MainWindow
{
    private ViewHost? _workspaceHost;
    private readonly List<ViewHost> _shellHosts = [];
    private readonly StateList<FileTab> _fileTabs = new();
    private readonly State<string> _activeFile = new("");
    private readonly State<string> _buildCount = new("0");
    private readonly State<string> _score = new("0");
    private readonly State<string> _violationCount = new("0");
    private readonly State<string> _violationColor = new("#CCCCCC");
    private readonly ToolTip _violationTooltip = new();
    private readonly State<string> _queueSummary = new("Queue: 0 left");
    private readonly State<int> _activePanel = new(-1);
    private readonly State<bool> _showComments = new(true);
    private readonly State<bool> _showQueue = new(true);
    private readonly State<bool> _toolsVisible = new(false);
    private readonly State<bool> _violationsSelected = new(false);
    private readonly StateList<string> _courses = new();
    private readonly StateList<string> _assignments = new();
    private readonly State<int> _courseIndex = new(-1);
    private readonly State<int> _assignmentIndex = new(-1);
    private sealed record FileTab(string Tag);

    private ViewHost ShellHost(Func<View> body)
    {
        var host = ReviewTheme.Host(body); _shellHosts.Add(host); return host;
    }

    // The pinned framework has horizontal flex only. This adapter supplies constrained
    // vertical fill and native splitter mechanics; all surrounding content is declarative.
    private static DockPanel FillBelow(FrameworkElement header, FrameworkElement body)
    {
        var dock = new DockPanel(); DockPanel.SetDock(header, System.Windows.Controls.Dock.Top);
        dock.Children.Add(header); dock.Children.Add(body); return dock;
    }

    private Menu? _applicationMenu;

    // The pinned framework has no menu primitive. Keep native menu keyboard and
    // popup behavior inside its adapter, with stable identity across picker updates.
    private Menu ApplicationMenu()
    {
        if (_applicationMenu != null) return _applicationMenu;
        MenuItem Action(string label, RoutedEventHandler handler)
        {
            var item = new MenuItem { Header = label };
            item.Click += handler;
            return item;
        }
        var file = new MenuItem { Header = "_File" };
        file.Items.Add(Action("_Open Submissions Folder", OpenFolderMenuItem_Click));
        file.Items.Add(new Separator());
        file.Items.Add(Action("E_xit", ExitMenuItem_Click));
        var settings = new MenuItem { Header = "_Settings" };
        settings.Items.Add(Action("_Programming Checks...", SettingsMenuItem_Click));
        settings.Items.Add(Action("_AI Provider...", LLMSettingsMenuItem_Click));
        settings.Items.Add(new Separator());
        settings.Items.Add(Action("_Setup Assignment...", SetupAssignmentMenuItem_Click));
        _applicationMenu = new Menu();
        _applicationMenu.Items.Add(file);
        _applicationMenu.Items.Add(settings);
        return _applicationMenu;
    }

    private View BuildHeader() => VStack(
        FlexRow(
            WpfUI.Native(ApplicationMenu).Id("application-menu"),
            Text("Course").Width(60).Flex(0),
            Picker(_courses.ToArray(), new Binding<int>(() => _courseIndex.Value, SelectCourse))
                .AccessibilityLabel("Saved course").Width(180).Flex(0),
            Text("Assignment").Width(90).Flex(0),
            Picker(_assignments.ToArray(), new Binding<int>(() => _assignmentIndex.Value, SelectAssignment))
                .AccessibilityLabel("Saved assignment").Width(220).Flex(0)).Spacing(8),
        VStack(
            Text("Assignment review").FontSize(20),
            Text("Set expectations, review submissions, and prepare feedback."),
            HStack(
                Button("1  Assignment", () => WorkspaceAssignment_Click(this, new())),
                Button("2  Open submissions", () => OpenFolderMenuItem_Click(this, new())),
                Button("3  Review with rubric", () => WorkspaceRubric_Click(this, new())),
                Button("4  Feedback", () => WorkspaceFeedback_Click(this, new()))).Spacing(8)
        ).Spacing(8).Padding(12).Background("#202B36")
        ).Spacing(8).Padding(10).Background(ReviewTheme.Tokens.Surface);

    private View BuildFileTabs() => HStack(_fileTabs.Select(tab => HStack(
        Button(System.IO.Path.GetFileName(tab.Tag), () => SelectTab(tab)).AccessibilityLabel("Open " + System.IO.Path.GetFileName(tab.Tag))
            .Background(_activeFile.Value == tab.Tag ? "#344457" : ReviewTheme.Tokens.Surface),
        Button("×", () => CloseFileTab(tab.Tag)).AccessibilityLabel("Close " + System.IO.Path.GetFileName(tab.Tag))
    ).Spacing(2).Id(tab.Tag)).ToArray()).Spacing(6);

    private View BuildNavigation()
    {
        var items = new List<View>();
        if (_showComments.Value) items.Add(Button("Comments", () => WorkspaceFeedback_Click(this, new()))
            .Background(_activePanel.Value == 0 ? "#344457" : ReviewTheme.Tokens.Surface).AccessibilityLabel("Comments panel"));
        if (_showQueue.Value) items.Add(Button("Job queue", () => ShowQueue_Click(this, new()))
            .Background(_activePanel.Value == 2 ? "#344457" : ReviewTheme.Tokens.Surface).AccessibilityLabel("Job queue panel"));
        items.Add(Button("Rubric", () => WorkspaceRubric_Click(this, new()))
            .Background(_activePanel.Value == 1 ? "#344457" : ReviewTheme.Tokens.Surface).AccessibilityLabel("Rubric panel"));
        return VStack(items.ToArray()).Spacing(6).Padding(5).Width(115);
    }

    private View BuildToolTabs() => FlexRow(
        Button("Console", () => SelectToolsPanel(false, true)).AccessibilityLabel("Console tab").Flex(0)
            .Background(!_violationsSelected.Value ? "#344457" : ReviewTheme.Tokens.Surface),
        Button("Violations", () => SelectToolsPanel(true, true)).AccessibilityLabel("Violations tab").Flex(0)
            .Background(_violationsSelected.Value ? "#344457" : ReviewTheme.Tokens.Surface),
        Button(_toolsVisible.Value ? "Hide panel" : "Show panel", () => SetToolsPanelVisible(!_toolsVisible.Value)).AccessibilityLabel("Toggle tools panel").Align(ViewAlignment.End)
        ).Spacing(8).Padding(4).Background(ReviewTheme.Tokens.Surface);

    private View BuildStatus()
    {
        var items = new List<View>();
        if (_showQueue.Value) items.Add(Button(_queueSummary.Value, () => ShowQueue_Click(this, new())).AccessibilityLabel("Queue status"));
        items.Add(Text("Builds: " + _buildCount.Value)); items.Add(Text("Score: " + _score.Value));
        items.Add(Button("Violations: " + _violationCount.Value, () => SelectToolsPanel(true, true)).Foreground(_violationColor.Value));
        return HStack(items.ToArray()).Spacing(14).Padding(6).Background(ReviewTheme.Tokens.Surface);
    }

    private void InitializeFrameworkShell()
    {
        ReviewTheme.Apply(this);
        InitializeNativeControls();
        commentsDetailsTab.Content = ShellHost(() => Text("Select a student to prepare comments and feedback.").Padding(20));
        commentsDetailsTab.SizeChanged += (_, _) => _feedbackHeight.Value = Math.Max(160, commentsDetailsTab.ActualHeight * 0.5);

        var navigation = new Grid();
        navigation.RowDefinitions.Add(new() { Height = new(1, GridUnitType.Star), MaxHeight = 300 });
        navigation.RowDefinitions.Add(new() { Height = new(5) });
        navigation.RowDefinitions.Add(new() { Height = new(1, GridUnitType.Star), MinHeight = 150 });
        var students = FillBelow(ShellHost(() => Text("SUBMISSIONS").FontSize(12).Padding(10)), ShellHost(() => WpfUI.Native(() => listBoxStudents).Id("students")));
        var files = FillBelow(ShellHost(() => Text("SUBMITTED FILES").FontSize(12).Padding(10)), ShellHost(() => WpfUI.Native(() => fileTreeView).Id("files")));
        var split = new GridSplitter { Height = 5, HorizontalAlignment = HorizontalAlignment.Stretch, ResizeDirection = GridResizeDirection.Rows };
        navigation.Children.Add(students); Grid.SetRow(split, 1); navigation.Children.Add(split); Grid.SetRow(files, 2); navigation.Children.Add(files);

        var editorLayer = new Grid();
        editorLayer.Children.Add(codeEditor); editorLayer.Children.Add(commentOverlay);
        emptyStateOverlay.Child = ShellHost(() => VStack(Text("Review student work").FontSize(22),
            Text("Choose an assignment and open a submissions folder."), Text("Select a student, then a file to review beside the rubric.")).Spacing(12).Padding(24));
        editorLayer.Children.Add(emptyStateOverlay);
        var tabScroll = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = ShellHost(BuildFileTabs) };
        var editorPane = FillBelow(tabScroll, ShellHost(() => WpfUI.Native(() => editorLayer).Id("annotated-editor")));

        var panes = new Grid();
        panes.ColumnDefinitions.Add(new() { Width = new(260), MinWidth = 160 });
        panes.ColumnDefinitions.Add(new() { Width = new(5) });
        panes.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star), MinWidth = 160 });
        panes.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        panes.ColumnDefinitions.Add(sidePanelColumn);
        panes.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        Add(panes, navigation, 0); Add(panes, new GridSplitter { Width = 5, HorizontalAlignment = HorizontalAlignment.Stretch }, 1);
        Add(panes, editorPane, 2); Add(panes, sidePanelSplitter, 3);
        Add(panes, FillBelow(ShellHost(() => Button("Collapse panel", CloseSidePanel)), ShellHost(() => WpfUI.Native(() => rightPanelTabs).Id("workspace-panels"))), 4);
        Add(panes, ShellHost(BuildNavigation), 5);

        violationsPanel.Child = ShellHost(() => WpfUI.Native(() => violationsList).Id("violations"));
        runtimeTerminalPanel.Child = ShellHost(() => WpfUI.Native(() => runtimeTerminalRichTextBox).Id("terminal"));
        toolsPanelContent.Children.Add(violationsPanel); toolsPanelContent.Children.Add(runtimeTerminalPanel);
        var workspace = new Grid();
        workspace.RowDefinitions.Add(new() { Height = new(1, GridUnitType.Star) });
        workspace.RowDefinitions.Add(new() { Height = GridLength.Auto });
        workspace.RowDefinitions.Add(toolsPanelRow);
        workspace.Children.Add(panes);
        var tools = ShellHost(BuildToolTabs); Grid.SetRow(tools, 1); workspace.Children.Add(tools);
        Grid.SetRow(toolsPanelContent, 2); workspace.Children.Add(toolsPanelContent);
        var shell = new DockPanel();
        var header = ShellHost(BuildHeader); DockPanel.SetDock(header, System.Windows.Controls.Dock.Top); shell.Children.Add(header);
        var status = ShellHost(BuildStatus); status.ToolTip = _violationTooltip; DockPanel.SetDock(status, System.Windows.Controls.Dock.Bottom); shell.Children.Add(status);
        shell.Children.Add(workspace);
        Content = _workspaceHost = ReviewTheme.Host(() => WpfUI.Native(() => shell).Id("resize-layout"));
        Closed += (_, _) =>
        {
            _commentLayer?.ClearComments();
            _feedbackHost?.Dispose(); _feedbackHost = null;
            _rubricHost?.Dispose(); _rubricHost = null;
            _gradingView.Dispose();
            foreach (var host in _shellHosts) host.Dispose();
            _shellHosts.Clear();
            _workspaceHost?.Dispose(); _workspaceHost = null;
        };
    }

    private static void Add(Grid grid, FrameworkElement child, int column)
    { Grid.SetColumn(child, column); grid.Children.Add(child); }
}
