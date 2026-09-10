# 来源、旧入口与历史基线

本文件登记当前试行稿的依据、实读范围、采用边界和旧入口去向。Workflow-SOP 基于既有工程经验整理，减少工具耦合与重复规则；来源不是机构认可、业务验证或效率收益证明。维护时按受影响主题复查，历史材料不自动作为当前操作规范。

## 固定来源

| 来源 | 使用内容与边界 |
|---|---|
| [Workflow-SOP 整理前基线](https://github.com/YIMO691/Workflow-SOP/tree/60ccde7bb1c05b90cb55c5ec603f14366d27ae64) | 原 SOP、代码/注释/分端规范、轻量记录和恢复经验；本轮不保留其固定 Gate 状态协议 |
| [Ares 流程讨论基线](https://github.com/YIMO691/ares-ai-engineering-harness/tree/f425027a31263cb4842c4c86f1d0448a8e89c2c3) | Context 来源、关键决策、直接协作、真实验证和模型自主原则；未导入 Web、CLI、数据库、Agent Framework 或 Change Lens 实现 |

来源仓库为私有，链接需要相应权限；本文件不扩大原内容的访问、复制或许可范围。本批按用户要求采用下列知识研究中的有限建议；知识库保留论证与证据边界，本 SOP 维护执行约定，其他研究与运行能力未自动接入。

## 2026-09-10 的学习采用

| 来源与固定位置 | 采用内容与限制 |
|---|---|
| [RR-025：规格澄清与变更](https://github.com/YIMO691/ares-ai-software-engineering/blob/86d9dd6cc30c3378b2fc1614a0031197d8f223f5/03_REFERENCE_RESEARCH/09_CROSS_RESEARCH/RR-025_SPEC_CLARIFICATION_AND_CHANGE.md) | 事实/目标区分、当前增量的实施条件与变更影响；本 SOP 不引入 Ares 专属 Ready/Run 状态 |
| [RR-026：Superpowers](https://github.com/YIMO691/ares-ai-software-engineering/blob/86d9dd6cc30c3378b2fc1614a0031197d8f223f5/03_REFERENCE_RESEARCH/09_CROSS_RESEARCH/RR-026_SUPERPOWERS_WORKFLOW.md) | 计划与规格冲突、完整任务审阅、恢复身份；局部辅助脚本观察不证明 Agent 运行失败 |
| [RR-027：需求技能与切片](https://github.com/YIMO691/ares-ai-software-engineering/blob/86d9dd6cc30c3378b2fc1614a0031197d8f223f5/03_REFERENCE_RESEARCH/09_CROSS_RESEARCH/RR-027_REQUIREMENT_SKILLS_AND_SLICING.md) | 场景辨义、依赖提问、行为切片与迁移例外；静态分析和未执行教学例子，不是效率验证 |
| [RR-020：超时与验收](https://github.com/YIMO691/ares-ai-software-engineering/blob/86d9dd6cc30c3378b2fc1614a0031197d8f223f5/03_REFERENCE_RESEARCH/09_CROSS_RESEARCH/RR-020_TIMEOUT_RECOVERY_AND_ACCEPTANCE.md) | 修正收藏示例中结果未知与明确失败的混淆；一次读取不能证明迟到写入不会发生 |
| [Superpowers 固定源](https://github.com/obra/superpowers/blob/b36e0829c6d0140e93cfef2ca599b1b07d4a7797/README.md) | v6.3.0；推荐子集、实读范围与差异见 [推荐目录](../skills/README.md) |
| [Matt Pocock 固定源](https://github.com/mattpocock/skills/blob/3cca18b368ae95cdbdebbff572ccafa662551015/README.md) | 需求/领域建模、规格与切片、测试与审阅；见 [推荐目录](../skills/README.md) |
| [FlameMida 固定源](https://github.com/FlameMida/spec-dev/blob/095eb40b94aceea7322332c5b00903776cdabd01/skills/requirement-analysis/SKILL.md) | 需求设计与现行条款；见 [推荐目录](../skills/README.md)，目录介绍不替代正文 |

本批直接复核推荐涉及的固定原文及版本，以自写说明、模板提示与教学例子落地；没有整包复制技能、安装插件或运行业务。研究报告所在 PR 的纳入状态不改变本 SOP 的试行定位；公司适用性仍需实际采用反馈。原始许可证与第三方声明继续由各固定来源解释，不因推荐扩大再分发权限。

## 技能阅读范围

本次只整合已有阅读记录，未增读实现或运行技能。推荐采用与差异见 [技能目录](../skills/README.md)，原详细卡片通过下方固定基线保留：

| 对象 | 已有实读范围与限制 |
|---|---|
| Superpowers v6.3.0 | 相关工作流技能、维护 PR 与辅助脚本见 RR-026。此前局部 helper 观察发现同名计划目录隔离边界；未运行 Agent，不能推断真实任务误读。参考设计、计划、诊断与完成证据，不默认导入强制批准、子代理编排或逐消息重验 |
| Matt Pocock 固定提交 | grill-me/grilling、grill-with-docs/domain-modeling、to-spec、to-tickets、tdd、code-review、wayfinder，范围见 RR-027。原版任务发布、目录与并行角色不自动采用；HEAD 差异审阅未必覆盖未提交改动；未纳入 in-progress 实验条目 |
| spec-dev v8.1.0 | requirement-analysis、澄清、场景、计划与执行的相关部分，以及漂移守卫主流程和 eval 输入/期望，范围见 RR-027。当前需求设计八步交接 writing-plans，不能用 Smithery 九阶段简介替代正文；文件同改不证明行为一致，eval 定义不是执行结果。兼容取舍需尊重仍有效的产品契约 |

上述原文可以支持作者如何组织工作这一判断，不能支持执行遵从率或生产效率提升。原许可证和第三方声明仍由固定来源解释，本仓库保留自写摘要与链接。

## 文档实践的定向核对

访问日期：2026-09-10。问题是现有 PRD/SDD 是否覆盖可验收需求、关键运行/部署场景和文档维护，而非寻找名称更新的一整套文件。以下是本 SOP 的直接资料核对与裁剪，不代表实证效果比较。

| 一手来源与版本 | 实际阅读范围、采用与限制 |
|---|---|
| [ISO/IEC/IEEE 29148:2018](https://www.iso.org/standard/72089.html) | 公开摘要与版本/生命周期信息：需求工程过程和信息项的范围；未读付费全文，不据此复制条款或宣称符合标准。页面仍列 2018 已发布版，并列 DIS 修订在研，草案不当现行要求 |
| [ISO/IEC/IEEE 42010:2022](https://www.iso.org/standard/74393.html) | 公开摘要与第二版信息：架构描述及其组织；摘要明确不规定开发方法、工具或文档介质，不能用它证明必须提交名为 SDD 的文件。未读全文 |
| [ISO/IEC/IEEE 29119-3:2021](https://www.iso.org/standard/79429.html) | 公开摘要与第二版信息：测试文档的范围；本轮未读其具体模板，不宣称本 TEST-PLAN 完整实现该标准 |
| [arc42 overview](https://arc42.org/overview/) 与 [质量要求说明](https://docs.arc42.org/section-10/) | 网页无固定版号；实读章节总览及第 10 节的质量场景说明。选取边界、运行、部署、质量、风险提示，以条件、刺激、响应和度量澄清验收；另读 [运行视图](https://docs.arc42.org/section-6/) 与 [部署视图](https://docs.arc42.org/section-7/) 的 Content/Form，分别采用代表性运行步骤与运行单元到设施的映射；未套用完整模板或验证架构质量 |
| [C4 diagrams](https://c4model.com/diagrams) 与 [Container 定义](https://c4model.com/abstractions/container) | 实读四层静态视图及辅助视图的选择说明、Container 定义与运行边界；来源说明无需用全四层。补读 [容器视图](https://c4model.com/diagrams/container) 与 [图形记法](https://c4model.com/diagrams/notation)，自绘导出系统的两张教学图，标明边界、类型、方向与未知技术；未盘点真实项目或评价绘图工具 |
| [MADR 4.0.0 模板](https://github.com/adr/madr/blob/4.0.0/template/adr-template.md) | 实读该版本完整模板，参考状态、背景、方案、后果与 confirmation 的内容；大部分元信息在原模板也可选，本 SOP 未导入 YAML 字段或多人审批表 |
| [Martin Fowler：Test Driven Development](https://martinfowler.com/bliki/TestDrivenDevelopment.html)（2023-12-11） | 实读方法说明与测试列表/失败测试—实现—重构循环，用于区分开发方法和文档名称；不是 TDD 对所有任务效率更高的证据 |
| [OpenAPI 3.2.0](https://spec.openapis.org/oas/v3.2.0.html)（2025-09-19） | 只读版本、What is the OpenAPI Specification 与文档状态；支持 HTTP 接口机器可读描述这一用途。未审计全部规范，不要求现有项目升级工具链或声称契约测试已通过 |

补充核对：[Google 的代码评审标准](https://google.github.io/eng-practices/review/reviewer/standard.html)，2026-09-10 实读标准、指导与争议处理段落，用于区分阻断项和可后续改进、以事实及持续代码健康支持取舍。它不代表本 SOP 已经通过团队采用验证。

由这些来源推得的本地建议是保留最小充分载体，补强内容及关系；这不是来源机构对本 SOP 的认可。后续项目出现漏验收、设计无法恢复、文档冲突，或相关来源正式更新且影响现用建议时，回到对应模板和条款修订；仅有新版本号不自动更换整套文档。

## AI 工具接入的定向核对

核对日期：2026-09-10。目的仅为让读者找到并验证项目指令入口；以下在线文档无固定产品版号，不据此宣称所有客户端行为一致。

| 一手来源 | 实际阅读范围与采用边界 |
|---|---|
| [Codex：Custom instructions with AGENTS.md](https://learn.chatgpt.com/docs/agent-configuration/agents-md) | 实读指令发现、项目分层、验证与排查；采用根指引、override 排查和新会话核对。未采用全局配置修改、放宽权限或自动安装策略 |
| [Claude Code：How Claude remembers your project](https://code.claude.com/docs/en/memory) | 实读项目文件位置、导入语法、AGENTS.md 兼容、目录加载与查看入口；采用根 CLAUDE.md 导入共用 AGENTS.md 的最小方式。未采用自动记忆、批量配置迁移或 hooks 方案 |

共用项目约定、只读检查和小任务试用是本 SOP 的本地适配建议；未在两个工具中运行加载或行为对照实验，不是官方认证。安装版本改变、路径或权限变化、漏加载及冲突反馈触发定向复查；只调整受影响入口，不因新版本重写核心流程。普通聊天的文件能力需按实际环境确认，不套用仓库型工具结论。

## GitHub 文档与协作模板的定向核对

核对日期：2026-09-10。以下为在线官方说明，无固定产品版号；用于本仓库的展示、路径和表单适配，不称为统一正文标准或 GitHub 认证。

| 官方来源 | 实读范围与采用边界 |
|---|---|
| [About READMEs](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-readmes) | 用途、开始方式、帮助/维护入口、标题导航与相对链接；据此整理首页，详细工程内容留在对应正文 |
| [PR 模板配置](https://docs.github.com/en/communities/using-templates-to-encourage-useful-issues-and-pull-requests/creating-a-pull-request-template-for-your-repository) | 支持路径、正文提示与默认分支生效条件；保留单份 Markdown PR 模板，不新增审批规则 |
| [Issue 模板配置](https://docs.github.com/en/communities/using-templates-to-encourage-useful-issues-and-pull-requests/configuring-issue-templates-for-your-repository)、[Issue 表单语法](https://docs.github.com/en/communities/using-templates-to-encourage-useful-issues-and-pull-requests/syntax-for-issue-forms) 与 [字段语法](https://docs.github.com/en/communities/using-templates-to-encourage-useful-issues-and-pull-requests/syntax-for-githubs-form-schema) | 实读目录/生效条件、name/description/body、markdown/textarea、id/attributes/validations；将原表单的信息拆成可回答的问题，不自动分派或新增标签 |

四阶段名称、中文记录名与按文档用途组织章节，是本 SOP 的本地编辑约定；对应写法见 [贡献指南](../CONTRIBUTING.md#文档职责与写法)。表单机制变化、断链、填写困难或描述冲突时定向修订；本地检查不证明 GitHub 线上表单已经启用。

## 旧入口去向

| 旧路径 | 当前入口 |
|---|---|
| SOP.md / TASK-LEVELS.md | [完整流程](WORKFLOW.md) 的过程、文档选择和结果判断 |
| AI-PLAYBOOK.md / AGENT-WORKSPACE.md | [工作指南](WORKFLOW.md) 的执行与恢复，以及 [工具接入](AI_COLLABORATION.md) |
| CODE-GUIDE.md / DOCUMENTATION-GUIDE.md | [ENGINEERING_RULES](ENGINEERING_RULES.md) 及 [模板入口](../templates/README.md) |
| START-HERE.md / prompts/ | [README 快速开始](../README.md#从这里开始) 和 AI 协作说明 |
| FORMAL-FEATURE / ALIGNMENT-GATE 模板 | [模板选择](../templates/README.md)，交付结论留在原任务/PR，与功能说明互相引用 |

旧路径从本分支主线移出，依赖旧路径的项目在采纳本版本时需更新引用；继续使用旧规范的项目可固定上面的旧 commit，不自动切换。L1/L2/L3 仅保留在旧例子的文件路径，不再用于选择一套文档；FAST/STANDARD/CRITICAL 不再是通用核心协议。

### 本次精简的旧入口

2026-09-10 较早批次曾调整为 AI 读取后执行、开发默认留痕和一份功能说明持续维护。其中单文档默认已被下方“双端功能的关联文档”修订替代；本节保留当时精简的来源与当前去向。这是工作约定变化，不是新增研究效果结论。整合前内容固定在 [daa538c 基线](https://github.com/YIMO691/Workflow-SOP/tree/daa538c5572c2bcff1a7e85624facf4898561dc6)，不改写历史或在当前树重复放归档副本。

| 旧入口或内容 | 当前去向 |
|---|---|
| docs/PRINCIPLES.md；AI_COLLABORATION 中的日常执行、文档、交接段落 | [工作指南](WORKFLOW.md)；AI_COLLABORATION 仅保留工具接入参考 |
| templates/TASK、PRD、SDD、TEST-PLAN、DELIVERY、ADR.md | [四类关联文档](../templates/README.md) 及 [按需方法](../templates/README.md#方法按需参考)，独立小修直接在原任务/PR 留痕 |
| WORKFLOW 原六图和可展开细节 | [一张主链路图](WORKFLOW.md#完整链路) 与同页条件说明；复杂分支回原工程规则查询 |
| L2-standard-feature 原完整运行演练及长示例 | [首次开发与后续修改](../examples/L2-standard-feature.md)，保留收藏失败/恢复的关键条件 |
| L3-complex-feature 的 PRD、SDD、TEST-PLAN、DELIVERY 和 adr/0001-use-async-export-job.md | [异步导出设计片段](../examples/L3-complex-feature/README.md)，保留取舍、关键风险及未验证边界 |
| skills/SUPERPOWERS、MATT_POCOCK、REQUIREMENT_ANALYSIS.md | [推荐目录](../skills/README.md) 与本页技能阅读范围；原文链接使用固定提交 |
| WORKFLOW 原“需求与现状”“约定与文档选择”“评审交付对齐与完成判断”等锚点 | [开始工作](WORKFLOW.md#开始工作)、[维护功能说明](WORKFLOW.md#维护功能说明)、[交付与后续](WORKFLOW.md#交付与后续) |

已有项目采用本版时更新入口和旧路径引用，保留其已有效的功能文档，不批量删除项目记录。需要详细旧写法时在上述固定版本查看对应路径，恢复时通过新提交/PR 实施。

## 双端功能的关联文档

2026-09-10 按用户明确的维护需求修订：新双端功能用功能说明作为统一入口，分别关联客户端实现、服务端实现和验证与验收；后续修改先梳理完整链路再更新受影响章节。这是使用约定调整，不是新增研究结论或已证明的效率提升；一端功能、小修和既有文档继续按实际职责裁剪。

被替代的单文档默认固定在 [3bcba48 基线](https://github.com/YIMO691/Workflow-SOP/tree/3bcba486634bb0a642e93c0aa86e84f7583cffec)。SPEC 保持路径，改为共同需求与导航入口；原“实现说明”去向为分端模板，原“验证与遗留”去向为验证模板，见 [模板目录](../templates/README.md)。WORKFLOW 原“四部分怎样更新”锚点改为 [各文档怎样更新](WORKFLOW.md#各文档怎样更新)，其余当前流程入口保留。

新增三个模板和三个收藏配套例子，仅用于体现不同维护职责与跨文档定位；未恢复整套旧 PRD/SDD/DELIVERY 文件。收藏仍是虚构、未执行示例；未纳入业务项目资料，未验证文档与真实产品的一致性。复查触发：实际采用出现找不到链路、重复维护、文档与代码漂移或分端职责不清时，以具体任务证据修订。

## 历史内容

旧 [研究](https://github.com/YIMO691/Workflow-SOP/tree/60ccde7bb1c05b90cb55c5ec603f14366d27ae64/research)、[试运行](https://github.com/YIMO691/Workflow-SOP/tree/60ccde7bb1c05b90cb55c5ec603f14366d27ae64/pilots)、[Gate 模拟器与夹具](https://github.com/YIMO691/Workflow-SOP/tree/60ccde7bb1c05b90cb55c5ec603f14366d27ae64/tests/workflow-gate) 留在该 Git 基线，不在新主线重复存放归档副本。

本轮移除模拟 Gate 的代码与对固定字段/措辞的 CI 要求。现有 CI 只查文档；这不授权业务项目跳过自己的测试、安全、审查或发布规则。没有重写 Git 历史、删除上游仓库、发布标签或宣布公司正式制度生效。

需要恢复旧内容时，可以在独立检出中读取固定基线，或通过新的提交/PR 恢复选定文件；不使用 force push 改写共享历史。历史版本说明保留在 [CHANGELOG](../CHANGELOG.md)。
