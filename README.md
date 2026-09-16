# Ares AI Engineering Harness

**让你专注需求、关键决策与验收，让 AI 自主完成有证据支持、可长期维护的工程交付。**

本仓库整合 Workflow-SOP、Ares Harness 和 Change Lens。原生 Codex 负责理解、规划、搜索、编辑和返工；Harness 保存版本、权限边界、运行结果和决定；Web 按需观察进度。

## 从这里开始

| 你现在要做什么 | 阅读入口 |
|---|---|
| 了解完整目标：从需求到运行反馈 | [目标 Harness](docs/TARGET_HARNESS.md)，按需展开其中的 26 类契约和完整示例 |
| 用当前实现完成一个需求 | [现行流程](docs/UNIFIED_WORKFLOW.md)：文档、约定、实施、验证、返工和验收 |
| 配置工具、查命令或排障 | [CLI 参考](docs/CODEX_DIRECT.md)：配置、字段、返回码和恢复 |

其他资料按需查[文档导航](docs/README.md)。这三条入口各自负责目标理论、现行操作和工具细节，无需先通读整个仓库。

## 目标与当前能力

完整目标链是：**来源 → Context → 边界 → PRD → SPEC → 设计与契约 → 计划与测试方案 → 实施 → 验证与审查 → 对齐 → 交付与验收 → 发布运行 → 评估反馈**。需求与未知分流、必要复杂度、项目 Context 与代码质量约定贯穿全程；通过真实任务与运行反馈评价质量、人的负担和总成本。完整职责在项目中可合并到少量既有文档。

当前主线是 **Codex Direct + Observer**。项目沿用功能说明及相关客户端、服务端、验证记录；Harness 的 L1/L2/L3 保存本轮约定快照。讨论确认后由同一 Primary 实施，经过验证、Align 和实际 Owner 验收完成任务。STANDARD/CRITICAL 使用独立只读 Reviewer，FAST 不启动 Reviewer。

当前已有冻结约定、源码/证据校验、返工与恢复记录；结构化质量支持、完整依赖图、场景级证据、真实效果评测和发布运行闭环仍是目标能力。对照表见[当前与目标差距](docs/TARGET_HARNESS.md#11-当前实现映射与演进次序)。工具检查成功不等于业务验收；Done 不自动提交、合并或发布。

## 快速开始

### 环境与配置

当前本地脚本使用 Windows、PowerShell 7、Git、.NET SDK 10 和已配置认证/沙箱的官方 Codex CLI。Change Lens 使用 Python 3.11+；统一检查另需 jsonschema 与 PyYAML。既有原生验证使用 Codex CLI 0.153.4，不把这个历史验证版本当成最新版本推荐。

将[配置示例](scripts/workbench.example.json)放到仓库外，填写本机绝对路径并保持 `ObserverOnly: true`。DataRoot、DocumentsRoot、ScratchRoot 及过程输出使用授权的 `D:/AgentWorkspace` 目录；CodexHome 使用已配置的原生 CLI home。凭证、本地配置、缓存和原始证据不提交到 Git。字段与可选 Change Lens 配置集中在 [CLI 配置](docs/CODEX_DIRECT.md#local-settings)。

### 启动当前流程

从仓库根构建命令入口并列出已登记项目：

```powershell
./scripts/Ares.ps1 -SettingsFile '<absolute-local-settings.json>' -Operation projects -Build
```

随后向当前 Codex 提供需求即可，例如：

```text
使用 Ares 处理以下需求。
目标项目：<项目路径>
需求来源：<资料及版本>
允许范围：<本轮可修改内容>
先核对现状、整理需求与验收条件，确认后按约定实施并交付。
```

Codex 维护项目登记、请求 JSON 和实际授权，无需你手写字段或到 Web 重填需求。完整推进与返工规则见[现行流程](docs/UNIFIED_WORKFLOW.md)。上述示例不替代真实实施或发布授权。

Web 与 Change Lens 均按需启用；[观察台启动](docs/CODEX_DIRECT.md#start-locally)和[变化报告](docs/CODEX_DIRECT.md#change-lens)见 CLI 参考。Web 读取已有记录，不接管原生对话，也不提供全部工具调用的实时遥测。

## 验证与贡献

验证本仓库使用统一入口；业务任务的 `verify` 则执行其登记的项目命令：

```powershell
./scripts/Test-Unified.ps1 -ScratchRoot '<absolute-task-scratch>' -EvidenceRoot '<absolute-task-evidence>' -InstallTestDependencies
```

统一检查覆盖 Harness、SOP 和 Change Lens，依赖与输出放在外部任务目录。[GitHub Actions](https://github.com/YIMO691/ares-ai-engineering-harness/actions/workflows/build.yml)保存远端实际结果；控制替身测试、部分分析与真实项目验收分别记录，不能据此宣称生产可靠性或正向 ROI。

贡献见 [CONTRIBUTING](CONTRIBUTING.md)，Issue/PR 与授权合并规则见 [GitHub 协作规范](docs/GITHUB_WORKFLOW.md)。商业源码、受限原始资料、认证、SQLite 和运行配置不上传；完整业务证据保留在授权位置，只公开安全摘要。

## 仓库与维护

| 位置 | 内容 |
|---|---|
| `src/`、`tests/`、`scripts/` | Harness 实现、验证、配置与启动 |
| `docs/` | 目标理论、现行流程、CLI、架构和协作参考；旧规格在 `docs/history/` |
| `workflow/` | 固定版本的 SOP 方法、模板与案例，日常执行以现行流程为入口 |
| `tools/change-lens/` | 可选变化分析器及其独立组件资料 |
| `.github/`、`AGENTS.md` | 协作模板、CI 和 Agent 维护指令 |

当前是单用户本地工具；`v0.2.0` 与旧 Web Fusion 作为兼容历史保留。硬中断需核对现场，不承诺无损恢复原生会话。组件范围见[当前架构](docs/architecture/ARCHITECTURE.md)和[Change Lens](tools/change-lens/README.md)。

项目由 [YIMO691](https://github.com/YIMO691) 维护。问题反馈见 [SUPPORT](SUPPORT.md)，安全问题按 [SECURITY](SECURITY.md) 私下报告。主项目尚未选择开源许可证；Change Lens 保留原 [MIT LICENSE](tools/change-lens/LICENSE)。来源与许可边界见[适配说明](docs/integrations/LOCAL_ADAPTATIONS.md)。
