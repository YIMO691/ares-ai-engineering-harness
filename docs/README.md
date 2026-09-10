# 文档导航

## 完整目标体系

| 文档 | 用途 |
|---|---|
| [目标 Harness 总纲](TARGET_HARNESS.md) | 来源、Context、边界、PRD、SPEC 到交付与运行反馈的全链；职责、完成条件、依赖失效、模型协作与当前差距 |
| [26 类文档契约](target-harness/ARTIFACT_CONTRACTS.md) | 每种理论产物的输入、内容、维护者、完成条件与消费者 |
| [全流程贯穿示例](target-harness/WALKTHROUGH.md) | 虚构跨端功能的全角色映射、失败返工、变更传播、交接和最终索引 |

这三份文档建立目标理论。项目落地时再确定载体和裁剪，不因目标说明出现新角色就改变当前 CLI、强制生成全部文件或新增审批。

## 当前使用与贡献

| 文档 | 用途 |
|---|---|
| [项目 README](../README.md) | 项目状态、环境、启动和支持 |
| [工作流概览](../WORKFLOW.md) | 角色、阶段、风险等级和恢复边界 |
| [当前研发流程](UNIFIED_WORKFLOW.md) | 现行 Direct 的讨论文件、实施依据、注释与测试、审查、验收及配置示例 |
| [CLI 参考](CODEX_DIRECT.md) | 操作字段、返回码、返工和恢复 |
| [GitHub 协作规范](GITHUB_WORKFLOW.md) | Issue/PR 留痕、检查与授权合并 |
| [贡献指南](../CONTRIBUTING.md) | 开发、文档与验证要求 |
| [架构](architecture/ARCHITECTURE.md) / [原则](architecture/PRINCIPLES.md) | 当前实现与责任边界 |
| [安全](../SECURITY.md) / [支持](../SUPPORT.md) | 本地边界与问题反馈 |

## SOP 与导入组件

[工作指南](../workflow/docs/WORKFLOW.md) 是当前 SOP 正文；[四类模板](../workflow/templates/README.md) 与 [示例](../workflow/examples/L2-standard-feature.md) 展示功能入口怎样关联两端实现和验证。[ARES_PROFILE](../workflow/ARES_PROFILE.md) 规定与当前 Harness 的衔接：项目文档持续维护，L1/L2/L3 保留为运行快照格式。

[Change Lens](../tools/change-lens/README.md) 保留自身接口、案例和已知限制。固定来源、原哈希及本地适配见 [导入清单](integrations/UPSTREAM_IMPORTS.json) 和 [适配说明](integrations/LOCAL_ADAPTATIONS.md)。导入目录中的上游说明不取代根 AGENTS 与当前集成规则，不为统一排版重写固定来源全文。

## 决策与历史

| 文档 | 适用范围 |
|---|---|
| [ADR 0001](decisions/0001-native-execution-boundary.md) | 原生执行边界，Direct 差异见 ADR 0003 |
| [ADR 0002](decisions/0002-workflow-fusion.md) | 旧 Web Fusion；新任务入口已被 ADR 0003 替代 |
| [ADR 0003](decisions/0003-codex-direct-observer.md) | 当前 Direct 与可选观察台 |
| [Phase 1 基线](ARES_AGENT_ENGINEERING_WORKBENCH_BASELINE_v1.0.md) / [C01–C10](CORE_CONTRACT_BOUNDARIES.md) | 历史设计快照，不代表当前新任务执行顺序 |
| [Mini Lab](history/MINI_LAB.md) | 退役原型与历史经验 |
| [研究参考](research/REFERENCES.md) | 设计参考及原验证范围，不是当前能力认证 |

## 写作与维护

面向当前使用者的文档采用 GitHub Flavored Markdown：单一一级标题、清晰的二级章节、相对链接、带语言的代码块和必要的表格。README 讲项目与启动，CONTRIBUTING 讲参与方式，Issue/PR 模板收集可核验信息，不把同一长模板套在所有文件上。

文档结论以实际代码和已授权需求对齐；历史内容保留时间/适用范围，不伪造历史状态。GitHub 推荐结构和模板语法的依据见 [格式依据](GITHUB_WORKFLOW.md#文档与模板格式依据)。
