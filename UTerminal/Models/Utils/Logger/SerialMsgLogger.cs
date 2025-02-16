using System;
using System.IO;
using log4net;

namespace UTerminal.Models.Utils.Logger;

public class SerialMsgLogger
{
    private static readonly Lazy<SerialMsgLogger> _instance = new(() => new SerialMsgLogger());
    public static SerialMsgLogger Instance => _instance.Value;
    
    private readonly ILog _log;
    private readonly SystemLogger _systemLogger = SystemLogger.Instance;

    public static string LogName => "SerialMessage";
    public string SerialLogFilePath { get; set; } = AppContext.BaseDirectory;
    public bool IsStartLogging { get; set; }

    /// <summary>
    /// Create a log manager with a log type.
    /// </summary>
    public SerialMsgLogger()
    {
        _log = LogManager.GetLogger(LogName);

        // if logger appenders exits, remove all
        if (_log.Logger is log4net.Repository.Hierarchy.Logger { Appenders.Count: > 0 } logger)
        {
            logger.RemoveAllAppenders();
        }
        
        _systemLogger.LogInfo("Serial Message Logger Initialized");
    }

    public void CreateLogFile()
    {
        var config = new LogConfig
        {
            FilePath = Path.Combine(SerialLogFilePath, $"DataReceived-{DateTime.Now:yyyyMMdd HHmmss}.log"),
            Layout = "[%date] %logger => %message%newline"
        };
        LoggerConfiguration.Configure(config, LogName);
    }
    
    public void Info(string message) => _log.Info(message);
}