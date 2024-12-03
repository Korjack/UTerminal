using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using ReactiveUI;
using UTerminal.Models;

namespace UTerminal.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly SerialDevice _serialDevice;
    
    // 기본값 설정
    public ObservableCollection<OptionRadioItem> DefaultComPortList { get; }
    public ObservableCollection<OptionRadioItem> DefaultBaudRateList { get; }
    public ObservableCollection<OptionRadioItem> DefaultDatabitsList { get; }
    public ObservableCollection<OptionRadioItem> DefaultParityList { get; }
    public ObservableCollection<OptionRadioItem> DefaultStopBitsList { get; }
    
    // Command 설정
    public ICommand QuitCommand { get; set; }
    public ICommand ReScanCommand { get; set; }
    public ICommand ComPortRadioChangedCommand { get; set; }
    public ICommand ConnectCommand { get; set; }

    public ObservableCollection<string> DataList
    {
        get => _dataList;
        set => this.RaiseAndSetIfChanged(ref _dataList, value);
    }
    private ObservableCollection<string> _dataList = new();
    

    public MainViewModel()
    {
        // 시리얼 설정 및 객체 초기화
        var settings = new SerialSettings();
        _serialDevice = new SerialDevice(settings);
        _serialDevice.GetPortNames();
        

        // 기본 설정값 표기
        DefaultComPortList = settings.RadioComPortItems;
        DefaultBaudRateList = settings.RadioBaudRateItems;
        DefaultDatabitsList = settings.RadioDataBitsItems;
        DefaultParityList = settings.RadioParityItems;
        DefaultStopBitsList = settings.RadioStopBitsItems;
        
        // 커맨드 초기화
        QuitCommand = ReactiveCommand.Create(QuitProgram);
        ReScanCommand = ReactiveCommand.Create(ReScanSerialPort);
        ComPortRadioChangedCommand = ReactiveCommand.Create<OptionRadioItem>(ComPortRadio_IsCheckedChanged);
        ConnectCommand = ReactiveCommand.Create(ConnectSerialPort);
    } 

    
    /// <summary>
    /// 프로그램을 종료합니다.
    /// </summary>
    void QuitProgram()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            desktopLifetime.Shutdown();
        }
    }

    /// <summary>
    /// 컴퓨터의 시리얼 포트 목록을 가져오고 반영합니다.
    /// </summary>
    void ReScanSerialPort()
    {
        _serialDevice.GetPortNames();
    }


    void ComPortRadio_IsCheckedChanged(OptionRadioItem item)
    {
        if (item.IsSelected)
        {
            Console.WriteLine(item.ToString());
        }
    }


    void ConnectSerialPort()
    {
        
    }
}