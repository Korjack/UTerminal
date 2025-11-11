using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace UTerminal.Converter;

/// <summary>
/// Bool 값을 상태 텍스트로 변환하는 컨버터
/// </summary>
public class BoolToStatusConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool boolValue && boolValue ? "Running" : "Stopped";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}