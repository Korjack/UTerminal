﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using Avalonia.ReactiveUI;
using ReactiveUI;
using UTerminal.Models;

namespace UTerminal.ViewModels;

public class MainViewModel : ViewModelBase
{
    #region 변수 영역

    #region Init Serial Device
    
    private readonly SerialDevice _serialDevice;
    public SerialSettings SerialSettings => _serialDevice.SerialSettings;
    
    // 시리얼 데이터 바인딩 변수
    private string _receivedData = string.Empty;
    public string ReceivedData
    {
        get => _receivedData;
        private set => this.RaiseAndSetIfChanged(ref _receivedData, value);
    }

    // 시리얼 연결 여부 바인딩 변수
    private bool _isConnected;
    public bool IsConnected
    {
        get => _isConnected;
        private set => this.RaiseAndSetIfChanged(ref _isConnected, value);
    }
    
    // 시리얼 데이터 뷰어 스트링
    private readonly StringBuilder _textBuilder = new();
    
    // Hz를 계산하여 바인딩할 속성
    private double _dataRate;
    public double DataRate
    {
        get => _dataRate;
        private set => this.RaiseAndSetIfChanged(ref _dataRate, value);
    }
    private int _messageCount = 0;
    
    #endregion
    
    
    #region Init Default Value
    
    public ObservableCollection<OptionRadioItem> DefaultComPortList { get; private set; }
    public Array BaudRatesOption => Enum.GetValues(typeof(BaudRateType));
    public Array ParitysOption => Enum.GetValues(typeof(ParityType));
    public Array DataBitsOption => Enum.GetValues(typeof(DataBitsType));
    public Array StopBitsOption => Enum.GetValues(typeof(StopBitsType));
    
    #endregion
    
    
    #region Init Command

    #region 기본메뉴 커맨드

    public ICommand QuitCommand { get; set; }
    public ICommand ReScanCommand { get; set; }
    public ICommand ConnectCommand { get; set; }

    #endregion

    #region 옵션 설정 커맨드

    public ICommand ComPortRadioChangedCommand { get; private set; }        // 포트 연결
    public ICommand SerialSettingChangedCommand { get; private set; }       // 시리얼 설정 라디오 버튼 변경 커멘드

    #endregion
    
    #endregion
    
    private readonly Stopwatch _stopwatch = new();

    #endregion
    

    #region 생성 및 초기화

    public MainViewModel()
    {
        var setting = InitializeSerialSettings();
        _serialDevice = InitializeSerialDevice(setting);
        InitializeSerialDataStream();
        InitializeCommands();
    }
    
    /// <summary>
    /// 시리얼 기본 설정값을 초기화 합니다.
    /// </summary>
    /// <returns><see cref="SerialSettings"/>을 반환합니다.</returns>
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
            .Select(x => x.EventArgs);

        serialDataStream
            .Buffer(TimeSpan.FromMilliseconds(100))
            .Where(messages => messages.Count > 0)
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Do(UpdateDataRate)
            .Select(ProcessMessages)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(UpdateUI);
    }
    
    /// <summary>
    /// 사용자 명령에 대한 처리를 초기화 합니다.
    /// </summary>
    private void InitializeCommands()
    {
        // 기본메뉴 커맨드
        QuitCommand = ReactiveCommand.Create(QuitProgram);
        ReScanCommand = ReactiveCommand.Create(ReScanSerialPort);
        ConnectCommand = ReactiveCommand.Create(ConnectSerialPort);
        
        // 옵션 설정 커맨드
        ComPortRadioChangedCommand = ReactiveCommand.Create<OptionRadioItem>(ComPortRadio_Clicked);
        SerialSettingChangedCommand = ReactiveCommand.Create<object>(SerialSettingRadio_Clicked);
    }


    #endregion
    
    
    #region 커맨드 함수 영역
    
    /// <summary>
    /// 프로그램을 종료합니다.
    /// </summary>
    private void QuitProgram()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            desktopLifetime.Shutdown();
        }
    }

    /// <summary>
    /// 컴퓨터의 시리얼 포트 목록을 가져오고 반영합니다.
    /// </summary>
    private void ReScanSerialPort()
    {
        _serialDevice.GetPortPaths();
    }
    
    /// <summary>
    /// 시리얼 연결
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
        
        Debug.WriteLine($"Serial Connect Status: {IsConnected}");
    }
    
    
    
    private void ComPortRadio_Clicked(OptionRadioItem item)
    {
        _serialDevice.SetPortPath(_serialDevice.SerialPortList[item.Value]);
    }

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
    
    #endregion


    #region 시리얼 데이터 스트림 처리 함수

    /// <summary>
    /// 메시지 데이터를 처리합니다.
    /// </summary>
    /// <param name="messages"><see cref="IList{SerialMessage}"/> 메시지 리스트</param>
    /// <returns><see cref="StringBuilder"/>의 ToString 결과</returns>
    private string ProcessMessages(IList<SerialMessage> messages)
    {
        foreach (var message in messages)
        {
            string s = Encoding.ASCII.GetString(message.Data) + Environment.NewLine;
            _textBuilder.Append(s);
            
            if (_textBuilder.Length > 10000)
            {
                _textBuilder.Remove(0, s.Length);
            }
        }
        
        return _textBuilder.ToString();
    }

    /// <summary>
    /// UI를 업데이트합니다.
    /// </summary>
    /// <param name="result">최종 Text 결과물 업데이트</param>
    private void UpdateUI(string result)
    {
        ReceivedData = result;
    }
    
    /// <summary>
    /// 메시지 수신 속도 업데이트
    /// </summary>
    /// <param name="messages"><see cref="IList{SerialMessage}"/> 메시지 목록</param>
    private void UpdateDataRate(IList<SerialMessage> messages)
    {
        _messageCount += messages.Count;

        if (!_stopwatch.IsRunning)
        {
            _stopwatch.Start();
        }
        else if (_stopwatch.ElapsedMilliseconds >= 1000)
        {
            DataRate = _messageCount / (_stopwatch.ElapsedMilliseconds / 1000.0);
            _messageCount = 0;
            _stopwatch.Restart();
        }
    }

    #endregion
    
}