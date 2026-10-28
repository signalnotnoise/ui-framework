using System.Windows;
using System.Windows.Input;

internal sealed class TrackedTextComposition(IInputElement source) : TextComposition(
    InputManager.Current, source, "", TextCompositionAutoComplete.Off)
{
    internal int Completions { get; private set; }
    internal Action? BeforeComplete { get; set; }

    internal void Update(string text)
    {
        Text = text;
        CompositionText = text;
        TextCompositionManager.UpdateComposition(this);
    }

    public override void Complete()
    {
        Completions++;
        BeforeComplete?.Invoke();
        base.Complete();
    }
}
