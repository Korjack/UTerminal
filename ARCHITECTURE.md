# UTerminal — Architecture

## 전체 구조 개요

```
UTerminal (Class Library)
├── Views/                    # Avalonia XAML 뷰
│   ├── Components/           # 재사용 패널 컴포넌트
│   ├── MainWindow.axaml      # 루트 윈도우
│   ├── MainView.axaml        # 메인 화면
│   ├── PresetModeView.axaml  # 프리셋 파싱 화면
│   └── MacroView.axaml       # 매크로 관리 창
├── ViewModels/               # ReactiveUI ViewModel
│   ├── MainViewModel.cs
│   ├── PresetModeViewModel.cs
│   └── MacroViewModel.cs
├── Models/
│   ├── Serial/               # 시리얼 포트 연결 계층
│   ├── Messages/             # 메시지 처리 계층
│   ├── Parser/               # 커스텀 패킷 파싱 계층
│   ├── Monitoring/           # 메시지 수신율 모니터링
│   ├── PortManager/          # 포트 탐색 및 선택
│   ├── Formatters/           # 바이트 → 문자열 포맷팅
│   └── Utils/                # 로거, 매크로 설정
├── Behaviors/                # Avalonia Behavior (AvaloniaEdit 바인딩)
├── Converter/                # XAML Value Converter
└── Styles/                   # 공용 컨트롤 스타일

UTerminal.Desktop (Executable)
└── Program.cs                # AppBuilder 진입점
```

---

## 계층별 설명

### 1. Serial 계층 (`Models/Serial/`)

시리얼 포트의 물리적 연결을 담당한다.

```
ISerialPort (인터페이스)
    └── SerialPortAdapter         ← System.IO.Ports.SerialPort 래퍼
            - Open() / Close()
            - WriteAsync(byte[])
            - SubscribeRawData()  ← 구독자 패턴으로 원시 데이터 브로드캐스트
            - StartReading()      ← 백그라운드 Task에서 루프 실행

ISerialService (인터페이스)
    └── SerialService             ← 고수준 연결 관리
            - Connect() / Disconnect()
            - WriteAsync(string)  ← SerialDataParser로 문자열 → bytes
            - OnRawDataReceived() ← ReadMode에 따라 패킷 경계 처리
            - MsgReceived (event) ← 완성된 ISerialMessage 발행
```

**ReadMode 처리 흐름 (`SerialService.OnRawDataReceived`):**

| ReadModeType | 동작 |
|---|---|
| `NewLine` | CR+LF 기준으로 패킷 구분 |
| `StxEtx` | 0x02(STX) ~ 0x03(ETX) + PacketSize 조건 충족 시 발행 |
| `Custom` | 사용자 정의 STX/ETX 바이트 사용 |

**설정 모델:**
- `SerialConnectionConfiguration` — PortName, BaudRate, Parity, DataBits, StopBits (연결 전 변경 가능)
- `SerialRuntimeConfiguration` — ReadMode, PacketSize, CustomStx/Etx (연결 중 변경 가능)

---

### 2. Messages 계층 (`Models/Messages/`)

수신된 원시 패킷을 버퍼링하고 화면 출력용 문자열로 변환한다.

```
ISerialMessage
    └── SerialMessage              ← Data(byte[]), DataSize, Timestamp, MessageType

IBufferManager<ISerialMessage>
    └── CircularBufferManager      ← 최대 N개 메시지 순환 저장

IMessageProcessor<ISerialMessage>
    └── SerialMsgProcessor
            - ProcessMessage()     ← 버퍼에 추가 + 로깅 + UI 이벤트 발행
            - ChangeFormat()       ← ASCII / HEX / UTF-8 전환
            - BufferUpdated (event)← 포맷된 전체 버퍼 문자열 발행
```

---

### 3. Parser 계층 (`Models/Parser/`)

프리셋 모드 전용. 사용자가 정의한 필드 구조에 따라 바이너리 패킷을 파싱한다.

```
IParsePreset
    └── ParsePreset                ← 프리셋 이름 + ParseData 목록
            ParseDataList: ObservableCollection<IParseData>

IParseData
    └── ParseData                  ← 필드 이름, ParseDataType, Length, ParsedValue
                                      LinkData: 가변길이 필드 연결 참조

SerialPresetParser
    └── ParseMessage(ISerialMessage)
            - 각 ParsePreset을 순서대로 파싱 시도
            - ParsePreset.GetTotalLength() 캐싱으로 길이 체크 우선 처리
```

**지원 데이터 타입 (`ParseDataType`):**
STX, ETX, Int8, UInt8, Int16, UInt16, Int32, UInt32, Float, Double, Byte(hex), String

**가변길이 필드:** `ParseData.LinkData`에 길이 필드를 참조 연결하여 런타임에 크기를 결정한다.

---

### 4. Monitoring 계층 (`Models/Monitoring/`)

```
IMessageRateMonitor
    └── MessageRateMonitor
            - RegisterMessage()    ← 메시지 수신 시 호출
            - CurrentRate (Hz)     ← 100ms 간격으로 MainViewModel이 폴링
```

---

### 5. PortManager (`Models/PortManager/`)

```
PortManager
    └── ScanPort()         ← 사용 가능한 시리얼 포트 목록 갱신
        SelectPort()       ← 목록에서 포트 선택
        CustomSelectPort() ← 직접 경로 입력
        PortList: ObservableCollection<PortInfo>
```

---

### 6. Formatters (`Models/Formatters/`)

```
MessageFormatter
    └── FormatMessages(IEnumerable<ISerialMessage>, EncodingBytes)
        FormatData(Span<byte>, EncodingBytes)
        ← ASCII / HEX / UTF-8 변환 + 타임스탬프 결합

TimeFormatter         ← 타임스탬프 → 문자열 포맷
```

---

### 7. Utils (`Models/Utils/`)

```
SystemLogger    (Singleton) ← log4net 기반, 시스템 이벤트 파일 로그
SerialMsgLogger (Singleton) ← 수신 시리얼 데이터 전용 파일 로그
MacroItems / MacroSettings  ← 매크로 항목 모델
```

로그 저장 위치: `%AppData%/UTerminal/` (OS 공통 `SpecialFolder.ApplicationData`)

---

## 데이터 흐름

### 메인 화면 수신 흐름

```
[시리얼 포트 하드웨어]
        │ 원시 바이트
        ▼
SerialPortAdapter.StartReading()        ← 백그라운드 Task
        │ SerialMessage (byte[])
        ▼
SerialPortAdapter.BroadcastRawData()    ← ReaderWriterLockSlim 보호
        │
        ▼
SerialService.OnRawDataReceived()       ← ReadMode에 따라 패킷 구분
        │ 완성된 ISerialMessage
        ▼
SerialService.MsgReceived (event)       ← Task.Run으로 비동기 발행
        │
        ▼
MainViewModel.InitializeSerialDataStream()
  Observable.Buffer(16.67ms)            ← 60fps 배치 처리
        │
        ▼
SerialMsgProcessor.ProcessMessage()     ← TaskpoolScheduler
  CircularBufferManager.Add()
  BufferUpdated (event)
        │
        ▼
MainViewModel.ReceivedSerialData        ← MainThreadScheduler → UI 바인딩
```

### 프리셋 파싱 흐름

```
SerialPortAdapter.BroadcastRawData()
        │ SerialMessage (원시, 패킷 경계 없음)
        ▼
PresetModeViewModel.OnRawDataReceived()
        │
        ▼
SerialPresetParser.ParseMessage()
  → 등록된 모든 ParsePreset을 순서대로 시도
  → 각 ParseData 필드를 offset 기반으로 파싱
  → ParseData.ParsedValue 업데이트 (UI 바인딩)
```

---

## ViewModel 구성

### MainViewModel

- `SerialConnectionConfiguration` / `SerialRuntimeConfiguration` — 설정 바인딩 대상
- `PortManager` — 포트 목록 UI 바인딩
- `ReceivedSerialData` — 수신 텍스트 (AvaloniaEdit 바인딩)
- `MessageRate` — 수신 Hz 표시
- `SelectFolderInteraction` — 폴더 선택 다이얼로그 (View → ViewModel)

### PresetModeViewModel

- `SerialPresetParser` — 프리셋 목록 및 파싱 결과 바인딩
- `CurrentPreset` — 현재 선택된 프리셋
- `FilteredPreset` — 검색 결과 (300ms Throttle)
- `ShowFilePickerInteraction` — 파일 선택 다이얼로그
- 파싱 통계: `TotalReceivedCount`, `SuccessParseCount`, `FailedParseCount`

### MacroViewModel

- `SendSerialDataCommand` — MainViewModel에서 주입받음
- 매크로 항목 목록 관리

---

## 주요 설계 결정

| 결정 | 이유 |
|---|---|
| `SerialPortAdapter`에서 구독자 배열(`Action[]`)을 직접 관리 | lock-free 읽기를 위한 copy-on-write 배열, `ReaderWriterLockSlim`으로 쓰기 보호 |
| `Observable.Buffer(16.67ms)` 배치 처리 | 고속 수신 시 UI 갱신을 60fps로 제한하여 렌더링 부하 감소 |
| `CircularBufferManager` | 오래된 메시지를 자동 폐기하여 메모리 상한 보장 |
| `SerialPresetParser._cachedTotalLength` | 패킷 크기 계산 결과 캐싱으로 반복 LINQ 제거 |
| `MemoryMarshal.Read<T>` 사용 | struct 타입 파싱 시 복사 없이 직접 읽기 |
| 프리셋 저장 형식: JSON | `System.Text.Json` 직렬화, `ParsePreset` 클래스 직접 매핑 |
