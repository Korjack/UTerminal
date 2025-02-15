using log4net;
using log4net.Appender;
using log4net.Layout;
using log4net.Repository.Hierarchy;

namespace UTerminal.Models.Utils;

public static class LoggerConfiguration
{
    public static void Configure(LogConfig config, string logName)
    {
        Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository();

        var fileAppender = new RollingFileAppender
        {
            Name = $"FileAppender-{logName}",
            File = config.FilePath,
            AppendToFile = true,
            RollingStyle = RollingFileAppender.RollingMode.Date,
            DatePattern = config.FilePattern,
            StaticLogFileName = true,
            MaxSizeRollBackups = config.MaxSizeRollBackups,
            MaximumFileSize = config.MaximumFileSize,
            Layout = new PatternLayout(config.Layout)
        };

        ((PatternLayout)fileAppender.Layout).ActivateOptions();
        fileAppender.ActivateOptions();

        if (hierarchy.GetLogger(logName) is Logger logger)
        {
            logger.AddAppender(fileAppender);
            logger.Level = config.LogLevel;
            logger.Additivity = false;

            hierarchy.Configured = true;
        }
        else
        {
            hierarchy.Configured = false;
        }
    }
}