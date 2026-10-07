using System.Text.Json;
using UTerminal.Models.Parser;

namespace UTerminal.Tests.Parser;

public class PresetJsonTests
{
    /// <summary>
    /// PresetModeViewModel.SavePreset / LoadPreset과 같은 방식으로 직렬화·역직렬화한다.
    /// </summary>
    [Fact(Skip = "D1 수정 전")]
    public void SaveThenLoad_RestoresFields()
    {
        var preset = new ParsePreset { Name = "p" };
        preset.ParseDataList.Add(new ParseData(preset, ParseDataType.STX, "stx"));
        preset.ParseDataList.Add(new ParseData(preset, ParseDataType.UInt16, "value"));

        var json = JsonSerializer.Serialize(preset);
        var loaded = JsonSerializer.Deserialize<ParsePreset>(json);

        Assert.NotNull(loaded);
        Assert.Equal(new[] { ParseDataType.STX, ParseDataType.UInt16 }, loaded!.ParseDataList.Select(x => x.ParseDataType));
    }
}
