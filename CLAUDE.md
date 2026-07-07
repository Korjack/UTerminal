# UTerminal — Claude 작업 지침

## 프로젝트 개요

**UTerminal**은 .NET 8 + Avalonia UI 기반의 크로스플랫폼 UART 시리얼 통신 모니터링 데스크톱 앱이다.
Windows / macOS / Linux 를 단일 코드베이스로 지원하며, 커스텀 바이너리 패킷 파싱(프리셋 모드)과 매크로 전송 기능을 포함한다.

## 솔루션 구조

```
UTerminal.sln
├── UTerminal/           # 핵심 라이브러리 (UI + 비즈니스 로직)
└── UTerminal.Desktop/   # 플랫폼 진입점 (Program.cs)
```

## 빌드 & 실행

```bash
# Debug 빌드 (개발)
dotnet build

# 플랫폼별 Release 배포
dotnet publish -c Release -r win-x64
dotnet publish -c Release -r osx-x64
dotnet publish -c Release -r linux-x64
```

테스트 프로젝트는 현재 없음. `SerialTest/` 폴더는 Python 기반 수동 테스트 도구(가상 시리얼 포트 시뮬레이터)이며 프로젝트 빌드에 포함되지 않는다.

## 주요 기술 스택

| 항목 | 버전 |
|---|---|
| .NET | 8.0 |
| Avalonia UI | 11.2.2 |
| ReactiveUI | (Avalonia.ReactiveUI 동반) |
| AvaloniaEdit | 11.1.0 |
| Avalonia.Xaml.Behaviors | 11.2.0.7 |
| log4net | 3.0.3 |
| System.IO.Ports | 9.0.0 |

`Directory.Build.props`에 `AvaloniaVersion` 속성이 정의되어 있으므로, Avalonia 패키지 버전은 반드시 이 파일을 통해 관리한다.

## 아키텍처 패턴

- **MVVM** — View(.axaml) / ViewModel(ReactiveObject) / Model 3계층
- **ReactiveUI** — 커맨드(`ReactiveCommand`), 반응형 프로퍼티(`RaiseAndSetIfChanged`), Observable 스트림
- **인터페이스 추상화** — `ISerialPort`, `ISerialService`, `IBufferManager`, `IMessageProcessor` 등 핵심 계층은 인터페이스로 분리

자세한 구조는 `ARCHITECTURE.md` 참조.

## 코딩 컨벤션

- **언어**: C# 최신 버전 (`LangVersion=latest`)
- **Nullable**: 전역 활성화 (`Nullable=enable`)
- **네임스페이스**: 파일 범위 네임스페이스(`namespace Foo.Bar;`) 사용
- **바인딩**: Compiled Bindings 기본값으로 사용(`AvaloniaUseCompiledBindingsByDefault=true`)
- **비동기**: I/O 작업은 `async/await` 사용; UI 스레드 복귀는 `ObserveOn(RxApp.MainThreadScheduler)`
- **로그**: `SystemLogger.Instance`(시스템), `SerialMsgLogger.Instance`(시리얼 데이터) 싱글턴 사용

## 커밋 메시지 규칙

```
<type>: <제목>
```

- type: `feat`, `fix`, `refac`, `docs`, `ci`, `test`
- 제목은 한국어 또는 영어 혼용 가능 (기존 커밋 참조)
- 예시: `feat: CI/CD 워크플로우 추가 및 Docker 초기 구성`

## 브랜치 전략

- `main` — 릴리즈 브랜치 (PR 머지 전 CI 빌드 통과 필수)
- `dev` — 개발 통합 브랜치 (push 시 CI 자동 실행)
- 기능 브랜치는 이슈 번호 기반: `21-feature-xxx`, `23-refac-xxx`

## CI/CD

`.github/workflows/dotnet-build.yaml` — PR(→main) 및 dev push 시 자동 실행.
Windows x64, macOS x64/arm64, Linux x64/arm64 총 5개 플랫폼 빌드를 검증한다.

## 주의 사항

- `PresetModeViewModel`에서 직렬 포트는 `ISerialPort`를 통해서만 구독한다. `SerialPortAdapter` 직접 참조 금지.
- `BroadcastRawData` 는 `ReaderWriterLockSlim`으로 구독자 배열을 보호한다. 구독자 변경 시 동일 패턴 유지.
- `SerialMsgProcessor`의 `BufferUpdated` 이벤트 구독은 `MainThreadScheduler`에서 수행해야 UI 스레드 안전성을 보장한다.
- 프리셋은 `<실행경로>/Preset/` 폴더에 JSON으로 저장된다. 경로 변경 시 `PresetModeViewModel.SavePreset()` 수정.
