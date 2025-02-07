using log4net.Core;
using UTerminal.Models;

namespace UTerminal.Tests
{
    [TestFixture]
    public class ULogManagerTests
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _testLogDirectory = Path.Combine(Path.GetTempPath(), "ULogManagerTests");
            Directory.CreateDirectory(_testLogDirectory);
        }

        [SetUp]
        public void Setup()
        {
            // 각 테스트 전에 기본 설정으로 초기화
            _defaultConfig = new LogConfig
            {
                FilePath = Path.Combine(_testLogDirectory, "test.log"),
                FilePattern = "'.'yyyy-MM-dd",
                Layout = "%date [%thread] %-5level %logger - %message%newline",
                MaxSizeRollBackups = 14,
                MaximumFileSize = "10MB",
                LogLevel = Level.Debug
            };

            _logManager = new ULogManager("TestLogger", _defaultConfig);
        }

        [TearDown]
        public void TearDown()
        {
            // 각 테스트 후 로그 파일 정리
            foreach (var file in Directory.GetFiles(_testLogDirectory, "*.log*"))
            {
                try
                {
                    File.Delete(file);
                }
                catch (IOException)
                {
                    // 파일이 사용 중일 수 있으므로 무시
                }
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            try
            {
                Directory.Delete(_testLogDirectory, true);
            }
            catch (IOException)
            {
                // 디렉토리 삭제 실패 시 무시
            }
        }

        private string _testLogDirectory;
        private LogConfig _defaultConfig;
        private ULogManager _logManager;

        [Test]
        public void Constructor_WithValidConfig_CreatesLogManager()
        {
            // Arrange & Act는 Setup에서 수행됨

            // Assert
            Assert.That(_logManager, Is.Not.Null);
            Assert.That(File.Exists(_defaultConfig.FilePath), Is.True);
        }

        [Test]
        public void Debug_WritesDebugMessage_MessageIsWrittenToFile()
        {
            // Arrange
            string testMessage = "Debug Test Message";

            // Act
            _logManager.Debug(testMessage);
            Thread.Sleep(100); // 파일 쓰기 완료 대기

            // Assert
            using var stream = File.Open(_defaultConfig.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            string logContent = reader.ReadToEnd();
            Assert.That(logContent, Does.Contain(testMessage));
            Assert.That(logContent, Does.Contain("DEBUG"));
        }

        [Test]
        public void Error_WritesErrorMessage_MessageIsWrittenToFile()
        {
            // Arrange
            string testMessage = "Error Test Message";

            // Act
            _logManager.Error(testMessage);
            Thread.Sleep(100); // 파일 쓰기 완료 대기

            // Assert
            using var stream = File.Open(_defaultConfig.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            string logContent = reader.ReadToEnd();
            Assert.That(logContent, Does.Contain(testMessage));
            Assert.That(logContent, Does.Contain("ERROR"));
        }

        [Test]
        public void ChangeLogFilePath_ToNewValidPath_ChangesLogFileLocation()
        {
            // Arrange
            string newLogPath = Path.Combine(_testLogDirectory, "test.log");
            string testMessage = "New Log File Test Message";

            // Act
            _logManager.ChangeLogFilePath(newLogPath);
            _logManager.Info(testMessage);
            Thread.Sleep(100); // 파일 쓰기 완료 대기

            // Assert
            Assert.That(File.Exists(newLogPath), Is.True);
            using var stream = File.Open(newLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            string logContent = reader.ReadToEnd();
            Assert.That(logContent, Does.Contain(testMessage));
        }

        [Test]
        public void Constructor_WithCustomLogLevel_RespectsLogLevel()
        {
            // Arrange
            _defaultConfig.LogLevel = Level.Info;
            _logManager = new ULogManager("TestLogger", _defaultConfig);
            string debugMessage = "Debug Should Not Appear";
            string infoMessage = "Info Should Appear";

            // Act
            _logManager.Debug(debugMessage);
            _logManager.Info(infoMessage);
            Thread.Sleep(100); // 파일 쓰기 완료 대기

            // Assert
            using var stream = File.Open(_defaultConfig.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            string logContent = reader.ReadToEnd();
            Assert.That(logContent, Does.Not.Contain(debugMessage));
            Assert.That(logContent, Does.Contain(infoMessage));
        }

        [Test]
        public void LogManager_WithCustomLayout_UsesCustomLayout()
        {
            // Arrange
            _defaultConfig.Layout = "%level - %message%newline";
            _logManager = new ULogManager("TestLogger", _defaultConfig);
            string testMessage = "Custom Layout Test";

            // Act
            _logManager.Info(testMessage);
            Thread.Sleep(100); // 파일 쓰기 완료 대기

            // Assert
            using var stream = File.Open(_defaultConfig.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            string logContent = reader.ReadToEnd();
            Assert.That(logContent, Does.Contain($"INFO - {testMessage}"));
            Assert.That(logContent, Does.Not.Contain("[%thread]")); // 기본 레이아웃의 스레드 정보가 없어야 함
        }
    }
}