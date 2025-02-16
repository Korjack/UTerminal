using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using ReactiveUI;
using UTerminal.Models.Messages;
using UTerminal.Models.Messages.Interfaces;
using UTerminal.Models.PortManager;
using UTerminal.Models.Serial;
using UTerminal.Models.Utils.Logger;
using UTerminal.Views;

namespace UTerminal.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly SerialService _serialService;                      // Serial Connection Management
    private readonly SerialMsgProcessor _serialMsgProcessor;            // Converts and processes messages of serial message type.
    
    public SerialConnectionConfiguration ConnectionConfig { get; }      // Settings closely related to serial connection
    public SerialRuntimeConfiguration RuntimeConfig { get; }            // Settings can be changed regardless of connection
    public PortManager PortManager { get; }                             // Port Manager for seek port path or set
    
    private readonly SerialMsgLogger _msgLogManager = SerialMsgLogger.Instance;     // Serial Message Log Manager
    private readonly SystemLogger _systemLogger = SystemLogger.Instance;            // System Log Manager

    #region Fields

    private bool _isConnected;                                              // Serial connection status
    private string _receivedSerialData = string.Empty;                      // Convert to string from serial message
    private bool _isSerialLogging;                                          // Serial data logging status
    private ObservableAsPropertyHelper<double> _messageRate;                // Message hz

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

    public bool IsSerialLogging
    {
        get => _isSerialLogging;
        private set => this.RaiseAndSetIfChanged(ref _isSerialLogging, value);
    }
    
    public double MessageRate => _messageRate.Value;

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
        InitializeObservable();
        
        _systemLogger.LogInfo("Initialized Application");
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
    /// Init observable value
    /// </summary>
    private void InitializeObservable()
    {
        // Update message hz on 100ms
        Observable.Interval(TimeSpan.FromMilliseconds(100))
            .Select(_ => _serialService.MessageRate)
            .ToProperty(this, nameof(MessageRate), out _messageRate);
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

    #region Default Main Menu

    public ICommand QuitCommand { get; private set; } = null!;          // Quit Program
    public ICommand ConnectCommand { get; set; } = null!;               // Connect Serial
    public ICommand ReScanCommand { get; set; } = null!;                // Scan Port List

    #endregion

    #region Serial Options

    public ICommand ComPortRadioChangedCommand { get; private set; } = null!;       // If Comport Changed
    public ICommand SerialSettingChangedCommand { get; private set; } = null!;      // If other port setting changed. (like baudrate, parity...)
    public ICommand EncodingBytesChangedCommand { get; private set; } = null!;      // Show serial data, base on Encoding
    public ICommand SendSerialDataCommand { get; private set; } = null!;            // Send serial data
    public ICommand ReadTypeChangedCommand { get; private set; } = null!;           // Choose how to read serial data

    #endregion

    #region Open Windows

    public ICommand OpenMacroWindowCommand { get; private set; } = null!;           // Open macro window

    #endregion

    #region Serial Logging

    public ICommand SerialLoggingCommand { get; private set; } = null!;         // Start logging serial data
    public ICommand SetSerialLogPathCommand { get; private set; } = null!;      // Set logging path

    #endregion
    
    /// <summary>
    /// Initialize commands
    /// </summary>
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

        SerialLoggingCommand = ReactiveCommand.Create<object>(StartSerialLogging);
        SetSerialLogPathCommand = ReactiveCommand.CreateFromTask(OnSelectSerialLogFolder_Click);
    }

    #endregion
    
    #region Command Method
    
    /// <summary>
    /// Quit Program
    /// </summary>
    private void QuitProgram()
    {
        _systemLogger.LogInfo("Application Shutdown");
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
        
        _systemLogger.LogSerialConnection(ConnectionConfig.PortName, IsConnected);
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
        
        _systemLogger.LogInfo($"Serial Setting set to {setting}");
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
        var oldMode = RuntimeConfig.ReadMode;
        RuntimeConfig.ReadMode = type switch
        {
            "NewLine" => ReadModeType.NewLine,
            "STX_ETX" => ReadModeType.StxEtx,
            "Custom" => ReadModeType.Custom,
            _ => RuntimeConfig.ReadMode
        };
        
        _systemLogger.LogConfigurationChange("Read Type Changed", oldMode.ToString(), type);
    }
    
    
    /// <summary>
    /// Start logging received serial data
    /// </summary>
    /// <param name="sender">Button</param>
    private void StartSerialLogging(object? sender)
    {
        var b = sender as Button;
        
        if(!IsSerialLogging) _msgLogManager.CreateLogFile();
        
        IsSerialLogging = !IsSerialLogging;
        
        var text = IsSerialLogging ? "Stop Data Logging" : "Start Data Logging";
        b!.Content = text;

        _msgLogManager.IsStartLogging = IsSerialLogging; 
        _systemLogger.LogInfo($"Serial Message Logging {text}");
    }


    /// <summary>
    /// Change Log Path on Click Button
    /// </summary>
    private async Task OnSelectSerialLogFolder_Click()
    {
        // Get folder path when click folder
        var result = await _selectFolderInteraction.Handle("");

        if (!string.IsNullOrEmpty(result))
        {
            _msgLogManager.SerialLogFilePath = result;
            _systemLogger.LogInfo($"Serial Message Logging Path Changed > {result}");
        }
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