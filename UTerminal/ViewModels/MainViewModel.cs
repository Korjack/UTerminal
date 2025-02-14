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
using UTerminal.Models.Messages;
using UTerminal.Models.Messages.Interfaces;
using UTerminal.Models.PortManager;
using UTerminal.Models.Serial;
using UTerminal.Models.Serial.Interfaces;
using UTerminal.Views;

namespace UTerminal.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly ISerialService _serialService;                             // Serial Connection Management
    private readonly SerialMsgProcessor _serialMsgProcessor;       // Converts and processes messages of serial message type.
    
    public SerialConnectionConfiguration ConnectionConfig { get; }      // Settings closely related to serial connection
    public SerialRuntimeConfiguration RuntimeConfig { get; }            // Settings can be changed regardless of connection
    
    public PortManager PortManager { get; }                             // Port Manager for seek port path or set

    #region Fields

    private bool _isConnected;                                  // Serial connection status
    private string _receivedSerialData = string.Empty;          // Convert to string from serial message

    #endregion

    #region Properties

    public string ReceivedSerialData { 
        get => _receivedSerialData;
        private set => this.RaiseAndSetIfChanged(ref _receivedSerialData, value);
    }
    
    public bool IsConnected
    {
        get => _isConnected;
        private set => this.RaiseAndSetIfChanged(ref _isConnected, value);
    }

    #endregion
    
    
    public MainViewModel()
    {
        ConnectionConfig = new SerialConnectionConfiguration();
        RuntimeConfig = new SerialRuntimeConfiguration();
        
        _serialService = new SerialService(ConnectionConfig, RuntimeConfig);
        PortManager = new PortManager(ConnectionConfig);
        
        _serialMsgProcessor = new SerialMsgProcessor(1024);
        
        InitializeSerialDataStream();
        InitializeSerialCommands();
        InitInteractions();
    }
    
    
    #region Obserable

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
                h => _serialMsgProcessor.BufferUpdated += h,
                h => _serialMsgProcessor.BufferUpdated += h)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(pattern => ReceivedSerialData = pattern.EventArgs);
    }


    /// <summary>
    /// Processes serial messages received via Observable.
    /// </summary>
    /// <param name="messages"><see cref="List{T}"/>Received message list</param>
    private void ProcessMessages(IList<ISerialMessage> messages)
    {
        foreach (var msg in messages)
        {
            _serialMsgProcessor.ProcessMessage(msg);
        }
    }

    #endregion

    #region Interactions

    // Interactions for selecting folders
    private Interaction<string?, string?> _selectFolderInteraction = null!;
    public Interaction<string?, string?> SelectFolderInteraction => _selectFolderInteraction;

    private void InitInteractions()
    {
        // Interaction Init
        _selectFolderInteraction = new Interaction<string?, string?>();
    }

    #endregion
    
    #region Commands

    #region Commands Init

    public ICommand QuitCommand { get; private set; } = null!;
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
    }

    #endregion
    
    #region Command Method
    
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
    /// Connect or Disconnect serial port.
    /// Connection status update on here
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
    /// If select port, change port name for serial connection
    /// </summary>
    /// <param name="o">
    /// Gets the selected PortInfo. In case of custom settings, it gets a string value for the port connection.
    /// <list type="bullet">
    /// <item><see cref="PortInfo"/></item>
    /// <item><see cref="string"/></item>
    /// </list>
    /// </param>
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
                _serialMsgProcessor.ChangeFormat(EncodingBytes.ASCII);
                break;
            case "HEX":
                _serialMsgProcessor.ChangeFormat(EncodingBytes.HEX);
                break;
            case "UTF8":
                _serialMsgProcessor.ChangeFormat(EncodingBytes.UTF8);
                break;
        }
    }


    /// <summary>
    /// Send serial data
    /// </summary>
    /// <param name="data">[<see cref="string"/>] data</param>
    private async Task SendSerialDataAsync_Clicked(string data)
    {
        // If data is null or not connected
        if (string.IsNullOrEmpty(data) || !IsConnected) return;

        await _serialService.WriteAsync(data);
    }


    /// <summary>
    /// Open macro window
    /// </summary>
    private void OpenMacroWindowAsync_Clicked()
    {
        var macroWindow = new MacroView
        {
            DataContext = new MacroViewModel(SendSerialDataCommand)
        };
        
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            macroWindow.Show(desktopLifetime.MainWindow!);
        }
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
    
    #endregion

    #region UI Viewer Serial Init
    
    public IEnumerable<BaudRateType> BaudRatesOption => BaudRateType.StandardBaudRates;
    public static Array ParityOption => Enum.GetValues(typeof(ParityType));
    public static Array DataBitsOption => Enum.GetValues(typeof(DataBitsType));
    public static Array StopBitsOption => Enum.GetValues(typeof(StopBitsType));

    #endregion
}