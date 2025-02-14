using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using ReactiveUI;
using UTerminal.Models.Serial;

namespace UTerminal.Models.PortManager;

public class PortManager : ReactiveObject
{
    private readonly SerialConnectionConfiguration _connectionConfig;
    private readonly int _maxPort;
    private PortInfo? _selectedPort;
    
    /// <summary>
    /// List of connectable ports read from the system
    /// </summary>
    public ObservableCollection<PortInfo> AvailablePorts { get; }

    /// <summary>
    /// Currently selected port information
    /// </summary>
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
    
    /// <summary>
    /// Get Port Name and Update <see cref="AvailablePorts"/>.
    /// </summary>
    public void ScanPort()
    {
        var portNames = SerialPort.GetPortNames();
        
        AvailablePorts.Clear();

        // Add port name at AvailablePorts
        foreach (var portName in portNames)
        {
            AvailablePorts.Add(new PortInfo(portName));
        }

        // If not full AvailablePorts, fill empty port
        for (int i = portNames.Length; i < _maxPort; i++)
        {
            AvailablePorts.Add(new PortInfo($"{i}", false));
        }
        
        // If cant find any port
        if (AvailablePorts.All(x => x.IsEnabled)) return;
        
        // Select first available port if exists
        // When the port list is refreshed with an empty state, set it to select the first one.
        var firstAvailablePort = AvailablePorts.FirstOrDefault(p => p.IsEnabled);
        if (firstAvailablePort != null)
        {
            firstAvailablePort.IsSelected = true;
            SelectedPort = firstAvailablePort;
        }
    }

    /// <summary>
    /// Select port by port path name
    /// </summary>
    /// <param name="portName">[<see cref="string"/>] port path</param>
    public void SelectPort(string portName)
    {
        if (SelectedPort != null)
        {
            if(SelectedPort.Name == portName) return;
        
            SelectedPort.IsSelected = false;
        }

        // Select port from AvailablePorts
        var newSelection = AvailablePorts.FirstOrDefault(p => p.Name == portName);
        if (newSelection != null && newSelection.IsEnabled)
        {
            newSelection.IsEnabled = true;
            SelectedPort = newSelection;
            _connectionConfig.PortName = newSelection.Name;
        }
    }

    /// <summary>
    /// If input custom port path, create port info
    /// </summary>
    /// <param name="portName">[<see cref="string"/>] port path</param>
    /// <remarks>See <see cref="PortInfo"/></remarks>
    public void CustomSelectPort(string portName)
    {
        var newPort = new PortInfo(portName);
        SelectedPort = newPort;
        _connectionConfig.PortName = newPort.Name;
    }
}