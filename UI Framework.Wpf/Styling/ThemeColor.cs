using System.ComponentModel;
using System.Windows.Media;

namespace UI_Framework.Wpf;

internal sealed class ThemeColor(Color value) : INotifyPropertyChanged
{
    private Color value = value;
    public Color Value
    {
        get => value;
        set
        {
            if (this.value == value) return;
            this.value = value;
            PropertyChanged?.Invoke(this, new(nameof(Value)));
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
}
