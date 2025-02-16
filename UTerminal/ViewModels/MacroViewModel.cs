using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using ReactiveUI;
using UTerminal.Models.Utils;
using UTerminal.Models.Utils.Logger;

namespace UTerminal.ViewModels;


public class MacroViewModel : ViewModelBase
{
    private readonly MacroSettings _settings;
    private readonly SystemLogger _systemLogger = SystemLogger.Instance;
    
    public ObservableCollection<MacroItems> MacroSendItems { get; }
    
    public ICommand SendCommand { get; }
    public ICommand MacroClosingCommand { get; }
    

    /// <summary>
    /// Initialize Window
    /// </summary>
    /// <param name="sendCommand">Connects a function with a predefined transmission.</param>
    public MacroViewModel(ICommand sendCommand)
    {
        // Commands Init
        SendCommand = sendCommand;
        MacroClosingCommand = ReactiveCommand.Create(WindowClosing);
        
        _settings = new MacroSettings(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Application.Current?.Name!), "macro_settings.json");
        MacroSendItems = new ObservableCollection<MacroItems>(_settings.Load());

        int count = 10 - MacroSendItems.Count;
        for (int i = 0; i < count; i++)
        {
            MacroSendItems.Add(new MacroItems
            {
                Label = $"Macro {i + 1}",
                MacroText = string.Empty
            });
        }
        
        _systemLogger.LogInfo("Macro Window Opened");
    }

    
    /// <summary>
    /// When window closing
    /// </summary>
    private void WindowClosing()
    {
        SaveSettings();
    }
    

    /// <summary>
    /// Save Setting
    /// </summary>
    private void SaveSettings()
    {
        _settings.Save(MacroSendItems.ToArray());
    }

}