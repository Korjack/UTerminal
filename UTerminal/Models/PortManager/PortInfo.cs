using ReactiveUI;

namespace UTerminal.Models;

public class PortInfo : ReactiveObject
{
    private string _name;
    private bool _isEnabled;
    private bool _isSelected;

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    public PortInfo(string name, bool isEnabled = true)
    {
        _name = name;
        _isEnabled = isEnabled;
        _isSelected = false;
    }
}