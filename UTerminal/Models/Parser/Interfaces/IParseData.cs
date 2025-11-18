namespace UTerminal.Models.Parser.Interfaces;

public interface IParseData
{
    ParseDataType ParseDataType { get; }
    int Size { get; }
    int Length { get; }
    
    IParseData? LinkData { get; }
    
    string Name { get; set; }
    
    object? ParsedValue { get; set; }
    string DisplayValue { get; }
}