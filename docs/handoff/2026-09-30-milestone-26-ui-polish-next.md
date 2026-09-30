# Handoff — Milestone 26: next slice (confirm choice first)

## Goal
UI parity polish with the macOS original continues. M12–M25 are done and
user-verified (see `docs/windows-port-plan.md` "Reopened" section). M25
(0.24.0) shipped the shop/usage extraction with two shop-parity bug fixes
(item-card two-step confirm, basic-egg card) — the engineering-quality list
named in the audit is now down to the game-tab builders. M26 candidates from
`docs/ui-parity-audit.md`: Tier-2 incident banner (statuspage.io — user said
"장애 배너까지는 굳이" at M23, reconfirm first), Tier-3 dark mode (large:
token swap + title bar), or the remaining engineering-quality slice
(companion header / dex tiles / catch cards / evolution line builders still
in `DashboardWindow.xaml.cs`, now 799 lines). Do NOT scaffold before
confirming the choice with the user. NOTE: if more than one session works
this same checkout in parallel, they will conflict (repo pushes straight to
`main`); coordinate landings.

## Workspace
- Checkout: `C:\Users\USER\my-pjts\poketoken-bars-windows` (Windows 11 25H2
  machine, target environment itself)
- Branch: `main` / Base: `main` (direct push, no PR flow)
- Commits: M25 = feat commit (ShopFlow + UsagePresentation + renderers +
  shop parity fixes + 0.24.0 bump) + docs close-out (plan + audit + THIS
  handoff)
- Commit convention: English conventional commits (`feat:`, `fix:`, `docs:`)

## Current State
- Done — M25 (user-verified on 0.24.0):
  - Extraction: `DashboardWindow.xaml.cs` 1,629→799 lines. Application
    `ShopFlow` owns bag/shop confirm state (ConfirmingBagItem /
    ConfirmingShopItem keyed by ItemKind, EggConfirm keyed by tier with the
    three-stage None/Confirm/ShinyWarning flow), the candy stepper
    (ClampCandy/StepCandy/MaxCandyCount/CandyXpHint), candy plan preview
    lines, and the post-action footer Feedback. `UsagePresentation` owns
    selectable-provider filtering, the unavailable note, ordered model rows,
    and model short names. `DailyTrendMetrics` gained Peak/ShowsCost/
    TodayUsage. Ui-internal `UsageHomeRenderer` (today card + provider
    chips/detail incl. chip selection + trend chart), `ShopCards` (bag +
    shop card builders), and `Paint` (element-scoped theme brush lookup,
    rarity brushes, alpha-tint hex) own the WPF building.
  - Bug fixes (both user-approved via PNG checkpoint): shop ITEM cards
    rendered their confirm row unconditionally (cancel was a visual no-op;
    macOS `ShopItemCard` is an inline two-step: idle price row → confirm
    row); the BASIC egg row (`Item=null, EggTier=null`) fell through
    `row.EggTier is { } tier` to the item-card builder and showed the
    rare-candy icon/description/owned count. Egg cards now escalate
    Confirm → ShinyWarning exactly like macOS `EggCard` (previously a shiny
    mon jumped straight to the warning).
  - Bag card background switched from opaque `#F8F8F8` to the macOS-verbatim
    `#0F000000` alpha tint (same as catch cards) — the theme-blind hex is
    gone. ImportSave now also resets pending confirm states
    (`ShopFlow.Reset()`).
- Evidence: `dotnet test PokeTokenBar.Windows.slnx` 515 green (499 + 16
  new: 9 ShopFlow incl. shiny escalation + wallet gating, 4
  UsagePresentation, 3 DailyTrendMetrics). Smoke harness
  `%TEMP%\opencode\ptb-m25-smoke\Program.cs` (logical-tree probing — see
  Hard-Won Context) renders shop-idle / shop-item-confirm / shop-egg-confirm
  / shop-egg-shiny-warning / bag-candy PNGs with per-card text/button
  assertions; all PASS. User approved the PNG checkpoint and manual smoke on
  published 0.24.0.
- The audit doc `docs/ui-parity-audit.md` tracks every remaining item — keep
  it updated when slices land.

## Locked Decisions
- Stack C# / .NET 10 (`net10.0`, UI `net10.0-windows` WPF +
  `Hardcodet.NotifyIcon.Wpf` 2.0.1 + `XamlAnimatedGif` 2.3.2), layering
  unchanged: Core pure, Providers parse, Platform.Windows owns paths/
  settings/diagnostics, Application owns engine + orchestration, UI consumes
  Application only. No MVVM framework.
- Theme is Fluent light ONLY (`src/Ui/Theme.xaml` is the single color/font/
  radius source; no hardcoded hex in windows or code-behind — use `Paint.`
  / `{DynamicResource}` / `SetResourceReference`; the only sanctioned
  hex-alpha tints follow the `ColorOf`/`#14`-prefix pattern without '#').
  Dashboard resizable (min 560x640, default 640x800); settings/detail
  windows fixed `CanMinimize`.
- Official provider limits stay OUT (needs OAuth credentials; violates the
  no-credentials rule) unless the user explicitly designs an opt-in.
- Pokemon base data stays the bundled schema-2 snapshot (offline-first);
  sprites remain the separate online-cached concern (`SpriteStore`).
- Packaging: version = Ui csproj, bumped with the shipping slice (0.24.0 =
  M25; M26 ships 0.25.0). Publish via `scripts\publish-windows.ps1` (kills
  the installed app; relaunch the exe yourself). THE SCRIPT NOW ALSO
  PRODUCES, in `artifacts\release\` (the single canonical artifact
  location): `PokeTokenBar-<version>-win-x64.zip` (exe only, pdbs excluded —
  README's install section names this exact zip), `release-notes-v<version>.md`
  (English) and `release-notes-v<version>.ko.md` (Korean), both generated
  from the committed root `CHANGELOG.md` + `CHANGELOG.ko.md` — it REFUSES to
  publish if the newest section of EITHER changelog doesn't match the csproj
  version, so add the version's section to BOTH files as part of every
  shipping slice. NOTE: `publish-windows.ps1` must stay UTF-8 WITH BOM (it
  contains Korean string literals; PS 5.1 parses BOM-less scripts as ANSI
  and fails). Releases by hand, `v<Version>` tags; update checker wired to
  `Strongorange/PokeTokenBar-windows`; notes = the generated cumulative
  files. `gh` CLI NOT authenticated and the GitHub API is 403 rate-limited
  without a token, so the user uploads the release + asset by hand in the
  browser.
- State under `%LOCALAPPDATA%\PokeTokenBar` (`PTB_STATE_DIR` overrides).
  Language is saved-game state via `SetLanguage`. README en+ko only.
- Never read/copy credentials. Comments: none unless asked. English commits.
  Commit + push when the milestone is verified (user's standing instruction:
  implement → validate → commit+push → write next handoff). Remote `origin`
  = `git@github-personal:Strongorange/PokeTokenBar-windows.git`.

## Decision Needed at Start (confirm with user before scaffolding)
Pick from `docs/ui-parity-audit.md`. Remaining:
- Tier 2: incident banner (statuspage.io, no auth) — deprioritized at M23
  kickoff; reconfirm interest first.
- Tier 3: dark mode (large: token swap + title bar).
- Engineering quality (user principle: every unit modular and individually
  testable): ◐ — game-tab builders still in `DashboardWindow.xaml.cs`:
  companion header/progress, dex tiles + rarity tally, catch cards,
  evolution line, combat text. Natural candidate if another visual slice
  touches the same code.

## Contracts
- Engine: `ApplyUsage(...)`, shop/candy/mint/difficulty members,
  `PlanRareCandyUse(int)`, `MaxRareCandyUseCount()`, `SetLanguage`,
  `SetRepresentative(int?, UnownForm?) -> bool`, `View()` →
  `CompanionGameView` (…, `EvoLine`, `DexRows`, `ShopRows`, `Bag`,
  `AvailableTokens`, `RecentEvents`, `EggGuarantee`, `RepresentativeSpeciesID`,
  `RepresentativeIsShiny`, `RepresentativeUnownForm`, `Status`,
  `StatusEvolvedName`, `CatchRows`, ends `UnownForms, AppLanguage`),
  `Detail(speciesID)`, `Changed`, `DrainNotices()`,
  `DrainCelebrations()`, `ExportSave`/`ImportSave`. NEW VIEW FIELDS GO
  BEFORE `UnownForms`.
- M25 additions (Application): `ShopFlow` (EggConfirmStage
  None/Confirm/ShinyWarning; ConfirmingBagItem/ConfirmingShopItem keyed by
  ItemKind; CandyCount + StepCandy/ClampCandy/MaxCandyCount/CandyXpHint;
  static CandyPreviewLines(plan, lang); Begin/Cancel/CommitBagConfirm,
  Begin/Cancel/CommitItemBuy, Begin/Cancel/Advance/CommitEggConfirm,
  SetFeedback, Reset) and `UsagePresentation` (SelectableProviders,
  UnavailableNote, OrderedModels, ModelShortName). Core:
  `DailyTrendMetrics.Peak/ShowsCost/TodayUsage`. Ui (internal):
  `UsageHomeRenderer`, `ShopCards`, `Paint` (Token/RarityBrush/HexBrush).
- `Detail(speciesID)` returns null unless the snapshot has a `details` entry
  for that species (relevant for smoke fixtures).
- Usage: `UsageDisplayState(...)` and `ProviderUsageSummary(...)` —
  extensions appended as optional params only.
- UpdateChecker (M17, unchanged): `UpdateCheckerOptions`, `Available`/
  `Skipped`/`UpdateTarget`/`SettingsNotice`, `CheckAsync`, `Consider`/
  `SkipCurrent`/`ShowSkippedAgain`, static `IsSafeReleaseUrl`.
- Settings: `AppSettings` incl. `SkippedUpdateVersion`,
  `UpdateNotificationsEnabled`; guarded `Application.Current is App` checks;
  `App.LaunchAtLoginEnabled`/`ApplyLaunchAtLogin(bool)`.
- Fixed UI strings live in `src/Core/DashboardText.cs` macOS-verbatim (ko/en/
  ja/es/fr/pt/de order matches `DashboardText.T`); add a Core.Tests string
  test in the same slice. M25 added none.
- Theme resources via `{DynamicResource <Key>Brush}`, `Paint.Token(element,
  key)` (element-scoped lookup — works under a bare `new Application()` in
  smoke harnesses), or code `SetResourceReference`.
- Sprites through `SpriteSlot`; item icons through `ItemIconSlot`; static
  thumbnails >44px use NearestNeighbor.
- Snapshot schema 2 (loader accepts 1). Regenerate only via
  `scripts/generate-pokemon-snapshot.ps1` under pwsh.

## Relevant Files
- `docs/ui-parity-audit.md` — the candidate list (update per slice)
- `src/Application/ShopFlow.cs`, `src/Application/UsagePresentation.cs`,
  `src/Ui/UsageHomeRenderer.cs`, `src/Ui/ShopCards.cs`, `src/Ui/Paint.cs` —
  all new in M25
- `src/Core/DailyTrendMetrics.cs` — Peak/ShowsCost/TodayUsage added (M25)
- `src/Ui/DashboardWindow.xaml.cs` — game-tab rendering + dialogs remain
  (799 lines); shop/usage code now delegates to the renderers
- `src/Ui/DashboardWindow.xaml` — shop cards host renamed `ShopCardsHost`
  (M25; avoids the class/field collision with Ui `ShopCards`)
- macOS references: `Sources/PokeTokenBar/UI/ShopView.swift`
  (`ShopItemCard.buyControls` two-step, `EggCard.controls` three-stage),
  `PopoverView.swift` (usage home), `BagView.swift`
- `docs/handoff/2026-09-30-milestone-25-ui-polish-next.md` — M25 kickoff
  handoff (superseded by this file)
- RELEASE.md / `docs/reference/release-workflow/` — macOS-original release
  runbook — NOT the Windows process; Windows releases are by hand per Locked
  Decisions

## Hard-Won Context
- Run/test: `dotnet test PokeTokenBar.Windows.slnx` (~15s, 515 tests) —
  `MSB1009` if you type `.sln`. Kill DEBUG `PokeTokenBar.exe` before
  testing (`MSB3027`); a running INSTALLED exe does NOT block builds.
- WPF emoji are MONOCHROME — never use emoji glyphs as primary visual
  design; draw vectors (`CelebrationGlyphs` pattern) or use sprites.
- Smoke-harness tree probing: use `LogicalTreeHelper` walks, NOT
  `VisualTreeHelper`, when asserting on code-built cards — the visual tree
  lags layout and returns partial children mid-frame (M25: title TextBlocks
  intermittently missing right after a click-triggered re-render). Also
  beware `List<string>.Contains(x)` (EXACT match) vs
  `anyString.Contains(x)` (substring) — a smoke predicate bound to
  List.Contains silently fails on "Mint  Owned ×2" ≠ "Mint". Match cards by
  substring over the text list, and re-find cards after every click
  (re-renders replace the Border objects).
- Engine state in tests: direct `engine.State.Inventory[...]`/`UsedSinceInstall`
  edits are UNSAVED — fine within one engine instance, lost if the engine is
  rebuilt from the same state file. Basic egg price = 1,000,000,000; fund
  wallets ≥ price or set `UsedSinceInstall` directly (2B is a good shop-test
  wallet). Shop prices SCALED by `_shopDifficulty` can differ between
  fixtures — never hardcode them in probes.
- WPF animation smoking: set
  `SynchronizationContext.SetSynchronizationContext(new
  DispatcherSynchronizationContext())` first (async void continuations must
  re-enter the dispatcher), `window.Show()` offscreen (Left=-2000) so
  animation clocks tick, then pump fixed durations via `DispatcherFrame` +
  Background-priority DispatcherTimer. Animated property getters return the
  CURRENT animated value — probe Opacity/ScaleX/Angle directly.
  RenderTargetBitmap captures mid-animation state. Wipe smoke `state-*`
  dirs per run (persistent engine state silently breaks hatch scenarios).
- Image verification without vision: this coding model cannot read images
  (Read tool returns no visual content in this environment). Substitute
  numeric/logical probes + System.Drawing pixel checks. The user suggested
  "Muse Spark 1.3 Free" as an auxiliary image-recognition model (memory:
  preferences/user/image_recognition) but NO endpoint/CLI/skill is
  configured on this machine — if the user provides access, wire it in for
  PNG checkpoints; until then the user is the eyes.
- WPF gotchas (still true): no `StackPanel.Spacing`; `new X { ... } {
  Children = ... }` invalid; `HexBrush("#14" + hex)` — hex WITHOUT '#';
  `Math.Round(float)` ambiguous → cast double; no implicit long→int;
  CS0165 with `is { } y` stored in another bool; `Freeze()` is a method
  call, not an object-initializer property.
- `CompanionGameView` field order is contract — new view fields go before
  `UnownForms`; current tests build via `engine.View()`.
- Capsule/pill pattern: Border CornerRadius=half-height + Padding 6-8,1-3 +
  8-10pt Bold text; rarity colors via `CompanionPresentation.RarityHex`
  (hex WITHOUT '#') for alpha tints; `#26F7630C`-style orange tint for
  growth/XP chips.
- Usage week window: Sunday start via `CultureInfo.InvariantCulture` — do
  NOT use CurrentCulture there.
- Codex fixture vectors are (Input, Cached, Output, Total); per-entry usage
  comes from `last_token_usage`.
- GitHub API from this machine without a token is rate-limited (60/h per
  IP); manual update checks masquerade as "up to date" on ANY fetch failure
  (macOS-faithful by design). M25-close find: the checker's real HTTP path
  sent no User-Agent, so GitHub answered 403 for EVERY real client since
  M17 — banners/balloons never fired in the wild, and the M17 "live chain
  confirmed" was actually a curl test, not the app (smoke tests inject a
  fake fetch). Fixed in `UpdateChecker.BuildRequest` (UA +
  vnd.github+json, regression-tested) — **ships in 0.25.0**; clients on
  ≤0.24.0 never see update banners and must watch releases manually.
  Lesson: verify network-facing fixes with a live version-bumped test
  build (`dotnet publish … -p:Version=0.23.1` run sandboxed against the
  real API), not with injected fakes. v0.24.0 upload DONE (tag v0.24.0 on
  the remote, zip + ko/en notes; update flow re-verified live on a 0.23.1
  test build).
- Normal and unrelated: git CRLF warnings; `docker-desktop` in `wsl -l -v`;
  SSH trap if push denied (`ssh-add -d <key>; ssh-add <key>`, verify
  `ssh -T git@github-personal` greets `Strongorange`).
- Already tried and rejected: file watchers over the WSL boundary, live
  PokeAPI for BASE DATA, macOS-card theme, 40px dex tiles,
  saturation-dimming, official limits via credentials, emoji-as-design
  glyphs (monochrome).

## Working Agreement
- Slice per milestone; report after the milestone or when blocked. Confirm
  the M26 choice with the user before building anything. Visual work gets a
  rendered sample/screenshot checkpoint BEFORE polishing everything
  (M24's mint-cluster + emoji-color corrections and M25's basic-egg-card
  catch both came from exactly this review loop).
- Evidence: unit tests for pure parts; UI behavior manual by the user;
  `PTB_STATE_DIR` sandbox + offscreen screenshot harness for layout checks.
- Core/Providers/Application stay UI-free; UI references Application only.
- Tools & skills: `openviking` MCP — read/write session-continuity events
  (see `viking://user/owner/memories/events/2026/09/`); model the next
  handoff on this file; update `docs/ui-parity-audit.md` when a slice lands.

## Open Risks
- v0.24.0 release upload is manual (user, browser) — zip + notes are ready
  in `artifacts\release\`; installed clients see no update until then.
- Celebrations drain in `UpdateGame` — if the dashboard is CLOSED when an
  event fires, the queue holds and plays on the next open (window recreation
  calls UpdateGame; macOS-parity). If the window exists but is MINIMIZED the
  cues play unseen — accepted edge case, revisit only if the user notices.
- Status-line burn is a refresh-delta approximation; `Tired` never occurs
  (no limits feature). Fine for a mood line.
- Catch-log chain names for OLD dex entries show "#id" fallbacks.
- Egg imminent/guarantee visuals + guaranteed-tier egg purchase still never
  seen live by the user (active mon throughout M20–M25). The smoke harness
  covers the flows logically.
- Shiny ✨ surfaces — still never seen live on a real shiny since 09-23.
- Launch-at-login registry failures revert the checkbox silently.
- XamlAnimatedGif hobby-maintained — static PNG fallback exists.
- Shop difficulty scaling means fixture shop prices drift between harness
  runs (observed 100M vs 200M mint) — probes must read prices from
  `view.ShopRows`, never hardcode.

## Acceptance Criteria
- [ ] M26 slice confirmed with the user before any code
- [ ] `dotnet test PokeTokenBar.Windows.slnx` fully green (515 + new)
- [ ] `docs/ui-parity-audit.md` updated for the shipped items
- [ ] Manual smoke on this machine incl. app restart; user confirms
      (add the v0.25.0 section to BOTH `CHANGELOG.md` and `CHANGELOG.ko.md`,
      publish via `scripts\publish-windows.ps1` — it fails without the
      matching changelog sections — bump `<Version>` to 0.25.0; relaunch the
      exe after publishing)
- [ ] Handoff for the next slice written and committed (if the series
      continues)

## Verification
- `dotnet test PokeTokenBar.Windows.slnx` — all tests pass
- Manual on this machine per slice; `PTB_STATE_DIR` sandbox for destructive
  checks; diagnostics log clean after runs

## Related Docs
- `docs/ui-parity-audit.md` — candidate list (authoritative)
- `docs/windows-port-plan.md` — canonical plan (closure + reopened polish)
- `docs/handoff/2026-09-30-milestone-25-ui-polish-next.md` — M25 kickoff
- `docs/handoff/2026-09-28-milestone-11-next-or-close.md` — pre-reopen
  handoff (full trap list, still canonical)
- OpenViking memory: `viking://user/owner/memories/events/2026/09/` —
  session continuity entries (M0–M25)

## Start Prompt
```text
Read docs/handoff/2026-09-30-milestone-26-ui-polish-next.md end to end.
Work in C:\Users\USER\my-pjts\poketoken-bars-windows, branch main, base main.
The project is reopened for UI parity polish; M12-M25 are done (0.24.0
shipped: shop/usage extraction, item-card two-step confirm, basic-egg card
fix, egg shiny re-confirm). The candidate list lives in
docs/ui-parity-audit.md — confirm the M26 slice with the user FIRST
(Tier-2 incident banner needs reconfirmation after the user deprioritized
it; Tier-3 dark mode is large; the game-tab builder extraction is the
engineering remainder). For visual work render a sample/screenshot
checkpoint before polishing everything. Implement that slice only, prove
pure parts with tests, update the audit doc, run the manual checklist, and
verify with dotnet test PokeTokenBar.Windows.slnx. Do not bundle other work
into the slice. If other threads are working this same checkout, coordinate
landings — this repo pushes straight to main.
```
