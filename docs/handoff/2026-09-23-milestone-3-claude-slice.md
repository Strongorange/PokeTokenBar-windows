# Handoff — Milestone 3: Claude Code vertical slice

## Goal
Implement Milestone 3 of the Windows port: parse Claude Code `.jsonl` logs into
`UsageEntry`s through the M2 scan foundation, deduplicate by
`(message.id, requestId)`, aggregate, cache parsed entries per file, and expose a
provider snapshot — with verified token totals for Windows-only, WSL-only, and
mixed Windows+WSL fixture scenarios.

## Workspace
- Checkout: `C:\Users\USER\my-pjts\poketoken-bars-windows` (Windows 11 25H2 machine, target environment itself)
- Branch: `main` / Base: `main` (this repo pushes directly to `main`; no PR flow used so far)
- Commits so far: `bb25608` docs: port plan · `f763205` docs: research + fixtures (M0) · `8083930` feat: pure core (M1) · `55b7701` feat: discovery + scan foundation (M2) · plus a docs commit adding the handoff notes themselves
- PR: none / title convention: English conventional commits (`feat:`, `fix:`, `docs:`)

## Current State
- Done — M2: `src/Platform.Windows` (discovery, incremental scanner, persistent cache) + `Tests/Platform.Windows.Tests`; 142 tests green (`104 Core + 38 Platform.Windows`)
- Done — manual verification on this machine: discovery returns exactly the 4 real roots (WSL UNC included, `docker-desktop` excluded); fixture scan via local and UNC paths gives identical results; second pass parses 0 files
- Not implemented: any log line parsing, `src/Providers` project, provider snapshot contract (M3); Codex (M4); Application/UI layers
- Solution: `PokeTokenBar.Windows.slnx` (`.slnx`, not `.sln` — dotnet SDK 10)

## Locked Decisions
- Stack C# / .NET, target `net10.0`. Do not retarget without asking.
- Layering per plan: `Core` pure, `Providers` parse logs (references Core only, no Windows APIs), `Platform.Windows` owns paths/WSL/settings/diagnostics, `Application` schedules/aggregate/cache, `UI` consumes results only.
- Provider parsing stays pure: `src/Providers` must not reference `Platform.Windows`. The glue (discovery → scanner → provider) is exercised in tests or later in `Application`; do not invert the layering to make providers call the scanner.
- Claude dedup id: `(message.id ?? "") + "|" + (requestId ?? "")` — a missing `requestId` is the empty string (Windows records may omit it entirely; M0 research).
- Line pre-filter before JSON parsing: line must contain `"usage"` and `"assistant"` (652+ files on the WSL root; the macOS parser does exactly this).
- Dedup semantics: per-file `DedupKeepMax`, then global `DedupKeepMax` after concatenating roots. Keep max-`Total` per id: streaming/restart duplicates of the same `message.id` grow `output`, so first-occurrence would undercount.
- `message.model` default `"unknown"` when absent. Unknown `usage` fields (`server_tool_use`, `cache_creation`, `iterations`, `service_tier`, …) must be ignored, not rejected.
- No provider-specific branches in shared aggregation (`if provider == "claude"` forbidden). OpenCode deferred.
- Never read/copy: `~/.claude/.credentials.json` (WSL), `%USERPROFILE%\.claude\settings.json` secrets, `auth.json` in `.codex` roots.
- Git: remote `origin` = `git@github-personal:Strongorange/PokeTokenBar-windows.git` (SSH host alias). Repo-local identity already configured.
- Commit + push only when the user asks (user drives per-milestone commits, e.g. "commit and push").

## Contracts
- Normalized record: `UsageEntry` (`src/Core/UsageAggregation.cs:5`) —
  `Id, Date, LocalDay, Model, Input, Output, CacheWrite, CacheRead, ExplicitCost?, CostIsEstimate?, CostUnavailable?` with `Total` computed.
  Claude mapping: `Id` = the dedup id above, `Date` = envelope `timestamp`, `LocalDay` = local-day string of `Date`, four token fields from `message.usage.{input_tokens, output_tokens, cache_creation_input_tokens, cache_read_input_tokens}`.
- Claude record shape (envelope): `type:"assistant"`, `message.usage` (4 token fields), `message.model`, `message.id`, top-level `requestId` (may be absent), `timestamp` with variable fractional digits — parse via `IsoDates.Date` (`src/Core/IsoDates.cs`), which already tolerates 1–3+ digit fractions.
- WSL Claude extras (`effort`, `origin`, stream-restart duplicates of the same `message.id`) — same dedup path, nothing special.
- M2 foundation API map (all in `src/Platform.Windows`):
  - `UsageRootDiscovery.Discover(UsageRootOptions?)` → `IReadOnlyList<UsageRoot>`; Claude roots are `UsageRootKind.ClaudeProjects`
  - `IncrementalLogScanner<TPayload>.Scan(roots, modifiedSince, parse)` → `IReadOnlyList<ScannedFile<TPayload>>` (`Parsed`/`Cached`/`ReadFailed`); call with `UsageScanCache<List<UsageEntry>>(cachePath, parserVersion)` — cache identity is (path, mtime ticks, size), parser-version invalidation built in
  - Scan floor: `UsageAggregation.EnrichmentScanStart(now)` — reuse, do not reinvent
  - Final aggregation input: `UsageAggregation.DedupKeepMax(files.SelectMany(f => f.Payload ?? []))`
- Expected M3 wiring (in tests until `Application` exists):
  ```csharp
  var cache = new UsageScanCache<List<UsageEntry>>(cachePath, parserVersion: 1);
  var scanner = new IncrementalLogScanner<List<UsageEntry>>(PhysicalFileSystemSource.Instance, cache);
  var files = scanner.Scan(claudeRoots, UsageAggregation.EnrichmentScanStart(now), ClaudeLogParser.Parse);
  var entries = UsageAggregation.DedupKeepMax(files.SelectMany(f => f.Payload ?? []));
  ```
- Fixtures: `docs/windows-port-research/fixtures/windows-claude.jsonl` (Claude 2.1.179, GLM proxy records, `requestId` absent) and `wsl-claude.jsonl` (2.1.252, `claude-opus-5` with cache fields, stream-restart duplicates). They are copied to test output as `fixtures/` by `Tests/Platform.Windows.Tests` — replicate that csproj snippet in the Providers test project.

## Relevant Files
- `docs/windows-port-plan.md` — milestone definitions; M3 exit criteria at "### 3."
- `docs/windows-port-research/README.md` — verified roots, Claude record quirks, lock evidence
- `src/Core/UsageAggregation.cs:5-82` — `UsageEntry`, `DedupKeepMax`, date keys
- `src/Core/IsoDates.cs` — tolerant ISO-8601 parsing (variable fractional digits)
- `src/Platform.Windows/IncrementalLogScanner.cs` — scan/cache/status semantics
- `src/Platform.Windows/UsageScanCache.cs` — persistence, version invalidation, 40-day prune
- `src/Platform.Windows/UsageRootDiscovery.cs`, `UsageRoots.cs` — root discovery + options
- `Tests/Platform.Windows.Tests/` — test conventions: temp paths, `[Collection("Platform diagnostics")]` for AppLog users, fixtures-as-content, fakes in `Fakes.cs`
- `Sources/PokeTokenBar/Core/LocalUsageReader.swift:355-418` — reference parser semantics (port conceptually)
- `Sources/PokeTokenBar/Core/LocalUsageCache.swift:159-174` — macOS claude blob model (`entries` cached per file)
- `AGENTS.md` — build/test commands and repo rules for agents

## Hard-won Context
- Run/test: `dotnet test PokeTokenBar.Windows.slnx` (~0.5s, 142 tests) — wrong-file error `MSB1009` if you type `.sln`.
- .NET 10 `Path.GetFullPath` no longer rejects `|`, `<`, NUL, etc. `PathNormalizer.Normalize` returns such strings; nonexistent paths are dropped later by existence checks. Do not rely on Normalize for input validation.
- `TPayload?` on an unconstrained generic is NOT `Nullable<T>`: with `T = int`, `payload ?? 0` does not compile; with `T = List<UsageEntry>`, `Payload` can be null (`ReadFailed` without a stale blob) — always `?? []` / null-check payloads.
- `AppLog` creates no file until the first write — an absent diagnostics log means a clean run.
- `AppLog` is process-global: every test class that touches it must share one xUnit collection (`[Collection("Platform diagnostics")]`, definition in `Tests/Platform.Windows.Tests/Diagnostics.cs`). Collection attribute syntax is positional: `[CollectionDefinition("name")]`.
- Scanner failure semantics: read/parse failure logs via `AppLog`, returns the previous blob payload as `ReadFailed`, and does NOT update the cached signature — the next refresh retries the file automatically. Match provider tests to this.
- xUnit `Assert.Single` with a predicate returns the single match and throws on 0 or 2+ matches — write predicates that are exclusive.
- WSL distro probe = `Task.Run` + `Wait(timeout)`; on timeout the abandoned task keeps sleeping on a pool thread (bounded by distro count, acceptable).
- Locked-file behavior: Windows-side `FileShare.None` holders make scanner reads throw `IOException` (logged, stale blob served); WSL `flock` never blocks UNC reads (M0 evidence). Partial final lines are dropped and logged by the scanner — parsers only ever see complete lines.
- Normal and unrelated: git CRLF warnings on commit; `docker-desktop` in `wsl -l -v`.
- SSH trap (if push denied): agent can hold a stale copy of `strong_oange_github`; symptom `agent refused operation` → fix `ssh-add -d <key>; ssh-add <key>`; verify with `ssh -T git@github-personal` → must greet `Strongorange`.
- Already tried and rejected: file watchers (WSL boundary), macOS `~/Library` path assumptions, parsing `wsl.exe -l` output.

## Working Agreement
- Slice per milestone; report after each milestone or when blocked. M3 = Claude provider ONLY (Codex is M4; no UI, no Application scheduling).
- Recommended structure: new `src/Providers` (references Core only) containing `ClaudeLogParser` (pure: `string` lines → `List<UsageEntry>`) and a provider snapshot contract (e.g. `IUsageProvider` returning provider id + entries); `Tests/Providers.Tests` with fixture tests. Integration through the M2 scanner stays in tests. Adjust only if a strictly cleaner split appears — keep Providers free of `Platform.Windows` references either way.
- Evidence: new xUnit tests using the two Claude fixtures + synthetic temp trees (Windows-only / WSL-only / mixed roots via temp dirs), hand-computed expected totals, cache round-trip test (`List<UsageEntry>` payloads survive save/load), repeat-scan no-double-count with real parser payloads.
- Core and Providers stay UI-free and never touch real logs in tests; `Platform.Windows` may read real roots in production code paths but tests inject temp/fake roots.
- Comments: none unless asked (repo rule). English commits. Do not update git config, force-push, or create PRs unless asked.
- Tools & skills: `openviking` MCP — read/write session-continuity events (see `viking://user/owner/memories/events/2026/09/`); use the `agent-handoff` skill pattern (or model on this file) when writing the next handoff.

## Open Risks
- Cold-parse cost of the real WSL root (652 files, actively written): line pre-filter + M2 cache make warm refreshes cheap; do not add parallelism unless measurements demand it.
- Same event via two different roots: solved globally by `DedupKeepMax` as long as ids are identical across roots (true for Claude copies).
- `requestId`-absent Windows records: ids collapse to `msg_id|` — intentional; do not synthesize a requestId.
- Timestamps in fixtures are sanitized-but-real; expected totals must be computed from the fixture files themselves, not from memory of the original sessions.

## Acceptance Criteria
- [ ] `src/Providers` project in solution, referencing Core only, with pure Claude parser + provider snapshot contract
- [ ] Parser handles both Claude fixtures: `requestId` absent (windows) and present (wsl), cache token fields, unknown `usage`/envelope fields ignored, non-assistant/non-usage lines skipped
- [ ] Per-file + global `DedupKeepMax` proven: stream-restart duplicate of the same `message.id` counted once, keeping max total
- [ ] Windows-only, WSL-only, and mixed Windows+WSL fixture scenarios produce hand-verified token totals
- [ ] Cache round-trip: `List<UsageEntry>` payloads survive `UsageScanCache` save/load; parser-version bump forces re-parse
- [ ] No provider-specific branches in Core or shared aggregation
- [ ] `dotnet test PokeTokenBar.Windows.slnx` fully green including new tests

## Verification
- `dotnet test PokeTokenBar.Windows.slnx` — all tests pass (existing 142 + new M3 tests)
- Manual on this machine (optional but cheap): wire discovery → scanner → parser over the two real Claude roots read-only and sanity-check that today's totals are non-zero and deduped (real roots: `C:\Users\USER\.claude\projects`, `\\wsl.localhost\Ubuntu-24.04\home\ubuntu\.claude\projects`)

## Related Docs
- `docs/windows-port-plan.md` — canonical plan (milestones 0–7 + Later)
- `docs/windows-port-research/README.md` — verified environment facts and evidence
- `docs/handoff/2026-09-22-milestone-2-scan-foundation.md` — previous handoff (M2)
- OpenViking memory: `viking://user/owner/memories/events/2026/09/` — session continuity entries (M0–M3)

## Start Prompt
```text
Read docs/handoff/2026-09-23-milestone-3-claude-slice.md end to end.
Work in C:\Users\USER\my-pjts\poketoken-bars-windows, branch main, base main.
Implement Milestone 3: a pure src/Providers project (Claude parser + provider snapshot contract, referencing Core only) with fixture-based xUnit tests covering requestId-absent windows records, wsl stream-restart dedup, unknown-field tolerance, and Windows-only/WSL-only/mixed token totals; prove cache round-trip and no-double-count by wiring the parser through the M2 IncrementalLogScanner in tests. Do not implement Codex (M4) or any UI. Verify with dotnet test PokeTokenBar.Windows.slnx.
```
