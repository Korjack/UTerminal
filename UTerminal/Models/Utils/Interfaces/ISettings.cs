namespace UTerminal.Models.Utils.Interfaces;

public interface ISettingsManager<T> where T : class
{
    public string FileName { get; set; }
    public string SettingPath { get; set; }

    /// <summary>
    /// Load Settings
    /// </summary>
    /// <returns>T</returns>
    T Load();

    /// <summary>
    /// Save Settings
    /// </summary>
    /// <param name="config">T</param>
    void Save(T config);
}