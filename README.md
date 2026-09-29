<div align="center">

<img src="assets/icon.png" width="128" alt="PokeTokenBar icon">

# PokeTokenBar

**Your AI coding tokens, hatched into Pokémon — right in your system tray.**

[![Release](https://img.shields.io/github/v/release/Strongorange/PokeTokenBar-windows?color=444d56&label=release)](https://github.com/Strongorange/PokeTokenBar-windows/releases)
[![Windows](https://img.shields.io/badge/Windows-10%2F11-0969da)](https://www.microsoft.com/windows/)
[![.NET](https://img.shields.io/badge/.NET-10-512bd4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-3fb950)](LICENSE)

**English** · [한국어](README.ko.md)

</div>

A personal Windows port of [chattymin/PokeTokenBar](https://github.com/chattymin/PokeTokenBar), the macOS menu-bar original. PokeTokenBar turns the AI coding tokens you're already burning — Claude Code, Codex, and OpenCode, on both Windows and WSL — into a growing **Pokémon companion**. Spend tokens, hatch an egg, evolve it through its real evolution line, graduate it into your Pokédex, and start again. Underneath the companion it's a precise usage tracker: today's and this month's tokens and cost, read straight from your local logs, with a month-to-date trend chart and a per-model breakdown.

> Token usage is read directly from local Claude Code, Codex, and OpenCode data — Windows-native and every WSL distro, deduplicated into one total (`totalTokens` = input + output + cache, local date) — no external CLI needed. Unofficial, non-commercial Pokémon fan project — see [License & disclaimer](#license--disclaimer).

## How it works

1. 🥚 **Code as usual.** The tokens you burn in Claude Code, Codex, or OpenCode incubate an egg — nothing extra to run.
2. 🐣 **Hatch.** Eggs hatch into Pokémon with real evolution lines from the bundled Gen 1–5 dataset, weighted by the official capture rate: commons hatch often, a legendary is a rare event. Every hatch rolls one of 25 natures — and once in a rare while, the egg hatches **✨ Shiny**.
3. ⚡ **Evolve.** Keep coding and it grows through its actual evolution tree (1/2/3 stages, branching), with a little flash celebration at each step.
4. 🎓 **Graduate & collect.** Final form + threshold permanently archives it in your **Pokédex** — rarer lines take longer — and a fresh egg arrives.
5. 🛒 **Spend at the Shop.** Every token you've used is spendable currency — buy a **Rare Candy** to grow your current Pokémon, a **Mint** that re-rolls its nature, a **Shiny Charm** that permanently raises your shiny odds, or an egg to send off your current companion and start over. Eggs come in three grades: a plain **Pokémon Egg**, an **Uncommon Egg** guaranteed to hatch Uncommon or better, and a **Rare Egg** guaranteed to hatch Rare or better.
6. 🔤 **Unown, all 28 forms.** Each Unown form you hatch fills its own slot in the species detail view, with a per-form sprite picker and collected-count on the dex tile.

## Tour

- **Tray companion.** An animated Gen-V sprite lives in the notification area; the tooltip shows today's and the month's totals; the right-click menu has refresh, dashboard, settings, floating pet, the diagnostics folder, and exit. A new GitHub release triggers a tray balloon and a dashboard banner with skip-this-version — "update" opens the release page in your browser (no auto-download).
- **Dashboard · Usage tab.** A month-to-date daily trend chart (hover a bar for that day's tokens and cost; the peak is marked and today is accented), a per-model breakdown of today's combined tokens when two or more models were used, per-provider rows (available / today / month, tokens and cost), and a combined footer.
- **Dashboard · Game tab.** A companion card with an animated sprite and a combat summary line, the shop and bag, and the **Pokédex** as a sprite tile grid (shiny ✨, raising arrow, full-info tooltips). Double-clicking a species opens a detail window: every individual you've owned with level, nature, ability, IVs, calculated stats, and known moves, plus base stats and the full move list — all localized.
- **Settings window.** UI language (**KO / EN / JA / ES / FR / PT / DE**, re-localizes everything live), per-provider additional scan folders, growth and shop difficulty (10–200%), floating-pet controls, and the updates section (notifications toggle, manual check).
- **Floating desktop pet.** Move your companion onto the desktop at any size from 48 to 384 px. Drag it wherever you like, double-click to open the dashboard, right-click to hide it again.
- **Save import / export.** One file carries the companion, the Pokédex, the bag, and your language choice — move a game between machines from the dashboard footer.

## Works with

| Tool | Tracked | Built-in log roots |
|---|---|---|
| **Claude Code** | today · month | `%USERPROFILE%\.claude\projects` + WSL `~/.claude/projects` |
| **Codex** | today · month | `%USERPROFILE%\.codex\sessions` (+ `archived_sessions`) + WSL `~/.codex/sessions` |
| **OpenCode** | today · month | `%USERPROFILE%\.local\share\opencode` + WSL `~/.local/share/opencode` |

Windows-native and WSL events are deduplicated and combined — Windows and WSL usage is one game, not two. Official account limits are **not** read; the app never touches credentials. If a tool keeps its logs somewhere unusual, add per-provider additional scan folders in **Settings** — each folder is parsed only by the provider you picked, and extras are added to the built-in roots, never replacing them.

## Install

Windows 10/11 — that's it. The exe is self-contained (the .NET 10 runtime travels inside).

1. Download `PokeTokenBar-<version>-win-x64.zip` from the [latest release](https://github.com/Strongorange/PokeTokenBar-windows/releases/latest) and unzip it anywhere.
2. Run `PokeTokenBar.exe`; it lives in the notification area.

The binary is unsigned, so SmartScreen may show "Windows protected your PC" on first run — choose **More info → Run anyway**.

App state, settings, the sprite cache, and diagnostics live under `%LOCALAPPDATA%\PokeTokenBar` (set a `PTB_STATE_DIR` environment variable to override). Deleting that folder resets the app; unzipping or deleting the exe never touches your save.

## Build from source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```powershell
dotnet test PokeTokenBar.Windows.slnx    # all unit + integration tests
dotnet run --project src/Ui              # debug run
```

`scripts/publish-windows.ps1` builds the single-file self-contained exe for personal installs. Architecture, layering, and the full milestone history are in [docs/windows-port-plan.md](docs/windows-port-plan.md).

## Data sources

| Source | Used for | Notes |
|---|---|---|
| `%USERPROFILE%\.claude\projects\**\*.jsonl` + WSL | Claude Code today/month | read directly; deduped by message id; incrementally cached |
| `%USERPROFILE%\.codex\sessions\**\*.jsonl` (+ `archived_sessions`) + WSL | Codex today/month | `token_count` events |
| `%USERPROFILE%\.local\share\opencode\opencode.db` + WSL | OpenCode today/month | SQLite, opened read-only; WSL databases are fingerprint-copied to a local scratch folder because UNC paths cannot host SQLite locks |
| `raw.githubusercontent.com/PokeAPI/sprites` | Pokémon & item sprites | runtime fetch; disk-cached under the app's data folder |
| `api.github.com` | update check | latest release tag; on launch and dashboard open, debounced to once per 30 minutes |

Pokémon base data — species, evolution lines, localized names, stats, abilities, moves — is **bundled** with the app as a snapshot generated from [PokéAPI](https://pokeapi.co/). The game works fully offline; only sprites are fetched at runtime.

## Privacy

- **Local-first.** Usage is read from local log files. Nothing is uploaded, no model turns are run, and no credentials or session keys are ever read — the app has no credential-store integration at all.
- **Outbound requests** go to two hosts: `raw.githubusercontent.com` (sprites) and `api.github.com` (update check). Neither carries your usage logs, prompts, or project paths.
- **Diagnostics** are rotating local text logs under the app's data folder (tray menu → open diagnostics folder). They record parse and refresh failures without credentials or full log entries.

## Differences from the macOS original

This is a personal port, not a line-for-line clone:

- **Tray + dashboard** instead of menu-bar text — Windows notification-area icons can't host macOS-style `NSStatusItem` text.
- **Three providers** (Claude Code, Codex, OpenCode) instead of thirteen.
- **No official-limit tracking** — no Keychain-equivalent access, no 5-hour / weekly limit rows or alerts.
- **Bundled Pokémon base data** — the macOS app fetches species data from PokéAPI at runtime; this port ships an offline snapshot instead.
- **No auto-update** — the new-version banner opens the release page in your browser.

## License & disclaimer

**MIT** — see [LICENSE](LICENSE). The MIT license covers this project's original source code only; it grants no rights to any third-party trademarks, artwork, or data accessed through the app.

PokeTokenBar is an **unofficial, non-commercial fan project**. It is **not affiliated with, endorsed, sponsored, or approved by Nintendo, Game Freak, Creatures Inc., or The Pokémon Company.** "Pokémon" and all related names, characters, and imagery are trademarks and copyrights of their respective owners.

- The app bundles a machine-generated data snapshot (names, evolution lines, stats, moves) derived from [PokéAPI](https://pokeapi.co/); sprites are fetched at runtime from PokéAPI's sprite repository and cached locally on the user's own device. Names, data, and sprite images remain the property of their respective owners.
- The app is provided free of charge for **personal, non-commercial use only.**
- If you are a rights holder with any concern about this project, please open an issue and it will be addressed promptly.

*Provided "as is", without warranty of any kind. This notice is not legal advice.*

## Acknowledgments

- [chattymin/PokeTokenBar](https://github.com/chattymin/PokeTokenBar) — the macOS original this port builds on.
- [PokéAPI](https://pokeapi.co/) — Pokémon data and sprites.
