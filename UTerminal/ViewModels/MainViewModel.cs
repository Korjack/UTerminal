using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using ReactiveUI;
using UTerminal.Models;
using UTerminal.Views;

namespace UTerminal.ViewModels;

public class MainViewModel : ViewModelBase
{
    public ICommand QuitCommand { get; private set; }
    
    public MainViewModel()
    {
        QuitCommand = ReactiveCommand.Create(QuitProgram);
        
        var setting = InitializeSerialSettings();
        _serialDevice = InitializeSerialDevice(setting);
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
    
    
    #region Serial
    
    #region Serial Setting
    
    private readonly SerialDevice _serialDevice;        // Init Serial Device
    public SerialSettings SerialSettings => _serialDevice.SerialSettings;
    
    public ObservableCollection<OptionRadioItem> DefaultComPortList { get; private set; } = null!;
    public IEnumerable<BaudRateType> BaudRatesOption => BaudRateType.StandardBaudRates;
    public static Array ParityOption => Enum.GetValues(typeof(ParityType));
    public static Array DataBitsOption => Enum.GetValues(typeof(DataBitsType));
    public static Array StopBitsOption => Enum.GetValues(typeof(StopBitsType));
    
    #endregion

    #region Fields

    private string _serialStringData = string.Empty;
    private bool _isConnected;
    private double _dataRate;
    private int _messageCount = 0;
    private readonly Stopwatch _hzStopwatch = new();
    
    private readonly SerialStringManager _serialStringManager = new(1000);
    public delegate Task SendSerialDataDelegate(string data);

    #endregion
    
    #region Properties

    public string SerialStringData
    {
        get => _serialStringData;
        private set => this.RaiseAndSetIfChanged(ref _serialStringData, value);
    }
    
    public bool IsConnected
    {
        get => _isConnected;
        private set => this.RaiseAndSetIfChanged(ref _isConnected, value);
    }
    
    public double DataRate
    {
        get => _dataRate;
        private set => this.RaiseAndSetIfChanged(ref _dataRate, value);
    }

    #endregion
    
    #region Initialize Method
    
    /// <summary>
    /// Init Serial Setting
    /// </summary>
    /// <returns><see cref="SerialSettings"/></returns>
    private SerialSettings InitializeSerialSettings()
    {
        var settings = new SerialSettings();
        
        // 기본 설정값 표기
        DefaultComPortList = settings.RadioComPortItems;

        return settings;
    }
    
    /// <summary>
    /// 시리얼 디바이스를 초기화 합니다.
    /// 기본 연결 설정값을 초기화 하여 전달하고, 연결 디바이스를 생성합니다.
    /// </summary>
    /// <param name="settings"><see cref="SerialSettings"/> 설정 값이 필요합니다.</param>
    /// <returns><see cref="SerialDevice"/> 반환</returns>
    private SerialDevice InitializeSerialDevice(SerialSettings settings)
    {
        var device = new SerialDevice(settings);
        device.GetPortPaths();
        return device;
    }
    
    /// <summary>
    /// 시리얼 연결 디바이스에 대한 데이터처리에 대한 스트림을 생성합니다.
    /// 데이터 수신 -> 데이터 처리 -> UI 업데이트를 수행합니다.
    /// </summary>
    private void InitializeSerialDataStream()
    {
        var serialDataStream = Observable.FromEventPattern<EventHandler<SerialMessage>, SerialMessage>(
                h => _serialDevice.MessageReceived += h,
                h => _serialDevice.MessageReceived -= h)
            .Select(x => x.EventArgs)
            .Do(UpdateDataRate);
        
        serialDataStream
            .Buffer(TimeSpan.FromMilliseconds(16.67))
            .Where(messages => messages.Count > 0)
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Select(ProcessMessages)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(UpdateUi);
    }
    
    /// <summary>
    /// 사용자 명령에 대한 처리를 초기화 합니다.
    /// </summary>
    private void InitializeSerialCommands()
    {
        // 기본메뉴 커맨드
        ConnectCommand = ReactiveCommand.Create(ConnectSerialPort);
        ReScanCommand = ReactiveCommand.Create(ReScanSerialPort);
        
        // 옵션 설정 커맨드
        ComPortRadioChangedCommand = ReactiveCommand.Create<object>(ComPortRadio_Clicked);
        SerialSettingChangedCommand = ReactiveCommand.Create<object>(SerialSettingRadio_Clicked);
        EncodingBytesChangedCommand = ReactiveCommand.Create<string>(EncodingByteRadio_Clicked);
        
        SendSerialDataCommand = ReactiveCommand.CreateFromTask<string>(SendSerialDataAsync_Clicked);
        OpenMacroWindowCommand = ReactiveCommand.Create(OpenMacroWindowAsync_Clicked);
        ReadTypeChangedCommand = ReactiveCommand.Create<string>(ReadTypeChanged_Clicked);
    }

    #endregion
    
    #region Commands
    
    public ICommand ConnectCommand { get; set; } = null!;
    public ICommand ReScanCommand { get; set; } = null!;
    
    public ICommand ComPortRadioChangedCommand { get; private set; } = null!;
    public ICommand SerialSettingChangedCommand { get; private set; } = null!;
    public ICommand EncodingBytesChangedCommand { get; private set; } = null!;
    public ICommand SendSerialDataCommand { get; private set; } = null!;
    public ICommand OpenMacroWindowCommand { get; private set; } = null!;
    public ICommand ReadTypeChangedCommand { get; private set; } = null!;

    #endregion
    
    #region Command Method
    
    /// <summary>
    /// Scan Serial Port Path
    /// </summary>
    private void ReScanSerialPort()
    {
        _serialDevice.GetPortPaths();
    }
    
    
    /// <summary>
    /// Connect Serial
    /// </summary>
    private void ConnectSerialPort()
    {
        if (_serialDevice.Connect())
        {
            IsConnected = _serialDevice.IsConnected;
        }
        else
        {
            _serialDevice.Disconnect();
            IsConnected = _serialDevice.IsConnected;
        }
    }
    
    
    /// <summary>
    /// Set Serial Port
    /// </summary>
    /// <param name="o">Selected Serial Port Radio Item or Serial port path</param>
    private void ComPortRadio_Clicked(object o)
    {
        if (o is OptionRadioItem item)
        {
            _serialDevice.SetPortPath(_serialDevice.SerialPortList[item.Value]);
        }
        else if (o is string s)
        {
            _serialDevice.SetPortPath(s);
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
                SerialSettings.BaudRate = baudRate;
                break;
            case ParityType parity:                     // Parity
                SerialSettings.Parity = parity;
                break;
            case DataBitsType dataBits:                 // DataBits
                SerialSettings.DataBits = dataBits;
                break;
            case StopBitsType stopBits:                 // StopBits
                SerialSettings.StopBits = stopBits;
                break;
            default:                                    // Default
                Debug.WriteLine($"Serial Setting Object: {setting} / Type: {setting?.GetType()}");
                break;
        }
        
        this.RaisePropertyChanged(nameof(SerialSettings));
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
                _serialStringManager.ChangeFormat(EncodingBytes.ASCII);
                break;
            case "HEX":
                _serialStringManager.ChangeFormat(EncodingBytes.HEX);
                break;
            case "UTF8":
                _serialStringManager.ChangeFormat(EncodingBytes.UTF8);
                break;
        }

        SerialStringData = _serialStringManager.GetCurrentString();
    }
    
    
    /// <summary>
    /// Send Serial Data 
    /// </summary>
    /// 
    private async Task SendSerialDataAsync_Clicked(string data)
    {
        if(string.IsNullOrEmpty(data)) return;
        if (!IsConnected) return;
        
        //TODO: String으로 들어온 Hex데이터를 처리해서 Byte로 넘길 수 있도록 수정 필요
        var byteData = Encoding.UTF8.GetBytes(data);
        if (await _serialDevice.WriteAsync(byteData))
        {
            // Serial Write Success
        }
    }


    private void OpenMacroWindowAsync_Clicked()
    {
        var macroWindow = new MacroView
        {
            DataContext = new MacroViewModel(SendSerialDataAsync_Clicked)
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
        _serialDevice.CurrentMode = type switch
        {
            "NewLine" => ReadMode.NewLine,
            "STX_ETX" => ReadMode.STX_ETX,
            "Custom" => ReadMode.Custom,
            _ => _serialDevice.CurrentMode
        };
    }
    
    
    #endregion
    
    #region Serial DataStream Function
    
    /// <summary>
    /// Process the message collected during the buffer
    /// </summary>
    /// <param name="messages"><see cref="IList{SerialMessage}"/> Message List</param>
    /// <returns>Combined <see cref="string"/> Output</returns>
    private string ProcessMessages(IList<SerialMessage> messages)
    {
        foreach (var message in messages)
        {
            _serialStringManager.Add(message);
        }
        
        return _serialStringManager.GetCurrentString();
    }
    
    
    /// <summary>
    /// Update UI
    /// </summary>
    /// <param name="result">Show <see cref="string"/> Data</param>
    private void UpdateUi(string result)
    {
        SerialStringData = result;
    }
    
    
    /// <summary>
    /// Message Received Rate Updater
    /// </summary>
    /// <param name="message"><see cref="SerialMessage"/> Message Type</param>
    private void UpdateDataRate(SerialMessage message)
    {
        _messageCount++;

        if (!_hzStopwatch.IsRunning)
        {
            _hzStopwatch.Start();
        }
        else if (_hzStopwatch.ElapsedMilliseconds >= 1000)
        {
            DataRate = _messageCount / (_hzStopwatch.ElapsedMilliseconds / 1000.0);
            _messageCount = 0;
            _hzStopwatch.Restart();
        }
    }

    #endregion
    
    #endregion
    
}