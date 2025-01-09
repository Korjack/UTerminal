using Avalonia.Controls;
using Avalonia.Input;

namespace UTerminal.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Hex Input Monitor
    /// </summary>
    private void CustomStxEtx_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Back or Key.Delete or Key.Left or Key.Right)
        {
            return;
        }

        var c = (char)e.Key;
        if (!(c is >= '0' and <= '9' || 
              c is >= 'A' and <= 'F' || 
              c is >= 'a' and <= 'f'))
        {
            e.Handled = true;
        }
    }
}