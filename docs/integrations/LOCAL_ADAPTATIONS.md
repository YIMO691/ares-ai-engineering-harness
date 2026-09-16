# 本地集成改动

## 固定来源

Workflow-SOP 已从 `bd1a20e661440d44d448d2f713aa69e18b260bea` 更新到 [main @ a261bca](https://github.com/YIMO691/Workflow-SOP/tree/a261bcaa1cb1c075f185e003d8ce73bbe7ee89ca)（2026-09-10）。逐文件原始 SHA-256 与目标路径见 [UPSTREAM_IMPORTS.json](UPSTREAM_IMPORTS.json)。清单记录导入前的源字节，本地适配可以与源哈希不同；Change Lens 的固定来源和文件哈希保持不变。

导入包括工作指南、工程规则、工具接入、四类关联文档模板、示例、技能参考、来源与协作资料。SOP 可按任务裁剪；项目功能文档持续维护，Harness 的 L1/L2/L3 保留为现有任务快照格式。当前文档维护与执行规则统一在[现行流程](../UNIFIED_WORKFLOW.md)维护。

## 本地适配

| 文件 | 适配内容 |
|---|---|
| workflow/UPSTREAM-AGENTS.md | 原 AGENTS.md 原文保存为来源，避免导入第二层维护指令 |
| workflow/README.md | 添加 Ares 阅读入口，标明上游指令的存档名称 |
| workflow/scripts/validate-workflow.ps1 | 必需入口使用 UPSTREAM-AGENTS.md，核对现行流程；已移除退役跳转页的强制要求 |
| docs/UNIFIED_WORKFLOW.md | 汇总当前流程与原 ARES_PROFILE 的项目文档/快照规则，作为唯一执行主说明 |
| 原 workflow/AI-PLAYBOOK.md、SOP.md、START-HERE.md、TASK-LEVELS.md | 重复跳转页已移除；使用 workflow/README.md、SOP 正文及模板目录 |
| src/Ares.Workbench.Infrastructure/DirectDocuments.cs | 新快照声明当前 SOP 固定版本及 Ares 快照格式；旧记录保留原 SopVersion |
| 根 AGENTS、README、贡献指南、docs 与 Issue/PR 模板 | 改用现行 SOP 入口与文档关系，不新增运行状态或人工审批 |

导入目录中的 `.github` 保留上游来源语义，不在本仓库触发独立 CI；根 Actions 经 Test-Unified 执行已适配的 SOP 检查。上游 SUPPORT、贡献与安全渠道仍指上游；Ares 维护入口使用根目录对应文件。

Change Lens 既有本地改动仍限于以下文件：

- tools/change-lens/src/aeh_change_lens/languages/csharp/revision_analysis.py
- tools/change-lens/tests/analyzer/test_worker.py
- tools/change-lens/tests/context/test_build_provenance.py

它们允许显式指定并在测试中复用外部构建 Worker，保持原分析契约与许可。

## 旧路径去向与回退

| 旧路径 | 当前阅读位置 |
|---|---|
| SOP.md、TASK-LEVELS.md | [工作指南](../../workflow/docs/WORKFLOW.md) 与 [模板选择](../../workflow/templates/README.md)；旧跳转页已移除 |
| AI-PLAYBOOK.md、START-HERE.md、prompts/ | [使用入口](../../workflow/README.md) 与现行流程；旧跳转页已移除 |
| templates/TASK、PRD、SDD、TEST-PLAN、DELIVERY、ADR、ALIGNMENT-GATE、DELIVERY-CHECKLIST、FORMAL-FEATURE | [四类文档模板](../../workflow/templates/README.md) 与 [工程规则](../../workflow/docs/ENGINEERING_RULES.md)；项目既有记录不自动迁移 |
| examples/L3-complex-feature 的拆分文档与 adr/ | [异步导出设计片段](../../workflow/examples/L3-complex-feature/README.md) |
| pilots/ | 上游固定旧版本历史；不作为现行执行规则 |

旧导入与本地适配可从 [Harness 融合前基线](https://github.com/YIMO691/ares-ai-engineering-harness/tree/f425027a31263cb4842c4c86f1d0448a8e89c2c3/workflow) 恢复；更完整的上游沿革见 [SOURCES](../../workflow/docs/SOURCES.md)。旧模板的下游项目继续遵循已有效的契约，新采纳者更新引用。回退以新提交恢复所需内容，不改写共享历史。

## 本轮文档归并

2026-09-10 在完整目标理论建立后整理阅读入口：README 只保留项目介绍、启动和导航，现行流程解释实际操作，CLI 参考集中配置、字段和 JSON。目标总纲、26 类契约与贯穿示例保持完整。

| 旧位置 | 当前归属 |
|---|---|
| 根 WORKFLOW.md、workflow/ARES_PROFILE.md | [现行流程](../UNIFIED_WORKFLOW.md)，原独立文件删除 |
| docs/architecture/PRINCIPLES.md | [当前架构的原则](../architecture/ARCHITECTURE.md#设计原则) |
| 根 ROADMAP.md | [旧 Roadmap](../history/ROADMAP.md)，原正文保留并补充历史标识 |
| docs/ARES_AGENT_ENGINEERING_WORKBENCH_BASELINE_v1.0.md、CORE_CONTRACT_BOUNDARIES.md | docs/history/ 同名文件，更新相对引用 |
| 现行流程中的详细配置、document/align JSON 与 Lens 参数 | [CLI 参考](../CODEX_DIRECT.md#local-settings) |

仓库内引用与校验规则已同步，旧文件路径不再保留额外跳转页。需要回查旧路径时使用[整理前基线](https://github.com/YIMO691/ares-ai-engineering-harness/tree/26e24733d3ce2640608f549a28ecabc0ed0895bb)；回退通过新提交恢复旧文件与引用。历史归档不改变当时规格，也不将旧授权应用到新任务。

## 维护时核对

更新源固定提交与源字节哈希，核对上述少量本地适配、旧路径和全部受影响引用，并同步新快照中的来源版本。文档检查验证结构和文件链接；运行回归覆盖现有冻结、验证、对齐及旧记录兼容，不把示例或控制替身当作真实业务验收。

## 2026-09-16 最终 Harness 融合与归并

目标总纲升级为 v2，将需求理解与未知分流、必要复杂度、项目模式、质量约定、领域校准、机械反馈及真实评测纳入同一设计。原 C01–C26 全部保留，相关契约和虚构贯穿示例同步补充；现行流程保持操作权威，字段与命令仍由 CLI 参考维护。

| 整理前位置 | 现在的权威位置 |
|---|---|
| research/CODE_QUALITY_ADOPTION.md 的来源、逐项映射、证据限制和维护规则 | [研究来源](../research/REFERENCES.md)；原独立页撤除 |
| 同页的目标职责、质量与兼容设计 | [最终 Harness](../TARGET_HARNESS.md)及其契约；当前用法在现行流程 |
| target-harness/CODE_QUALITY_PLAN.md 的实施顺序、校准、MINI 和实验方案 | [总纲第 11 节](../TARGET_HARNESS.md#11-当前实现映射与演进次序)与[第 13 节](../TARGET_HARNESS.md#13-如何判断是否适合-harness--model)；原独立页撤除 |

上述两页来自本地尚未发布的接入草稿，撤除重复入口后不保留跳转页。根 README、AGENTS、导航和现行流程同步引用；研究原件、[内容哈希清单](../research/CODE_QUALITY_SOURCES.json)、固定上游导入、SOP/Change Lens、运行源码及历史正文不因本轮整理改变。

未来维护沿三个主入口展开，不为每批研究新建接入总纲或平行计划。恢复本次归并前的两份本地草稿可用任务证据中的原字节备份与路径登记；已提交版本的回退采用新提交，保留历史并核对后续改动。文档融合没有启用新 CLI 状态、门禁、运行程序或业务验收。

## 2026-09-16 入口正文对齐修正

最终总纲 v2 更新后，入口文档仍有旧的文档链/组件导向和过宽的人工判断描述。本次直接修正 README 的目标、五部分职责与三层能力状态；现行流程的开头、角色、流程图与已有字段用法；当前架构的原则；AGENTS、贡献与支持说明；CLI 导言及研究版本说明。导航按同一目标与当前/目标边界组织，不新增 Markdown。

workflow/README.md 仅移动并重写已有 Ares 集成说明，置于上游正文之前，明确上游“本仓库”的指代与 Ares 的执行权威；排除该本地说明后，其余正文逐字核对不变。UPSTREAM_IMPORTS 与研究来源哈希清单不变，历史 ADR/规格、模板和运行代码不因本次修正改变。

验证除链接外，逐项核对目的、职责、当前方法与自动机制的区别、验收边界及下一步入口。未来目标变更按贡献指南核对受影响正文；回退以新的提交恢复，不改写已有合并历史。
