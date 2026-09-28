# Handoff — Milestone 11: next slice (plan + named niceties complete; user's choice)

## Goal
Milestones 0–10 of the Windows port are complete. The canonical plan has no
open sections and the last named nicety (combat-details enrichment) shipped in
M10 and was personally verified. There is no scheduled work left: M11 is
whatever the user wants (small niceties list below, defects, UI polish) — or
explicitly declaring the personal project done and switching to maintenance
mode. Confirm the slice with the user FIRST (see Decision Needed at Start); do
not start scaffolding before that.

## Workspace
- Checkout: `C:\Users\USER\my-pjts\poketoken-bars-windows` (Windows 11 25H2 machine, target environment itself)
- Branch: `main` / Base: `main` (this repo pushes directly to `main`; no PR flow used so far)
- Commits so far: `f763205` docs research+fixtures (M0) · `8083930` pure core (M1) ·
  `55b7701` discovery+scan (M2) · `bb422fd` Claude slice (M3) ·
  `113f7e0` Codex slice (M4) · `f1108dd` application+tray dashboard (M5) ·
  `3a88556` game engine+persistence (M6) · `dce0620` shop/candy/mint/difficulty
  (M6.5) · `6675770` floating pet/sprite cache/notifications (M7) ·
  `50f056f` single-file publish+install story (M8) · `b31cb0e` OpenCode
  provider (M9) · `a340745` docs M10 handoff · `97f07c6` combat details (M10) ·
  (M10 docs commit — see git log)
- Commit convention: English conventional commits (`feat:`, `fix:`, `docs:`)

## Current State
- Done — M10: combat-details enrichment, offline-first. Snapshot moved to
  schema 2 (`assets/pokemon-snapshot.json`, 237 KB → 3.9 MB): per-species
  default-form combat data (name/height/weight/baseExperience/genderRate/
  types/baseStats/abilities with hidden flags/every black-2-white-2 learnable
  move as `[slug, method, level, ...]` arrays) plus `resourceNames` — localized
  display names for all 18 types, 180 abilities, 555 moves in the app
  languages. 667 species entries (649 + later-gen chain members like Sylveon;
  harmless — `EvoLine` prunes trees to ids ≤ 649 at load).
- `scripts/generate-pokemon-snapshot.ps1` extended: species fetch now keeps
  `gender_rate`, fetches `pokemon/{id}` for every line species and
  `type|ability|move/{slug}` for every referenced slug. Must run under **pwsh**
  (PS7; `ForEach-Object -Parallel`). Deterministic ordering throughout; ~4 min,
  ~1500 requests at throttle 8.
- Loader: `PokemonLineSource` accepts schema 1 (no details — old fixtures keep
  working) and 2; exposes `Details(speciesID)` and
  `ResourceName(kind, slug, lang)` (kind = `PokemonResourceKinds.Type/
  Ability/Move`; falls back to `PokemonNameLocalization.Identifier`).
- Engine: `Profile.Enrich(details)` (Core, unchanged since M6) is now wired at
  hatch, evolution, graduation, egg-release, ctor startup migration, and save
  import — gender/ability materialize once, moves re-sync as level rises
  (`LevelUpMovesThrough(Level).TakeLast(4)`). New `Detail(speciesID)` builds a
  display snapshot (`CompanionDetailSnapshot`: localized species data +
  per-individual computed stats/known moves; individuals = matching dex
  entries by FinalID plus the raising mon when CurrentID matches).
- UI: game tab shows a combat summary line (Lv/gender/nature/ability/types/
  BST) under the active mon; double-clicking a dex list row opens
  `SpeciesDetailWindow` (individuals with stat bars + IV, base stats,
  abilities with hidden marks, full localized move list with methods).
- Evidence: 361 tests green (137 Core + 105 Providers + 49 Platform.Windows +
  70 Application) via `dotnet test PokeTokenBar.Windows.slnx`. New: 4 loader
  tests (schema-2 parse, schema-1 compat, schema-3 reject, localized resource
  fallback), 6 engine tests (hatch/evolve/graduate enrichment, ctor
  backfill of pre-existing saves, Detail localization + null without
  details). Live probe with the real snapshot: hatch → enrich → evolve →
  restart persistence all correct (Korean names/stats/moves).
- Version 0.10.0 published via `scripts\publish-windows.ps1 -Launch`; user
  manually confirmed (combat line, dex detail window). One defect found and
  fixed during smoke: dex double-click initially attached
  `MouseDoubleClick` on the ListBox — WPF routes it Direct, so it never fired
  on ListBoxItems; fixed with `ItemContainerStyle` + `EventSetter` on
  ListBoxItem.

## Locked Decisions
- Stack C# / .NET 10 (`net10.0`, UI `net10.0-windows` WPF +
  `Hardcodet.NotifyIcon.Wpf` 2.0.1 + `XamlAnimatedGif` 2.3.2,
  Application references `Microsoft.Data.Sqlite` 10.0.12), layering
  unchanged: Core pure, Providers parse, Platform.Windows owns
  paths/settings/diagnostics, Application owns engine + orchestration +
  provider IO that needs Platform types, UI consumes Application only. No
  MVVM framework. No provider-specific branches in aggregation/progression/UI.
- Pokemon base data stays the bundled snapshot (offline-first — now schema 2
  with combat details); sprites remain the separate online-cached concern
  (`SpriteStore`) — keep that split. Never fetch PokeAPI at runtime.
- Packaging stays personal-use: single-file self-contained exe in
  `%LOCALAPPDATA%\Programs\PokeTokenBar` + Start Menu shortcut. Version in
  the Ui csproj, bumped with the slice that ships it (0.10.0 = M10). No zip
  artifact, no installer, no signing, no auto-update.
- App state/settings/sprites/diagnostics stay under `%LOCALAPPDATA%\PokeTokenBar`
  (`PTB_STATE_DIR` overrides) — never in the install dir; installs never touch
  user data. OpenCode scratch copies live under
  `%LOCALAPPDATA%\PokeTokenBar\opencode\`.
- Difficulty + pet prefs live in `settings.json` (Platform.Windows), never in
  the save. macOS divergences (keep-earliest vs keep-max) stay unfixed.
- Never read/copy credentials (`auth.json`, `.credentials.json`,
  `settings.json` secrets, OpenCode `account`/`credential` tables).
  Comments: none unless asked. English commits. Commit + push only when the
  user asks. Git remote `origin` = `git@github-personal:Strongorange/PokeTokenBar-windows.git`.

## Decision Needed at Start (confirm with user before scaffolding)
- Slice choice (pick one; do not bundle):
  1. Declare done — switch to maintenance mode (fix defects as they appear,
     no scheduled slices).
  2. M9 OpenCode follow-up niceties (any subset): `OPENCODE_DATA_DIR` env
     override (macOS honors it; Windows port uses fixed profile paths +
     extras), exposing extra OpenCode roots in settings.json + UI, pruning
     the scratch copy dir.
  3. Anything else the user wants (defects, UI polish, metrics).

## Contracts
- Engine: everything through M10 — `ApplyUsage(...)`, shop/candy/mint/
  difficulty members, `View()` → `CompanionGameView`, `Detail(speciesID)` →
  `CompanionDetailSnapshot?` (null when the snapshot has no details for the
  species), `Changed`, `DrainNotices()`, `ExportSave`/`ImportSave`. UI/pet
  consume `View()`/`Changed`/`DrainNotices`/`Detail` only.
- Snapshot: schema 2 shape `{format, schema:2, generatedAt, source, bases,
  lines, names, details, resourceNames}`; loader MUST keep accepting schema 1
  (test fixtures). Regenerate only via `scripts/generate-pokemon-snapshot.ps1`
  under pwsh.
- Providers: `UsageProvider` seam + registration point (`ProviderCatalog`).
  Keep generic totals/progression provider-free.
- Settings: `AppSettingsFile` (`src/Platform.Windows/AppSettingsFile.cs`)
  holds growth/shop difficulty + pet prefs; extend it (not the save) for any
  new local preference.
- Packaging: version = Ui csproj `<Version>`; publish settings = win-x64
  pubxml only; install paths owned by `scripts/publish-windows.ps1`.

## Relevant Files
- `src/Application/PokemonLineSource.cs` (schema 2 loader, `Details`,
  `ResourceName`, `PokemonResourceKinds`)
- `src/Application/CompanionEngine.cs` (enrichment wiring, `Detail()`,
  `CompanionDetail*` records)
- `src/Ui/SpeciesDetailWindow.xaml`/`.cs`, `src/Ui/DashboardWindow.xaml`/
  `.cs` (combat line, dex double-click via ItemContainerStyle)
- `scripts/generate-pokemon-snapshot.ps1`, `assets/pokemon-snapshot.json`
- `Tests/Application.Tests/PokemonLineSourceTests.cs` (incl. `CombatSnapshot`
  fixture), `Tests/Application.Tests/CompanionEngineTests.cs`
- `docs/windows-port-plan.md` — all sections done incl. combat details
- `docs/handoff/2026-09-23-milestone-10-combat-details-or-next.md` — previous handoff
- `Temp\opencode\ptb-m10-probe.cs` — scratch probe OUTSIDE this repo (do not commit)

## Hard-won Context
- Run/test: `dotnet test PokeTokenBar.Windows.slnx` (~10s, 361 tests) — wrong
  file error `MSB1009` if you type `.sln`. `dotnet test` builds the Ui
  project (Debug) — kill any DEBUG `PokeTokenBar.exe` first (`MSB3027`);
  a running INSTALLED exe does NOT block Debug test builds.
- WPF `MouseDoubleClick` is `RoutingStrategy.Direct`: attaching it on a
  ListBox only fires on empty space, never on ListBoxItems. Use
  `ItemContainerStyle` + `<EventSetter Event="MouseDoubleClick">` on
  ListBoxItem (fixed once already — do not regress).
- PowerShell generator traps: must run under pwsh (PS7 `-Parallel`; PS5
  fails with AmbiguousParameterSet). Assigning a **function-returned**
  JsonObject to a JsonObject indexer fails ("cannot load an object of type
  JsonObject" — PSObject wrapping defeats JsonNode binding); build nodes
  inline in loops with directly-constructed `JsonObject::new()` values, as
  the current script does.
- Snapshot regeneration is idempotent-but-networked (~4 min); keep
  `generatedAt` fresh but everything else sorted for byte-stable diffs.
  667 detail species is expected (>649 chain members are pruned by
  `KeepingAnimatedSprites` at load).
- OpenCode data model facts (opencode 1.18.32): `message` rows mutate while
  streaming; `tokens.total` inconsistent about reasoning (never trust);
  Output = output+reasoning; free models report cost 0. SQLite over UNC/9P
  cannot lock: local = open read-only directly, `\\wsl.localhost` =
  fingerprint-gated db+wal copy to scratch.
- Fixture trap: `IncrementalLogScannerTests` enumerates EVERY `*.jsonl` in
  the test output `fixtures\` root — new fixtures must live in a
  subdirectory.
- `Assert.NotNull` does not give the compiler null-state for struct nullables
  (`UsageEntry?`) — use `.Value` or pattern matching. Raw interpolated
  strings (`$"""`) with literal JSON braces are a trap — build expected JSON
  with `JsonSerializer.Serialize`.
- PublishDir inside a pubxml resolves relative to the PROJECT directory;
  SDK appends source revision to ProductVersion (compare BEFORE `+`);
  WPF trimming unsupported (never PublishTrimmed); tray tooltip is
  usage-owned — version lives in the context menu header.
- Hardcodet tray events bypass `DispatcherUnhandledException` — wrap
  tray-sourced handler bodies ENTIRELY in try/catch. Balloon API is
  `ShowBalloonTip(title, message, BalloonIcon)` (three args).
- XamlAnimatedGif 2.3.2: `AnimationBehavior.SetSourceStream` + keep the
  stream referenced. WPF `SystemParameters` has no `DoubleClickTime` —
  P/Invoke `user32!GetDoubleClickTime`.
- Install dir is wiped on every publish — never point anything at files
  inside it. Sprite disk cache grows unbounded by design.
- Normal and unrelated: git CRLF warnings; `docker-desktop` in `wsl -l -v`;
  SSH trap if push denied (`ssh-add -d <key>; ssh-add <key>`, verify
  `ssh -T git@github-personal` greets `Strongorange`).
- Already tried and rejected: file watchers over the WSL boundary, live
  PokeAPI at runtime for BASE DATA, parsing `wsl.exe -l` output, version in
  the tray tooltip, opening WSL SQLite dbs over UNC directly (locked).

## Working Agreement
- Slice per milestone; report after the milestone or when blocked. Confirm
  the slice choice with the user first (this handoff's Decision Needed).
- Evidence: unit tests for pure parts; UI behavior manual by the user; the
  `PTB_STATE_DIR` sandbox + `dotnet run file.cs` seeding/probe tricks for
  destructive checks and live verification.
- Core/Providers/Application stay UI-free; UI references Application only.
- Tools & skills: `openviking` MCP — read/write session-continuity events
  (see `viking://user/owner/memories/events/2026/09/`); model the next
  handoff on this file.

## Open Risks
- Snapshot size (3.9 MB) is embedded in the single-file exe; acceptable now,
  re-check if a future schema adds per-form data. Loader stays schema-1
  compatible so a bad regeneration only loses details, not the app.
- OpenCode schema drift: the `message` table is upstream's internal storage;
  a future opencode release could rename columns or move to another store.
  Mitigation: reader failure = AppLog line + empty entries for that root,
  never a crash; ParserVersion bump + fixture update if the format changes.
- PokeAPI upstream data could change between regenerations; the app is
  offline-first so drift is invisible until an explicit regeneration.
- XamlAnimatedGif is hobby-maintained — static PNG fallback exists; do not
  build on animator internals.
- Balloon tips are best-effort (Focus Assist may suppress).

## Acceptance Criteria
- [ ] Slice confirmed with the user and implemented per its own plan (or the
      user explicitly closes the project into maintenance mode)
- [ ] `dotnet test PokeTokenBar.Windows.slnx` fully green (361 + new)
- [ ] Manual smoke on this machine incl. app restart; user confirms
      (for app slices: re-publish via `scripts\publish-windows.ps1` so daily
      use picks the change up; bump `<Version>` minor/patch accordingly)
- [ ] Handoff for the next slice written and committed (if the project
      continues)

## Verification
- `dotnet test PokeTokenBar.Windows.slnx` — all tests pass
- Manual on this machine per slice; `PTB_STATE_DIR` sandbox for destructive
  checks; diagnostics log clean after runs

## Related Docs
- `docs/windows-port-plan.md` — canonical plan (all sections complete)
- `docs/handoff/2026-09-23-milestone-10-combat-details-or-next.md` — previous handoff (M10 scope)
- OpenViking memory: `viking://user/owner/memories/events/2026/09/` — session continuity entries (M0–M10)

## Start Prompt
```text
Read docs/handoff/2026-09-28-milestone-11-next-or-close.md end to end.
Work in C:\Users\USER\my-pjts\poketoken-bars-windows, branch main, base main.
Milestones 0-10 are complete and verified; the plan and the last named nicety are done.
First confirm the M11 slice with the user (small niceties, defects/UI polish, or closing the project into maintenance mode — see Decision Needed at Start).
Then implement that slice only, prove pure parts with tests, run the manual checklist, and verify with dotnet test PokeTokenBar.Windows.slnx. Do not bundle other work into the slice.
```
