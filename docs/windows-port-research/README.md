# Windows Port Research — Milestone 0

Empirical findings captured on the target machine on 2026-09-22. This document
satisfies the Milestone 0 exit criteria of [windows-port-plan.md](../windows-port-plan.md):
documented roots and sanitized fixtures for all four initial sources.

## Research environment

| Component | Value |
|---|---|
| OS | Windows 11 25H2 (build 26200) |
| Windows Claude Code | 2.1.198 (`C:\Users\<user>\.local\bin\claude.exe`) |
| Windows Codex CLI | 0.140.0 (`C:\Users\<user>\AppData\Local\Programs\OpenAI\Codex\bin\codex.exe`) |
| WSL | WSL2, distribution `Ubuntu-24.04` (also `docker-desktop`, not a user distro) |
| WSL Claude Code | 2.1.278 (`/home/<user>/.local/bin/claude`) |
| WSL Codex CLI | npm install under nvm (`~/.nvm/versions/node/v24.11.0/bin/codex`); sessions record `cli_version` 0.153.4 |

## Verified usage roots

| Source | Root | Notes |
|---|---|---|
| Windows Claude | `%USERPROFILE%\.claude\projects\<munged-cwd>\<sessionId>.jsonl` | 17 `.jsonl` files. Munging: `:` and `\` become `-`; a session whose cwd was a WSL UNC path produces dirs such as `--wsl-localhost-ubuntu-24-04-home-...` |
| WSL Claude | `\\wsl.localhost\Ubuntu-24.04\home\<user>\.claude\projects\<munged-cwd>\...` | 652 `.jsonl` files, actively written |
| Windows Codex | `%USERPROFILE%\.codex\sessions\YYYY\MM\DD\rollout-<ts>-<uuid>.jsonl` | 12 rollout files |
| WSL Codex | `\\wsl.localhost\Ubuntu-24.04\home\<user>\.codex\sessions\YYYY\MM\DD\rollout-*.jsonl` | 1,793 rollout files, actively written |

Additional observations:

- `archived_sessions/` does **not** exist in either environment today. Both
  `.codex` directories have `session_index.jsonl`; the macOS parser does not
  read it and the Windows port should not depend on it.
- Both `.codex` directories contain sqlite state (`logs_*.sqlite`,
  `state_5.sqlite`, `goals_*.sqlite`, …). These are not usage sources for the
  current parser; documented as present-but-ignored.
- Files that must never be read or copied:
  `~/.claude/.credentials.json` (WSL), `%USERPROFILE%\.claude\settings.json`
  secrets, and `auth.json` in both `.codex` roots.

## Record format verification

All four sources match the shapes consumed by the macOS parser
(`Sources/PokeTokenBar/Core/LocalUsageReader.swift`), with the differences
listed below.

### Claude (`projects/**/*.jsonl`)

- Usage lines are `type:"assistant"` records with
  `message.usage.{input_tokens, output_tokens, cache_creation_input_tokens,
  cache_read_input_tokens}`, `message.model`, `message.id`, `timestamp`.
- Windows-side sessions **may omit `requestId` entirely** (observed on Claude
  Code 2.1.179 records; WSL 2.1.25x records carry `req_…`). Dedup key
  `message.id + "|" + requestId` must treat a missing `requestId` as empty.
- `usage` contains many extra fields (`server_tool_use`, `cache_creation`,
  `iterations`, `service_tier`, …). Parsers must ignore unknown fields.
- WSL records add `effort`, `origin`, and stream-restart duplicates of the
  same `message.id` — same dedup semantics as macOS.

### Codex (`sessions/**/rollout-*.jsonl`)

- `session_meta` (`payload.id`, else `payload.session_id`; `forked_from_id` /
  `parent_thread_id` for forks; `thread_source:"subagent"`), `turn_context`
  (`payload.model`), and `event_msg/token_count` with
  `payload.info.{total_token_usage, last_token_usage}` all match the macOS
  parser.
- Newer WSL CLI (0.153.4) adds: top-level `ordinal` on every line,
  `payload.session_id` alongside `payload.id`, `payload.history_mode`,
  and `info.model_context_window`. All are ignorable extras.
- Old sessions may emit `token_count` events with `info: null` and only
  `rate_limits` (observed in an April 2026 rollout). The reader must skip
  records without `info` rather than treat them as zero-token turns.
- Timestamps use variable fractional-second precision (`…29.74Z`,
  `…29.741Z`). ISO-8601 parsing must not assume exactly 3 digits.

### Cross-environment identity

- Windows Claude and WSL Claude are separate installations with separate
  session ids. A Windows session whose cwd was a WSL UNC path is a *distinct*
  session from any WSL-side session over the same repository — not a
  duplicate, and must not be merged.
- Duplicate exposure is still possible if a WSL root is discovered both via
  WSL discovery and via a user-configured additional scan root; the
  plan's stable-root-identity dedup covers this.

## File-lock and active-write behavior (empirical)

Tests run 2026-09-22 (PowerShell 7, WSL2 `\\wsl.localhost` UNC):

| Test | Setup | Result |
|---|---|---|
| A | Windows reader vs. Windows `FileShare.None` exclusive open | Read fails with `IOException` |
| B/C/D | WSL process appending a record every 100 ms (60 total) while holding `flock` for 3 s mid-run; Windows reads via UNC every 150 ms | 38/38 reads succeeded, 0 errors, 0 partial/malformed lines |
| E | WSL `flock` only (held 4 s), no writer | 16/16 UNC reads succeeded |

Conclusions for the scan layer:

- Linux advisory locks (`flock`) do not propagate across the UNC boundary and
  never block Windows reads.
- Appends observed during the test were newline-terminated when read, but the
  reader should still tolerate a partial final line (defensive).
- Open files with read/write share (`FileShare.Read` at minimum) and retry on
  `IOException` for Windows-native exclusive locks.
- Confirms the plan's choice of periodic incremental scanning over file
  watchers.

## Sanitized fixtures

`fixtures/` contains one file per source, generated from real sessions and
sanitized:

| File | Source session |
|---|---|
| `windows-claude.jsonl` | Windows Claude 2.1.179, GLM proxy usage records |
| `wsl-claude.jsonl` | WSL Claude 2.1.252, `claude-opus-5` with cache fields |
| `windows-codex.jsonl` | Windows Codex 0.140.0 rollout |
| `wsl-codex.jsonl` | WSL Codex 0.153.4 rollout (newer format: `ordinal`, `session_id`, `model_context_window`) |

Sanitization rules applied:

- All UUIDs, `msg_…`, and `req_…` ids replaced with zero-padded synthetic
  values (unique per record within a file).
- `cwd` redacted (`C:\redacted` / `/redacted`), `gitBranch` redacted,
  message content replaced with `[redacted]`.
- `session_meta.source`, `base_instructions`, developer/user instructions,
  and tool outputs dropped.
- Token counts, models, timestamps, and record structure preserved verbatim
  for parser testing.

## Milestone 0 status: complete

All four initial sources have documented roots, verified formats, and
sanitized fixtures. Next step: Milestone 1 (pure core), after fixing the
implementation stack.
