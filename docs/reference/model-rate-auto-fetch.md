# 모델 요금 자동 페치 (models.dev) 설계

상태: **구현 완료 (2026-10-02, M1–M3)**. 실측 검증 포함. 아직 커밋 전.

구현 요약:
- Core: `ModelsDevCatalog`(순수 파서) + `ModelPricing.ImportRemoteRates/ClearRemoteRates`(원자적 오버레이, 조회 순서 remote → 번들) + `antigravity-`/`-thinking` 변형 키 폴백
- Application: `ModelRateFeed`(페치+ETag 조건부 GET+디스크 캐시+침묵 폴백) + `ModelRateCacheCodec`(순수 코덱, schemaVersion 1)
- Ui: `App.OnStartup`에서 `LoadCached()` 후 기존 새로고침 깔때기에 `RefreshIfDueAsync` 얹음 — UI 코드 무변경
- 캐시: `%LocalAppData%\PokeTokenBar\model-rates.json`, 24h 갱신 주기
- 테스트: Core 12종(파서 선택 규칙/오버레이/교차검증/$0.001160 회귀), Application 9종(코덱 라운드트립/페치/304/간격 가드/불량 JSON/캐시 폴백) — 솔루션 585개 전부 통과

실측 (2026-10-02, 실제 네트워크+실제 로그):
- 라이브 페치 성공, 7개 타깃 모델 요금이 설계 문서 값과 정확히 일치 (gpt-6-luna 0.1/0.5/0.125/0.01 등)
- Codex 10/1 gpt-6-luna 사용분 60,977/5,091/1,036,544(in/out/cache-read) → **$0.0190 est.** (기존 미표시)
- opencode 10/2 glm-5.3 사용분 → **$12.36 est.**, glm-5.3-flash $0.03 (기존 전부 미표시)
- 간격 가드/ETag 재검증 정상 동작, 캐시 파일 생성 확인

남은 갭 (의도적): `composer-2.5`·무료 SKU(cost=0)는 Unavailable 유지, `antigravity-gemini-3-pro`는 `-preview` 이름 불일치로 Unavailable (gemini-3 preview 가격 미확정 — 기존 테스트 의존 유지).

## 배경

프로바이더 메뉴의 비용 표시는 `UsageBucket.Add`(`src/Core/UsageAggregation.cs`)의 3단 분기 —
`ExplicitCost`(Reported) → `ModelPricing` 요금표 추정(Estimated) → Unavailable — 로 동작하며,
Codex/opencode 로그에는 USD 비용이 없어(실측: Codex `token_count`엔 토큰 벡터만, opencode DB `cost` 필드는 12,600여 건 중 2건만 >0, 나머지 0) 요금표 커버 여부가 유일한 변수다.

현재 번들 표(`src/Core/ModelPricing.cs`)는 손관리라 신모델마다 릴리스를 거쳐야 한다.
최근 2일 Codex 사용 모델(`gpt-6-luna`, `gpt-6-sol`)이 표에 없어 비용이 미표시되는 것이 직접 동기.

## 실측 결과 (2026-10-02, 전부 검증된 사실)

### 엔드포인트

- `GET https://models.dev/api.json` — 5.05 MB, 다운로드 0.8초 (실측)
- `ETag` 존재(`"f9bd47…"`), `Cache-Control: must-revalidate`, CF 캐시 HIT → `If-None-Match` 조건부 GET으로 변경 시에만 전체 다운로드 가능
- 응답은 JSON. 최상위 키 = 프로바이더 ID 225개 (`providers` 래퍼 없음 — 주의)

### 스키마

```
{ "<providerId>": { "id", "name", "models": {
    "<modelId>": { ..., "cost": {
        "input": <USD per 1M tokens>,
        "output": <USD per 1M tokens>,
        "cache_read": <USD per 1M, optional>,
        "cache_write": <USD per 1M, optional> } } } } }
```

- 단위는 **per-million 그대로** — 번들 테이블(`ModelRate.PerMillion`)과 동일, 변환 불필요
- `cost` 자체가 없거나 모두 0인 엔트리 존재(무료/구독 SKU) → 임포트 스킵

### 커버리지 (실제 로그에서 관찰된 미커버 모델 전수)

| 모델 | 관찰처 | 선택된 소스 (우선규칙) | in/out/cw/cr (per-M USD) |
|---|---|---|---|
| gpt-6-luna | codex | `openai` | 0.1 / 0.5 / 0.125 / 0.01 |
| gpt-6-sol | codex | `openai` | 2 / 10 / 2.5 / 0.2 |
| gpt-5.4-mini | codex | `openai` | 0.75 / 4.5 / – / 0.075 |
| gpt-5.3-codex-spark | codex | `openai` | 1.75 / 14 / – / 0.175 |
| gpt-5.1-codex-max | codex, opencode | `opencode` | 1.25 / 10 / – / 0.125 |
| gpt-5.1-codex-mini | opencode | `opencode` | 0.25 / 2 / – / 0.025 |
| glm-5.3 / glm-5.3-flash | opencode(win) | `opencode`(=zai 동일가) | 1.4 / 4.4 / 0 / 0.26 |
| gemini-3-pro-preview | opencode(wsl) | 폴백(`302ai`) — `google` 엔트리는 cost 없음 | 2 / 12 / – / – |
| gemini-3-flash-preview | opencode(wsl) | `google` | 0.5 / 3 / – / 0.05 |
| deepseek-v3.2-speciale, grok-4.7 등 | opencode(wsl) | 폴백 | 존재 |

갭 (자동 커버 불가, 번들/매핑 규칙으로 대응):

- `antigravity-*` 4종: 해당 이름으로 없음. `"antigravity-"` prefix 제거 + `"-thinking"` 접미 제거로 `claude-opus-4-5`(anthropic 5/25/6.25/0.5), `gemini-3-pro-preview`(폴백) 매핑 가능 — 실측 확인
- `composer-2.5`: 카탈로그에 없음(3건 사용) → Unavailable 유지
- 무료 SKU(`glm-4.7-free`, `big-pickle`, `grok-code` 등): cost=0 → 스킵, Unavailable 유지

### 교차검증 — "공식 vendor 우선" 규칙의 정확성

동일 모델이 리셀러마다 다른 가격(nano-gpt는 gpt-5.6-sol을 2/10, azure는 4/20)로 등재된다.
우선순위 `openai > opencode > zai > google > anthropic > azure > (cost 있는 첫 엔트리)` 로 선택하면:

| 모델 | 번들 손관리 값 | models.dev 공식 vendor 값 | 일치 |
|---|---|---|---|
| gpt-5.6-sol | 4/20/5/0.4 | openai 4/20/5/0.4 | O |
| gpt-5.6-terra | 2/12/2.5/0.2 | openai 2/12/2.5/0.2 | O |
| gpt-5.5 | 5/30/–/0.5 | openai 5/30/–/0.5 | O |
| claude-opus-4-8 | 5/25/6.25/0.5 | anthropic 5/25/6.25/0.5 | O |
| gemini-2.5-pro | 1.25/10/–/0.125 | google 1.25/10/–/0.125 | O |

→ 규칙이 번들 테이블을 재현하므로, remote 병합 시 기존 추정치의 연속성이 유지된다.

### 실측 비용 샘플 (엔드투엔드)

실제 로그 `~\.codex\sessions\2026\10\01\rollout-…-01a0f48f….jsonl` (gpt-6-luna):
`last_token_usage` input_tokens=20,770 / cached=11,008 / output=148 →
파서 매핑 후 Input=9,762, CacheRead=11,008, Output=148 →
**$0.001160** (현재는 미표시; 장차 회귀 테스트 기대값으로 고정)

## 설계

### 레이어 (Core 순수성 규칙 준수)

- **Core** (`PokeTokenBar.Core`): I/O 없음
  - `ModelsDevCatalog.Parse(string json)` — 순수 함수: JSON → `IReadOnlyDictionary<string, ModelRate>`
    (우선순위 선택, 0-cost 스킵, prefix/접미 정규화 포함. 기존 `ModelPricing.ModelKey` 정규화 재사용 + `antigravity-`, `z-ai/`, `zai-org/` 등 추가)
  - `ModelPricing.ImportRemoteRates(IReadOnlyDictionary<string, ModelRate>)` — 원자적(Interlocked/Volatile) 오버레이 스왑.
    조회 순서: remote 오버레이 → 번들 테이블. remote가 번들 값을 덮어쓸 수도 있음(공식가 갱신 반영)하되, 파싱은 성공한 경우만 주입
- **Application** (`PokeTokenBar.Application`): I/O 담당
  - `ModelRateFeed` — `UpdateChecker`의 HttpClient 패턴 재사용 (UA, 15s 타임아웃)
    1. 시작 시 디스크 캐시 로드(`%LocalAppData%\PokeTokenBar\model-rates.json`: `{fetchedAt, etag, rates}`) → 즉시 주입
    2. 백그라운드 페치: `If-None-Match` → 304면 종료, 200이면 파싱 → 주입 + 캐시 저장
    3. 24h마다 재검증 (usage 리프레시 루프에 piggyback, 실패 무시)
  - 모든 실패 경로(타임아웃/불량 JSON/디스크 오류)는 침묵 폴백: 캐시 → 번들. UI 블록 없음
- **Ui**: 변경 없음 (`CostCoverage.Estimate` 배지 경로 그대로)

### 원칙

- 테스트는 네트워크 금지(리포지토리 규칙): 실측 api.json 슬라이스(수 KB)를 fixture로 커밋
- 스키마 방어: unknown 필드 무시, 파싱 실패 시 전체 폐기(부분 신뢰 금지)
- 캐시 파일에 `schemaVersion` 필드 — 불일치 시 무시하고 재페치

## 구현 마일스톤 (제안)

1. **M1 Core**: `ModelsDevCatalog` 파서 + `ModelPricing` 오버레이 + fixture 기반 테스트 (교차검증 5건, gpt-6-luna $0.001160 회귀 포함)
2. **M2 Application**: `ModelRateFeed` + 디스크 캐시 + mock HttpClient 테스트 (성공/304/타임아웃/불량 JSON/캐시 폴백)
3. **M3 연결**: 앱 시작 파이프라인에 연결, 실기기 실측(프로바이더 메뉴에 codex/opencode est. 비용 표시 확인) — UI 코드 무변경 목표

## 리스크 / 한계

- models.dev는 커뮤니티 관리 카탈로그 — 가격 정확성 무보증. 단 공식 vendor 엔트리와 번들 손관리 값의 일치를 실측 확인
- 구독 플랜(ChatGPT/zai coding plan) 실제 청구액과는 무관한 "API 단가 기준 추정" — 기존 Claude est.와 동일한 의미로 `est.` 라벨 유지
- 5MB 응답: ETag 조건부 GET으로 상시 비용은 304 왕복 수준
- 로컬 실측 기준 위 모델 요금은 2026-10-02 시점 값 — 구현 시 재검증
