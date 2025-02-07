using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;

namespace UTerminal.Models;

public class ULogManager
{
    private readonly ILog _log;
    private RollingFileAppender? _fileAppender;

    /// <summary>
    /// Create a log manager with a log type.
    /// </summary>
    /// <param name="logType">[<see cref="string"/>] Log type</param>
    public ULogManager(string logType)
    {
        _log = LogManager.GetLogger(logType);
    }

    /// <summary>
    /// Create a log manager with a log type.
    /// </summary>
    /// <param name="logType">[<see cref="string"/>] Log type</param>
    /// <param name="newConfig">[<see cref="LogConfig"/>] Customised log settings.</param>
    public ULogManager(string logType, LogConfig newConfig)
    {
        _log = LogManager.GetLogger(logType);

        // if logger appenders exits, remove all
        if (_log.Logger is Logger { Appenders.Count: > 0 } logger)
        {
            logger.RemoveAllAppenders();
        }

        ConfigureLogging(newConfig);
    }

    /// <summary>
    /// Set up logs from settings.
    /// </summary>
    /// <param name="config">[<see cref="LogConfig"/>] If you have customised settings</param>
    private void ConfigureLogging(LogConfig config)
    {
        Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository();

        _fileAppender = new RollingFileAppender
        {
            Name = $"RollingFileAppender-{_log.Logger.Name}",
            File = config.FilePath,
            AppendToFile = true,
            RollingStyle = RollingFileAppender.RollingMode.Date,
            DatePattern = config.FilePattern,
            StaticLogFileName = true,
            MaxSizeRollBackups = config.MaxSizeRollBackups,
            MaximumFileSize = config.MaximumFileSize,
            Layout = new PatternLayout(config.Layout)
        };

        ((PatternLayout)_fileAppender.Layout).ActivateOptions();
        _fileAppender.ActivateOptions();

        if (hierarchy.GetLogger(_log.Logger.Name) is Logger logger)
        {
            logger.AddAppender(_fileAppender);
            logger.Level = config.LogLevel;
            logger.Additivity = false;

            hierarchy.Configured = true;
        }
        else
        {
            hierarchy.Configured = false;
        }
    }

    /// <summary>
    /// Change the location of the logfile to be saved.
    /// </summary>
    /// <param name="newLogFilePath">[<see cref="string"/>]Folder path</param>
    public void ChangeLogFilePath(string newLogFilePath)
    {
        if (_fileAppender == null) return;

        _fileAppender.File = newLogFilePath;
        _fileAppender.ActivateOptions();
    }

    public void Debug(string message) => _log.Debug(message);
    public void Info(string message) => _log.Info(message);
    public void Warn(string message) => _log.Warn(message);
    public void Error(string message) => _log.Error(message);
    public void Fatal(string message) => _log.Fatal(message);
}

public class LogConfig
{
    public string FilePath { get; set; } = "./";
    public string FilePattern { get; set; } = "'.'yyyy-MM-dd";
    public string Layout { get; set; } = "%date [%thread] %-5level %logger - %message%newline";
    public int MaxSizeRollBackups { get; set; } = 14;
    public string MaximumFileSize { get; set; } = "10MB";
    public Level LogLevel { get; set; } = Level.Debug;
}