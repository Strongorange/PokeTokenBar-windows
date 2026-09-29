<div align="center">

<img src="assets/icon.png" width="128" alt="PokeTokenBar 아이콘">

# PokeTokenBar

**당신의 AI 코딩 토큰을 포켓몬으로 — 시스템 트레이에서.**

[![Release](https://img.shields.io/github/v/release/Strongorange/PokeTokenBar-windows?color=444d56&label=release)](https://github.com/Strongorange/PokeTokenBar-windows/releases)
[![Windows](https://img.shields.io/badge/Windows-10%2F11-0969da)](https://www.microsoft.com/windows/)
[![.NET](https://img.shields.io/badge/.NET-10-512bd4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-3fb950)](LICENSE)

[English](README.md) · **한국어**

</div>

macOS 메뉴바 앱 [chattymin/PokeTokenBar](https://github.com/chattymin/PokeTokenBar)의 개인용 Windows 이식판입니다. PokeTokenBar는 이미 쓰고 있는 AI 코딩 토큰 — Claude Code, Codex, OpenCode의 Windows·WSL 사용량을 — 자라나는 **포켓몬 파트너**로 바꿔 줍니다. 토큰을 쓰면 알이 부화하고, 실제 진화 계보를 따라 진화하며, 최종 진화 후 도감에 졸업하고, 다시 새 알이 시작됩니다. 파트너 아래에는 정확한 사용량 트래커가 있습니다. 오늘과 이번 달의 토큰·비용을 로컬 로그에서 직접 읽고, 월간 일별 추이 차트와 모델별 분해도 보여 줍니다.

> 토큰 사용량은 로컬 Claude Code·Codex·OpenCode 데이터에서 직접 읽습니다. Windows와 모든 WSL 배포판의 로그를 중복 제거해 하나로 합산합니다(`totalTokens` = input + output + cache, 로컬 날짜 기준). 외부 CLI는 필요 없습니다. 비공식·비상업 포켓몬 팬 프로젝트입니다 — [라이선스와 면책](#라이선스와-면책)을 참고하세요.

## 어떻게 자라나요

1. 🥚 **평소처럼 코딩하세요.** Claude Code, Codex, OpenCode에서 태우는 토큰이 알을 풉니다 — 따로 돌릴 것은 없습니다.
2. 🐣 **부화.** 알은 앱에 내장된 1~5세대 데이터에서 공식 capture rate 가중으로 부화합니다 — 흔한 포켓몬은 자주, 전설은 아주 드물게요. 부화할 때마다 25종 성격 중 하나가 정해지고, 아주 특별한 우연으로 **✨ 이로치**가 태어납니다.
3. ⚡ **진화.** 계속 코딩하면 실제 진화 트리(1/2/3단계, 분기 포함)를 따라 자라며, 단계마다 작은 축하 연출이 재생됩니다.
4. 🎓 **졸업과 수집.** 최종 진화형이 성장 임계에 도달하면 **도감**에 영구 보존되고 — 희귀할수록 오래 걸립니다 — 새 알이 도착합니다.
5. 🛒 **상점에서 쓰기.** 그동안 사용한 토큰이 곧 재화입니다. 현재 포켓몬을 성장시키는 **이상한 사탕**, 성격을 다시 정하는 **민트**, 이로치 확률을 영구히 올리는 **이로치 부적**, 지금 파트너를 보내고 다시 시작하는 알을 살 수 있습니다. 알은 세 종류입니다 — 일반 **포켓몬 알**, 고급 이상이 확정인 **고급 알**, 희귀 이상이 확정인 **희귀 알**.
6. 🔤 **안농, 28가지 모습 전부.** 부화한 안농 모습마다 종 정보 창에 자리가 채워지고, 도감 타일에는 수집한 모습 수가, 상세 창에는 모습 선택 그리드가 나타납니다.

## 둘러보기

- **트레이 파트너.** 움직이는 Gen-V 스프라이트가 알림 영역에 삽니다. 툴팁에 오늘·이번 달 합계가 표시되고, 우클릭 메뉴에서 새로 고침, 대시보드, 설정, 플로팅 펫, 진단 폴더, 종료를 쓸 수 있습니다. 새 GitHub 릴리스가 나오면 트레이 풍선 알림과 대시보드 배너가 뜹니다(이 버전 건너뛰기 지원). "업데이트"는 브라우저에서 릴리스 페이지를 여는 동작입니다 — 자동 내려받기는 하지 않습니다.
- **대시보드 · 사용량 탭.** 이번 달 일별 추이 차트(막대에 마우스를 올리면 그날의 토큰과 비용, 최댓값 표시, 오늘 강조), 두 개 이상 모델을 쓴 날의 오늘 모델별 분해, 도구별 행(사용 가능 여부 / 오늘 / 이번 달, 토큰과 비용), 합계 푸터로 구성됩니다.
- **대시보드 · 게임 탭.** 움직이는 스프라이트와 간단 전투 정보가 있는 파트너 카드, 상점과 가방, 스프라이트 타일 그리드로 된 **도감**(이로치 ✨, 키우는 중 표시, 상세 툴팁)이 있습니다. 종을 더블클릭하면 상세 창이 열립니다 — 지금까지 키운 각 개체의 레벨·성격·특성·개체값·계산된 능력치·배운 기술, 그리고 종족값과 전체 기술 목록까지, 모두 선택한 언어로 표시됩니다.
- **설정 창.** UI 언어(**한국어 / 영어 / 일본어 / 스페인어 / 프랑스어 / 포르투갈어 / 독일어**, 바로 다시 지역화), 도구별 추가 스캔 폴더, 성장·상점 난이도(10~200%), 플로팅 펫 설정, 업데이트 영역(알림 켜기/끄기, 지금 확인)이 있습니다.
- **바탕화면 플로팅 펫.** 파트너를 바탕화면에 48~384px 크기로 둘 수 있습니다. 드래그로 원하는 곳에 옮기고, 더블클릭하면 대시보드가 열리며, 우클릭으로 숨깁니다.
- **세이브 내보내기 / 가져오기.** 파트너, 도감, 가방, 언어 설정이 한 파일에 담깁니다 — 대시보드 하단 버튼으로 컴퓨터 사이에서 게임을 옮길 수 있습니다.

## 함께 쓰는 도구

| 도구 | 집계 | 기본 로그 경로 |
|---|---|---|
| **Claude Code** | 오늘 · 이번 달 | `%USERPROFILE%\.claude\projects` + WSL `~/.claude/projects` |
| **Codex** | 오늘 · 이번 달 | `%USERPROFILE%\.codex\sessions` (+ `archived_sessions`) + WSL `~/.codex/sessions` |
| **OpenCode** | 오늘 · 이번 달 | `%USERPROFILE%\.local\share\opencode` + WSL `~/.local/share/opencode` |

Windows와 WSL의 사용량은 중복 제거 후 합산됩니다 — 두 환경에서 함께 코딩해도 하나의 게임입니다. 공식 계정 한도는 읽지 않으며, 자격 증명은 어떤 형태로도 만지지 않습니다. 도구가 로그를 다른 곳에 둔다면 설정에서 도구별 추가 스캔 폴더를 지정하세요. 추가 경로는 선택한 도구의 파서만 읽고, 기본 경로를 대체하지 않고 더합니다.

## 설치

Windows 10/11이면 충분합니다. 실행 파일에 .NET 10 런타임이 포함된 자체 포함 빌드입니다.

1. [최신 릴리스](https://github.com/Strongorange/PokeTokenBar-windows/releases/latest)에서 `PokeTokenBar-<version>-win-x64.zip`을 내려받아 아무 곳에나 압축을 풉니다.
2. `PokeTokenBar.exe`를 실행하면 알림 영역에 자리 잡습니다.

실행 파일에 서명이 없어서 처음 실행 시 SmartScreen이 "Windows가 PC를 보호했습니다" 창을 보일 수 있습니다 — **추가 정보 → 실행**을 선택하세요.

앱 상태, 설정, 스프라이트 캐시, 진단 로그는 `%LOCALAPPDATA%\PokeTokenBar` 아래에 저장됩니다(`PTB_STATE_DIR` 환경 변수로 위치를 바꿀 수 있습니다). 이 폴더를 지우면 앱이 초기화되고, 실행 파일을 지우거나 옮겨도 세이브에는 영향이 없습니다.

## 소스에서 빌드

[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)가 필요합니다.

```powershell
dotnet test PokeTokenBar.Windows.slnx    # 전체 단위 + 통합 테스트
dotnet run --project src/Ui              # 디버그 실행
```

`scripts/publish-windows.ps1`는 개인용 설치에 쓰는 단일 파일 자체 포함 실행 파일을 만듭니다. 아키텍처, 계층 구조, 마일스톤 전체 기록은 [docs/windows-port-plan.md](docs/windows-port-plan.md)에 있습니다.

## 데이터 소스

| 소스 | 용도 | 비고 |
|---|---|---|
| `%USERPROFILE%\.claude\projects\**\*.jsonl` + WSL | Claude Code 오늘/이번 달 | 직접 읽기; 메시지 id로 중복 제거; 증분 캐시 |
| `%USERPROFILE%\.codex\sessions\**\*.jsonl` (+ `archived_sessions`) + WSL | Codex 오늘/이번 달 | `token_count` 이벤트 |
| `%USERPROFILE%\.local\share\opencode\opencode.db` + WSL | OpenCode 오늘/이번 달 | SQLite 읽기 전용; WSL 데이터베이스는 UNC 경로에서 SQLite 잠금이 불가해 지문 기반으로 로컬 임시 폴더에 복사해 읽습니다 |
| `raw.githubusercontent.com/PokeAPI/sprites` | 포켓몬·아이템 스프라이트 | 실행 중 내려받기; 앱 데이터 폴더에 디스크 캐시 |
| `api.github.com` | 업데이트 확인 | 최신 릴리스 태그; 시작 시와 대시보드 열 때, 30분 디바운스 |

포켓몬 기본 데이터 — 종, 진화 계보, 현지화 이름, 능력치, 특성, 기술 — 는 [PokéAPI](https://pokeapi.co/)로 생성한 스냅숏을 앱에 **함께 배포**합니다. 게임 자체는 완전히 오프라인으로 동작하고, 실행 중 내려받는 것은 스프라이트뿐입니다.

## 프라이버시

- **로컬 우선.** 사용량은 로컬 로그 파일에서 읽습니다. 아무것도 올리지 않고, 모델 호출을 하지 않으며, 자격 증명이나 세션 키를 일절 읽지 않습니다 — 자격 증명 저장소 연동 자체가 없습니다.
- **외부 요청**은 두 호스트뿐입니다: `raw.githubusercontent.com`(스프라이트)와 `api.github.com`(업데이트 확인). 어느 쪽도 사용량 로그, 프롬프트, 프로젝트 경로를 실어 나르지 않습니다.
- **진단 로그**는 앱 데이터 폴더 아래 로컬 텍스트 로그입니다(트레이 메뉴 → 진단 폴더 열기). 파싱·새로 고침 실패를 기록하되 자격 증명이나 로그 전문은 남기지 않습니다.

## macOS 원본과 다른 점

개인 이식판이어서 그대로 복제한 앱이 아닙니다:

- **메뉴바 글자 대신 트레이 + 대시보드** — Windows 알림 영역 아이콘은 macOS `NSStatusItem` 방식의 글자를 넣을 수 없습니다.
- **열세 도구가 아니라 세 도구**(Claude Code, Codex, OpenCode)를 지원합니다.
- **공식 한도 추적 없음** — 자격 증명 저장소 접근, 5시간/주간 한도 표시와 알림이 없습니다.
- **포켓몬 기본 데이터를 앱에 포함** — macOS는 실행 중 PokéAPI에서 종 데이터를 받지만, 이식판은 오프라인 스냅숏을 배포합니다.
- **자동 업데이트 없음** — 새 버전 배너는 브라우저에서 릴리스 페이지를 열어 줍니다.

## 라이선스와 면책

**MIT** — [LICENSE](LICENSE)를 참고하세요. MIT 라이선스는 이 프로젝트의 원본 소스 코드에만 적용되며, 앱을 통해 접근하는 제3자 상표, 아트워크, 데이터에 대한 권리를 부여하지 않습니다.

PokeTokenBar는 **비공식·비상업 팬 프로젝트**입니다. Nintendo, Game Freak, Creatures Inc., The Pokémon Company와 **제휴·승인·후원 관계가 없습니다**. "포켓몬"과 관련 이름·캐릭터·이미지는 각 소유자의 상표이며 저작권으로 보호됩니다.

- 앱에는 [PokéAPI](https://pokeapi.co/)에서 생성한 기계 생성 데이터 스냅숏(이름, 진화 계보, 능력치, 기술)이 포함되고, 스프라이트는 실행 중 PokéAPI 스프라이트 저장소에서 내려받아 사용자 장치에 캐시됩니다. 이름·데이터·스프라이트 이미지의 소유권은 각 소유자에게 있습니다.
- 이 앱은 **개인적·비상업 용도로만** 무료로 제공됩니다.
- 권리자로서 이 프로젝트에 대해 확인할 사항이 있다면 이슈를 열어 주세요. 빠르게 답변드리겠습니다.

*어떠한 보증도 없이 "있는 그대로" 제공됩니다. 이 고지는 법률 자문이 아닙니다.*

## 감사의 글

- [chattymin/PokeTokenBar](https://github.com/chattymin/PokeTokenBar) — 이 이식판의 기반이 된 macOS 원본.
- [PokéAPI](https://pokeapi.co/) — 포켓몬 데이터와 스프라이트.
