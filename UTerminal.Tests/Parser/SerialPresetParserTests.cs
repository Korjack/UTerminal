using UTerminal.Models.Messages;
using UTerminal.Models.Parser;

namespace UTerminal.Tests.Parser;

public class SerialPresetParserTests
{
    private static SerialMessage Message(params byte[] data) => new() { Data = data, DataSize = data.Length };

    /// <summary>
    /// 길이 필드 + 그 값만큼의 가변 필드로 이뤄진 프리셋을 만든다.
    /// PresetModeViewModel.AddField가 가변 필드를 만드는 방식과 같다.
    /// </summary>
    private static (ParsePreset preset, ParseData variable) VariablePreset(ParseDataType lengthType)
    {
        var preset = new ParsePreset();
        var length = new ParseData(preset, lengthType, "len");
        var variable = new ParseData(preset, ParseDataType.Byte, "payload", length: 0, linkData: length);
        preset.ParseDataList.Add(length);
        preset.ParseDataList.Add(variable);
        return (preset, variable);
    }

    [Theory(Skip = "A5 수정 전")]
    [InlineData(ParseDataType.UInt8, new byte[] { 0x02, 0xAA, 0xBB })]
    [InlineData(ParseDataType.UInt16, new byte[] { 0x02, 0x00, 0xAA, 0xBB })]
    [InlineData(ParseDataType.UInt32, new byte[] { 0x02, 0x00, 0x00, 0x00, 0xAA, 0xBB })]
    public void ParseMessage_VariableField_ReadsLengthFromEveryOfferedLengthType(ParseDataType lengthType, byte[] data)
    {
        // Arrange
        var (preset, variable) = VariablePreset(lengthType);
        var parser = new SerialPresetParser();
        parser.ParsePresetList.Add(preset);

        // Act
        var success = parser.ParseMessage(Message(data));

        // Assert
        Assert.True(success);
        Assert.Equal("AA BB", variable.ParsedValue);
    }

    [Fact]
    public void ParseMessage_VariableLengthBeyondData_Fails()
    {
        var (preset, _) = VariablePreset(ParseDataType.UInt8);
        var parser = new SerialPresetParser();
        parser.ParsePresetList.Add(preset);

        var success = parser.ParseMessage(Message(0x05, 0xAA));

        Assert.False(success);
    }

    [Fact(Skip = "A6 수정 전")]
    public void GetTotalLength_ReflectsFieldsAddedAfterFirstCall()
    {
        var preset = new ParsePreset();
        preset.ParseDataList.Add(new ParseData(preset, ParseDataType.UInt8));
        Assert.Equal(1, preset.GetTotalLength());

        preset.ParseDataList.Add(new ParseData(preset, ParseDataType.UInt16));

        Assert.Equal(3, preset.GetTotalLength());
    }

    [Fact(Skip = "A6 수정 전")]
    public void GetTotalLength_ReflectsClearedFields()
    {
        var preset = new ParsePreset();
        preset.ParseDataList.Add(new ParseData(preset, ParseDataType.UInt32));
        Assert.Equal(4, preset.GetTotalLength());

        preset.ParseDataList.Clear();

        Assert.Equal(0, preset.GetTotalLength());
    }
}
