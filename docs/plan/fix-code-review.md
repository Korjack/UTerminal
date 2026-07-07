# Code Review 결과 — fix/code-review 브랜치

**리뷰 대상 브랜치:** `fix/code-review`  
**베이스 브랜치:** `main`  
**리뷰 범위:** CI/CD 워크플로우, Docker 설정, 문서 파일 신규 추가 (4개 파일, 401줄)  
**리뷰 일자:** 2026-07-07

---

## 발견된 이슈 (심각도 순)

### 🔴 높음

#### 1. `dotnet publish` 프로젝트 경로 미지정

- **파일:** `.github/workflows/dotnet-build.yaml:24`
- **문제:** `dotnet publish -c Release -r win-x64`를 솔루션 루트에서 실행하면 `UTerminal.sln`의 두 프로젝트 모두(라이브러리 + 실행파일)가 publish 대상이 된다.
- **영향:** `UTerminal/`(클래스 라이브러리)는 Release 조건부 `RuntimeIdentifier`나 `SelfContained` 설정이 없는 상태로 `-c Release -r win-x64`를 받아 NETSDK1188/NETSDK1191 진단이 발생하거나 예상치 못한 위치에서 CI가 실패할 수 있다.
- **수정안:**
  ```yaml
  run: dotnet publish UTerminal.Desktop/UTerminal.Desktop.csproj -c Release -r win-x64
  ```

---

#### 2. `validation-summary` 잡의 skipped 동작

- **파일:** `.github/workflows/dotnet-build.yaml:58`
- **문제:** `validation-summary`에 `if: always()`가 없어 upstream 빌드가 실패하면 'failed'가 아닌 'skipped' 상태가 된다.
- **영향:** GitHub 브랜치 보호에서 `validation-summary`만 required status check로 등록하면 'skipped'가 통과로 처리되어 Linux/macOS 빌드가 깨진 PR도 머지 가능해진다. 반대로 `if: always()`를 추가하면 빌드 실패에 관계없이 `echo`가 성공하여 false-positive 녹색 체크가 발생한다.
- **수정안:** `validation-summary` 대신 개별 빌드 잡(`build-windows`, `build-macos`, `build-linux`)을 직접 required status check로 등록하거나, `if: ${{ failure() }}`로 실패 시 명시적으로 `exit 1`을 실행한다.

---

#### 3. `main` 브랜치 직접 push 트리거 없음

- **파일:** `.github/workflows/dotnet-build.yaml:9`
- **문제:** `on.push.branches`에 `main`이 없고 `dev`만 포함되어 있다.
- **영향:** 쓰기 권한을 가진 팀원이 `main`에 직접 push하면 워크플로우가 실행되지 않아 5개 플랫폼 빌드 검증 없이 커밋이 반영된다. GitHub 브랜치 보호의 direct push 금지가 설정되지 않은 경우 CI를 완전히 우회할 수 있다.
- **수정안:**
  ```yaml
  push:
    branches:
      - dev
      - main
  ```

---

### 🟠 중간

#### 4. 가변 `@v4` 태그 사용 — 공급망 공격 노출

- **파일:** `.github/workflows/dotnet-build.yaml:16` (총 6개 `uses:` 참조)
- **문제:** `actions/checkout@v4`, `actions/setup-dotnet@v4` 모두 SHA 고정 없이 가변 태그를 사용한다.
- **영향:** `v4` 태그가 악성 커밋으로 강제 push되면 워크플로우 파일 변경 없이 공격자 코드가 `GITHUB_TOKEN` 권한으로 실행된다. 이 워크플로우는 `main` 브랜치 게이트이므로 영향 범위가 크다.
- **수정안:**
  ```yaml
  - uses: actions/checkout@11bd71901bbe5b1630ceea73d27597364c9af683  # v4
  - uses: actions/setup-dotnet@3951f0dfe7a73e853dcca71044b6b0b5fd0f9d4a  # v4
  ```

---

#### 5. `fail-fast: false` 누락

- **파일:** `.github/workflows/dotnet-build.yaml:28` (`build-macos`, `build-linux` 동일)
- **문제:** matrix 전략에 `fail-fast: false`가 없어 GitHub 기본값 `true`가 적용된다.
- **영향:** `osx-x64` publish 실패 시 진행 중인 `osx-arm64` 잡이 즉시 취소된다. 5개 플랫폼 전체 결과를 한 번에 확인하려는 빌드 검증 목적에 어긋나며, arm64 전용 문제를 해당 실행에서 발견하지 못한다.
- **수정안:**
  ```yaml
  strategy:
    fail-fast: false
    matrix:
      runtime: [osx-x64, osx-arm64]
  ```

---

#### 6. `permissions` 블록 없음

- **파일:** `.github/workflows/dotnet-build.yaml` (최상위 레벨)
- **문제:** 워크플로우에 `permissions` 블록이 선언되어 있지 않아 `GITHUB_TOKEN`이 저장소 기본값으로 실행된다.
- **영향:** 조직 또는 저장소 기본값이 'Read and write permissions'(GitHub 레거시 기본값)인 경우, `dotnet publish` 중 로드된 악성 NuGet 패키지가 토큰을 이용해 커밋 push나 시크릿 유출을 시도할 수 있다.
- **수정안:**
  ```yaml
  permissions:
    contents: read
  ```

---

### 🟡 낮음

#### 7. `RuntimeIdentifier` 호스트 자동감지 — 잠재적 함정

- **파일:** `UTerminal.Desktop/UTerminal.Desktop.csproj:38`
- **문제:** Linux/macOS의 `RuntimeIdentifier`가 빌드 호스트 아키텍처 자동감지로 설정된다. 현재 CI는 CLI `-r` 플래그로 재정의되어 정상 동작하나, 플래그 제거 시 잘못된 아키텍처 바이너리를 오류 없이 생성한다.
- **영향:** `ubuntu-latest`(x64) 위의 `linux-arm64` matrix 잡에서 `-r linux-arm64` 플래그를 제거하면 `linux-x64` 바이너리가 조용히 생성된다.
- **수정안:** 현재는 즉각적 수정 불필요. CI에서 `-r` 플래그를 항상 명시적으로 지정하는 관행을 유지한다.

---

#### 8. NuGet 패키지 캐시 미설정

- **파일:** `.github/workflows/dotnet-build.yaml:19`
- **문제:** `actions/setup-dotnet@v4`의 `cache: 'nuget'` 옵션이 사용되지 않아 5개 잡이 매 실행마다 모든 의존성을 독립적으로 재다운로드한다.
- **영향:** Avalonia, ReactiveUI, log4net 등을 5개 잡 전체에서 반복 다운로드. nuget.org rate-limit 발생 시 코드 변경 없이 CI 전체 실패 가능. 잡당 60-120초 절약 가능.
- **수정안:**
  ```yaml
  - name: Setup .NET
    uses: actions/setup-dotnet@v4
    with:
      dotnet-version: '8.0.x'
      cache: 'nuget'
  ```

---

#### 9. Docker 설정 파일 커밋

- **파일:** `.docker/config.json:2`
- **문제:** 빈 Docker 인증 설정 파일이 저장소에 커밋되어 있으며, Docker 기본 경로(`~/.docker/config.json`)가 아니므로 자동 참조되지 않고 CI에서도 사용되지 않는다.
- **영향:** 향후 개발자가 이 패턴을 따라 `docker login` 자격증명을 같은 위치에 커밋하면 레지스트리 토큰이 git 이력에 영구 노출된다.
- **수정안:** 파일 삭제 또는 `.gitignore`에 `.docker/config.json` 추가.

---

#### 10. `build-windows` 잡 중복 구조

- **파일:** `.github/workflows/dotnet-build.yaml:13`
- **문제:** `build-windows`가 `build-macos`/`build-linux`의 matrix 패턴과 동일한 3단계 구조(checkout → setup-dotnet → publish)를 독립 잡으로 중복 작성하고 있다.
- **영향:** `win-arm64` 추가나 .NET 버전 변경 시 3개 잡을 각각 수정해야 한다.
- **수정안:** `win-x64`를 별도 잡 대신 통합 matrix로 포함하거나, 현 구조 유지 시 `setup-dotnet` 버전 등 공통 설정을 한 곳에서 관리할 수 있도록 `env:` 레벨에 공통값을 선언한다.

---

## 이슈 요약표

| # | 심각도 | 파일 | 이슈 |
|---|---|---|---|
| 1 | 🔴 높음 | dotnet-build.yaml:24 | `dotnet publish` 경로 미지정 → 라이브러리도 publish 대상 |
| 2 | 🔴 높음 | dotnet-build.yaml:58 | `validation-summary` skipped ≠ failed → 깨진 빌드 PR 머지 가능 |
| 3 | 🔴 높음 | dotnet-build.yaml:9 | `main` push 트리거 없음 → 직접 push 시 CI 전체 우회 |
| 4 | 🟠 중간 | dotnet-build.yaml:16 | `@v4` 가변 태그 → 공급망 공격 노출 |
| 5 | 🟠 중간 | dotnet-build.yaml:28 | `fail-fast: false` 누락 → 형제 matrix 잡 조기 취소 |
| 6 | 🟠 중간 | dotnet-build.yaml (최상위) | `permissions` 블록 없음 → 쓰기 권한 토큰 리스크 |
| 7 | 🟡 낮음 | UTerminal.Desktop.csproj:38 | RID 자동감지 → `-r` 플래그 제거 시 잘못된 아키텍처 바이너리 |
| 8 | 🟡 낮음 | dotnet-build.yaml:19 | NuGet 캐시 없음 → 반복 다운로드, rate-limit 리스크 |
| 9 | 🟡 낮음 | .docker/config.json:2 | Docker 설정 파일 커밋 → 향후 자격증명 노출 선례 |
| 10 | 🟡 낮음 | dotnet-build.yaml:13 | `build-windows` 중복 구조 → matrix 통합으로 해결 가능 |
