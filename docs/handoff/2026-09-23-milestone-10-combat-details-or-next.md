# Handoff — Milestone 10: next slice (plan complete; candidate: combat details)

## Goal
Milestones 0–9 of the Windows port are complete. The canonical plan has no
remaining open sections: pure core, discovery/scan, Claude, Codex, tray
dashboard, game engine/persistence, shop/candy/mint/difficulty, floating pet,
personal packaging, and the OpenCode provider (M9) are all done and personally
verified. The only named unscheduled nicety left is combat-details enrichment
(base stats/abilities/moves). Confirm the slice with the user FIRST (see
Decision Needed at Start); do not start scaffolding before that.

## Workspace
- Checkout: `C:\Users\USER\my-pjts\poketoken-bars-windows` (Windows 11 25H2 machine, target environment itself)
- Branch: `main` / Base: `main` (this repo pushes directly to `main`; no PR flow used so far)
- Commits so far: `f763205` docs research+fixtures (M0) · `8083930` pure core (M1) ·
  `55b7701` discovery+scan (M2) · `bb422fd` Claude slice (M3) · `113f7e0`
  Codex slice (M4) · `f1108dd` application+tray dashboard (M5) · `1765993`/`59bf551`
  handoff docs · `3a88556` game engine+persistence+dashboard game tab (M6) ·
  `1ac0a36` M6.5 handoff doc · `dce0620` shop/candy/mint/difficulty (M6.5) ·
  `056297c` M7 handoff doc · `6675770` floating pet/sprite cache/notifications (M7) ·
  `daf345d` M8 scope handoff doc · `50f056f` single-file publish+install story (M8) ·
  `a3d626c` M9 handoff doc · (M9 implementation commit TBD — see git log)
- Commit convention: English conventional commits (`feat:`, `fix:`, `docs:`)

## Current State
- Done — M9: OpenCode provider. New `OpenCodeUsageProvider` +
  `OpenCodeLogParser` (Providers, pure JSON parsing of `message.data` rows)
  and `OpenCodeDbReader` (Application, Microsoft.Data.Sqlite 10.0.12 — same
  layering precedent as `CodexRolloutEnumeration`: Platform-typed IO lives in
  Application, Providers stay pure). Registered in `ProviderCatalog.Default`
  via `ProviderCatalog.OpenCode(fs, scratch)`. New root kind
  `UsageRootKind.OpenCodeData` discovered at
  `%USERPROFILE%\.local\share\opencode` + every WSL distro's
  `/home/<user>/.local/share/opencode` + `UsageRootOptions.ExtraOpenCodeRoots`.
- Data source: OpenCode 1.x stores everything in WAL-mode SQLite
  `<data dir>/opencode.db`. Reader queries
  `SELECT id, time_created, data FROM message WHERE time_created >= $floor`
  (floor = `EnrichmentScanStart`; `message_session_time_created_id_idx`
  covering index exists). Only `message` is read — NEVER `account`,
  `credential`, `auth.json`, or `mcp-auth.json`.
- Entry mapping: role must be `assistant` + `tokens` object required; Output =
  `output + reasoning` (clamped); Input/CacheWrite/CacheRead direct; entry
  date = `time.completed ?? time.created ?? row time_created` (ms epoch);
  `tokens.total` is IGNORED (it excludes reasoning for ~6.7k observed rows but
  includes it for ~0.8k — upstream inconsistency); `cost` becomes
  `ExplicitCost` only when > 0 (this machine only has free/zen models where
  cost is always 0); ids are `opencode|<rootPath>|<msgid>`; zero-token
  in-flight rows are kept (Claude/Codex precedent; keep-max dedup fixes them
  when the row finishes).
- WSL UNC trap: opening `\\wsl.localhost\...` db directly fails with
  "database is locked" (SQLite locks don't work over 9P; same in reverse for
  /mnt/c from WSL — "disk I/O error"). Remote roots copy db+wal to
  `%LOCALAPPDATA%\PokeTokenBar\opencode\<sha256-16-of-path>\` (326 MB ≈ 0.8s)
  gated by db+wal (mtime,size) fingerprints — unchanged fingerprints reuse the
  cached entries; a stale wal copy is deleted when the source has none
  (checkpoint trap). Local disks open read-only directly (~60 ms).
- Evidence: 351 tests green (137 Core + 105 Providers + 49 Platform.Windows +
  60 Application) via `dotnet test PokeTokenBar.Windows.slnx`. New: 13 parser
  + 3 provider tests (fixture `docs/windows-port-research/fixtures/opencode/
  opencode-messages.jsonl`, sanitized: fake ids, no path fields, hand-built
  from observed value patterns) + 7 Application tests that build real SQLite
  dbs in temp and drive the full catalog/refresh.
- Real-data parity verified 2026-09-23: python ground-truth (copy db+wal to
  /tmp, same mapping) vs C# live probe through the real registration —
  EXACT match on both roots (win today 66,771,672 / month 87,249,918; WSL
  today 230,664 / month 515,057, Asia/Seoul). First comparison differed by
  +65,555 tokens because this opencode session itself was writing — expect
  live drift when comparing against the active Windows db.
- Version 0.9.0 published via `scripts\publish-windows.ps1 -Launch`; user
  manually confirmed (tray menu header 0.9.0, dashboard OpenCode row with
  real numbers, data continuity, restart persistence — record results in the
  M10 handoff if anything was missing).

## Locked Decisions
- Stack C# / .NET 10 (`net10.0`, UI `net10.0-windows` WPF +
  `Hardcodet.NotifyIcon.Wpf` 2.0.1 + `XamlAnimatedGif` 2.3.2,
  Application now references `Microsoft.Data.Sqlite` 10.0.12), layering
  unchanged: Core pure, Providers parse, Platform.Windows owns
  paths/settings/diagnostics, Application owns engine + orchestration +
  provider IO that needs Platform types, UI consumes Application only. No
  MVVM framework. No provider-specific branches in aggregation/progression/UI
  (ProviderId strings live only in providers/registration).
- Packaging stays personal-use: single-file self-contained exe in
  `%LOCALAPPDATA%\Programs\PokeTokenBar` + Start Menu shortcut. Version in
  the Ui csproj, bumped with the slice that ships it (0.9.0 = M9). No zip
  artifact, no installer, no signing, no auto-update.
- App state/settings/sprites/diagnostics stay under `%LOCALAPPDATA%\PokeTokenBar`
  (`PTB_STATE_DIR` overrides) — never in the install dir; installs never touch
  user data. The OpenCode scratch copies live under
  `%LOCALAPPDATA%\PokeTokenBar\opencode\`.
- Pokemon base data stays the bundled snapshot (`assets/pokemon-snapshot.json`,
  offline-first); sprites remain the separate online-cached concern
  (`SpriteStore`) — keep that split.
- Difficulty + pet prefs live in `settings.json` (Platform.Windows), never in
  the save. macOS divergences (keep-earliest vs keep-max) stay unfixed.
- Never read/copy credentials (`auth.json`, `.credentials.json`,
  `settings.json` secrets, OpenCode `account`/`credential` tables).
  Comments: none unless asked. English commits. Commit + push only when the
  user asks. Git remote `origin` = `git@github-personal:Strongorange/PokeTokenBar-windows.git`.

## Decision Needed at Start (confirm with user before scaffolding)
- Slice choice (pick one; do not bundle):
  1. Combat-details enrichment — base stats / abilities / moves from PokeAPI:
     snapshot regeneration + view surface. The only named remaining nicety.
  2. Anything else the user wants (defects, UI polish, metrics) — the plan is
     done, follow their direction.
- Optional small follow-ups from M9 (mention, do not start unprompted):
  `OPENCODE_DATA_DIR` env override (macOS honors it; Windows port currently
  uses fixed profile paths + extras), exposing extra OpenCode roots in
  settings.json, pruning the scratch copy dir.

## Contracts
- Engine: everything through M7 — `ApplyUsage(...)`, shop/candy/mint/
  difficulty members, `View()` → `CompanionGameView` (includes
  `ActiveSpeciesID`/`ActiveUnownForm`), `Changed`, `DrainNotices()`,
  `ExportSave`/`ImportSave`. UI/pet consume `View()`/`Changed`/`DrainNotices`
  only.
- Providers: `UsageProvider` seam + registration point (`ProviderCatalog`).
  OpenCode refresh = per-root `OpenCodeDbReader.ReadEntries(root, fs,
  floorMillis, tz, scratch)` → `BuildSnapshot` (keep-max dedup). New usage
  sources follow the same registration shape; keep generic totals/progression
  provider-free.
- Settings: `AppSettingsFile` (`src/Platform.Windows/AppSettingsFile.cs`)
  holds growth/shop difficulty + pet prefs; extend it (not the save) for any
  new local preference.
- Packaging: version = Ui csproj `<Version>`; publish settings = win-x64
  pubxml only; install paths owned by `scripts/publish-windows.ps1`.

## Relevant Files
- `src/Providers/OpenCodeLogParser.cs`, `src/Providers/OpenCodeUsageProvider.cs`
- `src/Application/OpenCodeDbReader.cs`, `src/Application/ProviderCatalog.cs`
- `src/Platform.Windows/UsageRoots.cs`, `src/Platform.Windows/UsageRootDiscovery.cs`
- `docs/windows-port-research/fixtures/opencode/opencode-messages.jsonl`
- `Tests/Providers.Tests/OpenCodeLogParserTests.cs`,
  `Tests/Providers.Tests/OpenCodeUsageProviderTests.cs`,
  `Tests/Application.Tests/OpenCodeProviderTests.cs`
- `docs/windows-port-plan.md` — all sections now done
- `docs/handoff/2026-09-23-milestone-9-opencode-or-combat-details.md` — previous handoff
- `Temp\opencode\` — scratch OUTSIDE this repo (M9 probes:
  `ptb-m9-schema.py`, `ptb-m9-sample.py`, `ptb-m9-groundtruth.py`,
  `ptb-m9-live-probe.cs` — do not commit)

## Hard-won Context
- Run/test: `dotnet test PokeTokenBar.Windows.slnx` (~10s, 351 tests) — wrong
  file error `MSB1009` if you type `.sln`. `dotnet test` builds the Ui
  project (Debug) — kill any DEBUG `PokeTokenBar.exe` first (`MSB3027`);
  a running INSTALLED exe does NOT block Debug test builds.
- OpenCode data model facts (opencode 1.18.32, check `opencode --version`):
  `message` rows MUTATE while streaming (`time_updated` > `time_created`);
  `data` JSON keys for assistant rows: role/modelID/providerID/cost/tokens
  {total,input,output,reasoning,cache{write,read}}/time{created,completed}/
  finish. `tokens.total` inconsistent about reasoning (never trust). Free
  models report cost 0. Content text lives in the `part` table (never read).
  User rows have no tokens. WSL may still carry a legacy `storage/` JSON
  tree — dead, ignore it.
- SQLite over UNC/9P cannot lock: local Windows path = open directly
  read-only (WAL readers coexist with a running opencode); `\\wsl.localhost`
  or `\\wsl$` = fingerprint-gated db+wal copy to scratch, then open the copy.
  Copying while opencode writes can theoretically tear; SQLite wal checksums
  tolerate torn tails, and failures degrade to "skip this root this round"
  (AppLog line) — acceptable for a usage dashboard.
- Fixture trap: `IncrementalLogScannerTests` enumerates EVERY `*.jsonl` in
  the test output `fixtures\` root — new fixtures must live in a
  subdirectory (`fixtures\opencode\`) and get their own `<None Include>`
  link in the test csproj.
- `Assert.NotNull` does not give the compiler null-state for struct nullables
  (`UsageEntry?`) — use `.Value` or pattern matching (CS1061 otherwise).
- Raw interpolated strings (`$"""`) with literal JSON braces are a trap —
  build expected JSON with `JsonSerializer.Serialize(new Dictionary...)`
  instead (also the file-based-app rule from M7).
- Epoch math trap: 2026-09-23T00:00:00Z = 1790121600000 ms. First attempt
  was off by exactly one day (day-of-year miscount) — derive timestamps from
  `new DateTimeOffset(2026,9,23,...).ToUnixTimeMilliseconds()` when in doubt.
- Ground-truth method for usage providers: python in WSL (copy db+wal to
  /tmp, open `file:...?mode=ro`) computing the same mapping, compared against
  a live C# probe via `dotnet run file.cs` with `#:project <abs path to
  Application csproj>`. Expect live drift vs the ACTIVE Windows db (the
  opencode session writing it is you).
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
- Pet window is not unit-test covered — drag/click changes need the manual
  checklist (drag / click no-op / double-click / right-click / restart).
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
- OpenCode schema drift: the `message` table is upstream's internal storage;
  a future opencode release could rename columns or move to another store.
  Mitigation: reader failure = AppLog line + empty entries for that root
  (provider stays available), never a crash; ParserVersion bump + fixture
  update if the format changes.
- Microsoft.Data.Sqlite adds e_sqlite3 to the single-file exe (native
  self-extract already handles it); pin 10.0.12 and re-check size after .NET
  upgrades.
- XamlAnimatedGif is hobby-maintained — static PNG fallback exists; do not
  build on animator internals.
- Balloon tips are best-effort (Focus Assist may suppress).

## Acceptance Criteria
- [ ] Slice confirmed with the user and implemented per its own plan
- [ ] `dotnet test PokeTokenBar.Windows.slnx` fully green (351 + new)
- [ ] Manual smoke on this machine incl. app restart; user confirms
      (for app slices: re-publish via `scripts\publish-windows.ps1` so daily
      use picks the change up; bump `<Version>` minor/patch accordingly)
- [ ] Handoff for the next slice written and committed

## Verification
- `dotnet test PokeTokenBar.Windows.slnx` — all tests pass
- Manual on this machine per slice; `PTB_STATE_DIR` sandbox for destructive
  checks; diagnostics log clean after runs

## Related Docs
- `docs/windows-port-plan.md` — canonical plan (all sections complete)
- `docs/handoff/2026-09-23-milestone-9-opencode-or-combat-details.md` — previous handoff (M9)
- OpenViking memory: `viking://user/owner/memories/events/2026/09/` — session continuity entries (M0–M9)

## Start Prompt
```text
Read docs/handoff/2026-09-23-milestone-10-combat-details-or-next.md end to end.
Work in C:\Users\USER\my-pjts\poketoken-bars-windows, branch main, base main.
Milestones 0-9 are complete and verified; the plan has no open sections.
First confirm the M10 slice with the user (combat-details enrichment vs whatever they want — see Decision Needed at Start).
Then implement that slice only, prove pure parts with tests, run the manual checklist, and verify with dotnet test PokeTokenBar.Windows.slnx. Do not bundle other work into the slice.
```
