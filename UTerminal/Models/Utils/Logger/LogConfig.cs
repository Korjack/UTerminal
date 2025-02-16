using log4net.Core;

namespace UTerminal.Models.Utils.Logger;

public class LogConfig
{
    public string FilePath { get; set; } = "./";
    public string FilePattern { get; set; } = "'.'yyyy-MM-dd";
    public string Layout { get; set; } = "%date [%thread] %-5level %logger - %message%newline";
    public int MaxSizeRollBackups { get; set; } = 14;
    public string MaximumFileSize { get; set; } = "10MB";
    public Level LogLevel { get; set; } = Level.Debug;
}