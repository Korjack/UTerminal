using System;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace UTerminal.Converter;

public class ByteToHexStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is byte byteValue)
        {
            return byteValue.ToString("X2");
        }
        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hexString)
        {
            // 2자리 16진수 문자열 검증
            if (hexString.Length > 2 || !hexString.All(Uri.IsHexDigit))
                return 0; // 또는 다른 기본값

            // string -> byte 변환
            if (byte.TryParse(hexString, NumberStyles.HexNumber, culture, out var result))
                return result;
        }
        
        return 0; // 또는 다른 기본값
    }
}