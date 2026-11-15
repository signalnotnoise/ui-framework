using System.Windows.Controls;
using UI_Framework;

namespace PackageWorkspace;

public sealed class WorkspaceModel
{
    public State<string> Title { get; } = new("Autumn release");
    public State<string> Notes { get; } = new("Native notes stay in the application model.");
    public State<int> Selected { get; } = new(0);
    public State<double> Pane { get; } = new(280);
    public NavigationStack<string> History { get; } = new("workspace");
    public bool Virtualized { get; init; } = true;
    public int Builds, Mounts, Unmounts, Creates, Updates, Releases;
    public List<WeakReference> NativeReferences { get; } = [];

    public TextBox CreateEditor()
    {
        Creates++;
        var editor = new TextBox { AcceptsReturn = true, TextWrapping = System.Windows.TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Text = Notes.Value, MinHeight = 120 };
        editor.TextChanged += EditNotes;
        NativeReferences.Add(new WeakReference(editor));
        return editor;
    }

    public void UpdateEditor(TextBox editor)
    {
        Updates++;
        if (editor.Text != Notes.Value) editor.Text = Notes.Value;
    }

    public void ReleaseEditor(TextBox editor)
    {
        if (editor.Parent is not null) throw new InvalidOperationException("Native release must follow detachment.");
        editor.TextChanged -= EditNotes;
        Releases++;
    }

    private void EditNotes(object sender, TextChangedEventArgs args) => Notes.Value = ((TextBox)sender).Text;
}
