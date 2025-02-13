using System.ComponentModel;

namespace UTerminal.Models.Serial;

public class SerialConstants
{
    public static class ControlCharacters
    {
        public const byte NEWLINE = 0x0A;
        public const byte CARRIAGE_RETURN = 0x0D;
        public const byte STX = 0x02;
        public const byte ETX = 0x03;
    }
}