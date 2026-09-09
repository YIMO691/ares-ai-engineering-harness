# Ares AI Engineering Harness

将需求讨论、工程文档、开发验证、独立审查和交付对齐连接成一套本地研发流程。Owner 与当前 Codex 原生 Primary 直接协作，Web 按需展示进度，Change Lens 辅助解释变化。

## 项目状态

当前主线采用 **Codex Direct + Observer**。Workflow-SOP 与 Change Lens 已按固定版本纳入同一仓库；旧 v0.2 / Workflow Fusion 路径保留兼容。`v0.2.0` 是历史基线，不代表当前推荐入口。

本项目由 [YIMO691](https://github.com/YIMO691) 维护，仓库保持私有。未接入外部知识仓库；需要参考时由 Owner 明确指定。

## 功能与流程

**策划需求 → Owner/Codex 讨论 → 分级工程文档 → Ready → 同一 Primary 实施 → Test / Reviewer / 返工 → Align → Owner 验收。**

- Context 记录实际观察和来源；文档保留目标、验收、非目标、边界、关键决定和验证方案。
- L1/L2/L3 决定文档深度；FAST/STANDARD/CRITICAL 决定检查强度。
- Harness 保存约定、版本、运行证据和真实决定；STANDARD/CRITICAL 使用独立只读 Reviewer。
- Web 默认只读；Change Lens 可选，保留 `PARTIAL` / `FAILED` 的真实状态。

详见 [工作流概览](WORKFLOW.md) 和 [完整使用说明](docs/UNIFIED_WORKFLOW.md)。

## 环境要求

| 组件 | 用途 |
|---|---|
| Windows、PowerShell 7、Git、.NET SDK 10 | 当前本地宿主与构建/测试 |
| 已配置认证和沙箱的官方 Codex CLI | 原生执行与独立 Reviewer；既有原生验证版本为 0.153.4 |
| Python 3.11+ | 可选 Change Lens；统一检查需要 jsonschema 和 PyYAML |

当前 Windows 输出路径限制在授权的 `D:/AgentWorkspace` 下。任务数据、缓存、日志、认证与构建输出使用仓库外的任务目录。

## 快速开始

从仓库根执行。先将 [配置示例](scripts/workbench.example.json) 复制到仓库外的本地 JSON 文件，填写绝对路径。保持 `ObserverOnly: true`；保留现有认证配置。字段说明见 [一次性配置](docs/UNIFIED_WORKFLOW.md#一次性配置)。

```powershell
./scripts/Ares.ps1 -SettingsFile '<absolute-local-settings.json>' -Operation projects -Build
```

该命令构建 CLI 并读取已登记项目。之后由当前 Codex 根据实际讨论准备请求文件，执行任务流程；Owner 无需手写 JSON。需要 Change Lens 时按使用说明配置 Worker，并加 `-BuildLens` 构建。

按需启动只读观察台：

```powershell
./scripts/Start-Workbench.ps1 -SettingsFile '<absolute-local-settings.json>' -Port 5271
```

打开 `http://127.0.0.1:5271/Observe`。脚本前台运行，Ctrl+C 停止 Web；CLI 工作不依赖 Web。由 Agent 在后台启动时使用隐藏进程，日志写入任务目录。

## 验证

```powershell
./scripts/Test-Unified.ps1 -ScratchRoot '<absolute-task-scratch>' -EvidenceRoot '<absolute-task-evidence>' -InstallTestDependencies
```

统一入口执行 Harness 构建/测试、SOP 文档检查、Roslyn Worker 构建和 Change Lens 测试。测试依赖写入指定 ScratchRoot。控制替身测试不代表真实 Codex 验收；真实 Unity 检查需要单独授权和配置。最新远程结果见 [GitHub Actions](https://github.com/YIMO691/ares-ai-engineering-harness/actions/workflows/build.yml)。

## 文档与贡献

- [文档导航与适用范围](docs/README.md)
- [GitHub 协作规范](docs/GITHUB_WORKFLOW.md) · [贡献指南](CONTRIBUTING.md)
- [公司 SOP 与模板](workflow/README.md) · [Ares 适配规则](workflow/ARES_PROFILE.md)
- [Change Lens](tools/change-lens/README.md) · [导入来源](docs/integrations/UPSTREAM_IMPORTS.json)
- [支持与问题反馈](SUPPORT.md) · [安全说明](SECURITY.md)

## 已知限制

单用户本地工具，不是多租户网络服务。Web 不转发当前 Primary 对话，也不采集其完整工具级遥测。Direct 模式不自动接管或恢复原生会话；硬中断需核对进程和源码后重新验证，每次 `verify` 建立新的 Run。

文档、源码和证据完整性检查不能独立证明所有业务验收语义；独立审查和实际 Owner 验收仍须如实记录。原生完整聊天不会自动导出为任务文档。Change Lens 的建议不是已执行测试，`PARTIAL` 不等于完整 Unity 语义验证。未声称生产项目验收或正向 ROI。

## 许可

主项目尚未选择开源许可证，不因仓库规范化而自动授予开源许可。导入 Change Lens 保留其 [MIT LICENSE](tools/change-lens/LICENSE)；来源和适配见 [导入说明](docs/integrations/LOCAL_ADAPTATIONS.md)。
