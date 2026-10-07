using System.Diagnostics;
using UTerminal.Models.Serial;

// SerialPortAdapter 수신 루프 점검용 수동 도구.
// args[0]: 앱이 여는 포트, args[1]: 반대편 포트(데이터를 써 넣을 경로)
// 1) 데이터 없이 연결을 유지한 1초 동안의 프로세스 CPU 시간
// 2) 연결 → 데이터 수신 → 해제를 3회 반복했을 때의 누적 수신 바이트
var config = new SerialConnectionConfiguration { PortName = args[0] };
var adapter = new SerialPortAdapter(config);
var received = 0;
adapter.SubscribeRawData(m => Interlocked.Add(ref received, m.DataSize));

Console.WriteLine($"open={adapter.Open()}");
await Task.Delay(2000);
var process = Process.GetCurrentProcess();
var before = process.TotalProcessorTime;
await Task.Delay(1000);
process.Refresh();
Console.WriteLine($"idle_cpu_ms_per_1s={(process.TotalProcessorTime - before).TotalMilliseconds:F0}");
adapter.Close();

for (var round = 1; round <= 3; round++)
{
    adapter.Open();
    await Task.Delay(200);
    await File.WriteAllBytesAsync(args[1], new byte[50]);
    await Task.Delay(300);
    adapter.Close();
    Console.WriteLine($"round{round} received_total={received} (expected {round * 50})");
    await Task.Delay(200);
}
