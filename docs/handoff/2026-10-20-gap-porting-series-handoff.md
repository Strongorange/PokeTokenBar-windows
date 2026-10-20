---
summary: "v2.5.4→#448 업스트림 동기화 + 0.27.0 릴리스(폼·리캡·백업·검색·OpenCode v2) 세션 핸드오프. 남은 갭 우선순위와 이번에 얻은 함정들."
read_when:
  - 갭 포팅 작업을 이어서 시작할 때 (다음 슬라이스 선택 전 필수)
  - 릴리스 절차·gh CLI 자동화를 다룰 때
  - RecapWindow/리캡/폼/스냅샷 코드를 수정할 때
---

# 2026-10-20 핸드오프 — 갭 포팅 시리즈 (v0.27.0 출시까지)

## 세션 결과 요약

- **upstream 동기화**: v2.5.4~v2.5.6 + #447, 이후 #448까지 머지 완료 (main = `d82867b` 기준, 충돌 4건 수동 해결 이력 있음)
- **릴리스 0.27.0 출시**: https://github.com/Strongorange/PokeTokenBar-windows/releases/tag/v0.27.0
  - **gh CLI가 이제 인증됨** (Strongorange, repo 스코프) — 릴리스 생성+에셋 업로드 자동화 성공. 단 **기본 대상이 upstream(chattymin)으로 잡히므로 반드시 `-R Strongorange/PokeTokenBar-windows` 명시**.
  - 절차: CHANGELOG.md/ko.md 양쪽 섹션 → csproj `<Version>` → 커밋/푸시 → `scripts/publish-windows.ps1`(빌드+로컬 설치 교체+zip+노트) → 태그 푸시 → `gh release create ... -R ... --notes-file artifacts/release/release-notes-vX.Y.Z.md`
- **테스트**: 695개 전부 통과 (Core 275 + Providers 110 + Platform 65 + UI 10 + Application 235)
- **커밋별 독립 검증 패턴 확립**: 각 커밋을 `git worktree add --detach` 임시 디렉토리에서 격리 빌드+전체 테스트 → 워크트리 제거. 이 세션 5커밋 모두 적용.

## 이번에 포팅한 것 (커밋 순)

| 커밋 | 기능 |
|---|---|
| `b362d49` | upstream 머지 (README는 포크 유지, defect-log 양쪽 보존, default.profraw 제거) |
| `b88b113` | 폼 컬렉션 #376 — `PokemonForms.cs`(Core 카탈로그), 저장 키 `form`/레거시 `unownForm`, 폼 이름은 **스냅샷에 임베드** (macOS는 런타임 30일 캐시 — 포트 유일 설계 편차, `scripts/generate-pokemon-snapshot.ps1`이 `pokemon-form` 리소스 74개 fetch) |
| `2f673f7` | 업데이트 쿨다운 #321 — 검증된 릴리스에만 스탬프 + in-flight 가드 + 프리릴리스 태그 거부 |
| `ae902b5` | 토큰 포맷 #365/#418 — 999,950→"1M", percent/cost 반올림 후 구간 분기 |
| `b97b387` | 도감 검색·정렬·필터 #329 — `DexQuery.cs`(매처+정렬 계약 단독), 악센트 무시 매칭 |
| `1b3b919` | 세이브 자동 백업 #330/#446 — `SaveSnapshots.cs`, 12h 간격·10개 보존·손상 복구·읽기-전-프루닝 |
| `7a14495` | OpenCode v2 #363 — `session_message` 병합 + v1 파서 role 완화(이식 중 발견한 잠복 버그) |
| `b8758be`+`39b813f`+`8b16b98`+`afe8c7f` | 사용량 리캡 #332/#367/#368 — `UsageRecap.cs`(Core 원장+계산) → `RecapQuery`(뷰모델) → `RecapWindow`(Pokédex LCD 카드, 검토자 토론 후 하이브리드 확정) |

## 남은 갭 우선순위 (사용자에게 보고 완료, 1 tier 시작 전)

1. **한국어 조사 자동 선택** #356 — `Sources/PokeTokenBar/Core/KoreanParticle.swift` 포팅 (종성 받침 계산 → 을/를, 이/가, 으로/로). DashboardText의 `을(를)`/`(으)로` 병기 문자열 전면 교체. 한국어 사용자에게 매일 보임.
2. **타입 배지** #391 — 게임 색상 타입 배지, 홈·도감 상세. 스냅샷 `details.types` + `resourceNames` 현지화 이미 준비됨.
3. **부화 알림 실제 샤이니 확률** #351 — 부적 보유 시 실제 확률 표기.
4. (2 tier) 게임 엔진 수정 묶음: Shedinja HP=1 #422, 진화체인 중복 제거 #424, 공개 Ditto 부스트 #378.
5. (3 tier) 일일 트렌드 프로바이더별 스택 #348, 성장 바 호버 #392, 슬라이더 라벨 #398, 유휴일 이력 #377.
6. (4 tier) 러시아어 #372 (규모 큼), 도감 스크롤 페이징 #393, 스프라이트 클릭→항목 #394, Back 복원 #435.
- **계속 연기**: 프로바이더 11종(실사용 없음), 공식 계정 한도 전체(Keychain 의존), 리캡 PNG export.

## 알려진 모듈화 부채 (사용자에게 보고 완료)

- `RecapWindow.xaml.cs`의 WPF 빌더(미터·리드아웃·졸업 스트립)가 코드비하인드에 있음 — 레포 자기 규약(M25: 순수 결정/GameTabPresentation + 렌더러 분리) 위반. 계산은 `RecapQuery`가 순수하게 분리돼 있어 리스크 낮음. 다음 대형 기능 전에 렌더러 추출 권장.
- `CompanionEngine.cs` ~1,600행 — BuildView 계열 렌더 빌더 분리 검토 시점.
- LCD 팔레트(#82FA9E 등)가 XAML/코드비하인드 이중 정의 — 상수 통합 여지.

## 이번 세션의 함정 (재발 방지)

- **WPF `StackPanel`에 `Spacing` 프로퍼티 없음** (UWP/MAUI 전용) — 세로 간격은 자식 Margin으로.
- **테마 키는 `WindowBgBrush`** — `WindowBrush` 아님. 존재하지 않는 DynamicResource는 조용히 투명이 되고 오프스크린 캡처에서 검은색으로 보임. 스크린샷 검토(비전 서브에이전트)가 이 버그를 잡았음 — UI 작업 시 캡처 하니스 + 비전 검증 패턴 유효 (`%TEMP%\opencode\ptb-recap-shot` 하니스 참조).
- **`UsageEntry`는 `readonly record struct`** — `Assert.NotNull` 불가, `is { } entry` 패턴 매칭 사용 (CS0173: `long`과 `null` 삼항도 주의).
- **DateTimeOffset(DateTime, TimeSpan) 생성자**는 Kind 미지정 DateTime 필요 — `.NET 10`에서는 (y,m,d,h,min,s,offset) 생성자가 안전.
- **gh CLI 기본 repo가 upstream** — 항상 `-R Strongorange/PokeTokenBar-windows`.
- **publish-windows.ps1은 실행 중인 앱을 죽이고 교체**하며 재실행하지 않음 — 수동 확인 필요.
- 도감·리캡 주간 계산은 **일요일 시작 고정** (`RecapPeriod.WeekStartCulture` 상수로 명명) — 로캘 전환은 주 총량·히트맵과 함께 전환해야 함.

## 다음 스레드 즉시 작업

1. `git pull` (main이 v0.27.0 + #448 머지까지 푸시됨 — 이 핸드오프 커밋 제외)
2. 1 tier부터: 조사 #356 → 타입 배지 #391 → 샤이니 확률 #351, 각각 독립 커밋
3. 검증: `dotnet test PokeTokenBar.Windows.slnx` + 커밋별 워크트리 격리 검증
