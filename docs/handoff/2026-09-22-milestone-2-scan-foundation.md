# Handoff — Milestone 2: Windows/WSL log discovery & incremental scan foundation

## Goal
Implement Milestone 2 of the Windows port: discover Claude Code/Codex log roots across Windows-native and WSL (UNC), scan them incrementally with a persistent cache, and protect against duplicate roots — without double-counting unchanged events on repeated refreshes.

## Workspace
- Checkout: `C:\Users\USER\my-pjts\poketoken-bars-windows` (Windows 11 25H2 machine, target environment itself)
- Branch: `main` / Base: `main` (this repo pushes directly to `main`; no PR flow used so far)
- Commits so far: `bb25608` docs: add Windows port plan · `f763205` docs: add Windows port research and sanitized fixtures (milestone 0) · `8083930` feat: port pure core and diagnostics to .NET (milestone 1)
- PR: none / title convention: English conventional commits (`feat:`, `fix:`, `docs:`)

## Current State
- Done — Milestone 0: roots verified + sanitized fixtures + lock research in `docs/windows-port-research/`
- Done — Milestone 1: pure C# core in `src/Core` (16 files), 104 xUnit tests green under `Tests/Core.Tests`
- Not implemented: `src/Platform.Windows` project, any discovery/scan/cache code, any provider parsing (M3/M4)
- Solution: `PokeTokenBar.Windows.slnx` (note: `.slnx`, not `.sln` — dotnet SDK 10 default)

## Locked Decisions
- Stack C# / .NET, target `net10.0` — user chose ".NET 8+"; net10.0 is current LTS and satisfies it. Do not retarget without asking.
- Layering per plan: `Core` pure (no Windows UI APIs, never reads real local logs), `Providers` parse logs, `Platform.Windows` owns paths/WSL/settings/diagnostics, `Application` schedules/aggregate/cache, `UI` consumes results only.
- Polling + incremental scan + persistent cache — NOT file watchers. WSL boundary makes Windows change notifications unreliable (verified in M0 research).
- No provider-specific branches in shared aggregation (`if provider == "claude"` forbidden). OpenCode provider explicitly deferred.
- Never read/copy: `~/.claude/.credentials.json`, `auth.json` in `.codex` roots, settings secrets.
- Git: remote `origin` = `git@github-personal:Strongorange/PokeTokenBar-windows.git` (SSH host alias; default agent key authenticates the wrong GitHub account). Repo-local identity `Strongorange <Strongorange@users.noreply.github.com>` already configured.
- Commit + push only when the user asks (user drives per-milestone commits in Korean, e.g. "commit and push").

## Contracts
- Scan target roots (verified in M0):
  - Windows Claude: `%USERPROFILE%\.claude\projects\<munged-cwd>\*.jsonl`
  - WSL Claude: `\\wsl.localhost\Ubuntu-24.04\home\<user>\.claude\projects\...`
  - Windows Codex: `%USERPROFILE%\.codex\sessions\YYYY\MM\DD\rollout-*.jsonl` (`archived_sessions` absent in this env but must be scanned if present)
  - WSL Codex: `\\wsl.localhost\Ubuntu-24.04\home\<user>\.codex\sessions\...`
- Normalized record: `UsageEntry` (`src/Core/UsageAggregation.cs:5`) — `Id, Date, LocalDay, Model, Input, Output, CacheWrite, CacheRead, ExplicitCost?, CostIsEstimate?, CostUnavailable?` with `Total` computed. M2 produces file candidates + cache identity; M3/M4 produce `UsageEntry`s.
- Cache identity: per-file blob keyed by absolute path, invalidated by `(mtime, size)`, with a parser version field (model: `Sources/PokeTokenBar/Core/LocalUsageCache.swift`).
- Scan window floor: `UsageAggregation.EnrichmentScanStart` (`src/Core/UsageAggregation.cs:194`) = min(month start, week start, now−5h) — reuse, do not reinvent.
- Dedup contract: `DedupKeepMax` keeps max-`Total` entry per id (`src/Core/UsageAggregation.cs:67`); duplicate roots must collapse BEFORE parsing cost is paid when roots normalize equal.
- Log format references: 4 sanitized fixtures in `docs/windows-port-research/fixtures/` (incl. quirks: Claude `requestId` may be absent; Codex timestamps have variable fractional digits; old Codex `token_count` can carry `info: null`).

## Relevant Files
- `docs/windows-port-plan.md` — milestone definitions; M2 exit criteria at "### 2."
- `docs/windows-port-research/README.md` — verified roots, record shapes, lock-test evidence
- `src/Core/UsageAggregation.cs:5-67` — `UsageEntry`, `UsageBucket`, dedup, date keys the scan layer must feed
- `src/Core/AppLog.cs` — diagnostics sink for scan failures (root missing, read failure, parse failure)
- `Sources/PokeTokenBar/Core/LocalUsageReader.swift:66-123` — macOS root enumeration model (what to port conceptually, not literally)
- `Sources/PokeTokenBar/Core/LocalUsageCache.swift` — cache persistence model (path-keyed, mtime+size, versioned)
- `AGENTS.md` — build/test commands and repo rules for agents

## Hard-won Context
- Run/test: `dotnet test PokeTokenBar.Windows.slnx` (≈0.1s, 104 tests) — wrong-file error `MSB1009` if you type `.sln`.
- WSL distro enumeration: do NOT parse `wsl.exe -l` output (UTF-16 + locale mojibake). Use registry `HKCU\Software\Microsoft\Windows\CurrentVersion\Lxss\{guid}\DistributionName`, then map to `\\wsl.localhost\<name>\home\<user>`. Exclude `docker-desktop` (not a user distro).
- Trap: UNC read of WSL file fails with `IOException` only for Windows-side exclusive opens; WSL `flock` never blocks UNC reads (M0 test: 38/38 reads OK during active append + flock). Still open files with `FileShare.Read` and tolerate a partial final line.
- Trap: UNC path I/O is slower than local — enumerate directories lazily, cache directory listings across refreshes when mtimes unchanged.
- Normal and unrelated: git CRLF warnings on commit; `docker-desktop` in `wsl -l -v` output.
- SSH trap (if push denied): agent can hold a stale copy of `strong_orange_github`; symptom `agent refused operation` → fix `ssh-add -d <key>; ssh-add <key>`; verify with `ssh -T git@github-personal` → must greet `Strongorange`.
- Already tried and rejected: file watchers (WSL boundary), macOS `~/Library` path assumptions (do not assume; discover).
- Cleanup owed (optional): temp scripts `C:\Users\USER\AppData\Local\Temp\opencode\{locktest,locktest2,make-fixtures}.ps1`, WSL `/home/ubuntu/ptb-locktest*.sh` already removed.

## Working Agreement
- Slice per milestone; report after each milestone or when blocked. M2 = discovery + scan + cache + duplicate-root protection ONLY (no line parsing — that is M3 Claude slice, M4 Codex slice).
- Evidence: new xUnit tests (temp paths + the 4 committed fixtures) + `dotnet test` fully green + a manual root-discovery check on this machine (it has all 4 real roots).
- Core project stays UI-free and never touches real logs; `Platform.Windows` may read real roots in production code paths but tests must inject temp/fake roots.
- Comments: none unless asked (repo rule). English commits. Do not update git config, force-push, or create PRs unless asked.
- Tools & skills: `openviking` MCP — search/read memories for session continuity (events under `viking://user/owner/memories/events/2026/09/22/`); `agent-handoff` skill (WSL `~/.agents/skills/agent-handoff`) when writing the next handoff.

## Open Risks
- `\\wsl.localhost` is not available in CI/other machines — tests must inject roots; real-UNC validation is a manual step on this machine only.
- Lxss registry may list distros whose VM is stopped → UNC probe must time out gracefully and log to diagnostics, not fail the refresh.
- net10.0 vs net8.0 policy formally undecided (recommendation: keep net10.0).
- Same physical log reachable via two custom roots is deduped by root normalization; the "same event from two DIFFERENT roots" case is only fully solved at entry level in M3/M4 (`DedupKeepMax` + Codex canonical ids).

## Acceptance Criteria
- [ ] `src/Platform.Windows` (or equivalent) project in solution, referencing Core only
- [ ] Discovery returns the 4 real roots on this machine + honors user-configured additional roots, excluding non-user distros
- [ ] Duplicate roots (same path via different casing/slash/custom+auto) are collapsed before scanning
- [ ] Incremental scan skips unchanged files (mtime+size), re-reads changed ones, tolerates partial last line and locked files (logged via `AppLog`, not thrown)
- [ ] Persistent cache survives process restart (temp-dir test) with version invalidation
- [ ] Repeated refresh with no file changes produces identical aggregate input (no double counting) — test proves it
- [ ] `dotnet test PokeTokenBar.Windows.slnx` green including new tests

## Verification
- `dotnet test PokeTokenBar.Windows.slnx` — all tests pass (existing 104 + new M2 tests)
- Manual on this machine: run the discovery entry point → expect exactly: Windows `.claude\projects`, WSL `\\wsl.localhost\Ubuntu-24.04\home\ubuntu\.claude\projects`, Windows `.codex\sessions`, WSL `...\.codex\sessions`; `docker-desktop` not present
- Repeat-scan test: scan twice over fixture tree → second pass parses 0 files (cache hits), aggregate unchanged

## Related Docs
- `docs/windows-port-plan.md` — canonical plan (milestones 0–7 + Later)
- `docs/windows-port-research/README.md` — verified environment facts and evidence
- OpenViking memory: `viking://user/owner/memories/events/2026/09/22/` — session continuity (M0/M1 completion entries)

## Start Prompt
```text
Read docs/handoff/2026-09-22-milestone-2-scan-foundation.md end to end.
Work in C:\Users\USER\my-pjts\poketoken-bars-windows, branch main, base main.
Implement Milestone 2: src/Platform.Windows discovery (registry-based WSL distro → UNC roots, Windows profile roots, user extra roots, duplicate-root normalization), incremental mtime+size scan with FileShare reads and partial-line tolerance, persistent versioned cache, and xUnit tests using temp dirs + docs/windows-port-research/fixtures. Do not implement log line parsing (M3/M4). Verify with dotnet test PokeTokenBar.Windows.slnx and the manual discovery check listed in the handoff.
```
