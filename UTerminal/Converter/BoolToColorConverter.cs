using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace UTerminal.Converter;

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool boolValue && boolValue ? Brushes.Green : Brushes.Red;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}