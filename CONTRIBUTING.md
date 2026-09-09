# Contributing

Use feature/*, fix/* or research/* branches. Open a focused issue for nontrivial work, implement the smallest change, run scripts/Test-Workbench.ps1, and submit a PR with acceptance evidence and known limits. Keep main recoverable. Never force push or automatically merge.

Use conventional commit prefixes (feat, fix, test, docs, chore, refactor). Add an ADR only for a real architecture boundary decision. Preserve legacy v0.2 behavior/tests when adding the Fusion path.

Runtime data, local settings, credentials, artifacts and target-project source must stay outside Git. Review the staged diff and file list before every push. Read SECURITY.md.

No license has been selected. Do not add one without the owner's explicit choice.
