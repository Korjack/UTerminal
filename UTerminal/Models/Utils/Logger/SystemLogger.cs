using System;
using System.IO;
using System.Runtime.CompilerServices;
using log4net;
using log4net.Core;

namespace UTerminal.Models.Utils.Logger;

public class SystemLogger
{
    private static readonly Lazy<SystemLogger> _instance = new(() => new SystemLogger());
    public static SystemLogger Instance => _instance.Value;
    
    private readonly ILog _log;

    public static string LogName => "SystemLog";
    // Avalonia Application이 없는 환경(테스트 등)에서는 App.axaml의 Name과 같은 값을 쓴다
    private static string AppName => App.Current?.Name ?? "UTerminal";
    public string SystemLogPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName, AppName + "-system.log");

    private SystemLogger()
    {
        var config = new LogConfig
        {
            FilePath = SystemLogPath,
            FilePattern = "'.'yyyy-MM-dd",
            Layout = "%date [%thread] %-5level %logger - %message%newline",
            LogLevel = Level.Info
        };

        _log = LogManager.GetLogger(LogName);
        LoggerConfiguration.Configure(config, LogName);
    }

    public void LogSerialConnection(string portName, bool isConnected)
    {
        var status = isConnected ? "Connected" : "Disconnected";
        _log.Info($"Serial port [{portName}] {status}");
    }

    public void LogConfigurationChange(string setting, string oldValue, string newValue)
    {
        _log.Info($"Configuration changed: {setting} from '{oldValue}' to '{newValue}'");
    }

    public void LogSystemError(Exception ex, [CallerMemberName]string operation = "")
    {
        _log.Error($"Error during {operation}: {ex.Message}");
    }

    public void LogInfo(string log, [CallerMemberName]string methodName = "") => _log.Info($"[{methodName}]{log}");
    public void LogError(string log) => _log.Error(log);
    public void LogWarning(string log) => _log.Warn(log);
}