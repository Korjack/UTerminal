using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using UTerminal.Models.Messages.Interfaces;

namespace UTerminal.Models.Parser;

/// <summary>
/// 커스텀 시리얼 데이터 파서
/// </summary>
public class CustomSerialParser : INotifyPropertyChanged
{
    private DateTime _lastParseTime;
    private bool _isValid;

    public ObservableCollection<ParseData> ParseFormat { get; set; } = new();

    /// <summary>
    /// 마지막 파싱 시간
    /// </summary>
    public DateTime LastParseTime
    {
        get => _lastParseTime;
        private set
        {
            _lastParseTime = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 마지막 파싱이 유효한지 여부
    /// </summary>
    public bool IsValid
    {
        get => _isValid;
        private set
        {
            _isValid = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 전체 데이터 길이 계산
    /// </summary>
    public int GetTotalLength()
    {
        return ParseFormat.Sum(p => p.GetSize());
    }

    /// <summary>
    /// ISerialMessage를 받아서 파싱
    /// </summary>
    /// <param name="message">시리얼 메시지</param>
    /// <param name="stxValue">STX 값 (기본: 0x02)</param>
    /// <param name="etxValue">ETX 값 (기본: 0x03)</param>
    /// <returns>파싱 성공 여부</returns>
    public bool ParseMessage(ISerialMessage message, byte stxValue = 0x02, byte etxValue = 0x03)
    {
        try
        {
            // 데이터 길이 체크
            if (message.Data.Length < GetTotalLength())
            {
                IsValid = false;
                return false;
            }

            // STX/ETX 검증
            if (!Validate(message.Data, stxValue, etxValue))
            {
                IsValid = false;
                return false;
            }

            // 파싱 실행
            Parse(message.Data);
            LastParseTime = message.Timestamp;
            IsValid = true;
            return true;
        }
        catch
        {
            IsValid = false;
            return false;
        }
    }

    /// <summary>
    /// 데이터 파싱 수행
    /// </summary>
    private void Parse(byte[] data)
    {
        int offset = 0;
        foreach (var parseData in ParseFormat)
        {
            parseData.ParsedValue = parseData.ParseDataType switch
            {
                ParseDataType.STX => data[offset],
                ParseDataType.ETX => data[offset],
                ParseDataType.Int8 => (sbyte)data[offset],
                ParseDataType.UInt8 => data[offset],
                ParseDataType.Byte => data.Skip(offset).Take(parseData.Length).ToArray(),
                ParseDataType.Hex => data.Skip(offset).Take(parseData.Length).ToArray(),
                ParseDataType.Int16 => BitConverter.ToInt16(data, offset),
                ParseDataType.UInt16 => BitConverter.ToUInt16(data, offset),
                ParseDataType.Int32 => BitConverter.ToInt32(data, offset),
                ParseDataType.UInt32 => BitConverter.ToUInt32(data, offset),
                ParseDataType.Float => BitConverter.ToSingle(data, offset),
                ParseDataType.Double => BitConverter.ToDouble(data, offset),
                ParseDataType.String => System.Text.Encoding.ASCII.GetString(data, offset, parseData.Length).TrimEnd('\0'),
                _ => null
            };

            offset += parseData.GetSize();
        }
    }

    /// <summary>
    /// STX, ETX 검증
    /// </summary>
    private bool Validate(byte[] data, byte stxValue, byte etxValue)
    {
        if (data.Length < GetTotalLength()) return false;

        var firstParse = ParseFormat.FirstOrDefault();
        var lastParse = ParseFormat.LastOrDefault();

        if (firstParse?.ParseDataType == ParseDataType.STX && data[0] != stxValue)
            return false;

        if (lastParse?.ParseDataType == ParseDataType.ETX && data[GetTotalLength() - 1] != etxValue)
            return false;

        return true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}