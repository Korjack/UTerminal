using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using ReactiveUI;
using UTerminal.Models.Messages;
using UTerminal.Models.Messages.Types;
using UTerminal.Models.Parser;
using UTerminal.Models.Serial.Interfaces;

namespace UTerminal.ViewModels;

/// <summary>
/// 파서 설정 창 ViewModel
/// </summary>
public class CustomParserViewModel : ReactiveObject
{
    public Interaction<Unit, IReadOnlyList<IStorageFile>> ShowFilePickerInteraction { get; } = new();
    
    #region private

    private readonly ISerialPort _serialPort;
    private IDisposable? _rawDataSubscription;

    // Field 추가용 프로퍼티
    private ParseFormatPreset? _currentPreset;
    private ParseDataType _selectedType;
    private ObservableCollection<ParseDataType> _availableTypes;
    private bool _isVariableField;
    private string _fieldName = "";
    private int _fieldLength = 1;
    private string _searchPresetName = "";

    // 상태
    private bool _isConnected;
    private int _totalReceivedCount;
    private int _successParseCount;
    private int _failedParseCount;

    private ObservableCollection<ParseFormatPreset> _filteredPreset;

    #endregion

    #region public

    public CustomSerialParser Parser { get; }

    public ObservableCollection<ParseDataType> AvailableTypes
    {
        get => _availableTypes;
        set => this.RaiseAndSetIfChanged(ref _availableTypes, value);
    }

    public ParseFormatPreset? CurrentPreset
    {
        get => _currentPreset;
        set => this.RaiseAndSetIfChanged(ref _currentPreset, value);
    }
    
    public ParseDataType SelectedType
    {
        get => _selectedType;
        set => this.RaiseAndSetIfChanged(ref _selectedType, value);
    }

    public string FieldName
    {
        get => _fieldName;
        set => this.RaiseAndSetIfChanged(ref _fieldName, value);
    }

    public int FieldLength
    {
        get => _fieldLength;
        set => this.RaiseAndSetIfChanged(ref _fieldLength, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        set => this.RaiseAndSetIfChanged(ref _isConnected, value);
    }

    public int TotalReceivedCount
    {
        get => _totalReceivedCount;
        set => this.RaiseAndSetIfChanged(ref _totalReceivedCount, value);
    }

    public int SuccessParseCount
    {
        get => _successParseCount;
        set => this.RaiseAndSetIfChanged(ref _successParseCount, value);
    }

    public int FailedParseCount
    {
        get => _failedParseCount;
        set => this.RaiseAndSetIfChanged(ref _failedParseCount, value);
    }

    public bool IsVariableField
    {
        get => _isVariableField;
        set
        {
            if (value)
            {
                AvailableTypes =
                [
                    ParseDataType.UInt8,
                    ParseDataType.UInt16,
                    ParseDataType.UInt32
                ];
                SelectedType = ParseDataType.UInt8;
            }
            else
            {
                AvailableTypes = new ObservableCollection<ParseDataType>(
                    Enum.GetValues<ParseDataType>()
                );
                SelectedType = ParseDataType.STX;
            }
            this.RaiseAndSetIfChanged(ref _isVariableField, value);
        }
    }

    public string SearchPresetName
    {
        get => _searchPresetName;
        set => this.RaiseAndSetIfChanged(ref _searchPresetName, value);
    }

    public ObservableCollection<ParseFormatPreset> FilteredPreset
    {
        get => _filteredPreset;
        set => this.RaiseAndSetIfChanged(ref _filteredPreset, value);
    }

    #endregion
    
    
    /// <summary>
    /// 초기화
    /// </summary>
    /// <param name="serialPort">사용중인 시리얼 서비스</param>
    public CustomParserViewModel(ISerialPort serialPort)
    {
        _serialPort = serialPort;

        // Parser 초기화
        Parser = new CustomSerialParser();

        // 사용 가능한 타입 목록
        AvailableTypes = new ObservableCollection<ParseDataType>(
            Enum.GetValues<ParseDataType>()
        );
        
        // 커멘드 초기화
        InitCommands();

        SelectedType = ParseDataType.STX;

        this.WhenAnyValue(x => x.SearchPresetName)
            .Throttle(TimeSpan.FromMilliseconds(300))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(FilterPresets);
    }
    
    # region Commands
    
    // Commands
    public ReactiveCommand<Unit, Unit> AddPresetCommand { get; private set; }
    public ReactiveCommand<Unit, Unit> RemovePresetCommand { get; private set; }
    public ReactiveCommand<Unit, Unit> AddFieldCommand { get; private set; }
    public ReactiveCommand<Unit, Unit> StartParsingCommand { get; private set; }
    public ReactiveCommand<Unit, Unit> StopParsingCommand { get; private set; }
    public ReactiveCommand<Unit, Unit> ClearFieldsCommand { get; private set; }
    public ReactiveCommand<Unit, Unit> SavePresetCommand { get; private set; }
    public ReactiveCommand<Unit, Unit> LoadPresetCommand { get; private set; }

    /// <summary>
    /// 커멘드 초기화
    /// </summary>
    private void InitCommands()
    {
        AddPresetCommand = ReactiveCommand.Create(AddNewPreset);
        RemovePresetCommand = ReactiveCommand.Create(RemovePreset);
        AddFieldCommand = ReactiveCommand.Create(AddField);
        StartParsingCommand = ReactiveCommand.Create(StartParsing,
            this.WhenAnyValue(x => x.IsConnected, connected => !connected));
        StopParsingCommand = ReactiveCommand.Create(StopParsing,
            this.WhenAnyValue(x => x.IsConnected));
        ClearFieldsCommand = ReactiveCommand.Create(ClearFields);
        SavePresetCommand = ReactiveCommand.Create(SavePreset);
        LoadPresetCommand = ReactiveCommand.CreateFromTask(LoadPreset);
    }
    
    #region Command Funcs

    /// <summary>
    /// 프리셋 추가
    /// </summary>
    private void AddNewPreset()
    {
        var newPreset = new ParseFormatPreset();
        Parser.ParseFormatPresets.Add(newPreset);
        CurrentPreset = newPreset;
    }

    private void RemovePreset()
    {
        if(CurrentPreset == null) return;
        Parser.ParseFormatPresets.Remove(CurrentPreset);
    }

    /// <summary>
    /// 필드 추가
    /// </summary>
    private void AddField()
    {
        if (CurrentPreset == null) return;
        
        var newField = new ParseData(CurrentPreset, SelectedType, FieldName, FieldLength);
        CurrentPreset?.ParseFormat.Add(newField);
        
        if (IsVariableField)
        {
            var variableField = new ParseData(CurrentPreset!, ParseDataType.Byte, FieldName,  length: 0, linkData: newField);
            CurrentPreset?.ParseFormat.Add(variableField);
        }

        // 입력 필드 초기화
        FieldName = "";
        FieldLength = 1;
    }
    

    /// <summary>
    /// 모든 필드 제거
    /// </summary>
    private void ClearFields()
    {
        if(CurrentPreset is null) return;
        
        CurrentPreset.ParseFormat.Clear();
        ResetStatistics();
    }

    /// <summary>
    /// 파싱 시작
    /// </summary>
    private void StartParsing()
    {
        if (Parser.ParseFormatPresets.Count == 0)
        {
            // TODO: 필드가 없으면 경고 표시
            return;
        }

        ResetStatistics();

        // 시리얼 데이터 스트림 구독
        _rawDataSubscription = _serialPort.SubscribeRawData(OnRawDataReceived);

        IsConnected = true;
    }

    /// <summary>
    /// 파싱 중지
    /// </summary>
    private void StopParsing()
    {
        _rawDataSubscription?.Dispose();
        _rawDataSubscription = null;
        IsConnected = false;
    }

    /// <summary>
    /// 통계 초기화
    /// </summary>
    private void ResetStatistics()
    {
        TotalReceivedCount = 0;
        SuccessParseCount = 0;
        FailedParseCount = 0;
    }

    /// <summary>
    /// 프리셋 저장
    /// </summary>
    private async void SavePreset()
    {
        if(CurrentPreset is null) return;

        string exePath = AppDomain.CurrentDomain.BaseDirectory;
        var json = JsonSerializer.Serialize(CurrentPreset);
        
        // 데이터 폴더 경로
        string dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Preset");
        if (!Directory.Exists(dataFolder))
        {
            Directory.CreateDirectory(dataFolder);
        }
        
        await File.WriteAllTextAsync(Path.Combine(dataFolder, CurrentPreset.Name + ".json"), json);
    }

    /// <summary>
    /// 프리셋 불러오기
    /// </summary>
    private async Task LoadPreset()
    {
        var files = await ShowFilePickerInteraction.Handle(Unit.Default);
        
        if (files.Count > 0)
        {
            foreach (var file in files)
            {
                var jsonString = await file.OpenReadAsync();
                var preset = await JsonSerializer.DeserializeAsync<ParseFormatPreset>(jsonString);
                
                if(preset == null) continue;
                
                Parser.ParseFormatPresets.Add(preset);
            }
        }
    }
    
    #endregion
    
    # endregion
    
    private void OnRawDataReceived(SerialMessage message)
    {
        TotalReceivedCount++;
    
        var success = Parser.ParseMessage(message);
    
        if (success)
        {
            SuccessParseCount++;
        }
        else
        {
            FailedParseCount++;
        }
    }
    
    private void FilterPresets(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            FilteredPreset = Parser.ParseFormatPresets;
        }
        else
        {
            var filtered = Parser.ParseFormatPresets.Where(x => 
                x.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            FilteredPreset = new ObservableCollection<ParseFormatPreset>(filtered);
        }
    }
}