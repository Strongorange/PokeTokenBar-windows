# Handoff — Milestone 23: next UI-parity slice (confirm choice first)

## Goal
UI parity polish with the macOS original continues. M12–M22 are done and
user-verified (see `docs/windows-port-plan.md` "Reopened" section). M22
(0.21.0) shipped the detail-window polish slice from `docs/ui-parity-audit.md`
(type capsules, individual picker, actual stats w/ IV, species-data section).
M23 is the next slice from that audit — confirm the choice with the user
FIRST (see Decision Needed at Start); do not start scaffolding before that.
NOTE: the user plans to finish the remaining audit items across MULTIPLE
threads. If more than one session works this same checkout in parallel, they
will conflict (repo pushes straight to `main`); coordinate so slices touching
the same files land sequentially.

## Workspace
- Checkout: `C:\Users\USER\my-pjts\poketoken-bars-windows` (Windows 11 25H2
  machine, target environment itself)
- Branch: `main` / Base: `main` (this repo pushes directly to `main`; no PR
  flow used so far)
- Commits: M22 = feat commit (detail-window polish + strings + 0.21.0 bump)
  + docs close-out (plan + audit + THIS handoff)
- Commit convention: English conventional commits (`feat:`, `fix:`, `docs:`)

## Current State
- Done — M22 (user-verified on 0.21.0): the species detail window now mirrors
  the macOS `PokemonDetailView` (CompanionView.swift 1047-1315) layout.
  - Header: name · #id, rarity-only secondary line, "✨ Shiny" line and
    accent "Raising" line (both hidden unless applicable, re-rendered on
    Unown form change), M21 ★/☆ representative toggle unchanged.
  - Species-data section (new, macOS `speciesSection`): localized type
    names as UPPERCASE capsules (accent-tinted `AccentSoftBrush`), height /
    weight / base-total value pairs (9pt label + 12pt semibold value), then
    possible abilities (hidden ones suffixed "(숨김)").
  - Individual picker (macOS `individualPicker`): when the form-filtered
    individual list has 2+ entries a ComboBox lists "#N · Lv. X" (index
    within the FILTERED list, NOT `CompanionDetailIndividual.Label`) and
    only the selected individual renders; form change resets to #1.
  - Individual card: level/gender/nature value pairs ("—" for empty),
    ability block (" · 숨겨진 특성" suffix when hidden), "실제 능력치"
    section — bars scaled by `PokemonStatCalculator.DisplayScaleMaximum`
    (already in Core, tested), right-aligned value + secondary "IV n"
    column — then known-move rows (name left, "Lv. n" right) or the
    NoLevelMoves line when empty.
  - XOR like macOS: individuals present → actual stats only; none → base
    stats section at fixed scale 300, no IV column (both via the new
    `AddStatRow(target, label, value, iv?, scaleMaximum)` Grid helper).
  - Data: NO engine change — `CompanionDetailSnapshot` already carried
    everything. Strings: 8 macOS-verbatim added (IndividualTitle, Level/
    Gender/NatureTitle, ActualStatsTitle, SpeciesDataTitle,
    HiddenAbilityTitle, NoLevelMoves); `IndividualsTitle` REMOVED (unused).
- Evidence: `dotnet test PokeTokenBar.Windows.slnx` 445 green (444 + 1
  detail-strings test). Smoke harness `%TEMP%\opencode\ptb-m22-smoke\
  Program.cs` (snapshot now has details for species 50; seeds a 3-individual
  multi-catch incl. a shiny one + an active raising mon, and a profile-less
  legendary) renders 3 PNGs (multi-first / multi-shiny after picker change /
  base-only) and probes picker items, IV rows, capsule count, XOR titles,
  hidden-ability suffix, shiny/raising lines. User verified the PNG
  checkpoint, then manual smoke on published 0.21.0 (detail window on real
  data, picker switching, restart).
- The audit doc `docs/ui-parity-audit.md` tracks every remaining item with
  macOS file references — keep it updated when slices land.

## Locked Decisions
- Stack C# / .NET 10 (`net10.0`, UI `net10.0-windows` WPF +
  `Hardcodet.NotifyIcon.Wpf` 2.0.1 + `XamlAnimatedGif` 2.3.2), layering
  unchanged: Core pure, Providers parse, Platform.Windows owns paths/
  settings/diagnostics, Application owns engine + orchestration, UI consumes
  Application only. No MVVM framework.
- Theme is Fluent light ONLY (`src/Ui/Theme.xaml` is the single color/font/
  radius source; no hardcoded hex in windows or code-behind — use `Token()`
  / `{DynamicResource}`; the only sanctioned hex-alpha tints follow the
  `ColorOf`/`#14`-prefix pattern without '#'). Dashboard resizable (min
  560x640, default 640x800); settings/detail windows fixed `CanMinimize`.
- Official provider limits stay OUT (needs OAuth credentials; violates the
  no-credentials rule) unless the user explicitly designs an opt-in.
- Pokemon base data stays the bundled schema-2 snapshot (offline-first);
  sprites remain the separate online-cached concern (`SpriteStore`). Shop/
  bag item icons use `FallbackEmoji`.
- Packaging: version = Ui csproj, bumped with the shipping slice (0.21.0 =
  M22; M23 ships 0.22.0). Publish via `scripts\publish-windows.ps1` (kills
  the installed app; relaunch the exe yourself). Releases by hand,
  `v<Version>` tags; update checker wired to `Strongorange/PokeTokenBar-windows`.
  v0.21.0 release prep: zip = `PokeTokenBar-<version>-win-x64.zip` containing
  ONLY PokeTokenBar.exe (pdbs excluded — README's install section names this
  exact zip), notes = changelog since v0.10.0 (0.11.0–0.21.0, M12–M22;
  see `docs/windows-port-plan.md` "Reopened" slices). `gh` CLI NOT
  authenticated and the GitHub API is 403 rate-limited without a token, so
  the user uploads release + asset by hand in the browser.
- State under `%LOCALAPPDATA%\PokeTokenBar` (`PTB_STATE_DIR` overrides).
  Language is saved-game state via `SetLanguage`. README en+ko only.
- Never read/copy credentials. Comments: none unless asked. English commits.
  Commit + push only when the user asks. Remote `origin` =
  `git@github-personal:Strongorange/PokeTokenBar-windows.git`.

## Decision Needed at Start (confirm with user before scaffolding)
Pick from `docs/ui-parity-audit.md` (each its own slice; user may pick
something new). Remaining:
- Tier 2: celebration animation package (flash/pop/✨/🎭/+XP/mint sparkle/
  egg wiggle; needs a celebration queue in the engine + WPF storyboards).
  Catch-log view (per-individual rows w/ evolution chains, natures,
  caught-at; Core `DexEntry` has the data). Companion status line (port
  macOS `computeState` machine). Incident banner (statuspage.io, no auth,
  network + privacy doc).
- Tier 3: shop item sprite images; dark mode; launch-at-login.
- Engineering quality (user principle: every unit modular and individually
  testable): extract pure UI-adjacent logic from `DashboardWindow.xaml.cs`
  (~1300 lines — dex filter decision, capsule/color mapping incl. `ColorOf`
  duplicating Theme brushes, bag/shop row presentation states, usage/provider
  presentation decisions added in M21) into Core/Application helpers with
  unit tests. Can ride along with a UI slice that touches the same code.

## Contracts
- Engine: `ApplyUsage(...)`, shop/candy/mint/difficulty members,
  `PlanRareCandyUse(int)`, `MaxRareCandyUseCount()`, `SetLanguage`,
  `SetRepresentative(int?, UnownForm?) -> bool`, `View()` →
  `CompanionGameView` (…, `EvoLine`, `DexRows`, `ShopRows`, `Bag`,
  `AvailableTokens`, `RecentEvents`, `EggGuarantee`, `RepresentativeSpeciesID`,
  `RepresentativeIsShiny`, `RepresentativeUnownForm`, ends `UnownForms,
  AppLanguage Language`), `Detail(speciesID)`, `Changed`, `DrainNotices()`,
  `ExportSave`/`ImportSave`. NEW VIEW FIELDS GO BEFORE `UnownForms`.
- `Detail(speciesID)` returns null unless the snapshot has a `details` entry
  for that species (relevant for smoke fixtures).
- `CompanionDetailSnapshot` unchanged in M22 (Individuals carry Label,
  UnownForm, IsShiny, IsRaising, Level, Gender, Nature, Ability,
  AbilityIsHidden, Stats(Name, Base, Iv, Value), Moves(Name,
  LearnedAtLevel)). `CompanionShopRow`/`CompanionBagItem` — record value
  equality powers confirm-state matching.
- Usage: `UsageDisplayState(AsOfUtc, Providers, Today*, Month*, MonthDaily?,
  TodayModels?, WeekTokens, WeekCost, Today/Week/MonthCostCoverage)` and
  `ProviderUsageSummary(…, Today/Month, today breakdown ×4, Week*,
  coverages, TodayModels?)` — extensions appended as optional params only.
- UpdateChecker (M17, unchanged): `UpdateCheckerOptions`, `Available`/
  `Skipped`/`UpdateTarget`/`SettingsNotice`, `CheckAsync`, `Consider`/
  `SkipCurrent`/`ShowSkippedAgain`, static `IsSafeReleaseUrl` (https +
  github.com host only; opens the release PAGE, assets are never
  auto-downloaded).
- Settings: `AppSettings` incl. `SkippedUpdateVersion`,
  `UpdateNotificationsEnabled`; guarded `Application.Current is App` checks.
- New fixed UI strings go in `src/Core/DashboardText.cs` macOS-verbatim
  (ko/en/ja/es/fr/pt/de order matches `DashboardText.T`); add a Core.Tests
  string test in the same slice. Detail-window keys added in M22:
  IndividualTitle, LevelTitle, GenderTitle, NatureTitle, ActualStatsTitle,
  SpeciesDataTitle, HiddenAbilityTitle, NoLevelMoves (IndividualsTitle
  removed).
- Theme resources via `{DynamicResource <Key>Brush}` or `Token(key)`; new
  windows merge Theme.xaml per-window AND in App.xaml.
- Sprites through `SpriteSlot` (cached-first, never block UI); static
  thumbnails >44px use NearestNeighbor.
- Snapshot schema 2 (loader accepts 1). Regenerate only via
  `scripts/generate-pokemon-snapshot.ps1` under pwsh.

## Relevant Files
- `docs/ui-parity-audit.md` — the candidate list (update per slice)
- `src/Ui/SpeciesDetailWindow.xaml(.cs)` — M22 layout: `Build` →
  `RenderIndividuals` (picker + card XOR `RenderBaseStats`) →
  `BuildSpeciesData` → `BuildMoveList`; helpers `AddSection(target, text)`,
  `AddSubSection`, `AddValuePairs`, `AddTypeCapsules`, `AddKnownMoveRow`,
  `AddStatRow(label, value, iv?, scaleMaximum)`; `_selectedIndividualIndex`
  resets on form change; `RenderIdentityLines` drives the ✨/Raising header
  lines
- `src/Core/DashboardText.cs` + Tests/Core.Tests/DashboardTextTests.cs
- `src/Ui/DashboardWindow.xaml(.cs)` — untouched in M22; still owns
  `Token`/`RarityBrush`/`HexBrush`/`ColorOf` helpers, 4 tab headers by
  index in `LocalizeStaticText` (dex tab = index 2, see `SelectDexTab`)
- `src/Core/PokemonProfile.cs` — `PokemonStatCalculator.DisplayScaleMaximum`
  (bars) + `Stats` (actual values incl. nature)
- macOS references: `Sources/PokeTokenBar/UI/CompanionView.swift`
  (`PokemonDetailView` 1047-1315, `individualPicker` 1182,
  `speciesSection` 1261, `statRow` 1251; CompanionHeader 487-717,
  DexSpeciesCell 1334), `PopoverView.swift` (header 150-224, providerRow
  203-281, ProviderTabBar 1041, `providerStatusBanner` 303), `BagView.swift`,
  `ShopView.swift`, `SettingsView.swift`
- `docs/handoff/2026-09-29-milestone-22-ui-polish-next.md` — M22 kickoff
  handoff (superseded by this file)
- RELEASE.md / `docs/reference/release-workflow/` — macOS-original release
  runbook (brew cask, release.sh, gh-pages) — NOT the Windows process;
  Windows releases are by hand per Locked Decisions

## Hard-Won Context
- Run/test: `dotnet test PokeTokenBar.Windows.slnx` (~15s, 445 tests) —
  `MSB1009` if you type `.sln`. Kill DEBUG `PokeTokenBar.exe` before
  testing (`MSB3027`); a running INSTALLED exe does NOT block builds.
- Smoke apps (`dotnet run Program.cs`): need
  `#:property TargetFramework=net10.0-windows`, `UseWPF=true`,
  `PublishTrimmed=false` (NETSDK1168), `#:project <abs Ui csproj>`;
  x:Name fields are internal → `FindName`. Seeding: `engine.State.Dex`
  (`DexEntry(baseID, finalID, chain, rarity, caughtAt, isShiny: …,
  nature: …, profile: …)` — `PokemonProfile` is a mutable class, set
  Level/Gender/AbilityName/AbilityIsHidden/IVs/Moves directly),
  `engine.State.Inventory[ItemKinds.Raw(kind)]`; first `ApplyUsage(map,
  date, true)` only sets the baseline (wallet 0) — a second higher call
  creates spendable delta. Synthetic DexEntries without `Names` render
  "#id" fallback names — harmless smoke artifact.
- Smoke detail-window specifics: the smoke snapshot needs a `details` entry
  for EVERY species you open (else `Detail` returns null silently); JSON
  edits in the raw-string literal break easily — mind commas between
  detail entries. Probe with `Visibility == Visibility.Visible` filtering —
  collapsed TextBlocks (identity lines) still carry their Text.
- Offscreen screenshots: arrange `window.Content`, `RenderTargetBitmap` at
  1.5x; tabs lazily realized — SelectedIndex then UpdateLayout, re-FindName
  after re-renders. `new SpriteStore(realCacheDir, _ => null)` = real
  cached sprites offline (works for FloatingPetWindow too).
- WPF gotchas: no `StackPanel.Spacing`; `new X { ... } { Children = ... }`
  is invalid C#; `HexBrush("#14" + hex)` — hex constants must NOT contain
  a '#' (FormatException at render time, not compile time);
  `Math.Round(float)` is ambiguous → cast to double first; `long` → `int`
  implicit conversions don't exist (bag Count cast); pattern variable from
  `x is { } y` is NOT definitely-assigned when the test result is stored in
  another bool used later (CS0165) — assign `var id = x;` then `id is null`
  checks instead.
- `CompanionGameView` field order is part of the contract — new view fields
  go before `UnownForms`; tests construct positionally in theory, current
  tests build via `engine.View()`.
- Capsule/pill pattern: Border CornerRadius=half-height + Padding 6-8,1-3 +
  8-10pt Bold text; rarity colors live in Theme (`RarityLegendaryBrush`
  #F7630C etc.) and `ColorOf` (hex WITHOUT '#') for alpha tints. Type
  capsules use `AccentSoftBrush` + uppercase 9pt Bold (M22).
- Usage week window: Sunday start via `CultureInfo.InvariantCulture` — do
  NOT use CurrentCulture there (test determinism across machines).
- Codex fixture vectors are (Input, Cached, Output, Total) and per-entry
  usage comes from `last_token_usage` — check before asserting breakdowns.
- Bag/shop confirm states are fields reset by full re-render on every
  `UpdateGame`; provider chip selection `_selectedProviderId` survives
  refreshes, falls back to first provider with usage.
- GitHub API from this machine without a token is 403 rate-limited; manual
  update checks masquerade as "up to date" (same as macOS). `gh` CLI NOT
  authenticated (verified again at M22 close). Releases: v0.10.0 + (being
  uploaded by hand) v0.21.0 with `PokeTokenBar-0.21.0-win-x64.zip`.
- Normal and unrelated: git CRLF warnings; `docker-desktop` in `wsl -l -v`;
  SSH trap if push denied (`ssh-add -d <key>; ssh-add <key>`, verify
  `ssh -T git@github-personal` greets `Strongorange`).
- Already tried and rejected: file watchers over the WSL boundary, live
  PokeAPI for BASE DATA, macOS-card theme (user picked Fluent from a
  rendered comparison), 40px dex tiles, saturation-dimming, official
  limits via credentials.

## Working Agreement
- Slice per milestone; report after the milestone or when blocked. Confirm
  the M23 choice with the user before building anything. Visual work gets a
  rendered sample/screenshot checkpoint BEFORE polishing everything (M13
  40px-tile rejection; M19/M20 mid-review course corrections both paid off).
- Evidence: unit tests for pure parts; UI behavior manual by the user;
  `PTB_STATE_DIR` sandbox + offscreen screenshot harness for layout checks.
- Core/Providers/Application stay UI-free; UI references Application only.
- Tools & skills: `openviking` MCP — read/write session-continuity events
  (see `viking://user/owner/memories/events/2026/09/`); model the next
  handoff on this file; update `docs/ui-parity-audit.md` when a slice lands.

## Open Risks
- v0.21.0 release upload is manual (user, browser): zip name must match
  README (`PokeTokenBar-0.21.0-win-x64.zip`), tag `v0.21.0`. Until it is
  uploaded, installed 0.10.0+ clients see no update; after upload the M17
  update checker wakes up for the first time in real conditions.
- Detail-window shiny/raising header lines key off profile-bearing
  individuals; a shiny dex entry WITHOUT a profile (synthetic-only edge so
  far) does not light the ✨ line. Harmless for real saves (hatches create
  profiles).
- Provider card collapses to hidden when NO provider has usage and none
  are unavailable (fresh install with empty logs) — today card still shows
  zeros; verify this reads fine on a fresh `PTB_STATE_DIR` sandbox.
- Unavailable-provider line is Windows-only (macOS hides them entirely) —
  kept for parity with the old list info; revisit if the user dislikes it.
- Egg imminent/guarantee visuals still never seen live (user had an active
  mon at M20/M21/M22 close). First guaranteed-tier egg purchase (상점 희귀
  알) is the live field test.
- Shiny ✨ species line now surfaced in the detail header (M22) — still
  never seen live by the user on a real shiny (no shiny mon since 09-23);
  representative row/settings do show ✨ from dex shiny state.
- XamlAnimatedGif hobby-maintained — static PNG fallback exists.

## Acceptance Criteria
- [ ] M23 slice confirmed with the user before any code
- [ ] `dotnet test PokeTokenBar.Windows.slnx` fully green (445 + new)
- [ ] `docs/ui-parity-audit.md` updated for the shipped items
- [ ] Manual smoke on this machine incl. app restart; user confirms
      (publish via `scripts\publish-windows.ps1`; bump `<Version>` to
      0.22.0; relaunch the exe after publishing)
- [ ] Handoff for the next slice written and committed (if the series
      continues)

## Verification
- `dotnet test PokeTokenBar.Windows.slnx` — all tests pass
- Manual on this machine per slice; `PTB_STATE_DIR` sandbox for destructive
  checks; diagnostics log clean after runs

## Related Docs
- `docs/ui-parity-audit.md` — candidate list (authoritative)
- `docs/windows-port-plan.md` — canonical plan (closure + reopened polish)
- `docs/handoff/2026-09-29-milestone-22-ui-polish-next.md` — M22 kickoff
- `docs/handoff/2026-09-28-milestone-11-next-or-close.md` — pre-reopen
  handoff (full trap list, still canonical)
- OpenViking memory: `viking://user/owner/memories/events/2026/09/` —
  session continuity entries (M0–M22)

## Start Prompt
```text
Read docs/handoff/2026-09-29-milestone-23-ui-polish-next.md end to end.
Work in C:\Users\USER\my-pjts\poketoken-bars-windows, branch main, base main.
The project is reopened for UI parity polish; M12-M22 are done (0.21.0
shipped: detail-window polish — type capsules, individual picker, actual
stats with IV, species-data section). The candidate list lives in
docs/ui-parity-audit.md — confirm the M23 slice with the user FIRST (Tier-2:
celebration animation package, catch-log view, status line, incident banner;
Tier-3 constrained; engineering-quality extraction can ride along). For
visual work render a sample/screenshot checkpoint before polishing
everything. Implement that slice only, prove pure parts with tests, update
the audit doc, run the manual checklist, and verify with dotnet test
PokeTokenBar.Windows.slnx. Do not bundle other work into the slice. If other
threads are working this same checkout, coordinate landings — this repo
pushes straight to main.
```
