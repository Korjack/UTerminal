using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using ReactiveUI;
using UTerminal.Models;
using UTerminal.Models.Interfaces;
using UTerminal.Views;

namespace UTerminal.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly ISerialService _serialService;
    private readonly IMessageProcessor _messageProcessor;
    
    public SerialConnectionConfiguration ConnectionConfig { get; private set; }
    public SerialRuntimeConfiguration RuntimeConfig { get; private set; }
    public PortManager PortManager { get; private set; }
    
    
    #region Serial Setting

    public string[] ComPortList { get; private set; } = ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9"];
    public IEnumerable<BaudRateType> BaudRatesOption => BaudRateType.StandardBaudRates;
    public static Array ParityOption => Enum.GetValues(typeof(ParityType));
    public static Array DataBitsOption => Enum.GetValues(typeof(DataBitsType));
    public static Array StopBitsOption => Enum.GetValues(typeof(StopBitsType));

    #endregion

    private string _serialStringData = string.Empty;
    public string SerialStringData { 
        get => _serialStringData;
        private set => this.RaiseAndSetIfChanged(ref _serialStringData, value);
    }
    
    private bool _isConnected;
    public bool IsConnected
    {
        get => _isConnected;
        private set => this.RaiseAndSetIfChanged(ref _isConnected, value);
    }
    
    // Interactions for selecting folders
    private Interaction<string?, string?> _selectFolderInteraction;
    public Interaction<string?, string?> SelectFolderInteraction => _selectFolderInteraction;
    
    
    #region Commands

    public ICommand QuitCommand { get; private set; }
    public ICommand ConnectCommand { get; set; } = null!;
    public ICommand ReScanCommand { get; set; } = null!;

    public ICommand ComPortRadioChangedCommand { get; private set; } = null!;
    public ICommand SerialSettingChangedCommand { get; private set; } = null!;
    public ICommand EncodingBytesChangedCommand { get; private set; } = null!;
    public ICommand SendSerialDataCommand { get; private set; } = null!;
    public ICommand OpenMacroWindowCommand { get; private set; } = null!;
    public ICommand ReadTypeChangedCommand { get; private set; } = null!;

    public ICommand SerialLoggingCommand { get; private set; } = null!;
    public ICommand SetSerialLogPathCommand { get; private set; } = null!;
    
    private void InitializeSerialCommands()
    {
        // 기본메뉴 커맨드
        QuitCommand = ReactiveCommand.Create(QuitProgram);
        ConnectCommand = ReactiveCommand.Create(ConnectSerialPort);
        ReScanCommand = ReactiveCommand.Create(PortManager.ScanPort);

        // 옵션 설정 커맨드
        ComPortRadioChangedCommand = ReactiveCommand.Create<object>(ComPortRadio_Clicked);
        SerialSettingChangedCommand = ReactiveCommand.Create<object>(SerialSettingRadio_Clicked);
        EncodingBytesChangedCommand = ReactiveCommand.Create<string>(EncodingByteRadio_Clicked);

        SendSerialDataCommand = ReactiveCommand.CreateFromTask<string>(SendSerialDataAsync_Clicked);
        OpenMacroWindowCommand = ReactiveCommand.Create(OpenMacroWindowAsync_Clicked);
        ReadTypeChangedCommand = ReactiveCommand.Create<string>(ReadTypeChanged_Clicked);

        // SerialLoggingCommand = ReactiveCommand.Create<object>(StartSerialLogging);
        // SetSerialLogPathCommand = ReactiveCommand.CreateFromTask(OnSelectSerialLogFolder_Click);

        _selectFolderInteraction = new Interaction<string?, string?>();
    }
    
    #endregion
    
    
    public MainViewModel()
    {
        ConnectionConfig = new SerialConnectionConfiguration();
        RuntimeConfig = new SerialRuntimeConfiguration();
        
        _serialService = new SerialService(ConnectionConfig, RuntimeConfig);
        PortManager = new PortManager(ConnectionConfig);
        
        _messageProcessor = new MessageProcessor(1024);
        
        InitializeSerialDataStream();
        InitializeSerialCommands();
    }

    /// <summary>
    /// Quit Program
    /// </summary>
    private void QuitProgram()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            desktopLifetime.Shutdown();
        }
    }
    

    /// <summary>
    /// 시리얼 연결 디바이스에 대한 데이터처리에 대한 스트림을 생성합니다.
    /// 데이터 수신 -> 데이터 처리 -> UI 업데이트를 수행합니다.
    /// </summary>
    private void InitializeSerialDataStream()
    {
        // Serial message receiver
        var serialDataStream = Observable.FromEventPattern<EventHandler<ISerialMessage>, ISerialMessage>(
                h => _serialService.MsgReceived += h,
                h => _serialService.MsgReceived -= h)
            .Select(x => x.EventArgs);

        serialDataStream
            .Buffer(TimeSpan.FromMilliseconds(16.67))
            .Where(messages => messages.Count > 0)
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Subscribe(ProcessMessages);

        // Processed message receiver
        Observable.FromEventPattern<EventHandler<string>, string>(
                h => _messageProcessor.BufferUpdated += h,
                h => _messageProcessor.BufferUpdated += h)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(pattern => SerialStringData = pattern.EventArgs);
    }


    /// <summary>
    /// Processes serial messages received via Observable.
    /// </summary>
    /// <param name="messages"><see cref="List{T}"/>Received message list</param>
    private void ProcessMessages(IList<ISerialMessage> messages)
    {
        foreach (var msg in messages)
        {
            _messageProcessor.ProcessMessage(msg);
        }
    }

    
    

    #region Command Method


    /// <summary>
    /// Connect Serial
    /// </summary>
    private void ConnectSerialPort()
    {
        if (!IsConnected)
        {
            IsConnected = _serialService.Connect();   
        }
        else
        {
            if (_serialService.Disconnect())
            {
                IsConnected = false;   
            }
        }
    }


    /// <summary>
    /// Set Serial Port
    /// </summary>
    /// <param name="o">Selected Serial Port Radio Item or Serial port path</param>
    private void ComPortRadio_Clicked(object o)
    {
        switch (o)
        {
            case PortInfo info:
                PortManager.SelectPort(info.Name);
                break;
            case string s:
                PortManager.CustomSelectPort(s);
                break;
        }
    }


    /// <summary>
    /// Change Serial Device Settings.
    /// </summary>
    /// <remarks>
    /// Divide based on the following type:
    /// <list type="bullet">
    ///     <item><description><see cref="BaudRateType"/></description></item>
    ///     <item><description><see cref="ParityType"/></description></item>
    ///     <item><description><see cref="DataBitsType"/></description></item>
    ///     <item><description><see cref="StopBitsType"/></description></item>
    /// </list>
    /// </remarks>
    /// <param name="setting"></param>
    private void SerialSettingRadio_Clicked(object? setting)
    {
        switch (setting)
        {
            case BaudRateType baudRate:                 // BaudRate
                ConnectionConfig.BaudRate = baudRate;
                break;
            case ParityType parity:                     // Parity
                ConnectionConfig.Parity = parity;
                break;
            case DataBitsType dataBits:                 // DataBits
                ConnectionConfig.DataBits = dataBits;
                break;
            case StopBitsType stopBits:                 // StopBits
                ConnectionConfig.StopBits = stopBits;
                break;
            default:                                    // Default
                Debug.WriteLine($"Serial Setting Object: {setting} / Type: {setting?.GetType()}");
                break;
        }
    }


    /// <summary>
    /// Convert Serial Buffer Text Type
    /// </summary>
    /// <param name="stringMode">Convert Type</param>
    private void EncodingByteRadio_Clicked(string? stringMode)
    {
        switch (stringMode)
        {
            case "ASCII":
                _messageProcessor.ChangeFormat(SerialConstants.EncodingBytes.ASCII);
                break;
            case "HEX":
                _messageProcessor.ChangeFormat(SerialConstants.EncodingBytes.HEX);
                break;
            case "UTF8":
                _messageProcessor.ChangeFormat(SerialConstants.EncodingBytes.UTF8);
                break;
        }
    }


    /// <summary>
    /// Send Serial Data 
    /// </summary>
    /// 
    private async Task SendSerialDataAsync_Clicked(string data)
    {
        if (string.IsNullOrEmpty(data)) return;
        if (!IsConnected) return;

        await _serialService.WriteAsync(data);
    }


    private void OpenMacroWindowAsync_Clicked()
    {
        // var macroWindow = new MacroView
        // {
        //     DataContext = new MacroViewModel(SendSerialDataAsync_Clicked)
        // };
        //
        // if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        // {
        //     macroWindow.Show(desktopLifetime.MainWindow!);
        // }
    }


    /// <summary>
    /// ReadType RadioButton changed
    /// </summary>
    /// <remarks><see cref="ReadMode"/></remarks>
    /// <param name="type"><see cref="string"/></param>
    private void ReadTypeChanged_Clicked(string type)
    {
        RuntimeConfig.ReadMode = type switch
        {
            "NewLine" => ReadModeType.NewLine,
            "STX_ETX" => ReadModeType.StxEtx,
            "Custom" => ReadModeType.Custom,
            _ => RuntimeConfig.ReadMode
        };
    }
    

    #endregion
    
}