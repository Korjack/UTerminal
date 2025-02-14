using ReactiveUI;

namespace UTerminal.Models.PortManager;

public class PortInfo(string name, bool isEnabled = true) : ReactiveObject
{
    private string _name = name;
    private bool _isEnabled = isEnabled;
    private bool _isSelected = false;

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    /// <summary>
    /// Check if the port is connectable
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
    }

    /// <summary>
    /// Check if the port is selected
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }
}