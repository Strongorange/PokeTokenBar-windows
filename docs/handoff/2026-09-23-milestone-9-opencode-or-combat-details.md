# Handoff — Milestone 9: next slice (scope to confirm: OpenCode research spike / combat details)

## Goal
Milestones 0–8 of the Windows port are complete (pure core through floating
pet and the personal packaging/install story, all personally verified). The
canonical plan has one remaining section — "### Later: OpenCode" — plus one
unscheduled nicety (combat-details enrichment). Daily use now runs from the
installed single-file exe at `%LOCALAPPDATA%\Programs\PokeTokenBar` via the
Start Menu shortcut. Confirm the slice with the user FIRST (see Decision
Needed at Start); do not start scaffolding before that.

## Workspace
- Checkout: `C:\Users\USER\my-pjts\poketoken-bars-windows` (Windows 11 25H2 machine, target environment itself)
- Branch: `main` / Base: `main` (this repo pushes directly to `main`; no PR flow used so far)
- Commits so far: `f763205` docs research+fixtures (M0) · `8083930` pure core (M1) ·
  `55b7701` discovery+scan (M2) · `bb422fd` Claude slice (M3) · `113f7e0` Codex
  slice (M4) · `f1108dd` application+tray dashboard (M5) · `1765993`/`59bf551`
  handoff docs · `3a88556` game engine+persistence+dashboard game tab (M6) ·
  `1ac0a36` M6.5 handoff doc · `dce0620` shop/candy/mint/difficulty (M6.5) ·
  `056297c` M7 handoff doc · `6675770` floating pet/sprite cache/notifications (M7) ·
  `daf345d` M8 scope handoff doc · `50f056f` single-file publish+install story (M8)
- Commit convention: English conventional commits (`feat:`, `fix:`, `docs:`)

## Current State
- Done — M8 (`50f056f`): personal packaging story. Ui csproj declares
  `<Version>0.8.0</Version>` + `<ApplicationIcon>` (real Win32 icon on the
  exe/shortcut/taskbar). Publish profile
  `src/Ui/Properties/PublishProfiles/win-x64.pubxml`: self-contained
  single-file win-x64, native libs for self-extract, compression
  (~75 MB exe, staging emits ONLY the exe + pdbs). `scripts/publish-windows.ps1`:
  kill installed instance (Path-matched, never the Debug dev instance) →
  `dotnet publish -c Release` → staged-exe version gate vs csproj → wipe+recreate
  install dir → copy exe (pdbs excluded) → refresh Start Menu shortcut
  (`%APPDATA%\Microsoft\Windows\Start Menu\Programs\PokeTokenBar.lnk`) →
  optional `-Launch`. Version shows as a disabled header item at the top of
  the tray context menu (tooltip stays usage-owned).
- New gate tests `Tests/Platform.Windows.Tests/PackagingTests.cs`: walk up to
  `PokeTokenBar.Windows.slnx`, assert Ui csproj has parseable non-zero
  `<Version>` + ApplicationIcon pointing at the bundled ico, and the pubxml
  declares self-contained single-file with the exact five properties.
- Evidence: 328 tests green (137 Core + 89 Providers + 49 Platform.Windows +
  53 Application) via `dotnet test PokeTokenBar.Windows.slnx`.
- Manual verification on this machine (2026-09-23, user through the real UI):
  installed exe launches from Start Menu shortcut, data continuity confirmed
  (same `%LOCALAPPDATA%\PokeTokenBar` state/settings/sprites as the old Debug
  daily runs — pokemon/candy/pet position intact across install + restart),
  tray menu header shows `PokeTokenBar 0.8.0`, diagnostics log clean.
- Daily use should now be the INSTALLED exe, not `src\Ui\bin\Debug\...`.
  Re-publish after any change with `scripts\publish-windows.ps1` (add
  `-Launch` to start it).
- Not implemented anywhere yet: OpenCode provider (plan "Later" — needs
  Windows/WSL storage research + sanitized fixtures), combat-details
  enrichment (base stats/abilities/moves). Public distribution (MSIX, winget,
  auto-update) stays deferred.

## Locked Decisions
- Stack C# / .NET 10 (`net10.0`, UI `net10.0-windows` WPF +
  `Hardcodet.NotifyIcon.Wpf` 2.0.1 + `XamlAnimatedGif` 2.3.2), layering
  unchanged: Core pure, Providers parse, Platform.Windows owns
  paths/settings/diagnostics, Application owns engine + orchestration, UI
  consumes Application only. No MVVM framework.
- Packaging stays personal-use: single-file self-contained exe in
  `%LOCALAPPDATA%\Programs\PokeTokenBar` + Start Menu shortcut. Version in
  the Ui csproj, bumped with the slice that ships it (0.8.0 = M8). No zip
  artifact, no installer, no signing, no auto-update.
- App state/settings/sprites/diagnostics stay under
  `%LOCALAPPDATA%\PokeTokenBar` (`PTB_STATE_DIR` overrides) — never in the
  install dir; installs never touch user data.
- Pokemon base data stays the bundled snapshot (`assets/pokemon-snapshot.json`,
  offline-first); sprites are the separate online-cached concern
  (`SpriteStore`) — keep that split.
- Difficulty + pet prefs live in `settings.json` (Platform.Windows), never in
  the save. macOS divergences (keep-earliest vs keep-max) stay unfixed.
- Never read/copy credentials (`auth.json`, `.credentials.json`,
  `settings.json` secrets). Comments: none unless asked. English commits.
  Commit + push only when the user asks. Git remote `origin` =
  `git@github-personal:Strongorange/PokeTokenBar-windows.git`.

## Decision Needed at Start (confirm with user before scaffolding)
- Slice choice (pick one; do not bundle):
  1. OpenCode provider — plan's "Later" section, now the last planned item.
     Gate: Windows/WSL storage locations and sanitized fixtures must be
     established first (research spike). Must use the existing provider
     contract; no changes to generic totals, progression, or UI architecture.
  2. Combat-details enrichment (base stats / abilities / moves from
     PokeAPI) — snapshot regeneration + view surface; unscheduled so far.
- If the user wants something else entirely (defects, UI polish, metrics),
  follow their direction — the plan is done.

## Contracts
- Engine: everything through M7 — `ApplyUsage(...)`, shop/candy/mint/
  difficulty members, `View()` → `CompanionGameView` (includes
  `ActiveSpeciesID`/`ActiveUnownForm`), `Changed`, `DrainNotices()`,
  `ExportSave`/`ImportSave`. UI/pet consume `View()`/`Changed`/`DrainNotices`
  only.
- Providers: `UsageProvider` seam + registration point (see Claude/Codex
  providers; macOS `CodexUsage*.swift` is the semantic reference for an
  OpenCode port). No provider-specific branches in aggregation/progression/UI.
- Settings: `AppSettingsFile` (`src/Platform.Windows/AppSettingsFile.cs`)
  holds growth/shop difficulty + pet prefs; extend it (not the save) for any
  new local preference.
- Packaging: version = Ui csproj `<Version>`; publish settings = win-x64
  pubxml only; install paths owned by `scripts/publish-windows.ps1` (there is
  deliberately no C# InstallLocations helper — the app never reads its
  install dir).

## Relevant Files
- `src/Ui/PokeTokenBar.Ui.csproj`, `src/Ui/Properties/PublishProfiles/win-x64.pubxml`
- `scripts/publish-windows.ps1` — publish/gate/install/shortcut
- `Tests/Platform.Windows.Tests/PackagingTests.cs` — packaging gate tests
- `src/Ui/App.xaml.cs` — tray menu version header (BuildMenu)
- `docs/windows-port-plan.md` — "### Personal packaging" + "### Later: OpenCode"
- `docs/handoff/2026-09-23-milestone-8-opencode-combat-details-packaging.md` — previous handoff
- `Temp\opencode\` — scratch (seeding scripts from M7 live under
  `Temp\opencode\ptb-m7-smoke\`, OUTSIDE this repo — do not commit)

## Hard-won Context
- Run/test: `dotnet test PokeTokenBar.Windows.slnx` (~4s, 328 tests) — wrong
  file error `MSB1009` if you type `.sln`. `dotnet test` builds the Ui
  project (Debug) — kill any DEBUG `PokeTokenBar.exe` first (`MSB3027`);
  a running INSTALLED exe does NOT block Debug test builds.
- PublishDir inside a pubxml resolves relative to the PROJECT directory
  (`src\Ui`), NOT the pubxml location — `..\..\artifacts\publish\win-x64\`
  = repo `artifacts\publish\win-x64\`. Two wrong attempts (`C:\Users\USER\
  artifacts`, `my-pjts\artifacts`) had to be manually deleted.
- The .NET SDK appends the source revision to InformationalVersion
  (`0.8.0+daf345d...`) — `VersionInfo.ProductVersion` carries it. The
  publish script compares the part BEFORE `+`; `AssemblyName.Version` stays a
  clean 3-part version for display.
- Tray tooltip is owned by `OnStateChanged` (usage text, replaced every
  refresh — App.xaml.cs). Version display belongs in the context menu header
  (disabled MenuItem); do not put the version in the tooltip.
- WPF single-file publish works on net10.0-windows with the pubxml's five
  properties; trimming is NOT supported for WPF (never add PublishTrimmed).
  Staging contains only the exe + pdbs; the script copies everything except
  `*.pdb` (future satellite files would ride along automatically).
- The publish script force-kills ONLY the installed instance (Path match);
  state writes are persisted at change time so this is safe in practice.
- PackagingTests locate the repo root by walking up from
  `AppContext.BaseDirectory` until `PokeTokenBar.Windows.slnx` exists — keep
  tests using this pattern self-contained (no env mutation).
- BAML event trap (M6.5, still applies): XAML attaches handlers BEFORE
  dependency properties — guard slider/checkbox handlers with a
  construction-phase check (`_engine is null`).
- Hardcodet tray events arrive via a WndProc hook that BYPASSES
  `DispatcherUnhandledException` — wrap tray-sourced handler bodies ENTIRELY
  in try/catch.
- Hardcodet 2.0.1 balloon API is `ShowBalloonTip(title, message, BalloonIcon)`
  — three args, NO timeout overload.
- XamlAnimatedGif 2.3.2: `AnimationBehavior.SetSourceStream` + `GetAnimator
  (image).Play()`; keep the stream referenced for as long as attached.
- WPF `SystemParameters` has no `DoubleClickTime` — P/Invoke
  `user32!GetDoubleClickTime`.
- `HttpContent` has no sync `ReadAsByteArray()` on net10 — see
  `SpriteStore.HttpFetch` (sync on purpose, background threads).
- File-based apps (`dotnet run file.cs`): lowercase `args`; `#:project`
  resolves relative to the SCRIPT; build JSON with `JsonObject` for anonymous
  types.
- Seeding sandboxes: pattern in `Temp\opencode\ptb-m7-smoke\seed-balloon.cs`
  — explicit `StateFilePath`, ApplyUsage baseline+hatch, mutate
  `State.Inventory`/`State.Active.UsedAtStage`, persist via a +1 ledger
  ApplyUsage.
- Double math expectations: assertions from observed behavior
  (`4_999_999 / 5M * 10M` floors to `9_999_998`). `Assert.Throws<T>` needs
  the EXACT type. Hatch check runs on EVERY apply; read `PendingUnownForm`
  BEFORE clearing it.
- PokéAPI quirks: Manaphy (490)/Phione (489) share chain 250 whose root is
  not the requested base — script re-roots; chains of ≤649 species include
  >649 members — filter at load. PowerShell snapshot-script traps: `break`
  after retry success; wrap `JsonArray`-returning functions with `,`; `@(a,
  $arr)` FLATTENS; `DeepClone()` before re-parenting.
- Normal and unrelated: git CRLF warnings; `docker-desktop` in `wsl -l -v`;
  SSH trap if push denied (`ssh-add -d <key>; ssh-add <key>`, verify
  `ssh -T git@github-personal` greets `Strongorange`).
- Already tried and rejected: file watchers over the WSL boundary, live
  PokeAPI at runtime for BASE DATA, parsing `wsl.exe -l` output, version in
  the tray tooltip.

## Working Agreement
- Slice per milestone; report after the milestone or when blocked. Confirm
  the slice choice with the user first (this handoff's Decision Needed).
- Evidence: unit tests for pure parts; UI behavior manual by the user; the
  `PTB_STATE_DIR` sandbox + `dotnet run file.cs` seeding tricks for
  destructive checks.
- Core/Providers/Application stay UI-free; UI references Application only.
- Tools & skills: `openviking` MCP — read/write session-continuity events
  (see `viking://user/owner/memories/events/2026/09/`); model the next
  handoff on this file.

## Open Risks
- OpenCode: storage locations on Windows/WSL unknown until the research
  spike; do NOT wire a provider before fixtures exist (same rule that made
  Claude/Codex slices safe).
- XamlAnimatedGif is a hobby-maintained package — if GIF playback ever
  breaks after a .NET upgrade, the static PNG fallback path already exists;
  do not build more on top of animator internals.
- Install dir is wiped on every publish (fresh copy) — never point anything
  (shortcuts aside) at files inside it, and never store data there.
- Sprite disk cache grows unbounded by design — keep, no prune unless asked.
- Pet window is not covered by unit tests (WPF) — changes to drag/click
  logic need the manual checklist (drag / click no-op / double-click /
  right-click / restart persistence).
- Balloon tips are best-effort (Windows may suppress under Focus Assist).

## Acceptance Criteria
- [ ] Slice confirmed with the user and implemented per its own plan
- [ ] `dotnet test PokeTokenBar.Windows.slnx` fully green (328 + new)
- [ ] Manual smoke on this machine incl. app restart; user confirms
      (for app slices: re-publish via `scripts\publish-windows.ps1` so daily
      use picks the change up; bump `<Version>` minor/patch accordingly)
- [ ] Handoff for the next slice written and committed

## Verification
- `dotnet test PokeTokenBar.Windows.slnx` — all tests pass
- Manual on this machine per slice; `PTB_STATE_DIR` sandbox for destructive
  checks; diagnostics log clean after runs

## Related Docs
- `docs/windows-port-plan.md` — canonical plan (M0–7 + Personal packaging + Later)
- `docs/handoff/2026-09-23-milestone-8-opencode-combat-details-packaging.md` — previous handoff (M8)
- OpenViking memory: `viking://user/owner/memories/events/2026/09/` — session continuity entries (M0–M8)

## Start Prompt
```text
Read docs/handoff/2026-09-23-milestone-9-opencode-or-combat-details.md end to end.
Work in C:\Users\USER\my-pjts\poketoken-bars-windows, branch main, base main.
Milestones 0-8 are complete and verified. First confirm the M9 slice with the user (OpenCode research spike vs combat-details enrichment — see Decision Needed at Start; follow the user if they name something else).
Then implement that slice only, prove pure parts with tests, run the manual checklist, and verify with dotnet test PokeTokenBar.Windows.slnx. Do not bundle the other candidate into this slice.
```
