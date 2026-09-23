using UI_Framework;

internal sealed class NotificationProbe : ObservableState
{
    internal void Notify() => NotifyChanged();
}
