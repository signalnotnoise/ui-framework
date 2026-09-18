namespace UI_Framework.Wpf;

internal sealed class VirtualRow(View view)
{
    internal View View = view;
    internal NodeSnapshot? Saved;
    internal VirtualRowPresenter? Presenter;
    internal bool RestoreFocus;
}
