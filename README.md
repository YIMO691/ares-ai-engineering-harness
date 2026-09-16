# Ares AI Engineering Harness

**让你专注需求、关键决策与验收，让 AI 自主完成有证据支持、可长期维护的工程交付。**

Ares 的最终目标是降低你的需求澄清和协调负担，让 AI 在真实项目中完成正确、复杂度适当、容易维护的改动，并用验证、实际验收和运行反馈持续改进。原生 Codex 负责调查、设计、编码和返工；Harness 负责约定、权限、版本、检查与证据。成功标准是缺陷与返工减少、人的投入和总成本合理；这些收益需要真实任务评测，当前尚未证明。

## 从这里开始

| 你现在要做什么 | 阅读入口 |
|---|---|
| 了解最终目标、整体设计与实施次序 | [最终 Harness v2](docs/TARGET_HARNESS.md)，按需展开契约与示例 |
| 用当前实现完成一个需求 | [现行流程](docs/UNIFIED_WORKFLOW.md)：文档、约定、实施、验证、返工和验收 |
| 配置工具、查命令或排障 | [CLI 参考](docs/CODEX_DIRECT.md)：配置、字段、返回码和恢复 |

其他资料按需查[文档导航](docs/README.md)。这三条入口各自负责目标理论、现行操作和工具细节，无需先通读整个仓库。

## 目标与当前能力

最终链路是：**表达意图 → 查证项目与解决关键未知 → 最小必要设计和质量约定 → 自主实现 → 验证、审查与交付 → 实际验收 → 发布运行与效果反馈**。五部分共同支撑这条链路：

| 部分 | 最终要解决的问题 |
|---|---|
| 需求理解与未知分流 | 你给出目标和关键约束，AI 自行查证工程事实，只对关键业务或授权缺口提问 |
| 工程控制 | 明确按哪个版本、范围和授权执行，保留变化、失败与恢复依据 |
| 上下文与代码质量 | 使用适用的项目模式，避免重复状态、无依据的抽象和无关改动 |
| 原生实现与可信交付 | AI 自主实施和修复，用工具结果、必要审查与真实验收支持交付 |
| 运行与效果改进 | 评价质量、维护性、人的负担和总成本，据此保留、改进或删除辅助机制 |

完整设计由[最终 Harness](docs/TARGET_HARNESS.md)维护，26 类契约是信息职责，可由少量既有项目文档承载。当前能力分三层理解：

| 状态 | 当前范围 |
|---|---|
| 已实现的运行机制 | Direct 的约定冻结、版本/源码/证据校验、配置的 Build/Test、按模式审查、返工、Align、实际验收记录与只读观察 |
| 由 Primary 按文档执行的方法 | 查证与未知分流、必要复杂度判断、适用模式选择和质量自检；复用现有 Context、Design、Verification 等载体 |
| 待实现或待验证的目标 | 结构化质量支持、自动模式/Context 管理、细粒度依赖与场景证据、项目领域规则校准、真实效果评测及发布运行集成 |

当前主线是 **Codex Direct + Observer**：同一 Primary 与你直接协作；L1/L2/L3 选择任务快照格式，FAST 不启动 Reviewer，STANDARD/CRITICAL 使用独立只读 Reviewer。文档方法不等于自动门禁，检查通过不等于业务验收，Done 不自动提交、合并或发布。能力差距和下一步见[演进次序](docs/TARGET_HARNESS.md#11-当前实现映射与演进次序)。

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
先查证项目事实，明确当前目标、非目标与验收条件。
沿用已有授权处理局部实现选择，只对影响业务或授权的关键未知提问。
按最小必要方案实施，验证行为与代码质量，并说明结果和限制。
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
| `src/`、`tests/`、`scripts/` | 当前工程控制机制、验证、配置与启动 |
| `docs/` | 目标理论、现行流程、CLI、架构和协作参考；旧规格在 `docs/history/` |
| `workflow/` | 固定版本的 SOP 方法、模板与案例，支撑现行流程 |
| `tools/change-lens/` | 可选变化分析器及其独立组件资料 |
| `.github/`、`AGENTS.md` | 协作模板、CI 和 Agent 维护指令 |

实现层整合 Ares Harness、Workflow-SOP 和可选 Change Lens；Web 按需观察。当前是单用户本地工具；`v0.2.0` 与旧 Web Fusion 作为兼容历史保留。硬中断需核对现场，不承诺无损恢复原生会话。组件范围见[当前架构](docs/architecture/ARCHITECTURE.md)和[Change Lens](tools/change-lens/README.md)。

项目由 [YIMO691](https://github.com/YIMO691) 维护。问题反馈见 [SUPPORT](SUPPORT.md)，安全问题按 [SECURITY](SECURITY.md) 私下报告。主项目尚未选择开源许可证；Change Lens 保留原 [MIT LICENSE](tools/change-lens/LICENSE)。来源与许可边界见[适配说明](docs/integrations/LOCAL_ADAPTATIONS.md)。
