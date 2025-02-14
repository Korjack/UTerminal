using System.Windows.Input;
using ReactiveUI;

namespace UTerminal.Models.Utils;

public class MacroItems : ReactiveObject
{
    private string? _macroText = string.Empty;
    public string? MacroText
    {
        get => _macroText;
        set => this.RaiseAndSetIfChanged(ref _macroText, value);
    }

    public string Label { get; set; } = string.Empty;
}