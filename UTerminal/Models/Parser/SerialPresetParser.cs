using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ReactiveUI;
using UTerminal.Models.Formatters;
using UTerminal.Models.Messages.Interfaces;
using UTerminal.Models.Parser.Interfaces;
using UTerminal.Models.Serial;

namespace UTerminal.Models.Parser;

/// <summary>
/// 커스텀 시리얼 데이터 파서
/// </summary>
public sealed class SerialPresetParser : ReactiveObject, ISerialPresetParser
{
    private readonly MessageFormatter _formatter = new();
    
    private DateTime _lastParseTime;
    private bool _isValid;
    private int _cachedTotalLength = -1;

    public ObservableCollection<IParsePreset> ParsePresetList { get; set; } = [];

    /// <summary>
    /// 마지막 파싱 시간
    /// </summary>
    public DateTime LastParseTime
    {
        get => _lastParseTime;
        private set => this.RaiseAndSetIfChanged(ref _lastParseTime, value);
    }

    /// <summary>
    /// 마지막 파싱이 유효한지 여부
    /// </summary>
    public bool IsValid
    {
        get => _isValid;
        private set => this.RaiseAndSetIfChanged(ref _isValid, value);
    }

    /// <summary>
    /// 전체 데이터 길이 계산
    /// </summary>
    private int GetTotalLength()
    {
        // 캐시가 유효하면 재사용
        if (_cachedTotalLength >= 0)
            return _cachedTotalLength;

        // LINQ 제거
        int total = 0;
        foreach (var preset in ParsePresetList)
        {
            total += preset.GetTotalLength();
        }

        _cachedTotalLength = total;
        return total;
    }

    /// <summary>
    /// ISerialMessage를 받아서 파싱
    /// </summary>
    /// <param name="message">시리얼 메시지</param>
    /// <param name="stxValue">STX 값 (기본: 0x02)</param>
    /// <param name="etxValue">ETX 값 (기본: 0x03)</param>
    /// <returns>파싱 성공 여부</returns>
    public bool ParseMessage(ISerialMessage message)
    {
        bool anySuccess = false;
        var dataLength = message.Data.Length;
    
        foreach (var preset in ParsePresetList)
        {
            if(preset.ParseDataList.Count == 0) continue;
            
            // 길이 체크를 먼저해서 불필요한 파싱 회피
            if (dataLength < preset.GetTotalLength())
            {
                preset.IsValid = false;
                continue;
            }
        
            bool success = ParsePreset(preset, message.Data);
            preset.IsValid = success;
        
            if (success) anySuccess = true;
        }
    
        LastParseTime = message.Timestamp;
        IsValid = anySuccess;
        return anySuccess;
    }
    
    private bool ParsePreset(IParsePreset preset, byte[] data)
    {
        try
        {
            int offset = 0;
        
            foreach (var parseData in preset.ParseDataList)
            {
                if (offset + parseData.Size > data.Length) return false;

                if (parseData.LinkData is { ParsedValue: not null })
                {
                    var length = (byte)parseData.LinkData.ParsedValue;
                    parseData.ParsedValue = FormatAsHex(data, offset, length);
                    offset += length;
                }
                else
                {
                    parseData.ParsedValue = parseData.ParsedValue = parseData.ParseDataType switch
                    {
                        ParseDataType.STX => ValidateSTX(data[offset], SerialConstants.ControlCharacters.STX),
                        ParseDataType.ETX => ValidateETX(data[offset], SerialConstants.ControlCharacters.ETX),
                        ParseDataType.Int8 => (sbyte)data[offset],
                        ParseDataType.UInt8 => data[offset],
                        ParseDataType.Byte => FormatAsHex(data, offset, parseData.Length),
                        ParseDataType.String => FormatAsString(data, offset, parseData.Length),

                        ParseDataType.Int16 => Read<short>(data, offset),
                        ParseDataType.UInt16 => Read<ushort>(data, offset),
                        ParseDataType.Int32 => Read<int>(data, offset),
                        ParseDataType.UInt32 => Read<uint>(data, offset),
                        ParseDataType.Float => Read<float>(data, offset),
                        ParseDataType.Double => Read<double>(data, offset),
                        _ => null
                    };
                    
                    offset += parseData.Size;
                }
                
            }
            return true;
        }
        catch (InvalidOperationException) { return false; }  // ✅ 이 Preset만 실패
        catch (Exception) { return false; }
    }

    /// <summary>
    /// STX 값 검증
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte ValidateSTX(byte actualValue, byte expectedValue)
    {
        if (actualValue != expectedValue)
        {
            throw new InvalidOperationException($"STX mismatch: expected 0x{expectedValue:X2}, got 0x{actualValue:X2}");
        }
        return actualValue;
    }

    /// <summary>
    /// ETX 값 검증
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte ValidateETX(byte actualValue, byte expectedValue)
    {
        if (actualValue != expectedValue)
        {
            throw new InvalidOperationException($"ETX mismatch: expected 0x{expectedValue:X2}, got 0x{actualValue:X2}");
        }
        return actualValue;
    }
    
    
    /// <summary>
    /// 데이터를 HEX 문자열로 포맷
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string FormatAsHex(byte[] data, int offset, int length)
    {
        var segment = data.AsSpan(offset, length);
        return _formatter.FormatData(segment, EncodingBytes.HEX);
    }

    /// <summary>
    /// 데이터를 문자열로 포맷
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string FormatAsString(byte[] data, int offset, int length)
    {
        var segment = data.AsSpan(offset, length);
        return _formatter.FormatData(segment, EncodingBytes.ASCII).TrimEnd('\0');
    }
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T Read<T>(byte[] data, int offset) where T : struct
    {
        return MemoryMarshal.Read<T>(data.AsSpan(offset, Unsafe.SizeOf<T>()));
    }
}