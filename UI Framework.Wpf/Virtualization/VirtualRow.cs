namespace UI_Framework.Wpf;

internal sealed class VirtualRow(View view)
{
    // Intrusive AVL metadata: zero height means detached; collection owns all links.
    internal VirtualRow? Left, Right, Parent;
    internal int Height, Count;
    internal View View = view;
    // Desired position for retained rows; old position for rows being removed.
    internal int TargetIndex;
    private VirtualRowSavedState? savedState;
    internal VirtualRowPresenter? Presenter;
    // Most logical rows have never been realized. Share optional snapshot storage
    // instead of carrying two snapshot references on every row in a large list.
    internal NodeSnapshot? Saved
    {
        get => savedState?.Component;
        set
        {
            if (value is not null) (savedState ??= new()).Component = value;
            else if (savedState is { } state)
            {
                state.Component = null;
                if (state.Focus is null) savedState = null;
            }
        }
    }

    internal RowFocusSnapshot? Focus
    {
        get => savedState?.Focus;
        set
        {
            if (value is not null) (savedState ??= new()).Focus = value;
            else if (savedState is { } state)
            {
                state.Focus = null;
                if (state.Component is null) savedState = null;
            }
        }
    }
}
