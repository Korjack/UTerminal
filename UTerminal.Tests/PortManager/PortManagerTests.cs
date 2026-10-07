using UTerminal.Models.Serial;
using PortManagerModel = UTerminal.Models.PortManager.PortManager;

namespace UTerminal.Tests.PortManager;

public class PortManagerTests
{
    [Fact(Skip = "A8 수정 전")]
    public void CustomSelectPort_WithNoPreviousSelection_SetsPortName()
    {
        var config = new SerialConnectionConfiguration();
        var manager = new PortManagerModel(config) { SelectedPort = null };

        manager.CustomSelectPort("/dev/ttyCUSTOM");

        Assert.Equal("/dev/ttyCUSTOM", config.PortName);
    }

    [Fact(Skip = "A8 수정 전")]
    public void SelectPort_WithNoPreviousSelection_SetsPortName()
    {
        var config = new SerialConnectionConfiguration();
        var manager = new PortManagerModel(config, maxPort: 1);
        var enabled = manager.AvailablePorts.FirstOrDefault(p => p.IsEnabled);
        if (enabled is null) return; // 이 환경에 시리얼 포트가 없으면 검증할 대상이 없다
        manager.SelectedPort = null;

        manager.SelectPort(enabled.Name);

        Assert.Equal(enabled.Name, config.PortName);
    }

    [Fact(Skip = "A9 수정 전")]
    public void CustomSelectPort_EmptyPath_KeepsPreviousPortName()
    {
        var config = new SerialConnectionConfiguration();
        using var _ = new SerialPortAdapterScope(config);
        var manager = new PortManagerModel(config);
        var before = config.PortName;

        manager.CustomSelectPort("");

        Assert.Equal(before, config.PortName);
    }

    /// <summary>
    /// 앱과 같이 SerialPortAdapter가 설정 변경을 구독한 상태를 만든다.
    /// </summary>
    private sealed class SerialPortAdapterScope(SerialConnectionConfiguration config) : IDisposable
    {
        private readonly SerialPortAdapter _adapter = new(config);
        public void Dispose() => _adapter.Close();
    }
}
