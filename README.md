# Ares AI Engineering Harness

Ares is a local software engineering workflow control plane around a mature coding agent. It coordinates tasks, policy, deterministic verification, independent review, recovery and human decisions.

It is not an IDE, a company-role simulation, or a replacement agent/session/tool runtime. Codex owns reasoning, native conversation context, search, editing, shell and sandbox execution. Ares exists to reduce the human effort of coordinating those steps, with measurable evidence.

## v0.2 baseline

Local Windows Web UI; FAST/STANDARD/CRITICAL; one Primary native Codex session; an independent read-only Reviewer; build/test, bounded rework, Stop/Cancel, same-Run Blocked/Resume and approval/history. STANDARD currently prepares then implements; Workflow Fusion is the next feature, not part of the v0.2.0 baseline.

This is a private single-user engineering tool. No proprietary target-project code, credentials or runtime records belong in this repository.

## Start

Requires PowerShell 7, .NET SDK 10, Git and an authenticated official Codex CLI (native integration verified with 0.153.4). Windows runtime outputs are currently restricted to `D:/AgentWorkspace`; select task-specific data/scratch locations outside the source repository.

Copy `scripts/workbench.example.json` to ignored `scripts/workbench.local.json` and replace every placeholder with an absolute path. Set DataRoot for durable local history, ScratchRoot for caches/build output, CodexHome for native sessions/auth, and the three executable paths. Keep this file local.

```powershell
pwsh -NoProfile -File ./scripts/Start-Workbench.ps1
```

Open http://127.0.0.1:5271 and keep the terminal open. Ctrl+C stops the server. A previously provisioned local installation can retain its existing data and CodexHome paths.

On Windows, native Codex sandbox provisioning may be needed once. The parameterized `Initialize-CodexSandbox.ps1` wrapper is an explicit administrator operation; the Web service runs as the normal user. It uses the official sandbox setup command and retains the established non-recursive sandbox-bin ownership compatibility step. Never broaden permissions to hide a failed run.

## Verify

```powershell
pwsh -NoProfile -File ./scripts/Test-Workbench.ps1 -ScratchRoot <absolute-task-scratch> -EvidenceRoot <absolute-task-evidence>
```

The script builds the solution and runs all tests, including a clearly labeled scripted-executor integration fixture. These tests do not impersonate a real Codex browser acceptance run. See [architecture](docs/architecture/ARCHITECTURE.md), [workflow](WORKFLOW.md), [contributing](CONTRIBUTING.md) and [security](SECURITY.md).

## Limits and next target

Single local user, serial queue. Stop terminates the current process tree and retains edits; Cancel cannot Resume. In-flight Run continuation is not restored after application restart. Repository/instruction fingerprint changes invalidate recovery. Allowed paths are business checks, not per-file OS ACLs.

Next: [Workflow Fusion](ROADMAP.md), joining Owner discussion and implementation in the same Primary native thread, freezing concise Ready anchors and requiring final Owner acceptance. No positive ROI is claimed from demo or helper tests.
