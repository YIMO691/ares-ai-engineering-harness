---
id: FEAT-0000
title: 正式功能文档入口
level: L3
status: draft
owner: 待填写
updated: YYYY-MM-DD
---

# 正式功能文档入口

本目录用于需要完整正规流程的 L3 功能。模板完整不代表每一节都必须填写：保留必填项，按实际风险填写条件项，无关内容删除或标记 `N/A：原因`。

## 1. 功能摘要

- 任务或项目：
- 当前 Gate：`Ready / Build / Loop / Align / Done`
- 当前结论：
- 下一步：

## 2. 文档与权威范围

| 文档 | 权威内容 | 状态 |
| --- | --- | --- |
| [PRD](../PRD.md) | 目标、范围、业务规则、验收标准 | Draft/Approved |
| [SDD](../SDD.md) | 关键设计、失败、兼容、迁移和回滚 | Draft/Approved |
| [TEST-PLAN](../TEST-PLAN.md) | 测试策略、场景、映射和退出条件 | Draft/Approved |
| [DELIVERY](../DELIVERY.md) | 最终实现、测试结果、偏移、Align 和交付决定 | Draft/Final |
| [ADR](../ADR.md) | 重大、长期或难逆的技术决定 | 按需 |

## 3. 推荐落地目录

```text
docs/features/FEAT-XXXX/
├── README.md
├── PRD.md
├── SDD.md
├── TEST-PLAN.md
├── DELIVERY.md
└── ADR/
    └── ADR-0001.md
```

使用时复制本入口和上表模板到功能目录。若项目已有任务系统，可以删除重复的负责人、评审人和状态字段，但不得删除目标、边界、验收、关键设计、验证和最终对齐内容。

## 4. 正式流程

```text
PRD → Ready
SDD + TEST-PLAN → Build
开发与验证 Loop
DELIVERY → Align
Align Pass → Done
```

重大决策才创建 ADR。原始测试日志、构建产物和截图保存在流水线或证据目录，DELIVERY 只记录结论和可定位链接。

## 5. 当前阻断项

- 无，或填写会阻止进入下一 Gate 的事项。
