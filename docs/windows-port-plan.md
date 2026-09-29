# Windows Port Plan

## Purpose

Build a personal-use Windows version of PokeTokenBar with feature parity where
it is practical, while keeping each functional area independently testable.
The initial target is a local development build, not a packaged public release.

The application will primarily run on Windows, but the user develops in WSL.
Usage must therefore include both Windows-native and WSL Claude Code/Codex
activity.

## Scope

### Initial providers

- Claude Code
- Codex
- OpenCode (added in M9; reads `opencode.db` under
  `%USERPROFILE%\.local\share\opencode` and each WSL distro's
  `/home/<user>/.local/share/opencode`)

### Initial features

- Local usage collection, aggregation, and caching
- Windows tray icon and a compact dashboard opened from it
- Pokemon progression, Pokedex, shop, and save import/export
- Local settings, sprite cache, and development diagnostics
- Floating pet, after the tray/dashboard vertical slice is stable

### Explicitly deferred

- Start automatically at sign-in
- Credential Manager integration
- Automatic updates, MSIX, winget, and public distribution
- Remote error reporting or Sentry
- Windows crash-reporting service integration
- Exact macOS menu-bar text layout

Windows notification-area icons cannot provide the same persistent inline,
multi-line token text as macOS `NSStatusItem`. The Windows design will use a
tray icon, tooltip, context menu, and compact dashboard instead.

## Usage semantics

Two kinds of measurements must remain separate.

1. **Local token usage** is calculated from local Claude Code and Codex logs.
   Windows-native and WSL log events are included in the same daily/monthly
   total after deduplication.
2. **Official account limits** are account-wide values. They must never be
   summed across Windows and WSL. This is deferred until an intentional,
   non-Keychain authentication approach is selected.

## Architecture

The UI must consume application results, not parse logs or make platform calls.

| Module | Responsibility | Validation |
|---|---|---|
| `Core` | Usage models, time/date rules, costs, Pokemon progression, save format | Unit tests |
| `Providers` | Claude/Codex log discovery contracts and parsers | Sanitized fixture tests |
| `Platform.Windows` | Windows paths, WSL discovery, local settings and diagnostic logs | Integration tests |
| `Application` | Refresh scheduling, aggregation, cache, deduplication, display state | Unit and integration tests |
| `UI` | Tray, dashboard, settings, and floating pet | Uses application interfaces only |

Each provider returns a common usage snapshot. Shared aggregation must not
contain conditions such as `if provider == "claude"`; a future OpenCode provider
is added through the same provider interface and registration point.

## Windows and WSL collection

### Windows-native roots

Discover the actual Claude Code and Codex log roots under the active Windows
user profile. Do not assume that macOS `~/Library/...` paths exist. Keep
automatic discovery and user-configured additional roots separate.

### WSL roots

Discover installed WSL distributions and their Linux home directories. Adapt
their CLI log locations to Windows-readable UNC paths, for example:

`\\wsl.localhost\\<distribution>\\home\\<user>\\...`

The Windows app should read those paths as files; parsers should not need to
know whether an event originated in Windows or WSL. The source identity is
retained for diagnostics and optional per-environment detail views.

WSL logs may be actively written and Windows file-change notifications may not
be reliable across the boundary. Start with periodic incremental scanning plus
a persistent cache rather than relying on file watchers.

### Deduplication

Windows and WSL paths can expose the same data through more than one root.
Deduplicate before aggregation using a stable source/file identity and, where
available, provider session/event identifiers. Tests must cover duplicate roots
and the same event discovered from two roots.

## Local diagnostics

No external error-reporting service is required. Write rotating local logs under
the app's local application-data directory. Record at least:

- unhandled exceptions with stack information;
- provider parse failures and their safe context;
- inaccessible/disappearing scan roots;
- read failures caused by files being written or locked;
- refresh, cache, and network failures.

Never write credentials, session keys, authorization headers, or complete
unredacted log entries to diagnostics.

## Milestones

### 0. Windows research environment

Prepare a Windows 11 machine or snapshot-capable VM using the same Windows and
WSL user profile in which the app will be used. Install and authenticate the
actual target CLIs. Capture sanitized Claude Code and Codex fixtures from both
Windows and WSL, and verify actual paths, formats, and file-lock behavior.

**Exit criteria:** documented roots and sanitized fixtures for all four initial
sources: Windows Claude, WSL Claude, Windows Codex, and WSL Codex.

### 1. Pure core and local diagnostics

Implement usage models, date boundaries, aggregation, cost calculations,
Pokemon progression, and save encoding without a UI dependency. Add rotating
local diagnostic logging.

**Exit criteria:** core tests run without Windows UI APIs or real local logs.

### 2. Windows/WSL discovery and incremental scan foundation

Implement Windows profile discovery, WSL distribution/home discovery, explicit
additional scan roots, incremental scanning, cache persistence, and duplicate
root protection.

**Exit criteria:** test fixtures are found and scanned through both Windows and
UNC WSL paths; repeated refreshes do not double-count unchanged events.

### 3. Claude Code vertical slice

Implement a complete Claude provider: discover roots, parse logs, normalize
events, deduplicate, aggregate, cache, and expose a provider snapshot.

**Exit criteria:** verified token totals for Windows-only, WSL-only, and mixed
Windows+WSL fixture scenarios.

### 4. Codex vertical slice

Implement the same end-to-end path for Codex, including sessions and archived
data that exist in the observed Windows/WSL formats.

**Exit criteria:** verified token totals for Windows-only, WSL-only, mixed, and
duplicate-discovery scenarios.

### 5. Tray and compact dashboard

Build the tray icon, tooltip/context menu, compact dashboard, manual refresh,
and a safe way to open the local diagnostics folder. UI reads application
display state only.

**Exit criteria:** UI can present each provider and the combined total without
direct file parsing or platform-specific provider logic.

### 6. Game and persistence integration

Connect the validated aggregate to Pokemon growth, Pokedex, shop, settings,
sprite cache, and save import/export. Preserve and test the existing save
schema where feasible.

**Exit criteria:** progression and saves remain correct across restarts and
with Windows/WSL combined usage.

### 7. Floating pet and personal-use hardening

Add the floating pet after core use is reliable. Test multiple monitors, DPI,
sleep/wake, temporarily unavailable WSL distributions, active log writes, and
large logs.

**Exit criteria:** stable personal daily use with recoverable local diagnostic
evidence for failures.

### Personal packaging (added after M7)

Daily use no longer runs from the Debug build output. `scripts/publish-windows.ps1`
stops a running installed instance, publishes the UI project as a single-file
self-contained win-x64 exe (`src/Ui/Properties/PublishProfiles/win-x64.pubxml`),
verifies the staged exe version against the `<Version>` in the Ui csproj, installs
it to `%LOCALAPPDATA%\Programs\PokeTokenBar`, and refreshes a Start Menu shortcut.
Version lives in the Ui csproj and shows at the top of the tray context menu. App state, settings,
sprites, and diagnostics stay under `%LOCALAPPDATA%\PokeTokenBar` (`PTB_STATE_DIR`
to override), so installs never touch user data. Public distribution (MSIX,
winget, automatic updates) remains deferred.

### Later: OpenCode

Done in M9 (`OpenCodeUsageProvider` + `OpenCodeDbReader`). Storage research
established that OpenCode 1.x keeps everything in a WAL-mode SQLite
`opencode.db` (message rows carry tokens/cost in `data` JSON; the old
`storage/` JSON layout is legacy). Local disks are opened read-only directly;
UNC paths (WSL `\\wsl.localhost\...`) cannot host SQLite locks, so the reader
copies db+wal to a scratch folder under `%LOCALAPPDATA%\PokeTokenBar\opencode`
  gated by db+wal fingerprints. Sanitized fixtures live under
`docs/windows-port-research/fixtures/opencode/`. No generic totals, game
progression, or UI architecture changes were made.

### Later: combat-details enrichment

Done in M10. The bundled snapshot moved to schema 2: beside the existing
bases/lines/names it now carries per-species default-form combat data
(height, weight, base experience, gender rate, types, base stats, abilities,
and every move learnable in the black-2-white-2 version group) plus localized
type/ability/move display names for the app languages. `scripts/
generate-pokemon-snapshot.ps1` regenerates it from PokeAPI; the app stays
offline-first and never fetches pokemon base data at runtime. The engine fills
the deferred profile fields (gender/ability/moves) at hatch, evolution,
graduation, release, and startup migration, and the dashboard shows a combat
summary line for the active mon plus a per-species detail window (individuals
with computed stats and known moves, base stats, abilities, full move list)
opened by double-clicking a dex row.

## Closure (M11, 2026-09-28)

The user declared the project complete. The plan, every named nicety
(including M10 combat details), and personal verification are done; version
0.10.0 is the final shipped build. From here the project runs in maintenance
mode: no scheduled slices; defects are fixed as they appear during daily use.

Distribution stays manual: releases are uploaded by hand to GitHub Releases
(web UI) using a zip of the single-file exe produced by
`scripts/publish-windows.ps1` (e.g. `artifacts/PokeTokenBar-<version>-win-x64.zip`).
No release automation, installer, signing, or auto-update was added, per the
personal-use scope. The unused `OPENCODE_DATA_DIR` compatibility nicety was
checked and deliberately skipped: this machine does not set it anywhere
(Windows env and WSL shell rc files verified), so the fixed-path discovery
covers actual usage.

## Reopened: UI parity polish (M12+, 2026-09-28)

After closure the user reopened the project for UI polish toward the macOS
original. First slice (M12, version 0.11.0): dashboard/tray/pet/detail
localization via `src/Core/DashboardText.cs` (seven languages, values copied
from the macOS `Localization.swift` so both apps read identically; the engine
localizes events, notices, shop and bag labels from the saved language) and
sprites through a cached-first `SpriteSlot` helper reusing `SpriteStore`
(animated active mon + detail header, static dex row thumbnails, egg image).

Second slice (M13, version 0.12.0): the dex text list became a sprite tile
grid (WrapPanel tiles: number, 64px nearest-neighbor sprite, name; shiny star,
raising arrow, full-info tooltip; double-click still opens the detail window).
Dashboard grew to 580x720 with a wider dex column.

Third slice (M14, version 0.13.0): the Usage tab gained a month-to-date daily
trend chart (caption with hover readout and peak, accent bar for today, dimmed
zero days, weekend ticks, sparse date axis; pure geometry in
`src/Core/DailyTrendMetrics.cs`, gated on `peak > 0`) plus a combined-today
model breakdown under the chart (two-plus models only). The refresh service
now surfaces `MonthDaily` and `TodayModels` on `UsageDisplayState`.

Fourth slice (M15, version 0.14.0): a settings window (tray menu entry and a
dashboard footer button) holding the language picker (seven languages, live
re-localization of dashboard/tray/pet menus via `CompanionEngine.SetLanguage`,
persisted in the save), per-provider additional scan folders (`scanRoots` in
settings.json, applied live to `UsageRootOptions` and followed by a refresh),
difficulty sliders (percent readout) and the floating pet controls. The game
tab no longer carries sliders or pet controls.

Fifth slice (M16, version 0.15.0): per-form Unown sprites. The dex tile for
Unown shows the plain name plus a collected-form count ("Unown 2/28", tooltip
"Unown forms 2/28"); the detail window gained a 28-form picker grid (7
columns, static per-form thumbs with a shiny star, unowned forms dimmed with
"Not collected", click selects) that filters the individuals list and switches
the hero sprite to that form. Engine surfaces this via
`CompanionUnownFormStatus(Form, IsShiny)` lists on `CompanionGameView` and
`CompanionDetailSnapshot` plus `UnownForm` per detail individual, and the
active Unown dex row keeps a plain name.

Sixth slice (M17, version 0.16.0, user-verified): new-version notifications,
porting the macOS `UpdateChecker` — GitHub `releases/latest` check on startup
and dashboard open (30-minute in-memory debounce, manual check bypasses it),
tray balloon + dashboard banner with skip-this-version, a settings "Updates"
section with a manual check and an update-notifications toggle, and "open the
release page" as the apply action (no auto-download; https + github.com URL
validation before opening). Pure version compare in
`src/Core/VersionText.cs`, the checker service with injectable
fetch/clock/skip store in `src/Application/UpdateChecker.cs`, skip version +
notifications toggle in settings.json. Dormant but correct until releases
newer than the installed version are uploaded (releases so far use
`v<Version>` tags; only v0.10.0 exists today).

Seventh slice (M18, version 0.17.0): evolution-line visuals and shiny
sparkle parity in the game tab, porting the macOS `EvoLineView`. The engine
now exposes `EvoLine` (structured `EvoLineItem`s: species id or one mystery
cell for branch points, state Done/Current/Future — the Core record types
that had existed unused since the initial port) on `CompanionGameView` next
to the label-only `StageItems`. The dashboard's old text chain (`A → B → C`
in `StageLine`) became a horizontal row of 40px static sprite cells with
arrow separators, future stages at 0.32 opacity, an accent dot under the
current stage, a bold `?` cell (tooltip `unknownNextEvolution`) where a
line branches, whole-line shiny sprites when the individual is shiny, and
an overflow ScrollViewer. The progress caption shows `finalForm`
("최종 진화체"/"Final form") instead of "Stage i/k" at the last stage. Shiny
markers switched from a gray `★` suffix to macOS-style `✨` on the companion
name (tooltip `dexShinyLabel` — the previously dormant `ShinyLabel`, now
capitalized per the macOS table), dex tile stars, species-detail individual
headers, and Unown form thumbs. New macOS-verbatim strings: `FinalForm`,
`UnknownNextEvolution`; `ShinyLabel` values corrected to the macOS
capitalization.

Eighth slice (M19, version 0.18.0, user-verified): a general visual-quality
pass over the dashboard, settings and detail windows, moving from bare default
WPF controls to a Fluent/Windows-11 light theme. `src/Ui/Theme.xaml` (merged in
App.xaml AND per-window so headless tests resolve resources) holds the design
tokens (window/card/text/accent/rarity brushes, Segoe UI Variable) and implicit
styles for Button (+ keyed accent variant), ListBox/ListBoxItem, TabControl/
TabItem (pivot tabs with accent underline), ProgressBar, ToolTip, CheckBox,
ComboBox/ComboBoxItem, Slider and a flat thin ScrollBar, plus keyed Card/
SectionHeader/Caption styles. The dashboard game/usage sections, settings
sections and detail header/content are grouped into cards; the update banner
is accent-tinted. Code-built visuals (trend bars, evo cells, dex captions,
detail rows) read theme brushes through a `Token(key)` helper. During review
the user asked for a bigger dex and macOS-parity shop, so the slice also
grew: the window became 640x800 and resizable (min 560x640), the dex moved to
its own full-width tab (7 columns of 76px tiles) with the game tab keeping a
companion-events card, and the shop moved to its own tab modeled on the macOS
ShopView — wallet card with a big spendable number plus per-entry cards
(emoji icon, description, owned count, price, per-card buy with inline
confirm, egg cards with a send-off warning and a second shiny warning, tier
capsules in rarity colors). The game tab's old shop list became a "bag" card
with the candy/mint use buttons. New macOS-verbatim strings for the shop/bag
(passive-owned, egg confirm, shiny-discard warning, companion-events header).

Ninth slice (M20, version 0.19.0, user-verified): the Tier-1 bundle from
`docs/ui-parity-audit.md` (a living macOS-vs-Windows gap list introduced with
this slice). Companion card parity with the macOS CompanionHeader: rarity
capsule at the name (rarity brush, uppercase), growth-boost capsule, egg
"About to hatch!" wording at >=90% progress with an accent color, egg
guarantee capsule (new `EggGuarantee` field on `CompanionGameView` from the
stored `EggTier`), and macOS-style progress captions ("X to next evolution" /
"X to graduation" / "X to hatch" instead of raw used/threshold). The dex tab
gained rarity filter capsules (count per rarity, tap to toggle) and an
empty state (animated Pikachu + copy). The game tab's bag card became
per-item cards: candy with a +/- stepper, live plan preview via the
previously-unused `PlanRareCandyUse` ("expected to graduate" / carryover XP /
discarded XP) and an inline use confirm; mint with its effect hint and
confirm; passive shiny charm as a green "active" row; empty bag shows a
Snorlax. `RarityLabel` values corrected to macOS capitalization
("Rare"/"Legendary" — were lowercase). ~22 macOS-verbatim strings added.

Remaining UI-parity candidates live in `docs/ui-parity-audit.md` (Tier 1:
representative Pokémon — engine state exists unused; Tier 2: usage home
redesign with provider chips, celebration animation package, catch-log view,
companion status line, detail-window polish, incident banner; Tier 3:
constrained items incl. official limits, rejected by the no-credentials
rule). Confirm the next slice with the user first. Windows keeps the tray +
dashboard model — inline menu-bar text stays out of scope by design.
