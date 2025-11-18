using System;
using System.Collections.ObjectModel;
using UTerminal.Models.Messages.Interfaces;

namespace UTerminal.Models.Parser.Interfaces;

public interface ISerialPresetParser
{
    ObservableCollection<IParsePreset> ParsePresetList { get; set; }

    
    bool ParseMessage(ISerialMessage message);
    DateTime LastParseTime { get; }
    bool IsValid { get; }
}