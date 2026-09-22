# Repository instructions

Read `CLAUDE.md` for the repository's contribution, testing, and release instructions.

## Windows port (.NET)

The Windows port lives in `PokeTokenBar.Windows.slnx` (`src/Core`, `Tests/Core.Tests`; see `docs/windows-port-plan.md` and `docs/windows-port-research/`). Build and test it with:

```
dotnet test PokeTokenBar.Windows.slnx
```

Core code must stay free of Windows UI APIs and must not read real local logs; tests use temp paths and fixtures only.

## Pull requests

Before creating or updating a PR, read `.github/PULL_REQUEST_TEMPLATE.md` and use its sections and checklist in the description. Fill in the type of change and the actual validation results. For UI changes, include the before/after comparison; remove the UI section only when there are no UI changes. Mark checklist items complete only when supported by the work performed. Keep the PR title and description in English.
