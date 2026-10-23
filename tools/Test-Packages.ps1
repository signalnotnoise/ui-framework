#requires -Version 7.0
param([switch]$NoBuild, [string]$Version, [string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location $projectRoot
try {
    $customVersion = -not [string]::IsNullOrWhiteSpace($Version)
    if ($customVersion -and $NoBuild) { throw 'A version override requires a matching build; omit -NoBuild.' }
    if (-not $customVersion) {
        [xml]$properties = Get-Content 'Directory.Build.props'
        $Version = [string]$properties.Project.PropertyGroup.Version
    }
    if ($Version -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z]+([.-][0-9A-Za-z]+)*)?$') { throw 'Invalid package version.' }
    $feed = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory, $projectRoot) } else { Join-Path $projectRoot 'artifacts/packages' }
    if ($customVersion -and (Test-Path (Join-Path $feed "SignalNotNoise.UI.$Version.nupkg"))) {
        throw 'This local package version already exists. Choose a new version instead of replacing it.'
    }
    foreach ($project in @('UI Framework/UI Framework.csproj', 'UI Framework.Wpf/UI Framework.Wpf.csproj')) {
        $arguments = @('pack', $project, '-c', 'Release', '-o', $feed, '--disable-build-servers', '-warnaserror', "-p:Version=$Version")
        if ($NoBuild) { $arguments += @('--no-build', '--no-restore') }
        & dotnet @arguments
        if ($LASTEXITCODE -ne 0) { throw "Packing failed: $project" }
    }
    # A unique consumer and package cache prevent an older build of this version from masking defects.
    $consumer = Join-Path $projectRoot ('artifacts/package-validation/' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $consumer -Force | Out-Null
    @"
<configuration><packageSources><clear /><add key="local" value="$([System.Security.SecurityElement]::Escape($feed))" /></packageSources></configuration>
"@ | Set-Content (Join-Path $consumer 'NuGet.Config')
    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType><TargetFramework>net10.0-windows</TargetFramework>
    <UseWPF>true</UseWPF><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable>
    <IsPackable>false</IsPackable><NuGetAudit>false</NuGetAudit>
  </PropertyGroup>
  <ItemGroup><PackageReference Include="SignalNotNoise.UI.Wpf" Version="[$version]" /></ItemGroup>
</Project>
"@ | Set-Content (Join-Path $consumer 'PackageSmoke.csproj')
    @'
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var cleanupPolicy = new WpfComCleanupPolicy(Dispatcher.CurrentDispatcher);
        var count = new State<int>(0);
        using var host = new ViewHost(() => VStack(
            Text($"Count: {count.Value}").FontSize(24),
            UI_Framework.UI.Button("Increment", () => count.Value++)
        ).Spacing(12).Padding(20));
        host.Measure(new Size(400, 300));
        host.Arrange(new Rect(0, 0, 400, 300));
        var button = Find<Button>(host);
        var label = Find<TextBlock>(host);
        if (label.Text != "Count: 0") throw new Exception("Initial text mismatch.");
        button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        if (count.Value != 1 || Find<TextBlock>(host).Text != "Count: 1")
            throw new Exception("Packaged state update failed.");
        if (!ReferenceEquals(button, Find<Button>(host)))
            throw new Exception("Button identity was not retained.");
        Console.WriteLine("Package installation, transitive core dependency, rendering, click, and state update passed.");
        var fileLabel = new TextBlock { Text = "Program.cs" };
        var close = new Button { Content = "Close" };
        var closed = 0;
        close.Click += (_, _) => closed++;
        var fileRow = new StackPanel { Orientation = Orientation.Horizontal };
        fileRow.Children.Add(fileLabel);
        fileRow.Children.Add(close);
        var fileButton = new Button { Content = fileRow };
        var themed = new ContentControl { Content = fileButton };
        ThemeStyles.Apply(themed, new ThemeTokens());
        themed.Measure(new Size(400, 300));
        themed.Arrange(new Rect(0, 0, 400, 300));
        themed.UpdateLayout();
        close.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
        if (fileLabel.ActualWidth <= 0 || close.ActualWidth <= 0 || closed != 1 || !ReferenceEquals(fileRow, fileButton.Content))
            throw new Exception("Packaged themed rich button content failed.");
        Console.WriteLine("Packaged themed file label and interactive close content passed.");
        var creates = 0;
        var releases = 0;
        var value = 0;
        TextBox? editor = null;
        using var island = new ViewHost(() => WpfUI.Native(
            () => { creates++; return editor = new TextBox { Text = "retained native editor" }; },
            control => control.Tag = value,
            control => { if (control.Parent is not null) throw new Exception("Release ran before detach."); releases++; }).Id("editor"));
        editor!.Select(2, 4);
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        var watch = System.Diagnostics.Stopwatch.StartNew();
        for (value = 1; value <= 1000; value++) island.Refresh();
        watch.Stop();
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
        if (creates != 1 || editor.Text != "retained native editor" || editor.SelectionLength != 4 || !Equals(editor.Tag, 1000))
            throw new Exception("Packaged native retention/update failed.");
        island.Dispose();
        island.Dispose();
        if (releases != 1) throw new Exception("Packaged native release was not exactly once.");
        Console.WriteLine($"Native interop: 1000 retained updates, {watch.Elapsed.TotalMilliseconds:F2} ms, {allocated} UI-thread bytes; one creation and one release. Diagnostic single run, not a historical comparison.");
        CheckStyleScope();
        CheckCompactInput();
        CheckCompactButton();
    }

    private static void CheckStyleScope()
    {
        var scope = new State<NativeStyleScope>(NativeStyleScope.PreserveNative);
        var creates = 0;
        var releases = 0;
        var editor = new TextBox { Text = "Retained query", FontSize = 16, Height = 36 };
        using var host = new ViewHost(() => WpfUI.Native(
            () => { creates++; return editor; }, scope.Value,
            update: control => control.Tag = "updated",
            release: control => {
                if (control.Parent is not null) throw new Exception("Scoped release ran before detach.");
                releases++;
            }).Id("scoped-editor"));
        ThemeStyles.Apply(host, new ThemeTokens());
        var hiddenTemplate = new ControlTemplate(typeof(TextBox))
        { VisualTree = new FrameworkElementFactory(typeof(Border)) };
        var hiddenStyle = new Style(typeof(TextBox));
        hiddenStyle.Setters.Add(new Setter(Control.TemplateProperty, hiddenTemplate));
        host.Resources[typeof(TextBox)] = hiddenStyle;
        using var source = new HwndSource(new HwndSourceParameters("Package native style scope")
        { Width = 400, Height = 120, PositionX = -10000, PositionY = -10000 });
        source.RootVisual = host;
        Layout(host);
        if (editor.Template.FindName("PART_ContentHost", editor) is null)
            throw new Exception("Packaged native style scope did not preserve the editing host.");
        editor.Select(2, 4);
        host.Refresh();
        if (creates != 1 || editor.SelectionStart != 2 || editor.SelectionLength != 4
            || !Equals(editor.Tag, "updated"))
            throw new Exception("Packaged scoped update lost native identity or selection.");
        scope.Value = NativeStyleScope.Inherit;
        Layout(host);
        if (!ReferenceEquals(hiddenStyle, editor.Style))
            throw new Exception("Packaged inherited style was not restored.");
        scope.Value = NativeStyleScope.PreserveNative;
        Layout(host);
        if (creates != 1 || editor.Text != "Retained query"
            || editor.Template.FindName("PART_ContentHost", editor) is null)
            throw new Exception("Packaged scope change recreated or damaged the native editor.");
        source.RootVisual = null;
        host.Dispose();
        host.Dispose();
        if (releases != 1) throw new Exception("Packaged scoped release was not exactly once.");
        Console.WriteLine("Packaged style scope, original overload, retained selection, scope changes and cleanup passed.");
    }

    private static void CheckCompactInput()
    {
        var query = new State<string>("Find this query");
        using var host = new ViewHost(() => HStack(
            TextField(query).Id("query").FontSize(16).Height(36)).Height(60).Padding(10));
        ThemeStyles.Apply(host, new ThemeTokens());
        using var source = new HwndSource(new HwndSourceParameters("Package compact input")
        { Width = 420, Height = 80, PositionX = -10000, PositionY = -10000 });
        source.RootVisual = host;
        Layout(host);
        var editor = Find<TextBox>(host);
        var scroll = (ScrollViewer)editor.Template.FindName("PART_ContentHost", editor);
        var character = editor.GetRectFromCharacterIndex(0);
        if (character.IsEmpty || scroll.ViewportHeight < character.Height
            || character.Top < 0 || character.Bottom > editor.ActualHeight)
            throw new Exception("Packaged compact input clips its query line.");
        editor.Select(2, 4);
        query.Value = "Find this query again";
        Layout(host);
        if (!ReferenceEquals(editor, Find<TextBox>(host)) || editor.SelectionStart != 2)
            throw new Exception("Packaged compact input lost retained identity or caret.");
        editor.Text = "User query";
        if (query.Value != "User query") throw new Exception("Packaged compact input binding failed.");
        source.RootVisual = null;
        Console.WriteLine("Packaged compact input viewport, retained caret and binding passed.");
    }

    private static void CheckCompactButton()
    {
        var button = new Button { Content = "×", FontSize = 10, Width = 18, Height = 18, Padding = new Thickness(0), VerticalAlignment = VerticalAlignment.Top };
        var root = new ContentControl { Content = button };
        ThemeStyles.Apply(root, new ThemeTokens());
        using var source = new HwndSource(new HwndSourceParameters("Package compact button")
        { Width = 420, Height = 120, PositionX = -10000, PositionY = -10000 });
        source.RootVisual = root;
        Layout(root);
        var chrome = (Border)button.Template.FindName("Chrome", button);
        var text = Find<TextBlock>(button);
        var bounds = text.TransformToAncestor(button).TransformBounds(new Rect(text.RenderSize));
        if (chrome.Padding != new Thickness(0) || Math.Abs(button.ActualHeight - 18) > 0.01
            || text.ActualWidth <= 0 || text.ActualHeight <= 0 || bounds.Left < 0 || bounds.Top < 0
            || bounds.Right > button.ActualWidth || bounds.Bottom > button.ActualHeight)
            throw new Exception("Packaged compact button clips its glyph or ignores native sizing/padding.");
        ThemeStyles.Apply(root, new ThemeTokens { Ink = "#EDF1F7", Surface = "#20252E" });
        Layout(root);
        if (!Equals(button.Content, "×") || ((Border)button.Template.FindName("Chrome", button)).Padding != new Thickness(0))
            throw new Exception("Packaged compact button lost content or native padding after retheming.");
        button.MinHeight = 24;
        Layout(root);
        if (Math.Abs(button.ActualHeight - 24) > 0.01) throw new Exception("Packaged compact button ignores local MinHeight.");
        button.ClearValue(FrameworkElement.MinHeightProperty);
        Layout(root);
        if (Math.Abs(button.ActualHeight - 18) > 0.01) throw new Exception("Packaged compact button did not restore explicit height.");
        button.ClearValue(FrameworkElement.HeightProperty);
        Layout(root);
        if (Math.Abs(button.ActualHeight - 36) > 0.01) throw new Exception("Packaged automatic button lost its theme minimum.");
        source.RootVisual = null;
        Console.WriteLine("Packaged compact button glyph, native padding, retheming and sizing precedence passed.");
    }

    private static void Layout(FrameworkElement host)
    {
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        host.Measure(new Size(420, 120));
        host.Arrange(new Rect(0, 0, 420, 120));
        host.UpdateLayout();
    }

    private static T Find<T>(DependencyObject root) where T : DependencyObject =>
        Descendants(root).OfType<T>().First();

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        yield return root;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            foreach (var child in Descendants(VisualTreeHelper.GetChild(root, index))) yield return child;
    }
}
'@ | Set-Content (Join-Path $consumer 'Program.cs')
    dotnet restore (Join-Path $consumer 'PackageSmoke.csproj') --configfile (Join-Path $consumer 'NuGet.Config') --packages (Join-Path $consumer 'packages') --disable-build-servers
    if ($LASTEXITCODE -ne 0) { throw 'Local package installation failed.' }
    dotnet run --project (Join-Path $consumer 'PackageSmoke.csproj') -c Release --no-restore --disable-build-servers
    if ($LASTEXITCODE -ne 0) { throw 'Package consumer smoke check failed.' }
    Write-Output "Validated packages: $feed"
} finally { Pop-Location }
