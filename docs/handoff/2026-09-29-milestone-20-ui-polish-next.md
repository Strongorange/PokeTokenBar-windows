# Handoff — Milestone 20: next UI-polish slice (confirm choice first)

## Goal
The project was closed into maintenance mode at M11, then reopened by the
user for UI parity polish with the macOS original. M12–M18 are done and
user-verified (see `docs/windows-port-plan.md` "Reopened" section). M19
(0.18.0) shipped the Fluent visual-quality pass AND, at the user's request
mid-review, a resizable window, a dedicated dex tab and a macOS-parity shop
tab. M20 is the next slice from the remaining candidates — confirm the
choice with the user FIRST (see Decision Needed at Start); do not start
scaffolding before that.

## Workspace
- Checkout: `C:\Users\USER\my-pjts\poketoken-bars-windows` (Windows 11 25H2
  machine, target environment itself)
- Branch: `main` / Base: `main` (this repo pushes directly to `main`; no PR
  flow used so far)
- Commits: M0–M9 unchanged (`f763205`…`b31cb0e`) · M10–M19 see
  `git log --oneline -20` · M19 = feat commit (Fluent theme + tabs) + docs
  close-out commit (plan + THIS handoff) on top
- Commit convention: English conventional commits (`feat:`, `fix:`, `docs:`)

## Current State
- Done — M19 task 1: Fluent/Win11 light theme. `src/Ui/Theme.xaml` is the
  single design source: brushes (WindowBg #F3F3F3, Card #FFFFFF + border
  #E5E5E5 radius 8, TextPrimary/Secondary/Tertiary #1B1B1B/#616161/#8A8A8A,
  Accent #0078D4 + hover #106EBE + pressed #005A9E + soft #E5F1FB, rarity
  brushes, Warning #B45F06, Tooltip #2B2B2B), `UiFont` = "Segoe UI Variable
  Text, Segoe UI", implicit styles (Button, ListBox/ListBoxItem, TabControl/
  TabItem pivot with accent underline, ProgressBar rounded fill, ToolTip,
  CheckBox vector check, ComboBox + ComboBoxItem, Slider pill track, flat
  10px ScrollBar) and keyed styles (`Card` Border, `SectionHeader`,
  `Caption`, `Tertiary`, `AccentButton`). It is merged in App.xaml AND in
  each window's Window.Resources — the per-window merge exists so headless
  smoke (plain `new Application()`) still resolves resources; keep that
  pattern when adding windows. Windows set Background/FontFamily/Foreground
  via DynamicResource. Code-behind colors go through
  `TryFindResource(key) as Brush` helpers (`Token` in DashboardWindow/
  SpeciesDetailWindow) — no hardcoded hex left in the UI layer.
- Done — M19 task 2 (user-requested growth of the slice): window 640x800,
  `ResizeMode="CanResize"`, MinWidth 560 / MinHeight 640; the dex became its
  own tab (full-width WrapPanel of 76px tiles, 7 columns at 640px, ~45 tiles
  visible; `DexHeader` count/wallet/hint moved with it); the game tab keeps
  a companion-events card (`EventsHeader` = macOS `companionNotificationsLabel`)
  plus export/import buttons; the shop became its own tab modeled on macOS
  `ShopView`: wallet card (SpendableLabel + big Consolas `SpendableAmount` +
  ShopHintText) and per-entry cards built in `RenderShopCards` —
  `CreateItemCard` (emoji `FallbackEmoji`, description, `OwnedCount` suffix,
  price + per-card buy with inline `BuyConfirm` confirm, passive-owned shows
  green ✓ `OwnedAlready`, can't-afford shows `NotEnoughTokens`) and
  `CreateEggCard` (tier capsule in rarity color, `EggDescription`, no-active
  → disabled buy + `EggShopLockedHint`, buy → inline `EggConfirm`, shiny →
  `FreshEggShinyWarning` + `FreshEggDiscardShiny` second confirm via
  `_confirmingShopRow`). The old game-tab shop list (`ShopList`, shared
  `BuyButton`, `OnBuyClick`) is GONE; the game tab has a "bag" card
  (`ShopHeader` relocalized to `BagTitle`, BagText, Use candy/mint/all
  buttons). `LocalizeStaticText` localizes 4 tab headers by index (Usage,
  Game, Dex, Shop) — keep indexes in sync.
- Strings: ~20 macOS-verbatim additions in `DashboardText.cs` (BagTitle,
  SpendableTokens, ShopHint, BuyLabel, BuyConfirm, CancelLabel,
  NotEnoughTokens, OwnedCount, ShopPriceLabel, OwnedAlready, ItemDescription
  (candy XP from `RareCandies.Xp`), EggDescription, EggShopLockedHint,
  EggConfirm, FreshEggShinyWarning, FreshEggDiscardShiny,
  CompanionEventsLabel). `SelectShopRowFirst`/`OwnedSuffix`/`NeedMoreTokens`/
  `BagLabel` are now unused by UI but kept (tests may still cover them).
- Evidence: `dotnet test PokeTokenBar.Windows.slnx` 438 green (435 + 3:
  shop-strings test, companion-events test, + the existing suite). Offscreen
  screenshot harness `%TEMP%\opencode\ptb-m19-smoke\Program.cs` (recreate if
  needed): renders game/usage/dex/shop tabs + settings + detail to PNGs and
  probes layout numbers (GameFooter bottom, WrapPanel columns, ShopCards
  count, SpendableAmount). Manual: 0.18.0 published via
  `scripts\publish-windows.ps1`, relaunched, user verified the themed
  windows, the dex tab + resizing, and the shop/bag split in the real app.
  Diagnostics log clean.
- Style direction was chosen by the user from a rendered 3-panel comparison
  image (current vs macOS-cards vs Fluent) — build such a sample BEFORE
  large visual work; the user rejected nothing here but picked Fluent over
  macOS-cards, light-only (no dark mode).

## Locked Decisions
- Stack C# / .NET 10 (`net10.0`, UI `net10.0-windows` WPF +
  `Hardcodet.NotifyIcon.Wpf` 2.0.1 + `XamlAnimatedGif` 2.3.2), layering
  unchanged: Core pure, Providers parse, Platform.Windows owns paths/
  settings/diagnostics, Application owns engine + orchestration, UI consumes
  Application only. No MVVM framework.
- Theme is Fluent light ONLY. Dark mode is deliberately out of scope (WPF
  needs manual title-bar + resource swap; separate slice if ever wanted).
  Theme.xaml is the only place for colors/fonts/radii — do not hardcode hex
  in windows or code-behind.
- Dashboard is now resizable (min 560x640, default 640x800); settings and
  detail windows stay fixed-size `CanMinimize`.
- Pokemon base data stays the bundled schema-2 snapshot (offline-first);
  sprites remain the separate online-cached concern (`SpriteStore`). Shop
  item icons use the Core `FallbackEmoji` (🍬/🌿/✨) — real item sprite
  images are a candidate, not a regression.
- Packaging stays personal-use: single-file self-contained exe in
  `%LOCALAPPDATA%\Programs\PokeTokenBar` + Start Menu shortcut. Version in
  the Ui csproj, bumped with the slice that ships it (0.18.0 = M19; M20
  ships 0.19.0). GitHub Releases uploaded by hand; `v<Version>` tags; the
  update checker is wired to `Strongorange/PokeTokenBar-windows`.
- App state/sprites/diagnostics under `%LOCALAPPDATA%\PokeTokenBar`
  (`PTB_STATE_DIR` overrides). Local preferences in settings.json. Language
  is saved-game state, set via `CompanionEngine.SetLanguage`, never
  settings.json.
- README policy: en + ko only, no macOS screenshots. Do not "restore" ja.
- Never read/copy credentials. Comments: none unless asked. English commits.
  Commit + push only when the user asks. Git remote `origin` =
  `git@github-personal:Strongorange/PokeTokenBar-windows.git`.

## Decision Needed at Start (confirm with user before scaffolding)
Remaining candidates after M19 (each its own slice; the user may also pick
something new — that is how M17/M18/M19 grew):
- Hatch/evolve celebration animation (macOS: white flash + spring pop +
  delayed ✨ burst on shiny hatch; macOS reference CompanionView.swift
  643–669 and the M18 handoff notes).
- Richer provider rows in the usage tab (name + today/month columns instead
  of one long string; macOS PopoverView provider sections).
- Shop item sprite images (PokeAPI items, cached like species sprites,
  emoji fallback) — macOS `ItemIconView`.
- Dark mode (bigger: token swap + title-bar handling).

## Contracts
- Engine: `ApplyUsage(...)`, shop/candy/mint/difficulty members,
  `SetLanguage(AppLanguage)`, `View()` → `CompanionGameView` (…, `StageItems`,
  `EvoLine`, `DexRows`, `ShopRows`, `Bag`, `AvailableTokens`, `RecentEvents`,
  ends `UnownForms, AppLanguage Language`), `Detail(speciesID)`, `Changed`,
  `DrainNotices()`, `ExportSave`/`ImportSave`.
- `CompanionShopRow(ItemKind? Item, Rarity? EggTier, string Label, long
  Price, bool CanBuy)` — value equality powers `_confirmingShopRow == row`.
- UpdateChecker (M17, unchanged): `UpdateCheckerOptions`, `Available`/
  `Skipped`/`UpdateTarget`/`SettingsNotice`, `CheckAsync`, `Consider`/
  `SkipCurrent`/`ShowSkippedAgain`, static `IsSafeReleaseUrl`.
- Settings: `AppSettings` incl. `SkippedUpdateVersion`,
  `UpdateNotificationsEnabled`; windows reach App only via guarded
  `Application.Current is App` checks so headless tests work.
- New fixed UI strings go in `src/Core/DashboardText.cs` (copy the macOS
  translation when the concept exists in `Localization.swift`; macOS t()
  argument order ko/en/ja/es/fr/pt/de matches `DashboardText.T`). Add a
  Core.Tests string test in the same slice.
- Theme resources: reference via `{DynamicResource <Key>Brush}` in XAML or
  the `Token(key)` helper in code-behind. New windows must merge Theme.xaml
  in their own Resources AND be added to App.xaml merges.
- Sprite rendering goes through `SpriteSlot` (cached-first; never block the
  UI thread on fetch). Static thumbnails >44px set
  `RenderOptions.SetBitmapScalingMode(NearestNeighbor)`.
- Snapshot: schema 2 shape; loader keeps accepting schema 1. Regenerate only
  via `scripts/generate-pokemon-snapshot.ps1` under pwsh.
- Packaging: version = Ui csproj `<Version>`; publish via
  `scripts/publish-windows.ps1` (kills the installed app and does NOT
  relaunch it — start the exe yourself after publishing).

## Relevant Files
- `src/Ui/Theme.xaml` — ALL tokens + control styles (see Current State)
- `src/Ui/App.xaml` — merges Theme.xaml; `src/Ui/{Dashboard,Settings,
  SpeciesDetail}Window.xaml` — merge it again per-window + card layout
- `src/Ui/DashboardWindow.xaml.cs` — `Token`, `LocalizeStaticText` (4 tabs),
  `UpdateShop`/`RenderShopCards`/`CreateItemCard`/`CreateEggCard`/
  `CreateShopCardHeader`/`RarityBrush`/`WrapShopCard`, `CommitEggPurchase`,
  `_confirmingShopRow`, `CreateDexTile` (76px tiles)
- `src/Core/DashboardText.cs` — shop/bag strings + Tests/Core.Tests/
  DashboardTextTests.cs (ShopStringsFollowMacOSTranslations,
  CompanionEventsLabelFollowsMacOSTranslations)
- `Sources/PokeTokenBar/UI/ShopView.swift` — macOS shop reference (wallet
  header, ShopItemCard, EggCard inline confirms) — keep in sync for future
  shop changes; `PopoverView.swift` for provider-row parity candidates
- `docs/windows-port-plan.md` — "Reopened: UI parity polish" section (M19
  paragraph + remaining candidates)
- `docs/handoff/2026-09-29-milestone-19-ui-polish-next.md` — M19 kickoff
  handoff; `docs/handoff/2026-09-28-milestone-11-next-or-close.md` —
  pre-reopen handoff with the full trap list (still canonical)

## Hard-won Context
- Run/test: `dotnet test PokeTokenBar.Windows.slnx` (~15s, 438 tests) — wrong
  file error `MSB1009` if you type `.sln`. Kill any DEBUG `PokeTokenBar.exe`
  before testing (`MSB3027`); a running INSTALLED exe does NOT block builds.
- File-based smoke apps (`dotnet run Program.cs`) need
  `#:property TargetFramework=net10.0-windows` AND `#:property UseWPF=true`
  AND `#:property PublishTrimmed=false` (NETSDK1168) when driving UI types;
  `#:project <abs path to Ui csproj>` for window access. x:Name fields are
  internal → use `window.FindName(...)`.
- Offscreen screenshots: arrange `window.Content` at the window size and
  `RenderTargetBitmap.Render(content)` at 1.5x DPI — no window shown needed.
  TabItem content is lazily realized: set SelectedIndex, then UpdateLayout,
  and re-FindName after every re-render. `new SpriteStore(realCacheDir,
  _ => null)` renders real cached sprites offline.
- WPF has no `StackPanel.Spacing` (UWP/MAUI only) — CS1026-style surprises;
  also `new X { ... } { Children = ... }` is invalid C#: put Children inside
  the single initializer braces or add imperatively. `CompanionShopRow` is a
  record → value equality (used for confirm-state matching).
- WrapPanel usable width is ~17px narrower than the ListBox viewport
  (scrollbar reservation) — 76px tiles give 7 columns at 640px; 80px would
  drop to 3+ at the old 580 width. Dex tile width is load-bearing.
- The style-compare sample (`%TEMP%\opencode\ptb-style-sample`) rendered
  current/macOS/Fluent panels side by side to PNG for the user to pick —
  reuse this pattern for any future "which look" decision.
- Engine wallet: `ApplyUsage(map, date, hasUsageData: true)` — the FIRST
  call only sets the install baseline (wallet 0); a second call with a
  higher total creates the spendable delta. `hasUsageData: false` applies
  nothing. Useful when seeding smoke states.
- xunit 2.9.3: `Assert.NotNull` returns void (CS0815). WPF traps still true:
  value-changed handlers must null-guard, horizontal StackPanel gives
  children unlimited width, keep the evo-line current-stage dot Hidden not
  Collapsed, tray handlers wrapped in try/catch.
- GitHub API from this machine WITHOUT a token is 403 rate-limited; manual
  update-check tests masquerade as "up to date" — same as macOS. `gh` CLI
  is NOT authenticated here. Releases: only `v0.10.0` exists; 0.11–0.18
  were never uploaded.
- Normal and unrelated: git CRLF warnings; `docker-desktop` in `wsl -l -v`;
  SSH trap if push denied (`ssh-add -d <key>; ssh-add <key>`, verify
  `ssh -T git@github-personal` greets `Strongorange`).
- Already tried and rejected: file watchers over the WSL boundary, live
  PokeAPI at runtime for BASE DATA, macOS-card theme (user picked Fluent
  from the comparison), 40px dex tiles (64px sprites + NearestNeighbor
  passed), macOS-style saturation-dimming (opacity-only on WPF).

## Working Agreement
- Slice per milestone; report after the milestone or when blocked. Confirm
  the M20 choice with the user before building anything. Visual work gets a
  rendered sample/screenshot checkpoint BEFORE polishing everything (M13
  40px-tile rejection; M19 mid-review grew the slice twice — both good
  outcomes because the user saw it early).
- Evidence: unit tests for pure parts; UI behavior manual by the user; the
  `PTB_STATE_DIR` sandbox + offscreen screenshot harness for layout-level
  checks.
- Core/Providers/Application stay UI-free; UI references Application only.
- Tools & skills: `openviking` MCP — read/write session-continuity events
  (see `viking://user/owner/memories/events/2026/09/`); model the next
  handoff on this file.

## Open Risks
- Shop inline-confirm state (`_confirmingShopRow`) resets on every
  UpdateGame re-render — acceptable today (confirm → buy is one click), but
  a future "re-render on background refresh mid-confirm" would cancel the
  confirm. Watch if refresh cadence changes.
- Resizable window + star-row layout: the usage tab's Providers list is a
  `*` row and the trend stretches, but nobody has tested extreme aspect
  ratios; min size 560x640 guards it.
- Events list single-line strings clip rather than trim (no TextTrimming on
  code-built rows — events are plain strings; fine at 594px width).
- Shiny ✨ visuals were never seen live by the user (no shiny mon at M18
  close; headless smoke covers the logic). First real shiny hatch is the
  field test.
- GitHub unauthenticated rate limit — startup + 30-min debounce keeps usage
  trivial; failures are invisible by design.
- Hand-uploaded releases may lag the installed version (0.11–0.18 never
  uploaded) — the checker stays dormant until a release ABOVE the installed
  version goes up.
- XamlAnimatedGif is hobby-maintained — static PNG fallback exists.

## Acceptance Criteria
- [ ] M20 slice confirmed with the user before any code
- [ ] `dotnet test PokeTokenBar.Windows.slnx` fully green (438 + new)
- [ ] Manual smoke on this machine incl. app restart; user confirms
      (re-publish via `scripts\publish-windows.ps1`; bump `<Version>` to
      0.19.0; relaunch the exe after publishing)
- [ ] Handoff for the next slice written and committed (if the series
      continues)

## Verification
- `dotnet test PokeTokenBar.Windows.slnx` — all tests pass
- Manual on this machine per slice; `PTB_STATE_DIR` sandbox for destructive
  checks; diagnostics log clean after runs

## Related Docs
- `docs/windows-port-plan.md` — canonical plan (closure + reopened UI polish)
- `docs/handoff/2026-09-29-milestone-19-ui-polish-next.md` — M19 kickoff
- `docs/handoff/2026-09-28-milestone-11-next-or-close.md` — pre-reopen handoff
  (full trap list, packaging/verification details)
- OpenViking memory: `viking://user/owner/memories/events/2026/09/` — session
  continuity entries (M0–M19)

## Start Prompt
```text
Read docs/handoff/2026-09-29-milestone-20-ui-polish-next.md end to end.
Work in C:\Users\USER\my-pjts\poketoken-bars-windows, branch main, base main.
The project is reopened for UI parity polish; M12-M19 are done (0.18.0
shipped: Fluent light theme, resizable 640x800 window, dedicated dex and
shop tabs with macOS-parity shop cards). Confirm the M20 slice with the
user FIRST (candidates: hatch/evolve celebration animation, richer provider
rows, shop item sprite images, dark mode, or their own pick). For visual
work, render a comparison sample or screenshot checkpoint before polishing
everything. Implement that slice only, prove pure parts with tests, run
the manual checklist, and verify with dotnet test PokeTokenBar.Windows.slnx.
Do not bundle other work into the slice.
```
