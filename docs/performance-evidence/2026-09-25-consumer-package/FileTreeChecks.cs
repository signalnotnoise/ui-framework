using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Lab_Feedback_WPF.Models;

internal static class FileTreeChecks
{
    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }

    internal static async Task Run(TreeView tree, Func<Task> layout, string folder, bool virtualized,
        ICSharpCode.AvalonEdit.TextEditor editor)
    {
        var directory = new FileSystemItem(Path.Combine(folder, "Nested"), true) { IsExpanded = true };
        var nested = new FileSystemItem(Path.Combine(folder, "Nested/Inner"), true) { IsExpanded = true };
        var selected = new FileSystemItem(Path.Combine(folder, "Nested/Inner/Checked.cs"), false);
        var uncheckedFile = new FileSystemItem(Path.Combine(folder, "Nested/Inner/Unchecked.cs"), false);
        Directory.CreateDirectory(Path.GetDirectoryName(selected.FullPath)!);
        const string sourceText = "// Synthetic file for retained-editor validation.\nclass Example {}\n";
        File.WriteAllText(selected.FullPath, sourceText);
        nested.Children.Add(selected); nested.Children.Add(uncheckedFile); directory.Children.Add(nested);
        for (var i = 0; i < 300; i++) nested.Children.Add(new FileSystemItem(Path.Combine(folder, $"Nested/Inner/Extra{i:D3}.cs"), false));
        tree.Items[0] = directory;
        await layout(); await layout();
        TreeViewItem Container(ItemsControl owner, object item) => owner.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem
            ?? throw new InvalidOperationException("Expected nested tree container was not realized.");
        var outerContainer = Container(tree, directory);
        var innerContainer = Container(outerContainer, nested);
        var selectedContainer = Container(innerContainer, selected);
        var nestedRealized = Enumerable.Range(0, nested.Children.Count).Count(i => innerContainer.ItemContainerGenerator.ContainerFromIndex(i) is not null);
        if (virtualized ? nestedRealized >= 200 : nestedRealized != nested.Children.Count)
            throw new InvalidOperationException($"Unexpected nested-file realization: {nestedRealized} of {nested.Children.Count}.");
        var treePeer = UIElementAutomationPeer.CreatePeerForElement(tree) as TreeViewAutomationPeer;
        _ = treePeer?.GetChildren();
        _ = UIElementAutomationPeer.CreatePeerForElement(outerContainer)?.GetChildren();
        _ = UIElementAutomationPeer.CreatePeerForElement(innerContainer)?.GetChildren();
        selectedContainer.IsSelected = true;
        await layout();
        if (editor.Text != sourceText) throw new InvalidOperationException("Selecting the file did not open the expected source.");
        var document = editor.Document;
        editor.Select(3, 7);
        var checkbox = Descendants<CheckBox>(selectedContainer).Single();
        checkbox.SetCurrentValue(ToggleButton.IsCheckedProperty, true);
        if (!selected.IsCheckedForAnalysis || AutomationProperties.GetName(checkbox) != "Include Checked.cs in review")
            throw new InvalidOperationException("Checkbox binding or accessible name failed.");
        if (treePeer?.GetName() != "Submitted files" || treePeer.GetPattern(PatternInterface.Selection) is not ISelectionProvider selection
            || selection.GetSelection().Length != 1)
            throw new InvalidOperationException("Tree automation selection failed.");
        var scroll = Descendants<ScrollViewer>(tree).First();
        scroll.ScrollToEnd(); await layout(); await layout();
        if (!ReferenceEquals(tree.SelectedItem, selected) || !selected.IsCheckedForAnalysis || !directory.IsExpanded || !nested.IsExpanded)
            throw new InvalidOperationException("Offscreen tree state was lost.");
        var last = Container(tree, tree.Items[999]);
        if (Descendants<CheckBox>(last).Single().IsChecked == true)
            throw new InvalidOperationException("Recycled checkbox leaked checked state to a different file.");
        scroll.ScrollToTop(); await layout(); await layout();
        if (!ReferenceEquals(document, editor.Document) || editor.Text != sourceText || editor.SelectionStart != 3 || editor.SelectionLength != 7)
            throw new InvalidOperationException("Recycling changed the open editor document or selection.");
        outerContainer = Container(tree, directory); innerContainer = Container(outerContainer, nested);
        selectedContainer = Container(innerContainer, selected);
        if (!selectedContainer.IsSelected || Descendants<CheckBox>(selectedContainer).Single().IsChecked != true
            || Descendants<CheckBox>(Container(innerContainer, uncheckedFile)).Single().IsChecked == true)
            throw new InvalidOperationException($"Restored tree state: selected={selectedContainer.IsSelected}, modelSelected={selected.IsSelected}, checked={Descendants<CheckBox>(selectedContainer).Single().IsChecked}, otherChecked={Descendants<CheckBox>(Container(innerContainer, uncheckedFile)).Single().IsChecked}.");
        var expansion = UIElementAutomationPeer.CreatePeerForElement(outerContainer)?.GetPattern(PatternInterface.ExpandCollapse) as IExpandCollapseProvider
            ?? throw new InvalidOperationException("Missing tree expansion automation pattern.");
        expansion.Collapse(); await layout();
        if (directory.IsExpanded) throw new InvalidOperationException("Collapse did not update expansion binding.");
        expansion.Expand(); await layout(); await layout();
        if (!directory.IsExpanded || !nested.IsExpanded || !selected.IsCheckedForAnalysis)
            throw new InvalidOperationException("Re-expansion lost model state.");
        var realized = Enumerable.Range(0, tree.Items.Count).Count(i => tree.ItemContainerGenerator.ContainerFromIndex(i) is not null);
        if (virtualized ? realized <= 0 || realized >= 200 : realized != 1000)
            throw new InvalidOperationException($"Unexpected file-tree realization: {realized}.");
    }
}
