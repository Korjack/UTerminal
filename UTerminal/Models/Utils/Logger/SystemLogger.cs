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

    private SystemLogger()
    {
        var config = new LogConfig
        {
            FilePath = Path.Combine(AppContext.BaseDirectory, App.Current.Name + "-system.log"),
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