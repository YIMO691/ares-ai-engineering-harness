# 当前架构

## 范围

当前推荐入口为 Codex Direct + Observer。Owner 与现有原生 Primary 直接讨论和实施；Harness 不启动、附着或恢复这个 Primary。旧 v0.2 与 Web Fusion 仅作为兼容路径保留。

完整目标参考架构见[目标 Harness](../TARGET_HARNESS.md)，包含文档/证据依赖、模型工作包、变化传播、交付运行与评估；其[当前实现映射](../TARGET_HARNESS.md#11-当前实现映射与演进次序)说明差距。本文继续只描述现有组件与运行行为，目标设计不等于已实现能力。

## 设计原则

1. Keep control and execution responsibilities separate.
2. Native capability first; use a thin adapter before adding new infrastructure.
3. Freeze intent and acceptance, leave implementation details flexible.
4. Separate natural-language guidance from enforced permission boundaries.
5. Use deterministic verification and independent read-only review.
6. Human judgment owns ambiguity, Ready and final acceptance.
7. Preserve real failure, timing and recovery evidence; never manufacture PASS.
8. Measure Owner coordination cost before expanding the platform.

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

实现入口：[DirectWorkflowService](../../src/Ares.Workbench.Application/DirectWorkflowService.cs)、[DirectCli](../../src/Ares.Workbench.Cli/DirectCli.cs)、[DirectDocuments](../../src/Ares.Workbench.Infrastructure/DirectDocuments.cs)。

## 数据与验证

SQLite 保存增量业务 JSON，旧记录继续可读。讨论文档写入 DocumentsRoot，Ready 保留冻结文本、版本、基线与真实授权。原始证据写入 DataRoot 的 artifacts，运行输出与缓存使用 ScratchRoot，均不纳入 Git。

Direct 每次 verify 使用独立 Run，执行提交证据、Build/Test、可选独立 Reviewer 和交付节点。修复发现交回当前 Primary，不在 Harness 中启动替代实现代理。Align 绑定当前 Run 的成功证据并核对文档、源码和原始证据完整性；它不能自行证明自然语言验收结论。

Change Lens 通过现有分析脚本和外部构建 Worker 读取冻结 Git HEAD 到提交 WORKTREE 的变化，保存解释附件及真实状态，不修改目标源码或审批。

## 权限与恢复

CLI 写操作与旧 Web 执行使用独占 writer lease。默认 Observer 不运行 RunWorker 或启动恢复，不接管 Primary。外部编辑器仍可改动源码，指纹检查提供失效检测而非逐文件 OS 权限隔离。

CRITICAL 在外部实施前记录冻结边界授权；最终验收是另一个决定。硬中断需核对进程、源码和状态，再 recover/重验；没有透明会话接管或完整 Primary 工具遥测。

## 历史兼容

Web Fusion 曾由宿主启动讨论并按观测到的原生 thread ID 继续实施，其对话账本只用于审计；该机制不描述当前 Direct Primary。差异见 [ADR 0002](../decisions/0002-workflow-fusion.md)、[ADR 0003](../decisions/0003-codex-direct-observer.md) 与 [工作流](../UNIFIED_WORKFLOW.md)。
