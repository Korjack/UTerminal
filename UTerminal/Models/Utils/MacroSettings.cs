using System.IO;
using System.Text.Json;
using UTerminal.Models.Utils.Interfaces;
using UTerminal.Models.Utils.Logger;

namespace UTerminal.Models.Utils;

public class MacroSettings(string settingPath, string fileName) : ISettingsManager<MacroItems[]>
{
    public string FileName { get; set; } = fileName;
    public string SettingPath { get; set; } = settingPath;
    
    private SystemLogger _systemLogger = SystemLogger.Instance;

    /// <summary>
    /// Load macro settings from file
    /// </summary>
    /// <returns><see cref="MacroItems"/>[] - Array of items</returns>
    public MacroItems[] Load()
    {
        if(string.IsNullOrEmpty(SettingPath) || string.IsNullOrEmpty(FileName)) return [];
        
        var filePath = Path.Combine(SettingPath, FileName);
        
        if (!File.Exists(filePath))
        {
            return [];
        }
        
        string jsonContent = File.ReadAllText(filePath);
        
        _systemLogger.LogInfo($"Macro List Loaded from {filePath}");
        
        return JsonSerializer.Deserialize<MacroItems[]>(jsonContent) ?? [];
    }

    /// <summary>
    /// Save macro settings on file
    /// </summary>
    /// <param name="config"><see cref="MacroItems"/>[] - Array of items</param>
    public void Save(MacroItems[] config)
    {
        if(string.IsNullOrEmpty(SettingPath) || string.IsNullOrEmpty(FileName)) return;

        var filePath = Path.Combine(SettingPath, FileName);
        var directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory!);
        }

        var jsonOption = new JsonSerializerOptions();
        jsonOption.WriteIndented = true;
        
        string jsonContent = JsonSerializer.Serialize(config, jsonOption);
        File.WriteAllText(filePath, jsonContent);
        
        _systemLogger.LogInfo($"Macro List Saved at {filePath}");
    }
}