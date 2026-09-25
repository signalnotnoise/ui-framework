using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;

internal sealed class SubmissionListScenario
{
    internal ListBox Control { get; }
    internal SubmissionListScenario(bool virtualized)
    {
        var row = new FrameworkElementFactory(typeof(TextBlock));
        row.SetBinding(TextBlock.TextProperty, new Binding());
        row.SetValue(FrameworkElement.HeightProperty, 24.0);
        var container = new Style(typeof(ListBoxItem));
        container.Setters.Add(new Setter(System.Windows.Controls.Control.PaddingProperty, new Thickness(0)));
        container.Setters.Add(new Setter(System.Windows.Controls.Control.BorderThicknessProperty, new Thickness(0)));
        Control = new ListBox
        {
            ItemsSource = Enumerable.Range(0, 1000).Select(i => $"Submission {i}").ToArray(),
            ItemTemplate = new DataTemplate { VisualTree = row }, ItemContainerStyle = container,
            SelectedIndex = 7, BorderThickness = new Thickness(0), Padding = new Thickness(0)
        };
        AutomationProperties.SetName(Control, "Submissions");
        ScrollViewer.SetHorizontalScrollBarVisibility(Control, ScrollBarVisibility.Disabled);
        ScrollViewer.SetCanContentScroll(Control, true);
        VirtualizingPanel.SetIsVirtualizing(Control, virtualized);
        VirtualizingPanel.SetVirtualizationMode(Control, VirtualizationMode.Recycling);
        VirtualizingPanel.SetCacheLength(Control, new VirtualizationCacheLength(1));
    }

    internal int RealizedCount => Enumerable.Range(0, 1000).Count(i => Control.ItemContainerGenerator.ContainerFromIndex(i) is not null);

    internal void CheckRetainedSelection()
    {
        if (Control.Items.Count != 1000 || Control.SelectedIndex != 7 || !Equals(Control.SelectedItem, "Submission 7"))
            throw new InvalidOperationException("Submission data or retained selection changed.");
    }

    internal async Task CheckScrollingAndAccessibility(Func<Task> layout, bool virtualized)
    {
        var peer = UIElementAutomationPeer.CreatePeerForElement(Control) as ListBoxAutomationPeer
            ?? throw new InvalidOperationException("Missing list automation peer.");
        if (peer.GetName() != "Submissions" || peer.GetPattern(PatternInterface.Selection) is not ISelectionProvider selection)
            throw new InvalidOperationException("Missing accessible list name or selection pattern.");
        foreach (var index in new[] { 500, 999, 0, 7 })
        {
            Control.SelectedIndex = index;
            Control.ScrollIntoView(Control.Items[index]);
            await layout();
            if (Control.ItemContainerGenerator.ContainerFromIndex(index) is not ListBoxItem { IsSelected: true } item
                || !Equals(item.Content, $"Submission {index}") || selection.GetSelection().Length != 1)
                throw new InvalidOperationException($"Scrolling/selection failed for submission {index}.");
            var origin = item.TranslatePoint(new Point(), Control);
            if (origin.Y < -1 || origin.Y + item.ActualHeight > Control.ActualHeight + 1)
                throw new InvalidOperationException($"Submission {index} was not brought into the viewport.");
            var realized = RealizedCount;
            if (virtualized ? realized <= 0 || realized >= 200 : realized != 1000)
                throw new InvalidOperationException($"Unexpected realized row count: {realized}.");
        }
        CheckRetainedSelection();
    }
}
