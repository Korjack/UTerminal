using System;
using System.Globalization;
using Avalonia.Data.Converters;
using UTerminal.Models;
using UTerminal.Models.Serial;

namespace UTerminal.Converter;

public class NumberToBaudrateConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is BaudRateType baudRateType)
        {
            return (int)baudRateType;
        }

        return 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Decimal number)
        {
            return new BaudRateType((int)number);
        }

        return 0;
    }
}