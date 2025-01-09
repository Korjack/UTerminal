using System;
using System.ComponentModel;
using Avalonia.Data.Converters;

namespace UTerminal.Converter;

public class EnumDescriptionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value == null) return string.Empty;
        
        var fi = value.GetType().GetField(value.ToString()!);
        var attributes = (DescriptionAttribute[])fi!.GetCustomAttributes(typeof(DescriptionAttribute), false);
        
        return attributes.Length > 0 ? attributes[0].Description : value.ToString()!;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}