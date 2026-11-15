using System.Windows;
using System.Windows.Controls;

namespace FocusComparison;

internal sealed record ContentLease(FrameworkElement Root, TextBox[] Editors, Action Release);
