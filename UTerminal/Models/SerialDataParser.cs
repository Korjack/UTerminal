using System;
using System.Linq;
using System.Text;

namespace UTerminal.Models;

public class SerialDataParser
{
    /// <summary>
    /// Parse hex string or string to byte array
    /// </summary>
    /// <param name="input">text or hex string(e.g., $01 $ff)</param>
    /// <returns><see cref="byte"/>[] - text or hex string to byte array</returns>
    public byte[] ParseToBytes(string input)
    {
        // remove white space
        var tokens = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        // check $XX type token
        bool isAllHex = tokens.All(token => 
            token.StartsWith('$') && 
            token.Length == 3 && 
            IsValidHexString(token[1..])
        );
        
        return isAllHex 
            ? tokens.Select(token => Convert.ToByte(token[1..], 16)).ToArray() 
            : Encoding.UTF8.GetBytes(input);
    }

    private static bool IsValidHexString(string hex)
    {
        return hex.Length == 2 && hex.All(c => "0123456789ABCDEFabcdef".Contains(c));
    }
}