using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace UTerminal.Models.Parser.Interfaces;

public interface IParsePreset
{
    string Name { get; set; }
    bool IsValid { get; set; }
    
    ObservableCollection<IParseData> ParseDataList { get; }

    int GetTotalLength();
}