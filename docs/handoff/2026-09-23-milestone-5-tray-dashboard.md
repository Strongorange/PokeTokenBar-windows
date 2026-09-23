# Handoff — Milestone 5: Application aggregation + tray dashboard

## Goal
Implement Milestone 5 of the Windows port: an `Application` layer that schedules
refreshes, orchestrates discovery → scan → provider snapshots → combined display
state (including out-of-window Codex parent closure), and a Windows tray icon with
a compact dashboard that consumes application display state only.

## Workspace
- Checkout: `C:\Users\USER\my-pjts\poketoken-bars-windows` (Windows 11 25H2 machine, target environment itself)
- Branch: `main` / Base: `main` (this repo pushes directly to `main`; no PR flow used so far)
- Commits so far: `f763205` docs: research + fixtures (M0) · `8083930` feat: pure core (M1) ·
  `55b7701` feat: discovery + scan foundation (M2) · `bb422fd` feat: Claude provider slice (M3) ·
  `974f68d` docs: M4 handoff · `113f7e0` feat: Codex provider slice (M4) · plus docs commits
- Commit convention: English conventional commits (`feat:`, `fix:`, `docs:`)

## Current State
- Done — M4: `src/Providers` has `CodexLogParser` (lines → `CodexParsedRollout`),
  `CodexRolloutResolver` (canonical ids, fork replay trimming, subagent
  preservation, keep-earliest dedup), `CodexParentClosure.Expand` + `CodexRolloutProbe`
  (pure core, injected I/O), `CodexUsageProvider`; provider contract generalized to
  `IUsageProvider<TPayload>` (`ClaudeUsageProvider : IUsageProvider<List<UsageEntry>>`,
  `CodexUsageProvider : IUsageProvider<CodexParsedRollout>`). 231 tests green
  (`104 Core + 38 Platform.Windows + 89 Providers`), all committed (`113f7e0`).
- Done — M4 manual verification on this machine (read-only, temp harness deleted):
  both real Codex roots discovered (Windows + `\\wsl.localhost\Ubuntu-24.04`), 16
  window files parsed 0 failed, 327 entries all distinct ids, warm refresh 16
  cached / 0 reparsed, totals stable, diagnostics log empty. Today (2026-09-23)
  Codex = 0 tokens — correct, last real Codex activity here was 2026-09-15.
- Not implemented: `Application` layer, UI, game integration (M6), floating pet (M7)
- Solution: `PokeTokenBar.Windows.slnx` (`.slnx`, not `.sln` — dotnet SDK 10)

## Locked Decisions
- Stack C# / .NET, target `net10.0`. Do not retarget without asking.
- Layering per plan: `Core` pure, `Providers` parse logs (references Core only, no
  Windows APIs), `Platform.Windows` owns paths/WSL/settings/diagnostics,
  `Application` schedules/aggregate/cache orchestration and may reference Core +
  Providers + Platform.Windows, `UI` consumes Application interfaces only.
- No provider-specific branches in shared aggregation or display state. Providers
  register through `IUsageProvider<TPayload>` at a single registration point;
  provider ids are `claude_code` and `codex`. Combined entries = concatenation of
  provider snapshot entries (id prefixes `msg_…|…` vs `codex|…` cannot collide;
  each provider already dedups inside its own snapshot — do NOT re-dedup across
  providers with keep-max, it would be wrong for codex).
- Codex keep-earliest vs Claude keep-max divergence is intentional; do not "fix".
- Application owns the Codex parent-closure wiring (the M4 deferral):
  1. Enumerate all rollout files per codex root as `CodexRolloutFile`
     (path, mtimeUtc, size) — full enumeration, not just the window.
  2. `windowFiles` = files with mtime ≥ `UsageAggregation.EnrichmentScanStart(now)`;
     scan them through `IncrementalLogScanner<CodexParsedRollout>` with
     `UsageScanCache<CodexParsedRollout>` and the `CodexLogParser.Parse` method group.
  3. `load` delegate = cache-first (`TryGet`/`TryGetAny` by fingerprint, else parse
     the file and `Put`). Out-of-window parents loaded this way must NOT be added to
     includedPaths (their entries are dependencies only).
  4. `sessionIdKnowledge` = `CodexSessionIdKnowledge.Unknown` for now (no persisted
     session-id index; macOS has one but our machine has few files — defer until a
     perf need is measured).
  5. `probeSessionId` = `CodexRolloutProbe.ProbeFile`. **nil-vs-throw distinction:**
     a thrown IOException from the probe (or load) must abort/skip this refresh
     round, never be treated as "no session id" and never persisted.
  6. Codex composition after `Expand`: call
     `CodexRolloutResolver.Resolve(rollouts, includedPaths)` directly and build the
     provider snapshot from the resolved entries. Do NOT filter rollouts to
     `includedPaths` before calling `BuildSnapshot` — the resolver needs the
     adopted parents present for the `bySession` lookup, and `BuildSnapshot`
     includes every payload it is given. `BuildSnapshot` remains the contract for
     the simple case (Claude, and Codex windows without out-of-window parents).
  - Claude path stays simple: scanner + `ClaudeUsageProvider.BuildSnapshot(files.Select(f => f.Payload ?? []))`.
- Scan cadence: manual refresh button + a periodic timer. Default interval
  5 minutes (constant in Application, not settings, until settings exist). Scan
  floor = `UsageAggregation.EnrichmentScanStart(now)`. Cache files live under the
  app's local app-data directory (`%LOCALAPPDATA%\PokeTokenBar\`), one cache file
  per provider (`usage-cache-claude.json`, `usage-cache-codex.json`).
- AppLog rotating diagnostics already exist (`src/Core/AppLog.cs` + Platform
  defaults). The dashboard context menu offers "Open diagnostics folder"; UI never
  writes diagnostics itself.
- Never read/copy: `auth.json` in `.codex` roots (also `~/.claude/.credentials.json`,
  `%USERPROFILE%\.claude\settings.json` secrets).
- Comments: none unless asked. English commits. Do not update git config,
  force-push, or create PRs unless asked. Commit + push only when the user asks.
- Git: remote `origin` = `git@github-personal:Strongorange/PokeTokenBar-windows.git`.

## Decision Needed at Start (confirm with user before scaffolding)
- UI framework: recommended **WPF** (`src/Ui/PokeTokenBar.Ui.csproj`,
  `<UseWPF>true</UseWPF>`) with the `Hardcodet.NotifyIcon.Wpf` package for the tray
  icon (mature, MIT, no WinForms interop baggage). Fallback if the user prefers
  zero third-party deps: WinForms `NotifyIcon` + a WPF dashboard window, or pure
  WinForms. The dashboard itself is a small compact window; either framework is
  fine, but pick once — the decision is expensive to reverse.
- Tray icon asset: generate a simple embedded resource (e.g. a Pokéball-style
  vector rendered to .ico at build time or a checked-in 32x32 .ico). No sprite
  pipeline yet (that is M6/M7 territory).

## Contracts
- Provider seam (all in `src/Providers`, unchanged from M4):
  - `IUsageProvider<TPayload>`: `ProviderId`, `DisplayName`,
    `TPayload ParseFile(string path, IReadOnlyList<string> lines)`,
    `UsageProviderSnapshot BuildSnapshot(IEnumerable<TPayload> filePayloads)`
  - `UsageProviderSnapshot(ProviderId, DisplayName, IReadOnlyList<UsageEntry>)`
  - `ClaudeLogParser.Parse(path, lines[, tz])` method group plugs into
    `IncrementalLogScanner<List<UsageEntry>>`; `CodexLogParser.Parse` into
    `IncrementalLogScanner<CodexParsedRollout>` (bare method groups compile —
    proven in tests)
- Aggregation (`src/Core/UsageAggregation.cs`): `Daily`, `Period`,
  `MonthDailySeries`, `ActiveBlock`, `LocalDay`, `TodayKey`, `EnrichmentScanStart`.
  All take `TimeZoneInfo?` — Application passes the local zone explicitly.
- Discovery (`src/Platform.Windows/UsageRootDiscovery.cs`):
  `Discover(UsageRootOptions?)` → `UsageRoot { Kind, Path }`; filter by kind:
  Claude = `ClaudeProjects`; Codex = `CodexSessions` + `CodexArchivedSessions`.
- Scanner/cache (`src/Platform.Windows`): `IncrementalLogScanner<TPayload>.Scan(
  roots, modifiedSince, parse)`, `UsageScanCache<TPayload>(path, parserVersion)`;
  scanner failures log via AppLog and serve stale payloads (`ScannedFileStatus.ReadFailed`).
  `Payload` can be null — always null-check / filter.
- Proposed Application shape (adjust if cleaner):
  - `UsageRefreshService`: `RefreshAsync(CancellationToken)` → `UsageDisplayState`;
    owns caches, timers (inject `Func<DateTimeOffset>` clock + delay source for
    tests), one refresh at a time (semaphore; manual refresh triggers immediate run).
  - `UsageDisplayState`: per-provider today/month totals + combined totals + last
    refresh time + per-provider availability. Pure data, UI-bindable, no logs.
  - Providers registered in one place (`IReadOnlyList<object>`-free — use a small
    `ProviderRegistration` record per provider with its scanner wiring).
- Expected M5 wiring (Application, codex part):
  ```csharp
  var all = EnumerateRolloutFiles(codexRoots);                    // CodexRolloutFile
  var window = all.Where(f => f.MtimeUtc >= floor).ToList();
  var scanned = scanner.Scan(codexRoots, floor, CodexLogParser.Parse);
  var (rollouts, includedPaths) = CodexParentClosure.Expand(
      window, all,
      file => LoadThroughCache(cache, file),       // parse+Put on miss
      _ => CodexSessionIdKnowledge.Unknown,
      file => CodexRolloutProbe.ProbeFile(file.Path));
  var entries = CodexRolloutResolver.Resolve(rollouts, includedPaths);
  var snapshot = new UsageProviderSnapshot(
      codex.ProviderId, codex.DisplayName, entries);
  ```
  (Watch out: `Expand`'s `windowFiles` must produce the same parsed rollouts the
  scanner already produced — load-through-cache makes the double parse a cache hit.)

## Relevant Files
- `docs/windows-port-plan.md` — milestone definitions; M5 exit criteria at "### 5."
- `docs/handoff/2026-09-23-milestone-4-codex-slice.md` — previous handoff (M4)
- `src/Providers/*.cs` — provider seam, Codex models/parser/resolver/closure/probe
- `src/Platform.Windows/{UsageRootDiscovery,IncrementalLogScanner,UsageScanCache}.cs`
- `src/Core/{UsageAggregation,IsoDates,AppLog}.cs`
- `Tests/Providers.Tests/CodexScannerIntegrationTests.cs` — scanner wiring reference
- `Tests/Providers.Tests/ClaudeScannerIntegrationTests.cs` — Claude wiring reference
- macOS reference for display shape: `Sources/PokeTokenBar/Companion.swift` and
  `UsageStore.swift` show how macOS composes per-provider today/month state
  (conceptual reference only — the Windows UI is tray + dashboard, not a menu bar)
- `PokeTokenBar.Windows.slnx` — add `src/Application`, `src/Ui`, `tests/Application.Tests`
- `AGENTS.md` / `CLAUDE.md` — build/test commands and repo rules

## Hard-won Context
- Run/test: `dotnet test PokeTokenBar.Windows.slnx` (~1s, 231 tests) — wrong-file
  error `MSB1009` if you type `.sln`.
- `TPayload?` on an unconstrained generic is NOT `Nullable<T>`: `Payload` can be
  null (`ReadFailed` without a stale blob) — always `?? []` / null-check / `.Where(p => p is not null)`.
- `AppLog` is process-global: every test class that touches it shares one xUnit
  collection (definition pattern in `Tests/Providers.Tests/Diagnostics.cs`).
- Records containing `List<T>` do NOT get structural equality from the record
  `Equals` — compare with xUnit `Assert.Equal(expectedList, actualList)` (collection
  comparator) or property-wise, never `Assert.Equal(rolloutA, rolloutB)`.
- STJ round-trip of `CodexParsedRollout` (cache TPayload) works including
  `DateTimeOffset? ForkedAt` and nested states — proven by
  `SecondScanServesCacheWithoutDoubleCounting`; keep it that way when touching models.
- WSL roots are actively written (Claude: 527 files, 2 reparsed on warm refresh;
  Codex: 16 window files, usually 0 reparsed) — a few `Parsed` results on warm
  refreshes are normal, not double-counting.
- Codex parse cost on this machine: cold ~2s / warm ~0.9s for the 16-file window —
  fast enough for a 5-minute timer on a background task; keep UI thread free.
- WSL file-change notifications are unreliable across the boundary — periodic scan
  + cache only; file watchers were already tried and rejected.
- Probe I/O failure vs "no metadata" distinction: `ProbeFile` throws vs returns
  null. Never catch-and-null inside Application just to keep going; skip the
  refresh round instead (log via AppLog).
- Partial final lines in actively-written logs are dropped by the scanner
  (`partial final line skipped` in diagnostics) — expected, not data loss.
- Normal and unrelated: git CRLF warnings on commit; `docker-desktop` in `wsl -l -v`;
  SSH trap if push denied (`ssh-add -d <key>; ssh-add <key>`, verify
  `ssh -T git@github-personal` greets `Strongorange`).
- Already tried and rejected: file watchers (WSL boundary), macOS `~/Library` path
  assumptions, parsing `wsl.exe -l` output, decoding fixed-size byte prefixes as UTF-8.

## Working Agreement
- Slice per milestone; report after each milestone or when blocked. M5 = Application
  orchestration + tray/dashboard UI ONLY (no game progression, no floating pet,
  no OpenCode provider). Confirm the UI framework decision with the user first.
- Evidence: Application.Tests with fake clock/scheduler + temp roots proving
  (a) combined today/month totals from both providers, (b) out-of-window parent
  pulled as dependency without contributing entries, (c) transient probe failure
  skips the round without poisoning later rounds, (d) manual+timer refresh single-flight,
  (e) repeat refresh does not double-count. UI smoke-checked manually on this
  machine (tray icon appears, tooltip shows totals, dashboard renders, refresh works).
- Core/Providers/Application stay UI-free; UI project references Application only
  (plus Presentation framework). Application may reference Platform.Windows.
- Tools & skills: `openviking` MCP — read/write session-continuity events (see
  `viking://user/owner/memories/events/2026/09/`); model the next handoff on this file.

## Open Risks
- Out-of-window parent loading is new wiring (was deferred in M4): an old parent
  with a recent fork must be pulled via closure expansion; with a 40-day cache
  prune and month-start scan floor the blast radius is small but real — the
  Application test (b) above is the acceptance gate.
- Tray icon behavior differs across Windows 11 (rounding, monochrome themes);
  keep the icon simple and test light/dark manually.
- Single-flight refresh + timer races: guard with `SemaphoreSlim(1,1)` and
  `TryWait`-style skip (or cancel-and-restart) — decide once, test it.
- If `Hardcodet.NotifyIcon.Wpf` is rejected, WinForms `NotifyIcon` inside a WPF
  app needs a stable `Form` context — known trap; avoid by choosing one framework
  for the whole UI.

## Acceptance Criteria
- [ ] `src/Application` exists with `UsageRefreshService` + `UsageDisplayState`,
      provider registration point for Claude + Codex, scheduling (timer + manual
      refresh, single-flight), scan floor via `EnrichmentScanStart`, per-provider
      caches under `%LOCALAPPDATA%\PokeTokenBar\`
- [ ] Codex refresh path enumerates all rollout files, expands parent closure
      (cache-first load, probe with nil-vs-throw honored), resolves with
      includedPaths so dependency-only rollouts contribute no entries
- [ ] Combined display state = today + month totals per provider and combined,
      built from provider snapshots via shared aggregation with no
      provider-specific branches in shared code
- [ ] Tray icon with tooltip (today combined total), context menu: Refresh,
      Open dashboard, Open diagnostics folder, Exit
- [ ] Compact dashboard shows per-provider today/month + combined + last refresh,
      manual refresh button, consumes Application display state only (no file or
      provider parsing in UI)
- [ ] `tests/Application.Tests` green covering (a)–(e) in Working Agreement
- [ ] `dotnet test PokeTokenBar.Windows.slnx` fully green (existing 231 + new)
- [ ] Manual smoke test on this machine: run the app, tray + dashboard work,
      refresh uses cache on warm rounds, diagnostics stay clean

## Verification
- `dotnet test PokeTokenBar.Windows.slnx` — all tests pass
- Manual on this machine: launch, verify tray tooltip non-zero today total
  (Claude ~149M tokens/day was measured in M3), dashboard shows both providers,
  two consecutive refreshes do not change totals, diagnostics log has no new failures

## Related Docs
- `docs/windows-port-plan.md` — canonical plan (milestones 0–7 + Later)
- `docs/windows-port-research/README.md` — verified environment facts and evidence
- `docs/handoff/2026-09-23-milestone-4-codex-slice.md` — previous handoff (M4)
- OpenViking memory: `viking://user/owner/memories/events/2026/09/` — session continuity entries (M0–M5)

## Start Prompt
```text
Read docs/handoff/2026-09-23-milestone-5-tray-dashboard.md end to end.
Work in C:\Users\USER\my-pjts\poketoken-bars-windows, branch main, base main.
First confirm the UI framework decision with the user (recommended: WPF + Hardcodet.NotifyIcon.Wpf).
Then implement Milestone 5: src/Application (UsageRefreshService with timer+manual single-flight refresh, provider registration for Claude+Codex, per-provider caches under %LOCALAPPDATA%\PokeTokenBar, Codex parent-closure wiring with cache-first load and nil-vs-throw probe semantics) and the tray + compact dashboard UI consuming Application display state only. Prove with tests/Application.Tests (combined totals, out-of-window parent dependency, transient probe failure skips round, single-flight, no double-count) and a manual smoke test. Do not implement game progression or the floating pet. Verify with dotnet test PokeTokenBar.Windows.slnx.
```
