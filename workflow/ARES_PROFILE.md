# Ares 集成执行规则

本文件将现有 SOP 接入当前 Harness；v0.4 仍为试运行规则，不新增独立审批系统。

1. Owner 与当前原生 Codex 直接讨论。先读目标项目规则和相关工程事实，再按 L1/L2/L3 读取本目录模板与示例。
2. 原始需求保留来源/版本；上下文记录观察与来源；决定写清选择与理由。成熟案例只在相关时引用，不自动读取外部知识库。
3. 使用 CLI create、document 生成 TASK/SPEC/PRD+SDD+TEST-PLAN。Brief 由 Codex 根据真实讨论整理，Owner 无需重复填写 JSON。生成器覆盖最低共同内容，领域细节应完整写入 Design/Context/验收；模板存在不等于内容已经充分。
4. agree 从已登记文档取得 Anchors/Checks，记录实际授权；begin 沿用当前 Primary。CRITICAL 使用现有边界授权。SOP 的 Build 指设计确认，不能用编译通过代替。
5. 按文档中的可验证增量实施。核心规则和缺陷优先 Red/Green/Refactor，无法适用时记录替代验证。过程文件留在 task scratch/evidence。
6. submit 保存变更说明和源码指纹，verify 执行构建/测试及 STANDARD/CRITICAL 独立 Reviewer。FAST 不启动 Reviewer。发现交给当前 Primary 修复，再提交检查。
7. align 为每项 AC 关联当前 Run 的 build/test artifact ID；人工项要求真实 Owner 确认。填写设计、偏移、清理与遗留，无未解决冲突才能 Align。L1/L2 结果追加 TASK/SPEC，L3 使用 DELIVERY。机器检查证据归属与完整性，Primary 对语义陈述负责；不是新的语义证明器。
8. accept 只记录实际最终接受。新任务没有 Align、源码/文档/证据变化时不能接受。不能把实施授权冒充验收。
9. 可选 lens 生成同一快照的变化解释；PARTIAL/FAILED 保留真实状态，验证建议不是已执行测试。Web 只读，不触发 Primary，也不改变审批。
10. 文档变化先 reopen 协调，再 document 建新版本并 agree；旧版本及冻结文本保留。普通实现细节写入提交说明和 Align；关键设计/契约、范围或目标变化需要重新约定。

L1/L2/L3 控制文档覆盖，FAST/STANDARD/CRITICAL 控制检查强度，两者不自动一一对应。
原生工具顺序仍由当前 Codex 遵循协议；Harness 不监控其每次编辑，不接管原生会话。
统一入口见 [使用说明](../docs/UNIFIED_WORKFLOW.md)。UPSTREAM-AGENTS.md 是来源存档，不是第二个自动入口。
