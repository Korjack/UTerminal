using System;

namespace UTerminal.Models.Formatters;

/// <summary>
/// Handles formatting of timestamps for serial messages
/// </summary>
public class TimeFormatter
{
    private static readonly char[] TimeFormatBuffer = new char[14]; // [HH:mm:ss.fff]

    /// <summary>
    /// Formats a DateTime into a char buffer in the format [HH:mm:ss.fff]
    /// </summary>
    /// <param name="time">The DateTime to format</param>
    /// <param name="buffer">The buffer to write the formatted time into</param>
    public void FormatTime(DateTime time, char[] buffer)
    {
        buffer[0] = '[';
        WriteDigits(time.Hour, buffer, 1);
        buffer[3] = ':';
        WriteDigits(time.Minute, buffer, 4);
        buffer[6] = ':';
        WriteDigits(time.Second, buffer, 7);
        buffer[9] = '.';

        int ms = time.Millisecond;
        buffer[10] = (char)('0' + ms / 100);
        buffer[11] = (char)('0' + (ms / 10) % 10);
        buffer[12] = (char)('0' + ms % 10);
        buffer[13] = ']';
    }

    private static void WriteDigits(int value, char[] buffer, int startIndex)
    {
        buffer[startIndex] = (char)('0' + (value / 10));
        buffer[startIndex + 1] = (char)('0' + (value % 10));
    }
}