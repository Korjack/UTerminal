using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using ReactiveUI;

namespace UTerminal.Models;

public class PortManager : ReactiveObject
{
    private readonly SerialConnectionConfiguration _connectionConfig;
    private readonly int _maxPort;
    private PortInfo? _selectedPort;
    
    public ObservableCollection<PortInfo> AvailablePorts { get; }

    public PortInfo? SelectedPort
    {
        get => _selectedPort;
        set => this.RaiseAndSetIfChanged(ref _selectedPort, value);
    }

    public PortManager(SerialConnectionConfiguration connectionConfig, int maxPort = 10)
    {
        _connectionConfig = connectionConfig;
        _maxPort = maxPort;
        AvailablePorts = new ObservableCollection<PortInfo>();
        
        ScanPort();
    }

    public void ScanPort()
    {
        var portNames = SerialPort.GetPortNames();
        
        AvailablePorts.Clear();

        foreach (var portName in portNames)
        {
            AvailablePorts.Add(new PortInfo(portName));
        }

        for (int i = portNames.Length; i < _maxPort; i++)
        {
            AvailablePorts.Add(new PortInfo($"{i}", false));
        }

        if (_selectedPort != null)
        {
            var existingPort = AvailablePorts.FirstOrDefault(p => p.Name == _selectedPort.Name);
            if (existingPort != null && existingPort.IsEnabled)
            {
                SelectPort(existingPort.Name);
            }
        }
        else
        {
            // Select first available port if exists
            var firstAvailablePort = AvailablePorts.FirstOrDefault(p => p.IsEnabled);
            if (firstAvailablePort != null)
            {
                firstAvailablePort.IsSelected = true;
                _selectedPort = firstAvailablePort;
            }
        }
    }

    public void SelectPort(string portName)
    {
        if (_selectedPort != null)
        {
            if(_selectedPort.Name == portName) return;
        
            _selectedPort.IsSelected = false;
        }

        var newSelection = AvailablePorts.FirstOrDefault(p => p.Name == portName);
        if (newSelection != null && newSelection.IsEnabled)
        {
            newSelection.IsEnabled = true;
            SelectedPort = newSelection;
            _connectionConfig.PortName = newSelection.Name;
        }
    }

    public void CustomSelectPort(string portName)
    {
        var newPort = new PortInfo(portName);
        SelectedPort = newPort;
        _connectionConfig.PortName = newPort.Name;
    }
}