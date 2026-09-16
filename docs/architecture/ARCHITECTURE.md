# 当前架构

## 范围

当前推荐入口为 Codex Direct + Observer。Owner 与现有原生 Primary 直接讨论和实施；Harness 不启动、附着或恢复这个 Primary。旧 v0.2 与 Web Fusion 仅作为兼容路径保留。

最终目标是减少 Owner 协调负担并获得可靠、可维护的工程交付。[最终 Harness v2](../TARGET_HARNESS.md)的五部分职责由原生 Agent、当前控制机制、工程方法和后续能力共同承担，不对应五个已实现服务。当前组件实现约定、权限、版本、验证与证据；需求分流和质量判断主要由 Primary/适用 Reviewer 按文档执行。自动模式管理、结构化质量支持、领域校准和真实评测仍见[能力差距](../TARGET_HARNESS.md#11-当前实现映射与演进次序)。

## 设计原则

1. 原生 Agent 调查、设计、编辑和返工；Harness 管理约定、边界、版本与真实结果。
2. 冻结意图和验收，保留授权范围内的实现自由度；工程可查或局部可逆的问题先由 Primary 处理。
3. 关键业务取舍、越权动作和实际验收由相应负责人决定，不把所有未知一律升级给人。
4. 文档指导与程序强制区分；适用规则必须有项目依据，质量自检不等于已实现机械门禁。
5. 复用原生能力和既有工具；新增抽象、状态、依赖或控制机制说明当前必要性。
6. 确定性检查提供事实，按模式启用独立只读 Reviewer；质量判断不能抵消正确性失败。
7. 保留真实失败、耗时、限制和恢复证据；不制造 PASS 或 Owner 决定。
8. 以质量、维护性、人的负担和总成本评价改进，收益由真实任务证明。

## 组件与职责

| 组件 | 责任 |
|---|---|
| Domain | Task、DirectAgreement、DocumentSet、Run、Align 与 Lens 等业务契约 |
| Application | 生命周期、约定冻结、检查编排、证据校验和 Owner 决定 |
| Infrastructure | SQLite、文档/证据文件、源码快照和受限命令执行适配 |
| AgentFramework adapter | 运行现有工作流定义，不替代 Codex 原生工具循环 |
| Codex adapter | 配置和调用官方 CLI，执行独立只读 Reviewer |
| CLI | 接受由当前 Primary 整理的业务请求，不依赖 Web |
| Web | 默认只读观察 Task、文档、运行证据、Align 和 Lens 报告 |
| 可选 ANGE Eval | 独立 Python 只读工具；检查显式协议/运行记录并输出描述性统计，不接管执行、不写数据库或验收状态 |

实现入口：[DirectWorkflowService](../../src/Ares.Workbench.Application/DirectWorkflowService.cs)、[DirectCli](../../src/Ares.Workbench.Cli/DirectCli.cs)、[DirectDocuments](../../src/Ares.Workbench.Infrastructure/DirectDocuments.cs)。

## 数据与验证

SQLite 保存增量业务 JSON，旧记录继续可读。讨论文档写入 DocumentsRoot，Ready 保留冻结文本、版本、基线与真实授权。原始证据写入 DataRoot 的 artifacts，运行输出与缓存使用 ScratchRoot，均不纳入 Git。

Direct 每次 verify 使用独立 Run，执行提交证据、Build/Test、可选独立 Reviewer 和交付节点。修复发现交回当前 Primary，不在 Harness 中启动替代实现代理。Align 绑定当前 Run 的成功证据并核对文档、源码和原始证据完整性；它不能自行证明自然语言验收结论。

Change Lens 通过现有分析脚本和外部构建 Worker 读取冻结 Git HEAD 到提交 WORKTREE 的变化，保存解释附件及真实状态，不修改目标源码或审批。

[ANGE Eval](../../tools/ange-eval/README.md)仅读取调用者提供的 JSON。协议哈希绑定运行配置及计划 assignment，报告失败、缺失和偏离；证据引用不自动解引用，真实数据不进入 Git。工具不引入新 Agent runtime，也不自动把 Direct 的阶段时间解释为人的活跃工作时间。

## 权限与恢复

CLI 写操作与旧 Web 执行使用独占 writer lease。默认 Observer 不运行 RunWorker 或启动恢复，不接管 Primary。外部编辑器仍可改动源码，指纹检查提供失效检测而非逐文件 OS 权限隔离。

CRITICAL 在外部实施前记录冻结边界授权；最终验收是另一个决定。硬中断需核对进程、源码和状态，再 recover/重验；没有透明会话接管或完整 Primary 工具遥测。

## 历史兼容

Web Fusion 曾由宿主启动讨论并按观测到的原生 thread ID 继续实施，其对话账本只用于审计；该机制不描述当前 Direct Primary。差异见 [ADR 0002](../decisions/0002-workflow-fusion.md)、[ADR 0003](../decisions/0003-codex-direct-observer.md) 与 [工作流](../UNIFIED_WORKFLOW.md)。
