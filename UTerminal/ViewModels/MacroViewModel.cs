using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using ReactiveUI;

namespace UTerminal.ViewModels;


/// <summary>
/// Management Macro View Items
/// </summary>
/// <remarks>
/// <list type="bullet">
///     <item>MacroText(<see cref="string"/>): Data for fast transmission of serial data</item>
///     <item>Label(<see cref="string"/>): Text displayed in the view</item>
///     <item>SendCommand(<see cref="ICommand"/>): Action to be performed when Enter or Send button is clicked</item>
/// </list>
/// </remarks>
public class MacroSendItem : ViewModelBase
{
    private string? _macroText = String.Empty;
    
    public string? Label { get; set; }
    public ICommand? SendCommand { get; set; }

    public string? MacroText
    {
        get => _macroText;
        set => this.RaiseAndSetIfChanged(ref _macroText, value);
    }
}

/// <summary>
/// Load and Save Macro Settings Info
/// </summary>
public class MacroSettings
{
    public List<MacroSendItem> MacroItems { get; set; } = [];
    
    /// <summary>
    /// Setting file path.
    /// </summary>
    /// <remarks>
    /// See OS Path
    /// <list type="bullet">
    ///     <item>MacOS : /User/{Username}/Library/Application Support/{App Name}/{File Name}.json</item>
    /// </list>
    /// </remarks>
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        Application.Current?.Name!,
        "macro_settings.json"
    );

    /// <summary>
    /// Load macro text from setting file
    /// </summary>
    /// <param name="command"><see cref="ICommand"/> Function to be connected</param>
    /// <returns><see cref="MacroSettings"/> setting info</returns>
    public static MacroSettings Load(ICommand command)
    {
        if (!File.Exists(SettingsPath))
        {
            return new MacroSettings();
        }

        var json = File.ReadAllText(SettingsPath);
        var macroTexts = JsonSerializer.Deserialize<List<string>>(json);

        if (macroTexts == null) return new MacroSettings();
        
        return new MacroSettings
        {
            MacroItems = macroTexts.Select((text, index) => new MacroSendItem
            {
                Label = $"Macro {++index}",
                MacroText = text,
                SendCommand = command
            }).ToList()
        };
    }

    
    /// <summary>
    /// Save a macro text to setting file
    /// </summary>
    public void Save()
    {
        var directory = Path.GetDirectoryName(SettingsPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory!);
        }

        var macroTexts = MacroItems.Select(item => item.MacroText).ToList();
        var json = JsonSerializer.Serialize(macroTexts);
        File.WriteAllText(SettingsPath, json);
    }
}


public class MacroViewModel : ViewModelBase
{
    #region Fields

    private readonly MainViewModel.SendSerialDataDelegate _sendSerialData;
    private readonly MacroSettings _settings;

    #endregion

    #region Property

    public ObservableCollection<MacroSendItem> MacroSendItems { get; }
    public ICommand MacroClosingCommand { get; }
    
    #endregion

    #region Initialize & Dipose

    /// <summary>
    /// Initialize Window
    /// </summary>
    /// <param name="serialDataDelegate">(<see cref="MainViewModel.SendSerialDataDelegate"/>) Connects a function with a predefined transmission.</param>
    public MacroViewModel(MainViewModel.SendSerialDataDelegate serialDataDelegate)
    {
        _sendSerialData = serialDataDelegate;           // Set serial write function
        
        // Commands Init
        var command = ReactiveCommand.CreateFromTask<string>(SendMacroDataAsync);
        MacroClosingCommand = ReactiveCommand.Create(WindowClosing);
        
        _settings = MacroSettings.Load(command);
        MacroSendItems = new ObservableCollection<MacroSendItem>(_settings.MacroItems);
        
        // If setting file load fail or empty
        if (MacroSendItems.Count == 0)
        {
            for (var i = 0; i < 10; i++)
            {
                MacroSendItems.Add(new MacroSendItem
                {
                    Label = $"Macro {i + 1}",
                    MacroText = string.Empty,
                    SendCommand = command
                });
            }
        }
    }

    
    /// <summary>
    /// When window closing
    /// </summary>
    private void WindowClosing()
    {
        SaveSettings();
    }
    
    #endregion

    #region Serial Send Delegate

    /// <summary>
    /// Calls a predefined function.
    /// </summary>
    /// <param name="data">Macro TextBox <see cref="string"/></param>
    private async Task SendMacroDataAsync(string data)
    {
        await _sendSerialData(data);
    }
    
    #endregion

    #region Private Method

    /// <summary>
    /// Save Setting
    /// </summary>
    private void SaveSettings()
    {
        _settings.MacroItems = MacroSendItems.ToList();
        _settings.Save();
    }

    #endregion
}