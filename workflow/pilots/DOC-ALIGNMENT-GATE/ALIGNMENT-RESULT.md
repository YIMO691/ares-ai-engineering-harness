---
id: FLOW-0001-ALIGN
title: 文档对齐 Gate 试运行对齐结果
status: passed
owner: 工作流维护者
updated: 2026-09-01
related: [FLOW-0001]
---

# 1. 权威来源

| 信息类型 | 本任务权威来源 | 最终实现/证据 |
| --- | --- | --- |
| 需求与验收 | PRD.md | README、SOP 和模板修改 |
| 关键设计 | SDD.md、ADR-0001 | SOP 的分层权威、偏移处理和分级执行 |
| 流程接口 | SDD 的 Gate 输入/输出 | DELIVERY-CHECKLIST、ALIGNMENT-GATE 模板 |
| 测试与结果 | TEST-PLAN.md | 结构检查、链接检查和三级桌面演练 |

# 2. 验收标准对齐

| AC | 最终结果 | 证据 | 结果 |
| --- | --- | --- | --- |
| AC-01 | 主流程已加入 Align | README 流程图、SOP 第 8 节 | Pass |
| AC-02 | 已采用分层权威 | SOP 8.1、ADR-0001 | Pass |
| AC-03 | L1/L2 只在原文档中记录结论 | TASK-LEVELS、L1/L2 示例 | Pass |
| AC-04 | 已定义五类偏移处理 | SOP 8.3、SDD 第 6 节 | Pass |
| AC-05 | 已走完当前流程并形成报告 | 本目录全部文档、PILOT-REPORT | Pass |

# 3. 偏移与处理

| 偏移 | 分类 | 修正方式 | 状态 |
| --- | --- | --- | --- |
| 原 Done 只有“文档已同步” | 设计缺口 | 增加 Align Gate 和细化检查 | Closed |
| “文档权威”范围不清 | 决策缺口 | 增加分层权威 ADR | Closed |
| TEST-PLAN 重复 SOP 的 TDD 规则 | 文档重复 | 改为只记录例外 | Closed |
| L1/L2 若新增报告会变重 | 流程风险 | 默认并入交付清单 | Closed |
| L3 示例没有真实证据却可能被误读为完成 | 验证缺口 | 新增 Fail 状态的对齐示例 | Closed |

# 4. Gate 结论

- 结果：`Pass`
- 结论：本流程功能的需求、设计、模板改动和验证结果一致，没有未解决的权威冲突。
