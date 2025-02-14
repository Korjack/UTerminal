using System.Text.Json;
using UTerminal.Models.Utils;

namespace UTerminal.Tests;

[TestFixture]
public class MacroSettingsTests
{
    private string _testDirectory;
    private string _testFileName;
    private MacroSettings _macroSettings;

    [SetUp]
    public void Setup()
    {
        // 테스트를 위한 임시 디렉토리 생성
        _testDirectory = Path.Combine(Path.GetTempPath(), "MacroSettingsTests");
        _testFileName = "test_macro_settings.json";
        _macroSettings = new MacroSettings(_testDirectory, _testFileName);

        // 테스트 시작 전 디렉토리가 존재한다면 삭제
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [TearDown]
    public void Cleanup()
    {
        // 테스트 후 임시 디렉토리 삭제
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Test]
    public void Load_WhenFileDoesNotExist_ReturnsNewMacroItems()
    {
        // Act
        var result = _macroSettings.Load();

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Save_WithValidMacroItems_SavesCorrectly()
    {
        // Arrange
        MacroItems[] macroItems = [new MacroItems { MacroText = "Test1" }, new MacroItems { MacroText = "Test2" }, new MacroItems { MacroText = "Test1" }];

        // Act
        _macroSettings.Save(macroItems);

        // Assert
        var filePath = Path.Combine(_testDirectory, _testFileName);
        Assert.That(File.Exists(filePath), Is.True);

        var savedJson = File.ReadAllText(filePath);
        var deserializedItems = JsonSerializer.Deserialize<MacroItems[]>(savedJson);

        Assert.That(deserializedItems, Is.Not.Null);
    }
    
}