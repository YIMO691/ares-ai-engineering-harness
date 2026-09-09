# Contributing

Use feature/*, fix/* or docs/* branches. Open a focused issue for nontrivial work, implement the smallest change, run scripts/Test-Unified.ps1, and submit a PR with acceptance evidence and known limits. Keep main recoverable. Never force push. Merge only after explicit Owner authorization and successful checks; do not enable unattended auto-merge.

Follow [GitHub workflow](docs/GITHUB_WORKFLOW.md): Issue → discussion documents → Ready → implementation → checks/review → Align → authorized PR merge. The Owner talks directly with Codex; Codex maintains GitHub records. Small documentation corrections may use a focused PR without a separate issue.

L1/L2/L3 selects document depth; FAST/STANDARD/CRITICAL selects verification strength. For STANDARD/CRITICAL, record the actual independent Reviewer outcome, its reviewed revision, and changes made afterward. CI success does not substitute for review or Owner acceptance.

Use conventional commit prefixes (feat, fix, test, docs, chore, refactor). Add an ADR only for a real architecture boundary decision. Preserve legacy v0.2 behavior/tests when adding the Fusion path.

Runtime data, local settings, credentials, artifacts and target-project source must stay outside Git. Review the staged diff and file list before every push. Read SECURITY.md.

No license has been selected. Do not add one without the owner's explicit choice.
