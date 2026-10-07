# C# 코드 리뷰 체크리스트

## 목표

`UTerminal/` C# 코드에서 확인된 문제를 기록하고 고친다. 품질 목표(`ARCHITECTURE.md` 1.2)의 1순위인 안정성을 해치는 문제를 우선한다.

## 방침

- **A 항목(자동 수정)**: 동작이 틀렸다는 것이 코드와 테스트로 드러나는 문제. 실패하는 테스트로 재현한 뒤 고치고, 항목마다 커밋한다.
- **D 항목(결정 필요)**: 고치는 방법에 따라 사용자가 보는 동작이나 파일 형식이 달라지는 문제. 선택지와 추천안만 기록하고, 유지보수자가 방향을 정한 뒤 고친다.
- 검증은 `UTerminal.Tests`(xUnit)로 한다. 하드웨어가 필요한 항목은 `socat` 가상 포트로 확인한다.

```bash
dotnet test UTerminal.Tests
```

---

## A. 자동 수정 항목

### - [x] A0. `SystemLogger`가 Avalonia 앱 밖에서 생성되지 않음

- **문제**: `SystemLogPath`가 `App.Current.Name`을 null 검사 없이 읽는다(`UTerminal/Models/Utils/Logger/SystemLogger.cs:17`). Avalonia `Application`이 없으면 `NullReferenceException`이 난다. `SystemLogger.Instance`를 필드에서 초기화하는 `SerialService`, `PortManager` 등을 테스트에서 만들 수 없다.
- **수정**: `App.Current?.Name ?? "UTerminal"`. 앱 실행 시 `App.Current.Name`은 `"UTerminal"`(`UTerminal/App.axaml:6`)이므로 로그 경로는 같다.
- **검증**: `SerialServiceTests`, `PortManagerTests`가 생성 단계에서 NRE 없이 실행된다.

### - [ ] A1. 연결 중 수신 루프가 CPU 코어 하나를 계속 사용

- **문제**: `StartReading`은 `BytesToRead`가 0이어도 대기 없이 반복한다(`UTerminal/Models/Serial/SerialPortAdapter.cs:173-193`). `socat` 가상 포트에 연결만 하고 데이터를 보내지 않은 1초 동안 프로세스 CPU 시간이 `1020ms`였다.
- **수정**: 읽을 바이트가 없으면 `await Task.Delay(1, token)`. 읽기 방식(`BytesToRead`만큼 읽기)은 바꾸지 않는다.
- **검증**: 같은 `socat` 측정에서 CPU 시간이 크게 줄어든다. `SerialService` 테스트 통과.

### - [ ] A2. 수신 패킷이 화면에 뒤섞인 순서로 표시될 수 있음

- **문제**: `RaiseMessageReceived`가 패킷마다 `Task.Run`으로 핸들러를 따로 실행한다(`UTerminal/Models/Serial/SerialService.cs:197-210`). 실행 순서가 보장되지 않아, 2000개를 순서대로 넣었을 때 받은 순서가 달랐다(`SerialServiceTests.MsgReceived_PreservesPacketArrivalOrder` 실패: `Collections differ`). 여러 스레드가 동시에 Rx 파이프라인에 값을 넣는 문제도 같은 원인이다.
- **수정**: `MsgReceived`를 수신 루프 스레드에서 동기 호출한다. 구독자인 `MainViewModel`의 Rx `Buffer`는 값을 목록에 넣기만 하므로 수신 루프를 오래 막지 않는다.
- **검증**: `MsgReceived_PreservesPacketArrivalOrder` 통과.

### - [ ] A3. 배치마다 버퍼 전체 포맷·UI 갱신이 메시지 수만큼 반복됨

- **문제**: `MainViewModel.ProcessMessages`가 배치의 메시지마다 `ProcessMessage`를 호출하고(`UTerminal/ViewModels/MainViewModel.cs:134-139`), `ProcessMessage`는 호출될 때마다 순환 버퍼 전체(최대 1024개)를 포맷해 `BufferUpdated`를 발생시킨다(`UTerminal/Models/Messages/SerialMsgProcessor.cs:30-52`). 16.67ms 배치 하나에 메시지가 N개면 전체 포맷과 UI 갱신도 N번 일어난다.
- **수정**: `SerialMsgProcessor.ProcessMessages(IEnumerable<ISerialMessage>)`를 추가한다. 모두 버퍼에 넣은 뒤 `BufferUpdated`를 한 번만 발생시키고, `MainViewModel`이 이 메서드를 호출한다.
- **검증**: 메시지 N개를 한 번에 처리하면 `BufferUpdated`가 1번 발생하는 테스트.

### - [ ] A4. `BufferUpdated` 구독 해지 핸들러가 `+=`

- **문제**: `FromEventPattern`의 remove 핸들러가 `h => _serialMsgProcessor.BufferUpdated += h`다(`UTerminal/ViewModels/MainViewModel.cs:114`). 구독을 해지하면 핸들러가 오히려 한 번 더 붙는다. 지금은 이 구독을 해지하는 곳이 없어 증상은 없다.
- **수정**: `-=`.
- **검증**: 빌드. 한 줄 수정이라 테스트를 두지 않는다.

### - [ ] A5. 가변 길이 필드의 길이 타입이 UInt8일 때만 동작

- **문제**: 길이 값을 `(byte)parseData.LinkData.ParsedValue`로 unboxing한다(`UTerminal/Models/Parser/SerialPresetParser.cs:110`). UI는 길이 타입으로 UInt8·UInt16·UInt32를 제공하므로(`UTerminal/ViewModels/PresetModeViewModel.cs:116-121`) UInt16·UInt32를 고르면 `InvalidCastException`이 나고 `catch (Exception)`(`SerialPresetParser.cs:141`)에 잡혀 파싱 실패로만 집계된다.
- **수정**: `Convert.ToInt32(parseData.LinkData.ParsedValue)`. 길이가 남은 데이터를 넘으면 예외에 기대지 않고 `false`를 반환한다.
- **검증**: `ParseMessage_VariableField_ReadsLengthFromEveryOfferedLengthType`(UInt8/16/32), `ParseMessage_VariableLengthBeyondData_Fails` 통과.

### - [ ] A6. 프리셋 길이 캐시가 필드 추가·전체 삭제 후에도 옛 값을 반환

- **문제**: `ParsePreset._cachedLength`는 `RemoveItem`에서만 무효화된다(`UTerminal/Models/Parser/ParsePreset.cs:95-99`). `AddField`(`PresetModeViewModel.cs:232`)와 `ClearFields`(`PresetModeViewModel.cs:253`)는 `ParseDataList`를 직접 바꾸므로, 한 번 길이를 계산한 뒤 필드를 바꾸면 `GetTotalLength()`가 옛 값을 돌려준다. 테스트에서 필드 추가 후 기대값 3에 실제 1, 전체 삭제 후 기대값 0에 실제 4였다.
- **수정**: 캐시를 없애고 매번 합산한다. 필드 수가 적어 비용이 작다.
- **검증**: `GetTotalLength_ReflectsFieldsAddedAfterFirstCall`, `GetTotalLength_ReflectsClearedFields` 통과.

### - [ ] A7. 호출되지 않는 `SerialPresetParser.GetTotalLength()`

- **문제**: `_cachedTotalLength`와 `GetTotalLength()`(`UTerminal/Models/Parser/SerialPresetParser.cs:22`, `47-62`)를 호출하는 곳이 없다.
- **수정**: 삭제.
- **검증**: 빌드, 전체 테스트.

### - [ ] A8. 선택된 포트가 없을 때 포트 선택이 `NullReferenceException`

- **문제**: `SelectPort`와 `CustomSelectPort`가 로그를 남기며 `oldPort.Name`을 읽는다(`UTerminal/Models/PortManager/PortManager.cs:104`, `120`). `SelectedPort`가 null이면 NRE가 난다. 컴파일러도 `PortManager.cs(120,65)`에 CS8602 경고를 낸다. `ScanPort`는 실제 포트가 `maxPort`(기본 10)개 이상이면 `AvailablePorts.All(x => x.IsEnabled)`가 참이 되어 첫 포트를 선택하지 않고 끝난다(`PortManager.cs:68`). 주석("If cant find any port")과 조건이 반대다.
- **수정**: `oldPort?.Name`. `ScanPort`의 조건을 `!AvailablePorts.Any(x => x.IsEnabled)`로 바꾼다.
- **검증**: `CustomSelectPort_WithNoPreviousSelection_SetsPortName`, `SelectPort_WithNoPreviousSelection_SetsPortName` 통과.

### - [ ] A9. 커스텀 포트 경로를 비우면 `ArgumentException`

- **문제**: 커스텀 포트 입력란이 포커스를 잃으면 입력값으로 `CustomSelectPort`가 호출된다(`UTerminal/Views/Components/SerialRuntimePanel.axaml:163-170`). 빈 문자열이면 `ConnectionConfig.PortName = ""` → `SerialPortAdapter.UpdatePortConfig`에서 `SerialPort.PortName` 설정이 `ArgumentException: The PortName cannot be empty.`를 던진다(`SerialPortAdapter.cs:98`).
- **수정**: `CustomSelectPort`가 빈 문자열·공백 경로를 무시한다.
- **검증**: `CustomSelectPort_EmptyPath_KeepsPreviousPortName` 통과.

---

## D. 결정 필요 항목

### - [ ] D1. 프리셋 불러오기가 필드가 있는 프리셋에서 항상 실패

- **문제**: `ParsePreset.ParseDataList`의 원소 타입이 인터페이스 `IParseData`라 `System.Text.Json`이 역직렬화하지 못한다. `PresetJsonTests.SaveThenLoad_RestoresFields`에서 `NotSupportedException: Deserialization of interface types is not supported. Type 'UTerminal.Models.Parser.Interfaces.IParseData'`가 났다. 이 예외가 `LoadPresetCommand`에서 처리되지 않아 앱이 종료되는지는 확인하지 않았다(추론: ReactiveUI의 처리되지 않은 커맨드 예외).
- **선택지**
  - (추천) 저장 전용 형식(DTO)을 둔다: `{ Name, Description, Fields: [{ Type, Name, Length, LinkIndex }] }`. 가변 필드의 `LinkData`를 인덱스로 저장해 복원할 수 있다. 기존에 저장된 파일과의 호환 여부를 함께 정해야 한다.
  - `IParseData`에 `JsonDerivedType`/커스텀 컨버터를 붙인다. 현재 파일 형식을 유지하지만 `ParsedValue`·`DisplayValue` 같은 실행 중 값까지 계속 저장된다.
- **테스트**: `PresetJsonTests.SaveThenLoad_RestoresFields`는 이 항목을 고칠 때까지 `Skip` 처리한다.

### - [ ] D2. 프리셋 모드가 패킷 경계 없이 원시 청크를 파싱

- **문제**: 프리셋 모드는 `ISerialPort`의 원시 청크(`BytesToRead`만큼 읽은 단위)를 그대로 파서에 넣고, 파서는 청크의 offset 0부터 해석한다(`PresetModeViewModel.cs:271`, `SerialPresetParser.cs:98-104`). 패킷이 청크 두 개로 나뉘거나 청크 하나에 여러 패킷이 들어 있으면 파싱이 실패한다.
- **선택지**
  - (추천) 프리셋 모드도 `SerialService`의 패킷 구분 결과(`MsgReceived`)를 쓴다. 읽기 방식(NewLine/STX-ETX/Custom)을 기본 모드와 공유한다. `CLAUDE.md`의 "`PresetModeViewModel`은 `ISerialPort`로만 구독한다" 규칙을 바꿔야 한다.
  - 파서가 자체 버퍼를 두고 STX 위치로 동기화한다. 기본 모드와 독립적이지만 패킷 구분 로직이 두 벌이 된다.

### - [ ] D3. 프리셋 모드가 UI 바인딩 속성을 수신 루프 스레드에서 변경

- **문제**: `PresetModeViewModel.OnRawDataReceived`가 수신 루프 스레드에서 카운터와 `ParseData.ParsedValue`를 바꾼다(`PresetModeViewModel.cs:341-355`, `SerialPresetParser.cs:111`). `ARCHITECTURE.md` 8.1의 스레드 규칙과 다르다. Avalonia에서 실제로 어떤 증상이 나는지는 확인하지 않았다.
- **선택지**
  - (추천) D2와 함께 처리한다. 메인 화면처럼 Rx `Buffer` → `ObserveOn(MainThreadScheduler)`로 넘겨 파싱 결과를 UI 스레드에서 반영한다.
  - 매 패킷을 `Dispatcher.UIThread.Post`로 넘긴다. 구현은 작지만 수신 속도가 높으면 UI 스레드 작업이 쌓인다.

### - [ ] D4. 파일 저장·불러오기 오류 처리

- **문제**
  - `SavePreset`이 `async void`다(`PresetModeViewModel.cs:299`). 프리셋 이름에 경로 구분자 등 파일 이름에 쓸 수 없는 문자가 있으면 예외가 호출자에게 전달되지 않는다.
  - `MacroSettings.Load`는 손상된 JSON에서 `JsonSerializer` 예외를 그대로 던진다(`UTerminal/Models/Utils/MacroSettings.cs:34`). 매크로 창을 여는 생성자에서 호출된다(`MacroViewModel.cs:36`).
  - 사용자에게 오류를 보여 주는 경로가 없다(`ARCHITECTURE.md` 8.4).
- **결정할 것**: 오류를 로그만 남기고 무시할지, 대화상자 등으로 사용자에게 보여 줄지. 방향이 정해지면 위 세 곳에 같은 방식으로 적용한다.

### - [ ] D5. 장치 연결이 끊겨도 연결 상태가 유지됨

- **문제**: 장치를 뽑아 수신 루프에서 예외가 나면 루프만 끝나고(`SerialPortAdapter.cs:200-203`), `MainViewModel.IsConnected`는 `true`로 남는다. 사용자는 Disconnect를 눌러야 다시 연결할 수 있다(추론. 실제 장치로 확인하지 않았다).
- **결정할 것**: 끊김을 감지해 자동으로 연결 해제 상태로 바꿀지, 재연결을 시도할지.

### - [ ] D6. 커스텀 STX/ETX 입력 필터

- **문제**: `CustomStxEtx_OnKeyDown`이 `(char)e.Key`로 문자를 판별한다(`UTerminal/Views/Components/SerialRuntimePanel.axaml.cs:25`). Avalonia `Key` 열거형 값은 문자 코드가 아니어서 판별이 맞지 않는다. `ByteToHexStringConverter.ConvertBack`은 잘못된 입력에 `byte`가 아닌 `int` 0을 반환한다(`UTerminal/Converter/ByteToHexStringConverter.cs:25`, `32`). 실제 입력에서 어떤 증상이 나는지는 GUI로 확인하지 않았다.
- **결정할 것**: 키 단위 필터를 유지할지(`TextInput` 이벤트에서 `e.Text`로 판별), 필터 없이 변환기 검증만 둘지. 수정 후 GUI 확인이 필요하다.

### - [ ] D7. STX-ETX 모드의 버퍼 처리

- **문제**
  - 수집 중에 STX 값이 다시 오면 기존 바이트를 버리지 않고 이어 붙인다(`SerialService.cs:160-165`).
  - 연결 중 `ReadMode`를 바꾸면 `_bufferList`의 잔여 바이트가 새 방식으로 이어서 처리된다(`ARCHITECTURE.md` 8.3).
- **결정할 것**: 페이로드 안에 STX와 같은 값이 나올 수 있는 바이너리 프로토콜이면, STX에서 버퍼를 초기화하는 방식은 정상 패킷을 깨뜨린다. 대상 프로토콜 기준으로 정한다. `ReadMode` 변경 시 버퍼를 비우는 것은 별다른 부작용이 없어 보이지만 D7 결정과 함께 처리한다.

### - [ ] D8. 원시 구독자 안에서 구독을 해지하면 수신 루프 종료

- **문제**: `BroadcastRawData`는 읽기 락을 쥔 채 구독자를 호출한다(`SerialPortAdapter.cs:212-227`). 구독자 안에서 해지하면 `EnterWriteLock`에서 `LockRecursionException`이 나고 수신 루프가 끝난다(`ARCHITECTURE.md` 8.2). 지금은 그렇게 호출하는 코드가 없다.
- **선택지**
  - (추천) 브로드캐스트 시 배열 참조만 읽고 락 없이 호출한다. 구독·해지는 이미 새 배열로 교체하므로 가능하다. `CLAUDE.md`의 "`ReaderWriterLockSlim`으로 구독자 배열을 보호" 규칙을 바꿔야 한다.
  - 현재 구조를 유지하고, 구독자 안에서 해지하지 말라는 규칙만 둔다(`ARCHITECTURE.md` 8.2에 이미 있다).
