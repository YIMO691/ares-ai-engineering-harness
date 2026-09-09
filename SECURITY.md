# Security

This local tool starts a real coding agent and configured build/test commands, which can modify the selected workspace. Use a review copy/worktree and a recoverable Git baseline. Do not grant untrusted repositories elevated authority.

Bind only to loopback. The application is single-user; it is not an authenticated multi-tenant network service. Antiforgery protection and local Host/remote address checks remain enabled.

Codex owns its sandbox and native session files. Reviewer uses read-only execution. Business allowed_paths and post-run checks supplement the native repository sandbox; they are not per-file ACLs.

Do not commit auth.json, CODEX_HOME, tokens, keys, local settings, SQLite, artifacts, evidence, browser profiles, commercial project sources or private company documents. Store runtime output outside the source tree. Report a suspected vulnerability privately to the repository maintainer with a minimal sanitized reproduction; never paste credentials or production source into issues.

Publishing baseline/feature commits does not authorize publishing target workspaces or runtime records.

## Baseline dependency notice

The historical v0.2.0 baseline resolves SQLitePCLRaw.lib.e_sqlite3 2.1.11 through Microsoft.Data.Sqlite. A clean CI restore identified [CVE-2025-6965 / GHSA-2m69-gcr7-jv3q](https://github.com/advisories/GHSA-2m69-gcr7-jv3q). The feature branch explicitly selects the compatible [2.1.13 bundle](https://www.nuget.org/packages/SQLitePCLRaw.bundle_e_sqlite3/2.1.13) and tests that the loaded engine is at least SQLite 3.50.2. NuGet audit remains enabled. Apply the dependency fix before using the historical baseline for ongoing work; tags are not rewritten.
