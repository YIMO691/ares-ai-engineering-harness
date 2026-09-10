# Ares 集成执行规则

通用方法以 [工作指南](docs/WORKFLOW.md) 为准，按任务与项目习惯裁剪；本页只规定它与现有 Harness 的衔接。SOP 不要求部署运行服务，Ares 的 CLI、风险模式和状态属于本项目已有运行契约。

完整理论角色和文档依赖见[目标 Harness](../docs/TARGET_HARNESS.md)与[文档契约](../docs/target-harness/ARTIFACT_CONTRACTS.md)。本页的四类项目文档与任务快照是当前适配，不能替代完整理论；目标能力未实现前不据此增加命令、强制文件或审批。实际项目后续按角色映射决定承载位置与适用性。

## 项目文档与任务快照

功能说明是持续维护入口，关联涉及端的实现和验证与验收。沿用项目已有路径、格式及有效手工内容；新记录参考 [模板目录](templates/README.md)。单端任务说明另一端不涉及的依据，独立小修可以使用原提单或 PR。以代码、必要测试和与实际结果一致的相关文档完成交付。

Harness 当前 `document` 仍接受 L1/L2/L3，分别生成 TASK、SPEC 或 PRD/SDD/TEST-PLAN；`align` 追加 TASK/SPEC 或写 DELIVERY。这些文件是外部 DocumentsRoot 中的**任务版本快照**，服务于冻结与证据校验，不另做项目的权威功能说明。现有文件名、Brief 结构和旧记录保持兼容，等级与 FAST/STANDARD/CRITICAL 不自动对应。

当前 Primary 用 `Brief.Source` 记录功能入口及版本，在 `Brief.Context` 记录相关客户端、服务端、验证文档的引用及本次观察，`Design` 和 Anchors 保留与本轮约定有关的摘要。路径本身不会被 CLI 自动展开、抓取或校验；不能只填链接而省略冻结意图。项目文档由 Primary 读写并按授权随代码提交，不复制一套长期 PRD/SDD/DELIVERY。

项目里的“功能说明 SPEC”与 DocumentsRoot 里的“L2 任务 SPEC”职责不同。已有项目采用 PRD/SDD 等名称时继续沿用，无须改名迁移。字段示例及验证边界见 [CLI 参考](../docs/CODEX_DIRECT.md#feature-records-and-task-snapshots)。

## 在当前会话中执行

1. Owner 与当前原生 Codex 直接讨论。检查目标项目指令、功能入口、相关代码与证据；未知业务取舍保留为问题，能够查明的事实先自行查证。按需参考规则与案例，不自动加载外部知识仓库。
2. 根据实际讨论整理任务局部请求，`create → document → agree` 登记来源、观察、目标、验收、边界、关键决定与验证方式。沿用真实授权；Owner 无需重填 JSON 或逐阶段批准。关键缺口只阻塞受影响的动作。
3. `begin` 后由同一 Primary 实施，按可验证的行为增量推进，更新受影响的功能记录。适合时采用 Red → Green → Refactor，其他情况记录实际验证方式；过程产物留在授权 scratch/evidence。
4. `submit → verify` 保存源码指纹并执行配置的构建/测试。STANDARD/CRITICAL 保留现有独立只读 Reviewer，FAST 不启动 Reviewer；不从通用 SOP 推导新的固定 Agent 角色或取消现有检查。
5. 发现实现问题按原约定返工并重新提交验证；需求、边界或关键契约改变时先协调，再 `reopen → document → agree`。失败没有新证据时调整诊断路线，不无限重试。冻结版本不能直接修改后沿用旧授权。
6. `align` 逐项关联当前 Run 的 build/test artifact ID，人工项需要真实确认；核对项目功能入口、两端实现、验证结论与最终代码，补齐偏移与遗留。项目验证文档保存可用证据引用及限制，运行快照保留机器所需摘要。
7. `accept` 只记录 Owner 实际最终接受；实施授权、验证通过与 GitHub 合并不能冒充业务验收。可选 `lens` 与 Web 保留只读观察边界和真实 PARTIAL/FAILED 状态。

机器校验文档快照、源码及证据的归属和完整性；不会自动证明外部链接正文、每条业务语义或人工验收。独立 Reviewer 的输入也有长度与访问边界。完整操作与当前限制见 [统一流程](../docs/UNIFIED_WORKFLOW.md)。
