# 工程工作流

本文描述当前推荐的 Codex Direct 模式。命令和请求结构见 [CLI 参考](docs/CODEX_DIRECT.md)，详细文档规则见 [统一流程](docs/UNIFIED_WORKFLOW.md)。

## 职责

| 参与方 | 职责 |
|---|---|
| Owner | 提供需求、澄清意图、授权边界和最终验收 |
| 当前原生 Primary | 与 Owner 讨论，核对上下文，整理文档，实施、返工与 Align |
| Harness | 冻结约定、安排检查、保存证据、校验变更与真实决定 |
| 独立 Reviewer | 只读审查要求、实现和证据，返回发现 |
| Web / Change Lens | 可选只读观察与变化解释，不代替执行或验收 |

## 主流程与产物

| 阶段 | 操作 | 留痕 |
|---|---|---|
| Discussing | 核对策划来源、上下文和待确认问题；`document` | L1 TASK；L2 SPEC；L3 PRD、SDD、TEST-PLAN |
| Ready | `agree` 冻结 Goal、Acceptance、Non-goals、Boundary、Key Decisions、Verification 和实际授权 | 文档版本、约定、策略与源码基线 |
| Implementing | `begin`；当前 Primary 按可验证增量修改 | 实际改动和决定；不启动替代 Primary |
| Submitted / Checking | `submit` → `verify` | 提交源码指纹、构建/测试、审查和每次 Run 的证据 |
| Rework / Blocked | 修复、重报或核对环境后重验 | 失败原因、返工和恢复记录；范围变化重新约定 |
| AwaitingAcceptance | `align`；可选 `lens` | AC 与当前证据映射、偏移/清理；L1/L2 更新原文档，L3 DELIVERY |
| Done | 实际 Owner 验收后 `accept` | 验收决定；不会自动 push、merge 或 release |

Align 是交付检查，不是独立的 Task 状态。新任务检查完成后仍需 Align；观察台可以显示“等待交付对齐”。文档、源码或证据变化会使相关操作被拒绝，不能沿用失效结果。

## 检查强度

| 工作流 | 实施前 | 提交后 |
|---|---|---|
| FAST | 已冻结约定与实际授权 | Build/Test → Align → Owner 验收；不启动 Reviewer |
| STANDARD | 已冻结约定与实际授权 | Build/Test → 独立 Reviewer → Align → Owner 验收 |
| CRITICAL | 额外记录对冻结写入边界的明确授权 | 与 STANDARD 相同；风险授权不代替最终验收 |

L1/L2/L3 与风险等级不一一对应。SOP 的 Build 表示适用任务的设计确认；工具 `build` 表示编译检查，两者含义不同。

## 返工与恢复

测试或 Reviewer 发现问题后返回当前 Primary，按 `begin → 修复 → submit → verify` 处理，每次验证创建新的 Run。Direct 模式不承诺自动同 Run 重放、固定两次返工预算或会话透明续接。

环境故障且提交源码未变时可重试 `verify`；源码变更需重新提交。目标、边界、指令或项目策略变化先 `reopen`，协调后生成新文档版本并 `agree`。硬崩溃先核对残留子进程，再 `recover`，保留原始证据。

## 兼容路径

旧 v0.2 角色顺序和 Web Workflow Fusion 只适用于已有兼容记录；其恢复与返工语义不应套用到 Direct。`ObserverOnly=false` 才启用旧 Web 执行入口，不能与 Direct 写入并行。历史说明见 [ADR 0002](docs/decisions/0002-workflow-fusion.md) 和 [Phase 1 基线](docs/ARES_AGENT_ENGINEERING_WORKBENCH_BASELINE_v1.0.md)。

## GitHub 交付

需要提交到仓库时，Issue/PR 保存安全需求摘要、版本、AC、检查/审查范围、Align 和实际合并授权。原始过程证据留在本地。业务验收与 GitHub 合并是不同决定，见 [GitHub 规范](docs/GITHUB_WORKFLOW.md)。
