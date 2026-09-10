# 本地集成改动

## 固定来源

Workflow-SOP 已从 `bd1a20e661440d44d448d2f713aa69e18b260bea` 更新到 [main @ a261bca](https://github.com/YIMO691/Workflow-SOP/tree/a261bcaa1cb1c075f185e003d8ce73bbe7ee89ca)（2026-09-10）。逐文件原始 SHA-256 与目标路径见 [UPSTREAM_IMPORTS.json](UPSTREAM_IMPORTS.json)。清单记录导入前的源字节，本地适配可以与源哈希不同；Change Lens 的固定来源和文件哈希保持不变。

导入包括工作指南、工程规则、工具接入、四类关联文档模板、示例、技能参考、来源与协作资料。SOP 可按任务裁剪；项目功能文档持续维护，Harness 的 L1/L2/L3 保留为现有任务快照格式。具体衔接仅在 [ARES_PROFILE](../../workflow/ARES_PROFILE.md) 维护。

## 本地适配

| 文件 | 适配内容 |
|---|---|
| workflow/UPSTREAM-AGENTS.md | 原 AGENTS.md 原文保存为来源，避免导入第二层维护指令 |
| workflow/README.md | 添加 Ares 阅读入口，标明上游指令的存档名称 |
| workflow/scripts/validate-workflow.ps1 | 必需入口使用 UPSTREAM-AGENTS.md，并检查 Ares 适配与兼容入口 |
| workflow/ARES_PROFILE.md | 项目功能记录与冻结快照的衔接、现行运行规则和真实能力边界 |
| workflow/AI-PLAYBOOK.md、SOP.md、START-HERE.md、TASK-LEVELS.md | Ares 本地兼容导航，原规则正文已退出当前树 |
| src/Ares.Workbench.Infrastructure/DirectDocuments.cs | 新快照声明当前 SOP 固定版本及 Ares 快照格式；旧记录保留原 SopVersion |
| 根 AGENTS、README、WORKFLOW、贡献指南、docs 与 Issue/PR 模板 | 改用现行 SOP 入口与文档关系，不新增运行状态或人工审批 |

导入目录中的 `.github` 保留上游来源语义，不在本仓库触发独立 CI；根 Actions 经 Test-Unified 执行已适配的 SOP 检查。上游 SUPPORT、贡献与安全渠道仍指上游；Ares 维护入口使用根目录对应文件。

Change Lens 既有本地改动仍限于以下文件：

- tools/change-lens/src/aeh_change_lens/languages/csharp/revision_analysis.py
- tools/change-lens/tests/analyzer/test_worker.py
- tools/change-lens/tests/context/test_build_provenance.py

它们允许显式指定并在测试中复用外部构建 Worker，保持原分析契约与许可。

## 旧路径去向与回退

| 旧路径 | 当前阅读位置 |
|---|---|
| SOP.md、TASK-LEVELS.md | [工作指南](../../workflow/docs/WORKFLOW.md) 与 [模板选择](../../workflow/templates/README.md)；保留旧入口跳转 |
| AI-PLAYBOOK.md、START-HERE.md、prompts/ | [使用入口](../../workflow/README.md) 与 ARES_PROFILE；前两者保留导航 |
| templates/TASK、PRD、SDD、TEST-PLAN、DELIVERY、ADR、ALIGNMENT-GATE、DELIVERY-CHECKLIST、FORMAL-FEATURE | [四类文档模板](../../workflow/templates/README.md) 与 [工程规则](../../workflow/docs/ENGINEERING_RULES.md)；项目既有记录不自动迁移 |
| examples/L3-complex-feature 的拆分文档与 adr/ | [异步导出设计片段](../../workflow/examples/L3-complex-feature/README.md) |
| pilots/ | 上游固定旧版本历史；不作为现行执行规则 |

旧导入与本地适配可从 [Harness 融合前基线](https://github.com/YIMO691/ares-ai-engineering-harness/tree/f425027a31263cb4842c4c86f1d0448a8e89c2c3/workflow) 恢复；更完整的上游沿革见 [SOURCES](../../workflow/docs/SOURCES.md)。旧模板的下游项目继续遵循已有效的契约，新采纳者更新引用。回退以新提交恢复所需内容，不改写共享历史。

## 维护时核对

更新源固定提交与源字节哈希，核对上述少量本地适配、旧路径和全部受影响引用，并同步新快照中的来源版本。文档检查验证结构和文件链接；运行回归覆盖现有冻结、验证、对齐及旧记录兼容，不把示例或控制替身当作真实业务验收。
