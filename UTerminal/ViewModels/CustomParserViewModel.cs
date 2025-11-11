using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using UTerminal.Models.Messages.Interfaces;
using UTerminal.Models.Parser;
using UTerminal.Models.Serial.Interfaces;

namespace UTerminal.ViewModels;

/// <summary>
/// 파서 설정 창 ViewModel
/// </summary>
public class CustomParserViewModel : ReactiveObject
{
    private readonly ISerialService _serialService;
    private IDisposable? _serialSubscription;

    // Parser
    public CustomSerialParser Parser { get; set; }
    public ObservableCollection<ParseDataType> AvailableTypes { get; set; }

    // Field 추가용 프로퍼티
    private ParseDataType _selectedType;
    private string _fieldName = "";
    private int _fieldLength = 1;

    // 상태
    private bool _isConnected;
    private int _totalReceivedCount;
    private int _successParseCount;
    private int _failedParseCount;

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

    // Commands
    public ReactiveCommand<Unit, Unit> AddFieldCommand { get; }
    public ReactiveCommand<ParseData, Unit> RemoveFieldCommand { get; }
    public ReactiveCommand<Unit, Unit> StartParsingCommand { get; }
    public ReactiveCommand<Unit, Unit> StopParsingCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearFieldsCommand { get; }
    public ReactiveCommand<Unit, Unit> SavePresetCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadPresetCommand { get; }

    public CustomParserViewModel(ISerialService serialService)
    {
        // DI에서 SerialService 가져오기 (또는 파라미터로 받기)
        _serialService = serialService;

        // Parser 초기화
        Parser = new CustomSerialParser();

        // 사용 가능한 타입 목록
        AvailableTypes = new ObservableCollection<ParseDataType>(
            Enum.GetValues<ParseDataType>()
        );

        SelectedType = ParseDataType.Int8;

        // Commands 초기화
        AddFieldCommand = ReactiveCommand.Create(AddField);
        RemoveFieldCommand = ReactiveCommand.Create<ParseData>(RemoveField);
        StartParsingCommand = ReactiveCommand.Create(StartParsing,
            this.WhenAnyValue(x => x.IsConnected, connected => !connected));
        StopParsingCommand = ReactiveCommand.Create(StopParsing,
            this.WhenAnyValue(x => x.IsConnected));
        ClearFieldsCommand = ReactiveCommand.Create(ClearFields);
        SavePresetCommand = ReactiveCommand.Create(SavePreset);
        LoadPresetCommand = ReactiveCommand.Create(LoadPreset);
    }

    /// <summary>
    /// 필드 추가
    /// </summary>
    private void AddField()
    {
        var newField = new ParseData(SelectedType, FieldName)
        {
            Length = FieldLength
        };

        Parser.ParseFormat.Add(newField);

        // 입력 필드 초기화
        FieldName = "";
        FieldLength = 1;
    }

    /// <summary>
    /// 필드 제거
    /// </summary>
    private void RemoveField(ParseData field)
    {
        Parser.ParseFormat.Remove(field);
    }

    /// <summary>
    /// 모든 필드 제거
    /// </summary>
    private void ClearFields()
    {
        Parser.ParseFormat.Clear();
        ResetStatistics();
    }

    /// <summary>
    /// 파싱 시작
    /// </summary>
    private void StartParsing()
    {
        if (Parser.ParseFormat.Count == 0)
        {
            // TODO: 필드가 없으면 경고 표시
            return;
        }

        ResetStatistics();

        // 시리얼 데이터 스트림 구독
        var serialDataStream = Observable.FromEventPattern<EventHandler<ISerialMessage>, ISerialMessage>(
                h => _serialService.MsgReceived += h,
                h => _serialService.MsgReceived -= h)
            .Select(x => x.EventArgs);

        _serialSubscription = serialDataStream
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Select(msg =>
            {
                TotalReceivedCount++;
                return new { Message = msg, Success = Parser.ParseMessage(msg) };
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(result =>
            {
                if (result.Success)
                {
                    SuccessParseCount++;
                }
                else
                {
                    FailedParseCount++;
                }
            });

        IsConnected = true;
    }

    /// <summary>
    /// 파싱 중지
    /// </summary>
    private void StopParsing()
    {
        _serialSubscription?.Dispose();
        _serialSubscription = null;
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
    private void SavePreset()
    {
        // TODO: JSON 등으로 프리셋 저장
    }

    /// <summary>
    /// 프리셋 불러오기
    /// </summary>
    private void LoadPreset()
    {
        // TODO: JSON 등에서 프리셋 불러오기
    }
}