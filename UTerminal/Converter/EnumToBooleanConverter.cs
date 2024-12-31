using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml.MarkupExtensions;
using DynamicData.Kernel;
using UTerminal.Models;

namespace UTerminal.Converter;

public class EnumToBooleanConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values?.Count != 2) throw new NotSupportedException();
        if (values[0] == null || values[1] == null) return false;
        
        return values[0]?.Equals(values[1]);
    }
}