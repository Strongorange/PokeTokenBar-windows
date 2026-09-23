# Handoff — Milestone 6: Game and persistence integration

## Goal
Implement Milestone 6 of the Windows port: connect the validated usage aggregate to
Pokemon progression. A game engine applies per-provider daily usage deltas to
`CompanionState` (growth → evolution → Pokedex, egg hatching), persists the state
under app data with backups, and offers save import/export through the existing
`SaveTransfer` codec. The dashboard grows a game tab; the tray stays as-is.

## Workspace
- Checkout: `C:\Users\USER\my-pjts\poketoken-bars-windows` (Windows 11 25H2 machine, target environment itself)
- Branch: `main` / Base: `main` (this repo pushes directly to `main`; no PR flow used so far)
- Commits so far: `f763205` docs: research + fixtures (M0) · `8083930` feat: pure core (M1) ·
  `55b7701` feat: discovery + scan foundation (M2) · `bb422fd` feat: Claude provider slice (M3) ·
  `113f7e0` feat: Codex provider slice (M4) · `1765993` docs: M5 handoff ·
  `f1108dd` feat: application layer and tray dashboard (M5)
- Commit convention: English conventional commits (`feat:`, `fix:`, `docs:`)

## Current State
- Done — M5: `src/Application` has `UsageRefreshService` (manual + 5-minute timer,
  single-flight join semantics: concurrent `RefreshAsync` callers share one in-flight
  task; failures abort the round, keep last state, log `refresh failed:`),
  `UsageDisplayState` (per-provider today/month tokens+cost, combined totals,
  `AsOfUtc`), `ProviderCatalog` (single registration point; Claude wiring simple
  scanner+snapshot; Codex wiring: full rollout enumeration → window filter by
  `EnrichmentScanStart` → `CodexParentClosure.Expand` with cache-first load and
  probe → `CodexRolloutResolver.Resolve(rollouts, includedPaths)`),
  per-provider caches under `%LOCALAPPDATA%\PokeTokenBar\` (`usage-cache-claude.json`,
  `usage-cache-codex.json`). `src/Ui` is WPF + `Hardcodet.NotifyIcon.Wpf` 2.0.1:
  tray icon (assets/pokeball.ico), tooltip "Today: X · Month: Y", context menu
  (Refresh / Open dashboard / Open diagnostics folder / Exit), compact dashboard
  window. 238 tests green (104 Core + 38 Platform.Windows + 89 Providers +
  7 Application), all committed (`f1108dd`).
- Done — M5 manual verification on this machine: tray pokéball icon + tooltip +
  dashboard confirmed by the user; startup refresh + 5-minute timer tick proven
  via cache mtime updates; diagnostics log completely clean across rounds; Exit
  menu works. Warm refresh re-uses cache (no reparses, no diagnostics noise).
- Not implemented: game engine wiring, persistence, sprite pipeline (M6),
  floating pet (M7)
- Solution: `PokeTokenBar.Windows.slnx` (`.slnx`, not `.sln` — dotnet SDK 10)

## Locked Decisions
- Stack C# / .NET, target `net10.0` (UI: `net10.0-windows`, WPF). Do not retarget
  without asking.
- Layering per plan: `Core` pure (game models/balance/save codec already there),
  `Providers` parse logs (references Core only), `Platform.Windows` owns
  paths/WSL/settings/diagnostics, `Application` schedules/aggregates and owns the
  game engine + persistence orchestration, `UI` consumes Application interfaces
  only.
- UI is WPF + `Hardcodet.NotifyIcon.Wpf`. No MVVM framework; keep the compact
  code-behind style established in M5 unless it starts hurting.
- Game engine placement: a pure engine class over `CompanionState` (apply usage
  delta, evolve, hatch, dex bookkeeping) belongs in `src/Application` as a plain
  class with injected RNG/save delegates so Core.Tests-style unit tests can drive
  it without UI or real files. Core stays untouched unless a model gap appears.
- Daily-claim ledger semantics (macOS parity, CompanionStore.swift ~558-627):
  `CompanionState.ClaimedTodayTokensByProvider` + `LastDate`. On each refresh:
  if `LastDate != todayKey` → reset ledger to today's per-provider totals (date
  rollover), set `LastDate`, do NOT apply usage; else delta per provider =
  today(provider) − claimed(provider), apply total delta to growth, update ledger.
  First launch (`InstallBaselineSet == false`) → set baseline without applying.
- Persistence: `CompanionState` JSON under
  `%LOCALAPPDATA%\PokeTokenBar\companion-state.json` (atomic write via temp +
  move, mirroring `UsageScanCache.Save`). Import/export via `SaveTransfer.Encode/
  Decode` (schema 2) with `BackupsToKeep` pre-import backups in the same folder.
  UI import/export uses a file dialog; UI never touches state encoding itself.
- No provider-specific branches in shared aggregation or display state (still
  true; the game engine consumes `UsageProviderSnapshot`-derived totals only,
  provider ids as ledger keys).
- Codex keep-earliest vs Claude keep-max divergence is intentional; do not "fix".
- Never read/copy: `auth.json` in `.codex` roots (also `~/.claude/.credentials.json`,
  `%USERPROFILE%\.claude\settings.json` secrets).
- Comments: none unless asked. English commits. Do not update git config,
  force-push, or create PRs unless asked. Commit + push only when the user asks.
- Git: remote `origin` = `git@github-personal:Strongorange/PokeTokenBar-windows.git`.

## Decision Needed at Start (confirm with user before scaffolding)
- Pokemon data source: macOS fetches evolution chains + names from PokeAPI at
  runtime (`PokeAPIClient.swift`, `PokemonNameLocalization.swift`) and caches.
  Options for Windows:
  1. Bundled sanitized JSON snapshot (deterministic, offline-first, testable;
     needs a one-time generation script from PokeAPI) — recommended for M6.
  2. Live PokeAPI client in Application (parity with macOS, network dependency,
     needs caching + failure policy).
  Ask the user; the snapshot keeps M6 offline and testable, live fetch can be M7+.
- Sprite scope for M6: the dashboard game tab needs species images. Options:
  static sprite sheet checked into assets (pick the same source macOS uses),
  PNG-per-species bundled folder, or defer visuals to M7 with text-only dex
  (fastest to ship, ugliest). Confirm with the user.
- Shop/candy/mints breadth: plan lists "shop" under M6 but the macOS shop surface
  (`ItemKind` pricing, candy windows, mints) is large. Propose: M6 = growth +
  evolution + dex + egg + persistence + import/export; shop/candy/mints as an
  M6.5 follow-up slice unless the user wants it all now. Confirm at start.

## Contracts
- Display/state seam (unchanged from M5, `src/Application`):
  - `UsageRefreshService.RefreshAsync(ct) → UsageDisplayState`,
    `RunAsync(ct)` periodic loop, `StateChanged` event, `Current`.
  - `UsageDisplayState(AsOfUtc, Providers[], TodayTokens, TodayCost,
    MonthTokens, MonthCost)`; `ProviderUsageSummary(ProviderId, DisplayName,
    Available, TodayTokens, TodayCost, MonthTokens, MonthCost)`.
  - `ProviderCatalog.Default(cacheDirectory, fileSystem, probeSessionId)` —
    registration point; provider ids `claude_code`, `codex`.
- Game models (`src/Core`, already ported and tested):
  - `CompanionState` (CompanionModel.cs): `InstallBaselineSet`,
    `UsedSinceInstall`, `SpentTokens`, `EggUsage`, `EggTier`, `Active` (MonState),
    `Dex` (List<DexEntry>), `CollectedFinals`, `Inventory`,
    `ClaimedTodayTokensByProvider`, `LastDate`, `Language`,
    `ReconcileRepresentativeSelection()`.
  - `MonState`: `BaseID`, `PathIDs`, `PlannedPathIDs`, `StageIndex`,
    `UsedAtStage`, `Rarity`, `TotalForms`, `IsShiny`, `Profile`,
    `PhaseThreshold`.
  - `PokemonBalance` (GameBalance.cs): `EggHatchThreshold = 5_000_000`,
    `GraduationTotal(rarity)`, `PhaseThreshold(rarity, totalForms, stageIndex,
    growthMultiplier)`, `RepeatGrowthMultiplier = 2`, difficulty
    `ClampDifficulty/Scaled/SnapDifficulty` (0.1–2.0), item prices
    (`RareCandies`, `Mints`, `ShinyCharms`, `FreshEggs`), `PokemonOdds`
    (shiny 1/64, ditto disguise 1/128).
  - `PokemonProfile.Generate(seed, growthTokens)`, `ApplyGrowth/AdvanceGrowth`,
    `Enrich(PokemonDetails)`, `ProfileRng`, `PokemonProfileMigration.Seed(text)`.
  - `SaveTransfer` (SaveTransfer.cs): `Encode(state, appVersion, deviceName,
    now)`, `Decode(bytes)` → `SaveEnvelope` (schema 2, format
    `poketokenbar.save`), `Sanitized`, `RebasedForThisDevice`,
    `BackupFileName`, `BackupsToKeep = 5`, `MaxFileBytes = 8 MiB`.
- macOS reference for engine behavior: `Sources/PokeTokenBar/Core/
  CompanionStore.swift` (claim ledger ~558-627, `applyUsage`, evolution plan
  `makeEvolutionPlan`, graduation/release flow, `backfillMissingDexNames`),
  `UsageStore.swift` `todayTokensByProvider`, `PokeAPIClient.swift`,
  `PokemonNameLocalization.swift`. Conceptual reference only.
- Aggregation (`src/Core/UsageAggregation.cs`) unchanged; engine keys off
  `UsageDisplayState.Providers[].TodayTokens` and provider ids.

## Relevant Files
- `docs/windows-port-plan.md` — milestone definitions; M6 exit criteria at "### 6."
- `docs/handoff/2026-09-23-milestone-5-tray-dashboard.md` — previous handoff (M5)
- `src/Application/*.cs` — refresh service, display state, provider catalog
- `src/Core/{CompanionModel,GameBalance,PokemonProfile,Evolution,SaveTransfer,
  PokemonNature,UnownForm,AppLanguage}.cs` — game core, already ported
- `src/Ui/{App.xaml.cs,DashboardWindow.xaml(.cs)}` — tray + dashboard shell
- `Tests/Core.Tests/` — existing game-model tests (DexEntry/MonState/save codec)
- `Tests/Application.Tests/UsageRefreshServiceTests.cs` — M5 evidence pattern
  (fake clock, gated roots, temp profile) to extend for engine tests
- `assets/pokeball.ico`, `scripts/generate-tray-icon.ps1` — icon pipeline
- `PokeTokenBar.Windows.slnx` — add new projects only if a `src/Game` split is
  agreed (default: engine lives in Application, no new project)
- `AGENTS.md` / `CLAUDE.md` — build/test commands and repo rules

## Hard-won Context
- Run/test: `dotnet test PokeTokenBar.Windows.slnx` (~2s, 238 tests) — wrong-file
  error `MSB1009` if you type `.sln`.
- WPF name traps: `PokeTokenBar.Application` namespace collides with
  `System.Windows.Application` → base `App : System.Windows.Application` fully
  qualified; `Path` needs explicit `using System.IO;` in WPF projects.
- Test-injected delay MUST honor cancellation: `(_, ct) => gate.Task.WaitAsync(ct)`
  — an assert failure before `cts.Cancel()` used to leak a runaway timer loop
  that flooded the default (real) AppLog after `ResetToDefault()` in Dispose.
  Always wrap timer tests in try/finally that cancels and awaits the loop.
- AppLog is process-global: every test class that touches it shares one xUnit
  collection (pattern in `Tests/Application.Tests/Diagnostics.cs` and
  `Tests/Providers.Tests/Diagnostics.cs`).
- Single-flight semantics chosen in M5: join-the-in-flight-task (concurrent
  callers get the same `UsageDisplayState` instance — `Assert.Same` proven);
  failures keep last state and propagate to the caller.
- Real-machine refresh behavior: startup refresh ~3-40s cold (527 WSL Claude
  files over `\\wsl.localhost`), warm rounds ~1-2s; timer tick writes only the
  claude cache (codex cache untouched when nothing new). A few `Parsed` results
  on warm refreshes are normal (actively-written WSL logs).
- Tray icon asset: PNG-frames ICO (16/32/48) generated by
  `scripts/generate-tray-icon.ps1` (System.Drawing), consumed via
  `IconSource = new BitmapImage(pack URI)`; regenerating requires pwsh on the
  Windows side. `Resource Include` needs `Link="Assets\pokeball.ico"` because the
  file lives in repo-root `assets\`.
- WSL file-change notifications are unreliable across the boundary — periodic
  scan + cache only; file watchers were already tried and rejected.
- Partial final lines in actively-written logs are dropped by the scanner —
  expected, not data loss; the only line a healthy app log may show.
- Records containing `List<T>` do NOT get structural equality — compare with
  collection `Assert.Equal` or property-wise.
- Normal and unrelated: git CRLF warnings on commit; `docker-desktop` in
  `wsl -l -v`; SSH trap if push denied (`ssh-add -d <key>; ssh-add <key>`,
  verify `ssh -T git@github-personal` greets `Strongorange`).
- Already tried and rejected: file watchers (WSL boundary), macOS `~/Library`
  path assumptions, parsing `wsl.exe -l` output, decoding fixed-size byte
  prefixes as UTF-8.

## Working Agreement
- Slice per milestone; report after each milestone or when blocked. M6 = game
  engine + persistence + import/export + dashboard game surface ONLY (no
  floating pet, no OpenCode provider; shop scope per the start decision).
  Confirm the data-source / sprite / shop-scope decisions with the user first.
- Evidence: new engine tests with fixed clock + temp state dir proving
  (a) first refresh sets baseline without growth, (b) second refresh applies
  per-provider deltas exactly once (no double-apply on repeat refresh),
  (c) date rollover resets the ledger without applying, (d) evolution +
  graduation + dex entry creation on synthetic totals, (e) save → load round
  trip preserves state (schema 2), import-with-backup keeps old file
  recoverable, (f) engine is idempotent when totals do not change.
- Core/Providers/Application stay UI-free; UI project references Application
  only. Application may reference Platform.Windows.
- Tools & skills: `openviking` MCP — read/write session-continuity events (see
  `viking://user/owner/memories/events/2026/09/`); model the next handoff on
  this file.

## Open Risks
- Evolution data dependency: if the bundled-snapshot option is chosen, the
  snapshot generator (one-time PokeAPI pull) must sanitize/license-check names
  and chain trees; keep the snapshot small (only the species the game rolls).
- Engine save collisions: usage refresh (ledger write) and UI actions (shop,
  language change) both write `companion-state.json` — single writer
  (lock or marshal through the engine) or atomic writes will race.
- `Assert.Same`-style identity assumptions in M5 tests rely on single-flight
  returning the same instance — engine hooks must not break that.
- Large `Dex` + `Names` dictionaries make the save file grow; `MaxFileBytes`
  8 MiB cap is generous but watch it with bundled names.
- Win11 tray theming is fine for the pokéball now; game-tab visuals may need
  light/dark awareness later (defer).

## Acceptance Criteria
- [ ] Game engine in `src/Application` applies daily per-provider deltas to
      `CompanionState` with the claim-ledger semantics above (baseline, delta,
      rollover), pure and unit-tested
- [ ] Persistence: state file under `%LOCALAPPDATA%\PokeTokenBar\`, atomic
      writes, load-on-start, survives app restart with progression intact
- [ ] Import/export through `SaveTransfer` (schema 2) with pre-import backups;
      reject/rollback path tested for wrong-schema files
- [ ] Evolution + graduation + dex entry creation work end-to-end from real
      refresh data on this machine (manual check against a fresh save)
- [ ] Dashboard exposes the game surface agreed at start (growth/stage, dex
      count, egg progress at minimum)
- [ ] `tests/Application.Tests` (or a new engine test file) covers (a)–(f)
- [ ] `dotnet test PokeTokenBar.Windows.slnx` fully green (existing 238 + new)
- [ ] Manual smoke test on this machine: fresh start → baseline set; usage
      refresh → growth advances; restart → state persists; import/export
      round-trips; diagnostics stay clean

## Verification
- `dotnet test PokeTokenBar.Windows.slnx` — all tests pass
- Manual on this machine: run the app, verify baseline then growth on a second
  refresh, kill + restart to confirm persistence, export + import the save,
  confirm the backup file exists, diagnostics log has no new failures

## Related Docs
- `docs/windows-port-plan.md` — canonical plan (milestones 0–7 + Later)
- `docs/handoff/2026-09-23-milestone-5-tray-dashboard.md` — previous handoff (M5)
- OpenViking memory: `viking://user/owner/memories/events/2026/09/` — session continuity entries (M0–M6)

## Start Prompt
```text
Read docs/handoff/2026-09-23-milestone-6-game-persistence.md end to end.
Work in C:\Users\USER\my-pjts\poketoken-bars-windows, branch main, base main.
First confirm the three start decisions with the user (pokemon data source, sprite scope, shop scope).
Then implement Milestone 6: a pure game engine in src/Application (claim-ledger daily deltas, growth, evolution, dex, egg), CompanionState persistence under %LOCALAPPDATA%\PokeTokenBar with atomic writes, SaveTransfer import/export with backups, and the agreed dashboard game surface. Prove with tests (baseline, single-apply, rollover, evolution end-to-end, save round-trip, import backup, idempotence) and a manual smoke test including an app restart. Do not implement the floating pet or the OpenCode provider. Verify with dotnet test PokeTokenBar.Windows.slnx.
```
