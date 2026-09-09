# Ares AI Engineering Harness

在一个本地项目中完成需求讨论、工程文档、开发验证、独立审查和变化解释。Owner 与当前 Codex 直接协作，Web 是可选只读观察台。

- [统一流程与使用入口](docs/UNIFIED_WORKFLOW.md)
- [GitHub 协作与合并规范](docs/GITHUB_WORKFLOW.md) · [贡献指南](CONTRIBUTING.md)
- [公司 SOP 与模板](workflow/README.md) · [Ares 执行规范](workflow/ARES_PROFILE.md)
- [Change Lens](tools/change-lens/README.md)：只读变化解释，保留开发预览与 PARTIAL 边界
- [原直接协作说明](docs/CODEX_DIRECT.md)

新任务：讨论 → 文档 → Ready → 当前 Codex 实施 → 测试/独立 Reviewer → Align → Owner 验收。复用原生工具，不自建 Agent/Session Runtime，不自动接入外部知识库。

本仓库保持私有；商业源码、认证、SQLite 和运行证据不纳入 Git。SOP 与 Change Lens 按固定版本导入，原仓库未删除或归档，许可与来源保留。

## Current Workbench and historical baseline

The immutable v0.2.0 tag contains the sanitized original Workbench. The current Workbench includes Workflow Fusion: DISCUSSING with Primary → Owner freezes Ready → same native session implements → deterministic verification and independent Reviewer → Owner acceptance. FAST/STANDARD/CRITICAL, rework, Stop/Cancel, Blocked/Resume, approval and history reuse the existing Workbench.

The former Mini Lab teaching prototype is retired. Its design lessons, evaluation catalogue and recovery reference are retained in [Mini Lab history](docs/history/MINI_LAB.md). This repository is the sole maintained project.

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
pwsh -NoProfile -File ./scripts/Test-Unified.ps1 -ScratchRoot <absolute-task-scratch> -EvidenceRoot <absolute-task-evidence> -InstallTestDependencies
```

The script builds the solution and runs all tests, including a clearly labeled scripted-executor integration fixture. These tests do not impersonate a real Codex browser acceptance run. See [architecture](docs/architecture/ARCHITECTURE.md), [workflow](WORKFLOW.md), [contributing](CONTRIBUTING.md) and [security](SECURITY.md).

## Limits

Single local user, serial queue. Stop terminates the current process tree and retains edits; Cancel cannot Resume. In-flight Run continuation is not restored after application restart. Repository/instruction fingerprint changes invalidate recovery. Allowed paths are business checks, not per-file OS ACLs.

Ready freezes project policy and Git/instruction state. A change requires returning to discussion and freezing a new version, not silently broadening an active Run. Final Owner Accept is required for Done; CRITICAL approval alone does not complete the Task. See [Workflow Fusion decisions](docs/decisions/0002-workflow-fusion.md). No positive ROI is claimed from demo or helper tests.
