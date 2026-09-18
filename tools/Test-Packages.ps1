param([switch]$NoBuild)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location $projectRoot
try {
    $feed = Join-Path $projectRoot 'artifacts/packages'
    foreach ($project in @('UI Framework/UI Framework.csproj', 'UI Framework.Wpf/UI Framework.Wpf.csproj')) {
        $arguments = @('pack', $project, '-c', 'Release', '-o', $feed, '--disable-build-servers', '-warnaserror')
        if ($NoBuild) { $arguments += @('--no-build', '--no-restore') }
        & dotnet @arguments
        if ($LASTEXITCODE -ne 0) { throw "Packing failed: $project" }
    }
    [xml]$properties = Get-Content 'Directory.Build.props'
    $version = $properties.Project.PropertyGroup.Version
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
