# L3 示例：异步报表导出

本示例演示复杂功能应如何拆分文档，而不是规定所有 L3 项目必须使用相同章节。

## 为什么是 L3

- 涉及 Web、API、任务执行器和对象存储多个系统。
- 导出数据受用户权限约束，并可能包含敏感字段。
- 大数据量任务存在超时、重试、重复请求和容量风险。
- 同步生成与异步任务之间存在需要长期保留的技术选择。

## 文档

- [PRD](PRD.md)：用户能力、范围和验收标准。
- [SDD](SDD.md)：任务模型、权限、状态和失败恢复。
- [TEST-PLAN](TEST-PLAN.md)：跨层、故障和安全验证。
- [DELIVERY](DELIVERY.md)：实施、测试、偏移、Align 和最终交付决定的唯一汇总。
- [ADR-0001](adr/0001-use-async-export-job.md)：采用异步任务的决定。
- [ALIGNMENT-GATE](ALIGNMENT-GATE.md)：演示独立审计场景；没有真实实现证据时 Gate 必须保持 Fail。

真实项目中，实施结果和交付检查应继续关联到同一个任务与 PR/MR。通常只使用 DELIVERY 完成 Align；只有审计要求才额外保留独立 ALIGNMENT-GATE。
