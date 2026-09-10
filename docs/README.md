# 文档导航

日常只按当前问题选择入口。每类内容有一个权威位置，其余文档引用它；完整理论不因入口精简而裁剪。

## 三个主入口

| 入口 | 负责内容 | 按需展开 |
|---|---|---|
| [目标 Harness](TARGET_HARNESS.md) | 完整文档链、职责、完成条件、依赖失效、模型协作与能力差距 | [26 类契约](target-harness/ARTIFACT_CONTRACTS.md)、[贯穿示例](target-harness/WALKTHROUGH.md) |
| [现行流程](UNIFIED_WORKFLOW.md) | 当前 Direct 的文档维护、约定、实施、检查、返工和实际验收 | [模板](../workflow/templates/README.md)、[工程规则](../workflow/docs/ENGINEERING_RULES.md) |
| [CLI 参考](CODEX_DIRECT.md) | 配置、命令/字段、请求示例、返回码、恢复和观察工具 | [配置文件示例](../scripts/workbench.example.json) |

目标描述应有的完整职责，现行流程描述已实现行为；目标不会自动增加 CLI 状态、门禁、独立文件或审批。第一次启动看[项目 README](../README.md#快速开始)。

## 按需参考

| 要解决的问题 | 权威位置 |
|---|---|
| 贡献与验证 | [贡献指南](../CONTRIBUTING.md)；具体 GitHub 留痕与合并规则见[协作规范](GITHUB_WORKFLOW.md) |
| 组件和设计原则 | [当前架构](architecture/ARCHITECTURE.md)；决策依据见 [ADR 0001](decisions/0001-native-execution-boundary.md)和 [ADR 0003](decisions/0003-codex-direct-observer.md) |
| 文档如何写、功能如何修改 | [模板目录](../workflow/templates/README.md)与[首次开发/后续修改示例](../workflow/examples/L2-standard-feature.md)；通用方法见 [SOP 正文](../workflow/docs/WORKFLOW.md) |
| 工具接入或方法卡点 | [SOP 工具参考](../workflow/docs/AI_COLLABORATION.md)、[可选方法](../workflow/skills/README.md) |
| Change Lens 独立使用与边界 | [组件 README](../tools/change-lens/README.md) |
| 支持、安全与 Agent 维护规则 | [SUPPORT](../SUPPORT.md)、[SECURITY](../SECURITY.md)、[AGENTS](../AGENTS.md) |

SOP 和 Change Lens 目录保留固定来源的说明、模板、示例和组件资料；它们是按需参考库，不是第二套 Ares 日常入口。上游维护指令不覆盖根 AGENTS 与现行流程。

## 历史与来源

以下材料用于追溯，不作为当前任务指令或未来版本承诺：

- [旧 Roadmap](history/ROADMAP.md)：早期工作包的计划与限制。
- [Phase 1 基线](history/ARES_AGENT_ENGINEERING_WORKBENCH_BASELINE_v1.0.md)、[C01–C10](history/CORE_CONTRACT_BOUNDARIES.md)：历史设计契约。
- [ADR 0002](decisions/0002-workflow-fusion.md)、[Mini Lab](history/MINI_LAB.md)：旧 Web Fusion 与退役原型。
- [研究参考](research/REFERENCES.md)：设计来源与原验证范围，不是能力认证。
- [固定导入清单](integrations/UPSTREAM_IMPORTS.json)、[适配及旧路径去向](integrations/LOCAL_ADAPTATIONS.md)：来源、哈希、归并和回退。

## 维护方式

新增内容先放入已有权威文档；仅在读者、职责或生命周期明显不同时拆分。README 只讲项目、入口和启动；流程只讲当前操作；字段与 JSON 只在 CLI 参考；目标理论保留完整角色；历史规格归历史目录。模板与案例按需打开，不复制到主流程全文。

使用 GitHub Flavored Markdown、必要的相对链接和图表，核对受影响链接与锚点。历史正文保留当时语义，变更只补状态说明和必要导航；上游固定快照只做有记录的本地适配。格式依据见[协作规范](GITHUB_WORKFLOW.md#文档与模板格式依据)。
