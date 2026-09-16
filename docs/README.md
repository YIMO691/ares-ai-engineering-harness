# 文档导航

Ares 的目标是减少 Owner 协调负担，交付正确、可维护且有证据支持的工程结果。下面三个入口分别回答“最终如何实现”“现在如何使用”“命令如何执行”；研究、历史和上游资料按需查证。

第一次接触项目，先读 [README 的思想脉络](../README.md#为什么会有-ares)：从黑盒执行的控制问题，理解开发流程、代码质量和需求复杂度为什么会汇合为现在的 Harness。详细出处见[研究脉络](research/REFERENCES.md#思想脉络与设计取舍)。

## 三个主入口

| 要解决的问题 | 阅读位置 | 内容边界 |
|---|---|---|
| 项目最终要实现什么、下一步实现什么 | [最终 Harness v2](TARGET_HARNESS.md) | 五部分职责：需求理解、工程控制、上下文与代码质量、原生实现与可信交付、运行与效果改进；含能力差距与演进次序 |
| 用现在的实现完成一个任务 | [现行流程](UNIFIED_WORKFLOW.md) | 讨论、文档、约定、实施、检查、返工、Align 和真实验收 |
| 如何配置、执行命令或排障 | [CLI 参考](CODEX_DIRECT.md) | 字段、JSON、命令、返回码、恢复与观察工具 |

最终 Harness 有两份按需附件：[26 类契约](target-harness/ARTIFACT_CONTRACTS.md)查具体信息职责，[贯穿示例](target-harness/WALKTHROUGH.md)看全链如何配合。它们不要求创建 26 个文件。首次启动见[项目 README](../README.md#快速开始)。

## 按需参考

| 需要什么 | 权威位置 |
|---|---|
| 现有代码组件与设计决定 | [当前架构](architecture/ARCHITECTURE.md)、[原生边界 ADR](decisions/0001-native-execution-boundary.md)、[Direct ADR](decisions/0003-codex-direct-observer.md) |
| 项目文档格式与示例 | [模板目录](../workflow/templates/README.md)、[功能示例](../workflow/examples/L2-standard-feature.md)；[SOP 正文](../workflow/docs/WORKFLOW.md)和[工程规则](../workflow/docs/ENGINEERING_RULES.md)按任务选读 |
| 工具与可选方法 | [SOP 工具参考](../workflow/docs/AI_COLLABORATION.md)、[方法目录](../workflow/skills/README.md)、[Change Lens](../tools/change-lens/README.md) |
| 如何贡献、验证与合并 | [贡献指南](../CONTRIBUTING.md)、[GitHub 协作规范](GITHUB_WORKFLOW.md) |
| 支持、安全及 Agent 约束 | [SUPPORT](../SUPPORT.md)、[SECURITY](../SECURITY.md)、[AGENTS](../AGENTS.md) |

SOP 与 Change Lens 保留固定来源和独立组件职责；不作为第二套 Ares 日常入口，上游指令不覆盖根 AGENTS 与现行流程。

## 历史与来源

- [研究来源](research/REFERENCES.md)：来源到目标章节的映射、内容哈希索引和证据限制；不重复维护执行方案。
- [固定导入清单](integrations/UPSTREAM_IMPORTS.json)、[适配与旧路径去向](integrations/LOCAL_ADAPTATIONS.md)：上游身份、归并记录及回退。
- [旧 Roadmap](history/ROADMAP.md)、[Phase 1 基线](history/ARES_AGENT_ENGINEERING_WORKBENCH_BASELINE_v1.0.md)、[C01–C10](history/CORE_CONTRACT_BOUNDARIES.md)：当时的计划和规格。
- [Fusion ADR](decisions/0002-workflow-fusion.md)、[Mini Lab](history/MINI_LAB.md)：旧模式与原型经验，不代表当前能力。

## 维护方式

新增资料先决定归属：目标与计划归总纲，当前操作归流程，字段/命令归 CLI，来源归研究页，历史保留当时语义。契约与案例只展开需要的细节；同一规则在一处定义，其余引用。

合并文档保留有效语义、来源与适用边界，更新全部引用；仅在读者、职责或生命周期明显不同时拆分。上游固定快照和独立组件不因减少文件数直接删除，适配需留记录。格式与链接按[协作规范](GITHUB_WORKFLOW.md#文档与模板格式依据)核对。
