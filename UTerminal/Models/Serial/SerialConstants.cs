using System.ComponentModel;

namespace UTerminal.Models;

public class SerialConstants
{
    public static class ControlCharacters
    {
        public const byte NEWLINE = 0x0A;
        public const byte CARRIAGE_RETURN = 0x0D;
        public const byte STX = 0x02;
        public const byte ETX = 0x03;
    }
    
    public enum EncodingBytes
    {
        [Description("ASCII")] ASCII = 0,
        [Description("HEX")] HEX = 1,
        [Description("UTF8")] UTF8 = 2
    }
}