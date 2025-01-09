using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace UTerminal.Converter;

public class BoolToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            // parameter로 전달된 문자열을 ','로 분리
            if (parameter is string paramString)
            {
                var options = paramString.Split(',');
                if (options.Length == 2)
                {
                    // True일 때는 첫 번째 값, False일 때는 두 번째 값 반환
                    return boolValue ? options[0] : options[1];
                }
            }
            // parameter가 없을 경우 기본값 사용
            return boolValue ? "활성화됨" : "비활성화됨";
        }
        return "상태 알 수 없음";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}