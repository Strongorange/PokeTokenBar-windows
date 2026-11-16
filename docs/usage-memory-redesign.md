---
summary: "사용량 캐시 · 기록 읽기 구조 때문에 앱이 켜질 때 2GB 를 할당하던 문제의 측정 결과와 재설계안. SQLite 저장소 + 덧붙은 줄만 읽기 + 화면에는 합계만 전달."
read_when:
  - 사용량 스캔 · 캐시(UsageScanCache, IncrementalLogScanner, ProviderCatalog)를 고칠 때
  - 앱 메모리 사용량이 다시 커졌을 때
  - Claude · Codex 파서 계약을 바꿀 때
---

# 사용량 메모리 재설계

## 결론

- 앱이 켜질 때 약 2GB 를 할당하고, .NET 이 늘린 메모리를 돌려주지 않아 Private 이 1.6GB 까지 오릅니다. 계속 들고 있는 객체는 150~250MB 뿐입니다.
- 원인은 세 가지입니다. 캐시 전체를 JSON 하나로 읽고 쓰는 것, 바뀐 기록 파일을 처음부터 다시 읽는 것, 1년 치 개별 이벤트 26만 건을 그대로 들고 다니는 것입니다.
- 캐시를 SQLite 로 옮기고, 파일마다 읽은 위치를 기억해 덧붙은 줄만 읽고, 화면에는 합계만 넘깁니다. 계산상 시작 할당은 2GB 에서 0.1GB 안팎으로, 5분 갱신 할당은 353MB 에서 10MB 이하로 줄어듭니다.

## 측정 결과 (2026-11-16, 설치본 0.29.0)

레포 밖 측정 프로그램이 실제 부품을 단계마다 새 프로세스에서 돌려 잰 값입니다. 캐시 로드는 2회, 나머지는 1회 측정입니다.

| 단계 | 언제 | 순간 할당 | 계속 들고 있는 객체 |
|---|---|---|---|
| Codex 캐시 로드 (85MB JSON, 이벤트 15만) | 시작 | 1,071MB | 98MB |
| Claude 캐시 로드 (26MB JSON, 이벤트 11만) | 시작 | 301MB | 171MB |
| 첫 갱신 (캐시 로드 제외) | 시작 | 586MB | 두 캐시 합쳐 157MB |
| 두 캐시 저장 | 5분마다 | 379MB | — |
| 5분 갱신 전체 | 5분마다 | 353MB | 157MB |

처음엔 5분 갱신 353MB 의 대부분을 저장 비용으로 봤습니다. 측정 없이 뺄셈으로 짐작한 것이었고 틀렸습니다. 1단계에서 저장을 스트림으로 바꿔 저장 할당이 23MB 로 줄었는데도 5분 갱신은 355MB 그대로였습니다. 남은 비용은 1년 치 이벤트 목록을 매번 새로 만들고 집계하는 부분입니다(4단계 대상).
| 205MB 기록 파일 읽기 + 파싱 | 그 파일이 바뀔 때마다 | 1,223MB (최대 WS 1.18GB) | 1MB |

- 실행 중인 앱에서 `dotnet-gcdump` 로 힙을 뜨자(이때 GC 가 돎) Private 이 1.6GB 에서 905MB 로 내려갔습니다.
- 이벤트 26만 건 중 최근 7일 치는 9천 건(3.4%)입니다. 날짜 × 모델 조합은 5,704개가 상한입니다.

GC 직후 Private 1,054MB 의 구성 (`dotnet-counters`, 1회):

| 구분 | 크기 |
|---|---|
| WPF · 그래픽 · 네이티브 (Private − GC committed) | 113MB |
| .NET 이 잡아 두고 비어 있는 영역 (committed − 힙) | 315MB |
| 큰 객체 영역(LOH)의 빈 공간 | 379MB |
| 살아 있는 객체 (gen2 164MB + LOH 83MB) | 247MB |

앱 기본 메모리는 113MB 이고, 나머지는 큰 문자열을 만들었다 버린 뒤 .NET 이 돌려주지 않은 공간이 대부분입니다.

macOS 원본에도 같은 부류가 기록돼 있습니다(`docs/reference/defect-log.md` "크기 상한이 없는 사용자 파일을 통째로 읽지 마라", #184). 원본은 Codex 만 바이트 `0x0A` 기준 청크 스트림으로 바꿔 최대 RSS 를 416MB 에서 66MB 로 줄였고, Claude 는 실측 뒤로 미뤄 뒀습니다. Windows 포트는 두 파서 모두 통째로 읽습니다.

## 원인 구조

| 위치 | 문제 |
|---|---|
| 파서 계약 `IUsageProvider.ParseFile(path, IReadOnlyList<string> lines)` | 파일 전체를 줄 목록으로 받게 정해져 있어 이어 읽기 · 한 줄씩 처리가 계약 단계에서 막힘 |
| `IncrementalLogScanner` · `ProviderCatalog.ReadLogLines` | 바뀐 파일을 `ReadToEnd` 로 전부 읽고 줄 문자열을 다시 만듦. 같은 읽기 코드가 두 곳에 있음 |
| `UsageScanCache` | 메모리 색인 · 저장 형식(파일 하나를 통째로 다시 씀) · 보존 정책(400일)이 한 클래스에 섞임. 파서 결과를 그대로 저장 |
| `UsageProviderSnapshot.Entries` | 5분마다 1년 치 이벤트 전체 목록을 만들어 넘기고, 집계 함수 6개가 각각 처음부터 돎 |
| `ProviderCatalog` | static 조립 코드의 람다 안에 Codex 처리 순서(포크 확장 · 리플레이 제거 · 저장)가 들어 있음 |
| 테스트 | 큰 입력의 메모리 사용량을 보는 테스트가 없어 기록이 쌓이며 커진 걸 못 잡음 |

## 목표

| 항목 | 지금 | 목표 |
|---|---|---|
| 시작 할당 | 약 2GB | 100MB 이하 |
| 5분 갱신 할당 | 353MB | 10MB 이하 |
| 큰 세션 진행 중 갱신 할당 | 1.2GB | 덧붙은 바이트에 비례 (수 MB) |
| 계속 들고 있는 사용량 객체 | 150~250MB | 40MB 이하 |
| Private (작업 관리자) | 최대 1.6GB | 300MB 이하 |

메모리 목표는 테스트로 고정합니다(아래 "테스트").

## 설계

### 1. 저장소: SQLite 한 파일 (`usage.db`)

`Microsoft.Data.Sqlite` 는 OpenCode 읽기에 이미 씁니다.

- `files(path, provider, mtime_ticks, size, read_offset, parser_state)`: 파일마다 어디까지 읽었는지와 이어 읽을 때 필요한 파서 상태
- `events(provider, id_hash, file_id, ts, local_day, model, input, output, cache_write, cache_read, total, cost_unavailable)`: 정규화한 이벤트. `(provider, id_hash)` 유니크
  - Claude 의 `DedupKeepMax` 는 `INSERT ... ON CONFLICT DO UPDATE ... WHERE excluded.total > events.total` 로 옮김. 이어 쓴 세션이 옛 메시지를 다른 파일에 다시 적어도 한 번만 셈
  - `id_hash` 는 ID 문자열의 64비트 해시. 이벤트마다 긴 ID · 세션 ID 문자열을 들고 있지 않음
- 일별 합계는 처음엔 `GROUP BY local_day, model` 쿼리로 구함(`local_day` 색인). 1년 치 26만 행이라 충분히 빠를 것으로 예상하고, 느리면 합계 테이블을 더함
- 보존 기간 400일은 저장소의 삭제 쿼리 한 곳에서만 다룸

### 2. 기록 읽기: 덧붙은 줄만

1단계에서 구현함. 저장소는 아직 JSON 캐시입니다.

- `LogLineReader` 가 저장된 오프셋부터 바이트 `0x0A` 기준으로 줄을 자르고 한 줄씩 넘김. 마지막 줄이 덜 써졌으면 그 줄 앞까지를 다음 오프셋으로 저장
- 오프셋 바로 앞 64바이트의 지문(`LogResumePoint.Anchor`)을 같이 저장. 파일이 오프셋보다 짧아졌거나 지문이 다르면 다시 쓴 파일로 보고 처음부터 읽음
- 파서 계약: `LogParser<TPayload>(Filter, Parse)`
  - `Filter(ReadOnlySpan<byte>)`: 파서가 쓰던 문자열 검사와 같은 뜻의 바이트 검사. 통과하지 못한 줄은 문자열로 만들지 않음
  - `Parse(path, IEnumerable<string> lines, TPayload? resumeFrom)`: `resumeFrom` 은 앞부분을 읽은 결과. 처음부터 읽을 땐 null
  - Claude: 상태 없음. 앞부분 결과에 새 줄을 합친 뒤 `DedupKeepMax`
  - Codex: 세션 ID · 직전 누적 사용량 · 모델 · turn 번호를 `CodexParsedRollout.Resume` 에 담아 이어 감
- Codex 포크 묶음에서 쓰던 별도 읽기 코드(`ProviderCatalog.ReadLogLines`)는 스캐너의 `Load` 로 합침

### 3. Codex 포크 처리

- 지금처럼 부모 · 자식 묶음(`CodexParentClosure`) 단위로 `CodexRolloutResolver` 를 돌림. 대신 바뀐 파일이 속한 묶음만 다시 계산함
- 묶음 계산에 필요한 부모 이벤트는 저장소에서 그 묶음 것만 불러옴. 계산 결과로 그 묶음의 이벤트 행을 지우고 다시 넣음
- 리플레이 판정 규칙(`MaximumReplayGap`)은 바꾸지 않음

### 4. 화면에 넘기는 값: 합계 질의

`UsageRefreshService` 는 이벤트 목록 대신 질의 인터페이스를 받습니다.

```csharp
public interface IUsageQuery
{
    DailyUsage? Day(string localDay, bool includeModels);
    PeriodUsage Period(string key, string fromDay, string toDay);
    IReadOnlyList<DailyUsage> DailySeries(string fromDay, string toDay);
    BlockUsage? ActiveBlock(DateTimeOffset now);
}
```

- SQLite 구현과, 지금의 `UsageAggregation` 함수를 감싼 메모리 구현 두 개를 둠. 메모리 구현은 테스트 기준값으로 씀
- `usage-ledger.json`(사용량 요약)은 이번 범위에서 그대로 둠

### 5. 옮겨 가기

- 새 버전 첫 실행에서 옛 JSON 캐시를 지우고 기록을 한 번 처음부터 읽음. 한 줄씩 읽으니 메모리는 목표 안에 머물고, 시간만 걸림
- 다시 읽는 동안에는 `usage-ledger.json` 값으로 화면을 채움
- 파서 버전이 바뀔 때도 같은 경로를 씀

## 모듈 경계

| 프로젝트 | 들어갈 것 | 의존 |
|---|---|---|
| Core | `IUsageQuery`, 메모리 구현, 이벤트 · 합계 레코드 | 없음 |
| Providers | `IIncrementalLogParser` Claude · Codex 구현, Codex 묶음 계산 | Core |
| Platform.Windows | `LogLineReader`, `SqliteUsageStore`(`IUsageQuery` 구현) | Core |
| Application | 제공자별 조립 클래스(`ClaudeUsageSource` · `CodexUsageSource`), 갱신 순서 | 전부 |

`ProviderCatalog` 의 람다 안 로직은 조립 클래스로 옮기고, `ProviderCatalog` 는 조립만 합니다. `UsageScanCache` 와 `IncrementalLogScanner` 는 지웁니다.

## 테스트

- 이어 읽기(`IncrementalUsageScanTests`): fixture 를 줄 끝 · 줄 중간 어디서 나눠 두 번에 읽어도 한 번에 읽은 결과와 같음. 다시 쓴 파일 · 줄어든 파일은 처음부터 읽음
- `LogLineReaderTests`: 덜 써진 마지막 줄, BOM, 청크 경계의 멀티바이트 문자, 청크보다 긴 줄, 필터, 오프셋 · 지문
- 저장소: 같은 ID 가 다른 파일에서 다시 와도 한 번만 셈, 큰 값이 남음, 400일 지난 행 삭제
- 질의: SQLite 구현과 메모리 구현이 같은 입력에서 같은 값을 냄
- 메모리 예산(`UsageScanMemoryBudgetTests`, 실패하면 빌드 실패)
  - 64MB 가짜 기록을 처음 읽을 때 할당 32MB 미만, 1MB 덧붙인 뒤 다시 읽을 때 8MB 미만. 덧붙인 뒤에는 파서에 넘어간 줄 수가 덧붙인 사용량 줄 수와 같아야 함
  - 필터를 무시하게 하면 처음 읽기 예산에서, 이어 읽기를 끄면 줄 수 확인에서 실패하는 것을 결함 주입으로 확인함. 바이트 필터 때문에 처음부터 다시 읽어도 할당은 작아서, 이어 읽기는 할당량으로는 못 잡음
  - (4단계) 이벤트 30만 건 저장소로 시작 + 1회 갱신 할당 100MB 이하
- 옛 경로와 같은 값인지 확인: 로컬 실제 기록으로 옛 파이프라인과 새 파이프라인의 날짜 · 모델별 합계를 비교하는 일회성 스크립트. 실제 기록을 읽으므로 레포 테스트에는 넣지 않음

## 예상 절감 (계산, 측정 아님)

| 항목 | 지금 (측정) | 바꾼 뒤 (계산) | 근거 |
|---|---|---|---|
| 시작 할당 | 약 2GB | 0.1GB 안팎 | JSON 전체 로드가 없어짐. 시작 때 읽는 건 파일 색인 4.5천 행과 화면용 합계 질의 |
| 5분 갱신 할당 | 353MB | 10MB 이하 | 전체 저장이 없어짐. 덧붙은 줄(지금 활발한 세션 기준 수 MB)과 바뀐 행만 씀 |
| 큰 세션 진행 중 갱신 | 1.2GB | 수 MB | 205MB 파일도 덧붙은 부분만 읽음 |
| 계속 들고 있는 사용량 객체 | 150~250MB | 20~40MB | 이벤트를 메모리에 두지 않음. 합계 질의 결과(5,704행 상한)와 최근 5시간 구간 이벤트만 |
| Private | 최대 1.6GB, GC 직후 0.9~1.05GB | 200~250MB | 기본 113MB(측정) + 살아 있는 객체 20~40MB + 갱신 때 순간 할당 여유 |

큰 문자열을 만들지 않으면 LOH 빈 공간(379MB)과 비어 있는 committed 영역(315MB)이 생길 원인이 없어집니다.

## 진행 단계

각 단계는 따로 머지하고 릴리스할 수 있습니다.

0. 기본 메모리 측정: 2026-11-16 측정함(113MB, 위 "측정 결과")
1. 줄 단위 파서 계약과 `LogLineReader`, 캐시 스트림 읽기 · 쓰기, 메모리 예산 테스트: 구현함, 0.29.1(아래 "1단계 결과")
2. 1단계 뒤 측정으로 계획을 바꿈(아래 "2단계 결과"). 날짜별 합계 표, 갱신 뒤 압축 GC, 스캔 프로세스 분리: 구현함, 0.30.0
3. Codex 저장을 SQLite 로 옮기고 묶음 단위 재계산: 보류. 스캔을 분리해서 UI 메모리에는 효과가 없고, 5분마다 뜨는 스캔 프로세스의 순간 메모리와 CPU 시간만 줄어듦
4. 원래 4단계(화면이 합계 질의를 씀)는 2단계의 날짜별 합계 표로 대신함. Claude SQLite 는 줄일 수 있는 양이 26MB / 5분이라 하지 않음

## 1단계 결과 (2026-11-16, 실제 기록, 1회)

| 항목 | 바꾸기 전 | 1단계 뒤 |
|---|---|---|
| 205MB 기록 파일 읽기 + 파싱 | 할당 1,223MB, 최대 WS 1.18GB | 할당 34MB, 최대 WS 53MB |
| Codex 캐시 로드 | 할당 1,071MB | 할당 218MB |
| Claude 캐시 로드 | 할당 301MB | 할당 35MB |
| 두 캐시 저장 | 379MB | 23MB |
| 앱 시작 (캐시 로드 + 첫 갱신) 뒤 Private | 1,359MB | 342MB |
| 5분 갱신 할당 | 353MB | 355MB (변화 없음, 4단계 대상) |
| 기록 전체를 처음부터 다시 읽기 | 276초 | 42초 (최대 WS 343MB) |

- 옛 코드(`main`)와 새 코드로 실제 기록을 새로 스캔해 날짜 · 모델별 합계를 비교했습니다. 측정 당일을 뺀 376행이 모두 같았습니다. 당일 행은 두 실행 사이에 진행 중인 세션이 사용량을 더 써서 새 코드 쪽만 늘었습니다.
- 캐시 형식 버전을 2로 올려서, 새 버전 첫 실행은 기록을 처음부터 다시 읽습니다(위 42초).

## 2단계 결과 (2026-11-16, 1회)

1단계 뒤 5분 갱신 355MB 를 제공자별로 나누니 Claude 26MB, Codex 205MB, OpenCode 71MB 였습니다. 원래 2단계(Claude SQLite)로 줄일 수 있는 건 26MB 뿐이라 계획을 바꿨습니다. 또 18분 켜 둔 0.29.1 의 Private 530MB 중 228MB 는 버렸는데 돌려주지 않은 공간이었습니다(LOH 빈 공간 92MB 포함).

| 변경 | UI Private | 내용 |
|---|---|---|
| 0.29.1 | 515~530MB | |
| 날짜별 합계 표(`DailyUsageTable`) | 5분 갱신 355MB → 304MB | 제공자가 이벤트 목록 대신 날짜별 합계를 넘기고, 갱신 서비스는 표를 합침. 비용은 이벤트마다 더해서 장문 단가(272k 초과)가 그대로 적용됨 |
| 갱신 뒤 압축 GC(`ManagedMemory.ReturnFreedMemory`, `System.GC.ConserveMemory=5`) | 313~328MB | LOH 까지 압축해 버린 공간을 돌려줌 |
| 스캔 프로세스 분리(`UsageScanProcess`) | 168~183MB | 갱신마다 `PokeTokenBar.exe --scan` 을 띄워 상태 JSON 만 받음. 실패하면 그 회차는 UI 프로세스에서 스캔. 스캔 프로세스는 5분마다 몇 초, 최대 WS 396MB |

- 스캔 분리 뒤 UI 프로세스 177MB 의 구성: .NET 힙 42MB(살아 있는 객체 약 25MB), 네이티브 약 135MB. 네이티브에는 WPF 하드웨어 렌더링이 불러온 그래픽 드라이버 몫이 들어 있음
- 소프트웨어 렌더링(`RenderMode.SoftwareOnly`) 실험: Private 168MB → 139MB, 펫만 떠 있을 때 CPU 0.06% → 0.05%. 대시보드를 연 상태의 CPU 는 안 잼. 적용 안 함
- 옛 · 새 계산의 날짜 · 모델별 합계 비교는 1단계에서 했고, 2단계는 같은 계산을 표로 옮긴 것이라 `DailyUsageTableTests` 로 대조함

## 위험

- macOS 원본과 구조가 갈라집니다. 원본의 사용량 스캔 변경을 이후에 옮길 때 손으로 맞춰야 합니다.
- 첫 실행에서 7GB 가 넘는 기록을 `\\wsl.localhost` 로 다시 읽습니다. 시간이 얼마나 걸리는지는 2단계에서 잽니다.
- Codex 묶음 단위 재계산이 지금과 다른 합계를 낼 수 있습니다. 옛 · 새 파이프라인 비교 스크립트로 날짜 · 모델별 합계가 같은지 확인한 뒤 옛 경로를 지웁니다.

## 참고

- 측정 프로그램 · 힙 덤프: `C:\Users\USER\Downloads\poketoken-measure\` (레포 밖, 커밋 안 함)
- 측정 대상 코드: `src/Platform.Windows/UsageScanCache.cs`, `src/Platform.Windows/IncrementalLogScanner.cs`, `src/Application/ProviderCatalog.cs`, `src/Application/UsageRefreshService.cs`, `src/Core/UsageAggregation.cs`
