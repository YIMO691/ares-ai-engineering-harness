# 按问题选择技能

遇到具体卡点时参考一个合适的方法，结果写回对应的功能说明、端实现或验证记录。这里是推荐目录，未自动安装、加载或验证业务效果；没有实际启用时称为参考阅读。明确的小任务直接按 [工作指南](../docs/WORKFLOW.md) 推进。

## 推荐目录

| 当前问题 | 固定来源 | 输入与应留下的结果 |
|---|---|---|
| 想法模糊、关键取舍未定 | Matt：[grill-me](https://github.com/mattpocock/skills/blob/3cca18b368ae95cdbdebbff572ccafa662551015/skills/productivity/grill-me/SKILL.md) / [grill-with-docs](https://github.com/mattpocock/skills/blob/3cca18b368ae95cdbdebbff572ccafa662551015/skills/engineering/grill-with-docs/SKILL.md)（常被称为 grillwithme） | 任务与项目事实 → 当前目标、术语、决定与剩余缺口 |
| 跨模块需求需要比较方案 | [requirement-analysis](https://github.com/FlameMida/spec-dev/blob/095eb40b94aceea7322332c5b00903776cdabd01/skills/requirement-analysis/SKILL.md) 或 Superpowers [brainstorming](https://github.com/obra/superpowers/blob/b36e0829c6d0140e93cfef2ca599b1b07d4a7797/skills/brainstorming/SKILL.md) | 需求、契约与约束 → 方案理由、范围和验收场景 |
| 需求明确，需要安排增量 | Matt [to-tickets](https://github.com/mattpocock/skills/blob/3cca18b368ae95cdbdebbff572ccafa662551015/skills/engineering/to-tickets/SKILL.md) 或 Superpowers [writing-plans](https://github.com/obra/superpowers/blob/b36e0829c6d0140e93cfef2ca599b1b07d4a7797/skills/writing-plans/SKILL.md) | 当前说明与依赖 → 可验证行为、必要接口和检查；不预写未知实现 |
| 反复修补仍不知原因 | Superpowers [systematic-debugging](https://github.com/obra/superpowers/blob/b36e0829c6d0140e93cfef2ca599b1b07d4a7797/skills/systematic-debugging/SKILL.md) | 症状、时序与实际错误 → 区分原因的检查及有依据的修复 |
| 准备交付，需要核对依据 | Superpowers [verification-before-completion](https://github.com/obra/superpowers/blob/b36e0829c6d0140e93cfef2ca599b1b07d4a7797/skills/verification-before-completion/SKILL.md) | 当前改动和验收 → 实际证据、版本与缺口 |
| 测试或评审缺少判断依据 | Matt [tdd](https://github.com/mattpocock/skills/blob/3cca18b368ae95cdbdebbff572ccafa662551015/skills/engineering/tdd/SKILL.md) / [code-review](https://github.com/mattpocock/skills/blob/3cca18b368ae95cdbdebbff572ccafa662551015/skills/engineering/code-review/SKILL.md) | 可观察行为、规格与完整改动 → 有独立预期的检查、具体缺陷与重验范围 |

## 使用边界

- 沿用已有决定与功能文档，不把推荐依次跑一遍。外部技能的文件布局、额外审批、代理数量和发布操作不自动成为项目要求；不相容时只参考适用方法，或明确适配后再启用。
- 实际安装、自动触发和依赖按所用工具及用户授权处理，推荐名称不是跨工具通用命令。原版 to-spec/to-tickets 的外部发布不因阅读获授权；未提交改动也不能假定已被基于 HEAD 的评审覆盖。
- 验证按真实行为、版本、环境与影响范围选择；不因某方法偏好重复运行仍适用的检查，也不省略数据库、兼容或关键权限验证。不得照抄会暴露环境变量值的诊断例子。

## 来源与版本

本目录整合 2026-09-10 已有阅读记录，未新增运行实验。Superpowers 为 v6.3.0；Matt 与 spec-dev 使用上表固定提交，spec-dev 为 v8.1.0。grill-me 组合 grilling，grill-with-docs 还涉及 domain-modeling，正式启用需核对依赖。Smithery 目录简介不替代 spec-dev 当前正文。

实读范围及限制集中在 [来源记录](../docs/SOURCES.md#技能阅读范围)，详细研究由其中 RR-026/027 承载；旧卡片见固定历史入口。只保留自写摘要与链接，原许可和第三方声明继续有效。

## 版本更新与试用反馈

沿已有双周学习安排复查，目录本身不创建提醒或自动更新。只有新版本影响当前做法，或真实任务出现重复劳动、遗漏、交接问题时，才调整推荐并记下版本差异与理由。实际使用反馈留在原任务，说明用了什么、改变什么及观察依据；没有收益可以撤回，一次任务不证明普遍效率提升。
