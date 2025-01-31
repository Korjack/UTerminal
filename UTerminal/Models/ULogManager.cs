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

    public ULogManager(string logType)
    {
        _log = LogManager.GetLogger(logType);
    }

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

    public void ChangeLogFilePath(string newLogFilePath)
    {
        if (_fileAppender == null) return;

        _fileAppender.File = newLogFilePath;
        _fileAppender.ActivateOptions();
    }

    // 로그 메서드들
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