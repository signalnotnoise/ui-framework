using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

public sealed class SelectionProbe : Component
{
    public State<int> Selected { get; set; } = null!;
    public int Id { get; set; }
    public bool RenderedSelection;
    public int Builds;
    private Computed<bool>? selected;
    public override View Body()
    {
        selected ??= new(() => Selected.Value == Id);
        RenderedSelection = selected.Value;
        Builds++;
        return Text(RenderedSelection.ToString());
    }
}
