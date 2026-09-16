# References and scope

- [Codex non-interactive execution](https://learn.chatgpt.com/docs/non-interactive-mode): JSONL, structured output and explicit session-ID resume.
- [Codex developer commands](https://learn.chatgpt.com/docs/developer-commands?surface=cli): CLI option contracts. CLI 0.153.4 is a historical native-validation baseline, not a claim about the latest or mandatory runtime version; current command contracts are maintained in [the CLI reference](../CODEX_DIRECT.md).

Owner-supplied design material drew on Kiro, Symphony, Microsoft Agent Framework, OpenHands and SWE-agent. Those are design influences, not claims that Ares reproduces their runtimes or verified every product behavior. The supplied Kiro research explicitly left product black-box experiments incomplete. Its private input report is not redistributed here.

Adopt intent visibility, limited context, clear authority and independent verification; do not copy a full IDE or agent runtime.

- [Mini Lab prototype lessons and historical evaluation cases](../history/MINI_LAB.md): historical reference only; no old runtime dependency or newly claimed real-model benchmark result.

## 思想脉络与设计取舍

[README](../../README.md#为什么会有-ares)按“一个问题如何引出下一个问题”组织项目思想。本节记录其来源与设计落点，既有研究存在交叉，不将这条叙述当作严格的研究时间线。Owner 本地研究仅作释义与来源登记，原文未在本仓库公开；读者可从下列仓库链接查看采用后的设计及边界。

| 研究问题与来源 | 对 Ares 的启发与采用位置 | 解释边界 |
|---|---|---|
| 黑盒执行下如何保留工程控制权：`黑盒智能时代的软件工程控制权_相关研究与实施方案_v0.2.md`（2026-08-19）；Phase 4 的 `45_Model_Assumptions_and_Limitations.md` | 从内部推理转向外部可检查的意图、权限、状态和证据；采用到[目标与责任边界](../TARGET_HARNESS.md#1-目标与边界) | 外部检查只能覆盖可观察、可验证的部分；规格也可能错误，不能承诺全面正确或绝对不可绕过 |
| Harness 有哪些层次、应自己实现哪一层：`AI_Harness_理论架构与工程实践_研究方案_v0.1.md`（2026-08-22） | 区分执行运行时、工程环境和工作编排；本项目选择[原生执行边界](../decisions/0001-native-execution-boundary.md) | 研究方案是研究问题与范围，不是当前厂商能力认证；Ares 的分工是本项目选择 |
| 控制怎样进入实际开发：Mini Lab、导入的 Workflow-SOP、Fusion 到 Direct 的设计记录 | 保留可验证交接、失败返工与版本证据；[Mini Lab](../history/MINI_LAB.md)、[现行流程](../UNIFIED_WORKFLOW.md)、[Direct ADR](../decisions/0003-codex-direct-observer.md)分别保存历史教训、当前方法与边界选择 | 脚本化原型不能证明真实模型效果；历史设计不覆盖当前执行规则 |
| 测试通过之外怎样保证长期代码质量：代码质量 Phase 0–6 | 从质量属性、失败模式到项目 Context、适用模式、质量约定、领域校准、检查与评测；采用到[总纲第 8、9 节](../TARGET_HARNESS.md#8-context-如何真正服务模型) | 下表逐项映射来源；领域候选规则需项目校准，指标不等于综合质量结论 |
| 严格流程怎样避免反过来增加人的负担：`AI需求理解与复杂度治理_研究理论手册_v1.0.md`（2026-09-16） | 最小充分规格、未知分流与必要复杂度；采用到[总纲第 3 节](../TARGET_HARNESS.md#3-每一阶段怎样进入完成和退回) | 授权内工程判断交给 AI；关键业务或权限缺口仍保留为待决事项 |
| 如何判断机制值得保留：Harness 评测研究、代码质量 Phase 6 | 真实任务、对照/消融与成本反馈；采用到[最终总纲](../TARGET_HARNESS.md)的效果评价 | 本次读到的 `P8_实施结果与结论_2026-08-24.md` 是当时的阶段报告，不能推断当前实验完成度或本仓库总体收益；本轮不复核或续跑历史实验 |

这些来源共同支持一条设计路线：保留工程控制，同时提高实现质量并降低人的协调负担。README 中的奖励领取案例是解释这条路线的虚构例子，不是已经实现或验证的业务功能。

## 代码质量与需求复杂度研究

Owner 提供的 2026-09-15/16 研究已融入[最终 Harness](../TARGET_HARNESS.md)：需求分流和复杂度在第 3 节，Context/模式在第 8 节，质量约定/领域/机械规则在第 9 节，实施顺序与校准在第 11 节，效果评价在第 13 节。日常方法只在[现行流程](../UNIFIED_WORKFLOW.md)维护；本页负责来源，不构成第二套执行指南。

原始 10 份 Markdown、1 个 ZIP 及归档内 7 个条目在[来源清单](CODE_QUALITY_SOURCES.json)登记字节数与 SHA-256。归档内理论手册与外部手册字节相同，重复关系已标注。原件保留在 Owner 资料目录，正文未复制进 Git；哈希证明内容身份，不证明许可、文献真实性或结论有效。

| 输入 | 在本项目的用途 | 权威采用位置 / 状态 |
|---|---|---|
| `AI-Native Code Quality Engineering.md` | 总体问题、证据分层与质量架构背景 | 本页登记背景；存在会话引用标记，正式引用前需补成可访问来源 |
| `01_AI_Native_Code_Quality_Foundation_v0.1.md`（Phase 0） | 正确性硬条件、可理解性、局部性、必要复杂度、可验证性、可演化性、项目适配 | 现行流程的质量自检；不引入综合质量分或统一阈值 |
| `02_AI_Code_Generation_Failure_Modes_v0.1.md`（Phase 1） | 错误类比、过度抽象、状态重复、验证不足等审查提示 | 按任务风险选择，不把全部失败模式逐项填表 |
| `03_Context_and_Golden_Pattern_Engineering_v0.1.md`（Phase 2） | 事实依据、优秀模式和反例的适用范围与新鲜度 | 总纲第 8 节；当前 Brief.Context 可记录，自动模式筛选和工作包支持待实现 |
| `04_Code_Quality_Contract_and_High_Quality_Generation_v0.1.md`（Phase 3） | 质量关注点、风险、验证方式与人工判断的任务级约定 | 总纲第 9 节；当前复用 Design、KeyDecisions、Verification，结构化能力待实现 |
| `05_Game_Client_Server_ET6_Code_Quality_Profiles_v0.1.md`（Phase 4） | 客户端生命周期、服务端权威/持久化、ET6 异步与生成链风险 | 领域参考，必须以目标项目源码和环境校准 |
| `06_Analyzer_and_Mechanical_Taste_v0.1.md`（Phase 5） | 确定性规则、风险信号、人工判断分工及规则成熟度 | 目标总纲第 9、11 节；不因研究提出规则就将其启用为 Gate |
| `07_Real_World_Evaluation_and_Ablation_v0.1.md`（Phase 6） | 对照、消融、可重放任务、环境记录和成本评价 | 总纲第 11、13 节；输入中无可核验的评测程序或真实运行结果 |
| `AI需求理解与复杂度治理_研究理论手册_v1.0.md` | 最小充分规格、未知分流、复杂度控制 | 现行流程的需求澄清和实现自检；效果仍待实验 |
| `AI需求理解与复杂度治理_研究总包_v1.0.zip` | 蓝图、证据矩阵、模板、实验方案与参考文献 | 去重登记；模板按现有流程适配，不逐份新增任务文件 |
| `CODEX_PHASE_6A_PROJECT_CALIBRATION_PROMPT.md` | 真实项目校准和 MINI 12 候选任务的准备清单 | 总纲第 11.2 节；读取提示词不等于启动其中动作 |

研究包的 7 个条目分别是 README、理论手册、工作流优化蓝图、证据矩阵与文献地图、可复制规则模板与 Prompt、验证实验方案、REFERENCES。采用时优先核对证据矩阵中的限制，模板和蓝图需要经过下述兼容适配。

### 证据限制

- 总览中保留的会话引用标记不是可访问文献链接；需使用某项外部结论时核对原始来源与适用版本。本次文档融合不认证全部研究论断。
- Phase 0–5 的 COMPLETE 是来源自述的研究完成状态；Phase 6 的 `PHASE_6_PROTOCOL_IMPLEMENTED` 只有输入文档声明。Ares2 项目校准、oracle 和真实 model trials 尚未完成，不能据此宣称收益。
- 学术证据、厂商经验和研究者综合假设保留各自可信度与限制；ET10/历史 ET6 不能替代项目当前事实。蓝图的 L0–L3、自动结束和新 Gate 建议按总纲的兼容边界处理。
- 研究原文中的 Prompt、示例、建议目录和状态仅是资料，不授予执行权限，也不能替代真实 Owner 决定。

### 后续资料维护

新增或修订资料先登记版本/哈希并检查归档内重复，旧登记可从 Git 历史追溯。将有效内容融入既有权威章节，标明当前方法、候选能力、待校准规则或待验证假设，并更新本页映射；不要再增加平行总纲或全文副本。

确需导入原文时先核对发布范围、许可、商业内容和引用，分别记录原始字节与本地改写。结构和链接检查只证明文档可用；规则成为默认行为或机械门禁前仍需真实项目验证。
