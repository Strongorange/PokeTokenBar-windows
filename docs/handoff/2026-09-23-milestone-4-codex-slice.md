# Handoff — Milestone 4: Codex vertical slice

## Goal
Implement Milestone 4 of the Windows port: parse Codex `rollout-*.jsonl` logs into
`UsageEntry`s through the M2 scan foundation, map turn deltas, resolve fork replay
and subagents with canonical dedup, cache parsed rollouts per file, and expose a
provider snapshot — with verified token totals for Windows-only, WSL-only, mixed,
and duplicate-discovery fixture scenarios.

## Workspace
- Checkout: `C:\Users\USER\my-pjts\poketoken-bars-windows` (Windows 11 25H2 machine, target environment itself)
- Branch: `main` / Base: `main` (this repo pushes directly to `main`; no PR flow used so far)
- Commits so far: `bb25608` docs: port plan · `f763205` docs: research + fixtures (M0) · `8083930` feat: pure core (M1) · `55b7701` feat: discovery + scan foundation (M2) · `bb422fd` feat: Claude provider slice (M3) · plus docs commits
- Commit convention: English conventional commits (`feat:`, `fix:`, `docs:`)

## Current State
- Done — M3: `src/Providers` (pure `ClaudeLogParser`, `IUsageProvider`, `UsageProviderSnapshot`, `ClaudeUsageProvider`) + `Tests/Providers.Tests`; 162 tests green (`104 Core + 38 Platform.Windows + 20 Providers`), all committed and pushed (`bb422fd`)
- Done — M3 manual verification on this machine: both real Claude roots scanned read-only, 527 files parsed 0 failed, today's deduped totals non-zero, warm refresh 525 cached / 2 reparsed (WSL root actively written — expected)
- Not implemented: Codex parsing (`M4`); Application scheduling; UI
- Solution: `PokeTokenBar.Windows.slnx` (`.slnx`, not `.sln` — dotnet SDK 10)

## Locked Decisions
- Stack C# / .NET, target `net10.0`. Do not retarget without asking.
- Layering per plan: `Core` pure, `Providers` parse logs (references Core only, no Windows APIs), `Platform.Windows` owns paths/WSL/settings/diagnostics, `Application` schedules/aggregate/cache, `UI` consumes results only. Providers must not reference `Platform.Windows`; scanner wiring stays in tests until `Application` exists.
- **Codex is NOT a per-file `List<UsageEntry>` parser.** A fork file's usage can only be
  finalized by comparing it with its parent rollout, so the per-file parse produces a
  **parsed rollout** (sessionID, parentSessionID, forkedAt, isSubagent, events with usage
  states) and the cache stores that (`UsageScanCache<TPayload>` with
  `TPayload = CodexParsedRollout`). macOS parity: `LocalUsageCache` caches
  `CodexParsedRollout` blobs, not entries (`codexParserVersion = 6` there; ours starts at 1).
- Provider contract generalizes to the payload: recommend
  `IUsageProvider<TPayload>` with `TPayload ParseFile(string path, IReadOnlyList<string> lines)`
  and `UsageProviderSnapshot BuildSnapshot(IEnumerable<TPayload> filePayloads)`;
  `ClaudeUsageProvider : IUsageProvider<List<UsageEntry>>` (mechanical M3 adjustment),
  `CodexUsageProvider : IUsageProvider<CodexParsedRollout>` with `ProviderId "codex"` /
  `DisplayName "Codex"` (macOS parity). Adjust only if a strictly cleaner split appears.
- Turn delta mapping (from `payload.info.last_token_usage`): `Input = max(0, input_tokens −
  cached_input_tokens)`, `CacheRead = cached_input_tokens`, `Output = output_tokens`
  (already includes reasoning), `CacheWrite = 0` — macOS parity; `cache_write_input_tokens`
  feeds the usage-state fingerprint but is NOT mapped into entries.
- Model: `turn_context.payload.model` is sticky for subsequent token_count events; default
  `"codex"`; a missing model keeps tokens but stays unpriced until a turn_context identifies it.
- Line pre-filter markers before JSON parsing: `session_meta`, `"model"`, `token_count`
  (most lines are response_item/delta).
- In-file normalization: consecutive token_count events with an identical full
  `(cumulative|last)` state within the same session collapse to one.
- #278 total-only turns: when every `last` component is 0 but `last.total_tokens > 0`,
  trust the total ONLY when (no cumulative) OR (cumulative components all 0 with positive
  total) OR (`last.total == cumulative.total`); then `Input = lastTotal, Output = 0,
  CacheRead = 0, CostUnavailable = true`. Never trust the fork post-replay zero-context shape.
- Old records: `token_count` with `info: null` (April 2026 rollouts) — skip the record
  entirely, never a zero-token turn.
- Canonical entry id: `codex|{ownerSessionID}|{epoch}|{cumulativeFingerprint}|{lastFingerprint}`;
  epoch increments when the cumulative vector decreases or the owner session changes.
  Positional `codex|{file}|{turn}` only for legacy events without state/session id.
- Final dedup for codex is **keep-earliest by canonical id**, NOT keep-max (the token
  vector is already part of the id). Keep this inside the codex resolver; do not change
  `UsageAggregation.DedupKeepMax` or add provider branches to Core.
- Fork replay: child drops the leading common prefix of identical full usage states vs the
  best parent (largest `replayCount > 0`). Subagent rollouts are never trimmed
  (`thread_source:"subagent"` or `source.subagent`). Parent not found (manual fork):
  fallback trims leading events whose inter-event gap < 1s (`forkReplayMaximumGap`),
  subagents exempt. Unmatched suffix after an embedded parent meta is child-owned.
- Parent discovery: candidates filtered by filename containing the parent id (usable only
  when the id is ≥4 chars with a letter/digit — degenerate ids match everything,
  measured 0.009s → 18.2s on 300 files), then a 1MiB-capped metadata probe that only
  decodes newline-complete lines (multibyte char cut ⇒ whole-string decode failure).
  Adoption always verifies the actual payload session id. Probe outcome nil-vs-throw must
  stay distinct: transient I/O failure must not be persisted as "no session id".
- Session id resolution in `session_meta.payload`: `id` first, else `session_id`;
  parent: `forked_from_id` else `parent_thread_id`; empty strings are null.
- Roots: M2 discovery already emits `UsageRootKind.CodexSessions` and optional
  `CodexArchivedSessions` for Windows and every WSL distro home. `archived_sessions/` does
  not exist on this machine today — treat as optional, include when present.
- Windows/WSL copies are separate installations — never merge sessions across them;
  duplicate exposure (same rollout via two roots) is handled by canonical ids + keep-earliest.
- Never read/copy: `auth.json` in `.codex` roots (also `~/.claude/.credentials.json`,
  `%USERPROFILE%\.claude\settings.json` secrets).
- No provider-specific branches in shared aggregation. Comments: none unless asked.
  English commits. Do not update git config, force-push, or create PRs unless asked.
- Git: remote `origin` = `git@github-personal:Strongorange/PokeTokenBar-windows.git`.
  Commit + push only when the user asks.

## Contracts
- Normalized record: `UsageEntry` (`src/Core/UsageAggregation.cs:5`) — unchanged from M3.
- Codex record shapes (envelope): top-level `timestamp` + `type` + `payload`;
  `session_meta` (payload id/session_id, forked_from_id/parent_thread_id,
  thread_source, source.subagent, cli_version), `turn_context` (payload.model), and
  `event_msg` with `payload.type:"token_count"`, `payload.info.{total_token_usage,
  last_token_usage, model_context_window}` plus `rate_limits`. Timestamps use variable
  fractional precision (`…29.74Z`, `…29.741Z`) — parse via `IsoDates.Date`.
- Fixture deltas: windows-codex (CLI 0.140.0) has no `ordinal`, no `payload.session_id`,
  no `cache_write_input_tokens`; wsl-codex (0.153.4) adds `ordinal` on every line,
  `payload.session_id`, `history_mode`, `info.model_context_window`,
  `cache_write_input_tokens` — all ignorable extras for parsing.
- Hand-verified fixture totals (RE-COMPUTE from the fixture files before asserting —
  they are the ground truth, and check the invariant: sum of entry totals must equal the
  final `total_token_usage.total_tokens`):
  - windows-codex: 4 entries; Input 153,073 / Output 5,979 / CacheWrite 0 / CacheRead
    161,280 / **Total 320,332**; model `gpt-5.5`; UTC day `2026-06-17`.
  - wsl-codex: 4 entries; Input 31,891 / Output 504 / CacheWrite 0 / CacheRead
    95,232 / **Total 127,627**; model `gpt-5.6-terra`; UTC day `2026-09-06`
    (KST `2026-09-07` — inject a fixed `TimeZoneInfo` in tests).
  - mixed: **447,959** (8 entries).
- Parsed-rollout model must be `System.Text.Json`-round-trippable (it is the cache
  `TPayload`): prefer records/properties without surprises; `DateTimeOffset` fields fine.
- M2/M3 foundation API map (all in `src/Platform.Windows` / `src/Providers`):
  - `UsageRootDiscovery.Discover(UsageRootOptions?)` → roots by kind; codex roots are
    `CodexSessions` + `CodexArchivedSessions`
  - `IncrementalLogScanner<TPayload>.Scan(roots, modifiedSince, parse)`; call with
    `UsageScanCache<CodexParsedRollout>(cachePath, CodexLogParser.ParserVersion)`
  - `IUsageProvider<TPayload>.ParseFile` method group plugs straight into the scanner
    delegate (same shape as M3's `ClaudeLogParser.Parse` wiring)
  - Scan floor: `UsageAggregation.EnrichmentScanStart(now)`
- Fork/subagent scenario fixtures already exist in-repo and are sanitized:
  `Tests/PokeTokenBarTests/Fixtures/CodexFork/{parent,child,sibling}.jsonl` and
  `Fixtures/CodexSubagent/{parent,child,parent-v145,child-v145}.jsonl`. Include them in
  `Tests/Providers.Tests` via a csproj `<None Include="..\PokeTokenBarTests\Fixtures\...">`
  snippet instead of copying files.
- Expected M4 wiring (in tests until `Application` exists):
  ```csharp
  var cache = new UsageScanCache<CodexParsedRollout>(cachePath, CodexLogParser.ParserVersion);
  var scanner = new IncrementalLogScanner<CodexParsedRollout>(PhysicalFileSystemSource.Instance, cache);
  var rollouts = scanner.Scan(codexRoots, UsageAggregation.EnrichmentScanStart(now), CodexLogParser.Parse)
      .SelectMany(f => f.Payload ?? []);
  var snapshot = new CodexUsageProvider().BuildSnapshot(rollouts);
  ```
  (Out-of-window parent loading is an Application concern — defer; M4 tests keep every
  file inside the window.)

## Relevant Files
- `docs/windows-port-plan.md` — milestone definitions; M4 exit criteria at "### 4."
- `docs/windows-port-research/README.md` — codex record quirks, roots, lock evidence
- `docs/handoff/2026-09-23-milestone-3-claude-slice.md` — previous handoff (M3)
- `src/Providers/UsageProvider.cs`, `ClaudeUsageProvider.cs`, `ClaudeLogParser.cs` — M3 seam to generalize
- `src/Platform.Windows/IncrementalLogScanner.cs`, `UsageScanCache.cs`, `UsageRootDiscovery.cs` — scan/cache/discovery
- `Tests/Providers.Tests/` — M3 test conventions: fixtures-as-content, `[Collection("Providers diagnostics")]`, temp trees, `Assert.Single` predicate form
- macOS reference (`Sources/PokeTokenBar/Core/LocalUsageReader.swift`), port conceptually:
  - `CodexUsageVector`/`CodexUsageState` :514-559 — 6-field vector (input, cachedInput,
    cacheWriteInput, output, reasoningOutput, total), fingerprint, billableComponents, isLower
  - `parseCodexRollout` :611-691 — markers, sticky model, same-state normalization
  - `expandCodexParentClosure` :784-845 — injected load/sessionIDKnowledge/probeSessionID
  - `codexSessionMeta` :871-884 — id/session_id, forked_from_id/parent_thread_id, subagent
  - `probeCodexRolloutSessionID` :890-967 — 1MiB cap, newline-complete decoding, stop at token_count
  - `resolveCodexRollouts` :972-1038 — memoized resolve, best parent prefix, owned entries
  - `dedupCodexCanonicalEntries` :1042-1054 — keep-earliest
  - `comparableUsagePrefixCount` :1058-1072 — nil means structurally incomparable
  - `fallbackReplayCount` :1074-1087 — 1s gap trim, subagent exempt
  - `resolveOwnedEvents` :1089-1138 — owner/epoch logic, canonical id rewrite
  - `codexEntryTrustingTotalOnlyLast` :1142-1165 and `parseCodexLine` :1183-1228,
    `shouldTrustCodexTotalOnlyLast` :1235-1241 — #278 rules
- `Tests/PokeTokenBarTests/LocalUsageReaderTests.swift:620-1000` — the fork/subagent test
  scenarios to mirror (replay burst, metadata delay, subagent preservation, fork-of-fork,
  degenerate filename hint, orphan parent)
- `AGENTS.md` / `CLAUDE.md` — build/test commands and repo rules

## Hard-won Context
- Run/test: `dotnet test PokeTokenBar.Windows.slnx` (~0.5s, 162 tests) — wrong-file error `MSB1009` if you type `.sln`.
- `TPayload?` on an unconstrained generic is NOT `Nullable<T>`: `Payload` can be null
  (`ReadFailed` without a stale blob) — always `?? []` / null-check payloads.
- `AppLog` is process-global: every test class that touches it shares one xUnit collection
  (definition in `Tests/Providers.Tests/Diagnostics.cs`). Scanner failures log via AppLog
  and serve the stale blob without updating the cached signature.
- xUnit `Assert.Single` with a predicate returns the single match and throws on 0 or 2+.
- M3 provider tests inject `TimeZoneInfo.Utc` for deterministic LocalDays; the scanner
  delegate is a lambda `(p, l) => CodexLogParser.Parse(l, Tz)` while one test keeps the
  bare method group to prove the handoff wiring compiles.
- Fixture sanitization replaced ids with unique synthetic values, so research fixtures
  contain NO fork/duplicate scenarios — replay semantics need the CodexFork/CodexSubagent
  fixtures or synthetic lines.
- Swift autoreleasepool notes are GC-irrelevant in C#, but the memory shape is not: the M2
  scanner reads whole files (`ReadAllLines`) — fine for Claude (652 files) and acceptable
  for now; the macOS 1MiB streaming probe exists to avoid full reads when hunting parents.
- WSL codex root is actively written (1,793 rollouts) — expect a few `Parsed` files on
  warm refreshes; that is not double-counting.
- Normal and unrelated: git CRLF warnings on commit; `docker-desktop` in `wsl -l -v`;
  SSH trap if push denied (`ssh-add -d <key>; ssh-add <key>`, verify
  `ssh -T git@github-personal` greets `Strongorange`).
- Already tried and rejected: file watchers (WSL boundary), macOS `~/Library` path
  assumptions, parsing `wsl.exe -l` output, decoding fixed-size byte prefixes as UTF-8.

## Working Agreement
- Slice per milestone; report after each milestone or when blocked. M4 = Codex provider
  ONLY (no UI, no Application scheduling). Recommended structure: `CodexLogParser`
  (pure lines → `CodexParsedRollout`), `CodexRolloutResolver` (pure rollouts → entries),
  parent-closure expansion + probe core with injected I/O delegates, all in
  `src/Providers`; `CodexUsageProvider` completes `IUsageProvider<TPayload>`.
- Evidence: research-fixture totals (windows/wsl/mixed + invariant), CodexFork/CodexSubagent
  fixture scenarios, synthetic #278 / info-null / same-state / degenerate-hint lines,
  duplicate-discovery (same rollout via two roots), cache round-trip with rollout payloads,
  parser-version bump reparse, repeat-scan no double count.
- Core and Providers stay UI-free and never touch real logs in tests;
  `Platform.Windows` may read real roots in production code paths but tests inject temp/fake roots.
- Tools & skills: `openviking` MCP — read/write session-continuity events (see
  `viking://user/owner/memories/events/2026/09/`); model the next handoff on this file.

## Open Risks
- Fork-of-fork and orphan-parent cases are subtle; mirror the macOS tests named in
  Relevant Files before trusting any simplification.
- `CodexParsedRollout` as cached `TPayload` must survive STJ save/load including
  `DateTimeOffset?` (`forkedAt`) and nested event states — round-trip test is acceptance.
- Out-of-window parent loading (old parent, recent fork) is deferred to the Application
  milestone; with a 40-day cache prune and month-start scan floor the blast radius is small
  but non-zero — note it in the M5 handoff when wiring scheduling.
- Keep-earliest vs keep-max divergence between providers is intentional; do not "fix".

## Acceptance Criteria
- [ ] `src/Providers` gains pure Codex parser (lines → parsed rollout), resolver
      (rollouts → `UsageEntry`s with canonical ids, replay trimming, keep-earliest dedup),
      and `CodexUsageProvider`; provider contract generalized to `IUsageProvider<TPayload>`
      with `ClaudeUsageProvider` adjusted mechanically
- [ ] Both research fixtures parsed: CLI 0.140.0 and 0.153.4 shapes, extras ignored,
      `info:null` records skipped, #278 total-only trust rules verified, sticky model
- [ ] Windows-only / WSL-only / mixed totals hand-verified against the fixtures
      (windows 320,332; wsl 127,627; mixed 447,959 — recompute from files)
- [ ] Fork replay proven with CodexFork fixtures: replay burst trimmed, real turns kept,
      fork-of-fork resolves ancestors, subagent events preserved (CodexSubagent fixtures),
      orphan parent falls back to timing trim, degenerate filename hint does not fan out
- [ ] Duplicate discovery: same rollout exposed via two roots counted once
- [ ] Cache round-trip: `CodexParsedRollout` payloads survive save/load;
      parser-version bump forces reparse; repeat scan never double-counts
- [ ] No provider-specific branches in Core or shared aggregation
- [ ] `dotnet test PokeTokenBar.Windows.slnx` fully green including new tests

## Verification
- `dotnet test PokeTokenBar.Windows.slnx` — all tests pass (existing 162 + new M4 tests)
- Manual on this machine (optional but cheap, read-only): wire discovery → scanner →
  resolver over the real codex roots (`C:\Users\USER\.codex\sessions`,
  `\\wsl.localhost\Ubuntu-24.04\home\ubuntu\.codex\sessions`) and sanity-check that
  today's totals are non-zero, deduped, and a warm refresh mostly serves cache

## Related Docs
- `docs/windows-port-plan.md` — canonical plan (milestones 0–7 + Later)
- `docs/windows-port-research/README.md` — verified environment facts and evidence
- `docs/handoff/2026-09-23-milestone-3-claude-slice.md` — previous handoff (M3)
- OpenViking memory: `viking://user/owner/memories/events/2026/09/` — session continuity entries (M0–M4)

## Start Prompt
```text
Read docs/handoff/2026-09-23-milestone-4-codex-slice.md end to end.
Work in C:\Users\USER\my-pjts\poketoken-bars-windows, branch main, base main.
Implement Milestone 4: a pure Codex slice in src/Providers — CodexLogParser (lines → parsed rollout with usage states, sticky model, same-state normalization, #278 rules, info:null skip), CodexRolloutResolver (canonical ids, fork replay trimming, subagent preservation, keep-earliest dedup), parent-closure expansion + probe core with injected delegates, and CodexUsageProvider — generalizing the provider contract to IUsageProvider<TPayload>. Prove with xUnit tests: research fixture totals (windows 320,332 / wsl 127,627 / mixed 447,959, recomputed from the files), CodexFork/CodexSubagent fixture scenarios, duplicate-discovery, cache round-trip of rollout payloads, and parser-version reparse, all wired through the M2 IncrementalLogScanner in tests. Do not implement UI or Application scheduling. Verify with dotnet test PokeTokenBar.Windows.slnx.
```
