using System;
using System.Globalization;
using Avalonia.Data.Converters;
using UTerminal.Models.Parser;

namespace UTerminal.Converter;

public class TypeNeedsLengthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ParseDataType type)
        {
            return type == ParseDataType.String || 
                   type == ParseDataType.Byte || 
                   type == ParseDataType.Hex;
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}