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

OpenCode is intentionally deferred. The provider architecture must allow it to
be added later without adding provider-specific branches to common aggregation,
game progression, or UI code.

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

Add an `OpenCodeUsageProvider` only after its Windows/WSL storage locations and
sanitized fixtures are established. It must use the existing provider contract
and must not change generic totals, game progression, or UI architecture.
