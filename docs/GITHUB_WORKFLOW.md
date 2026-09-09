# GitHub 协作与合并规范

Owner 与当前 Codex 直接讨论；Codex 从实际讨论维护文档、Issue 和 PR。GitHub 保存可提交的需求摘要、代码、规范与交付记录；Web 只读观察。工程执行规则见 [统一流程](UNIFIED_WORKFLOW.md)，本文约定 GitHub 留痕和合并方式。

## 流程与记录

| 阶段 | 必要动作 | GitHub 记录 |
|---|---|---|
| 需求与讨论 | 读取真实来源和上下文，确认目标与问题 | 非简单任务建立 Issue，记录安全来源引用、目标、边界、验收 |
| 文档与 Ready | 按 L1/L2/L3 形成文档，冻结六项意图和真实授权 | Issue 记录 Task ID、文档版本或安全引用、验收项及授权来源 |
| 实施 | 在 feature/* 或 fix/* 分支按可验证增量开发 | 提交描述实际行为；重大边界决定才写 ADR |
| 验证与审查 | 执行统一检查；STANDARD/CRITICAL 保留独立 Reviewer | PR 记录实际命令结果、所审 revision、失败/返工和限制 |
| Align | 对齐验收项与当前证据，解决文档与实现偏移 | PR 汇总 AC → 证据、偏移与遗留；源码变化后重验 |
| 合并 | 最终 revision 的 CI 成功、阻断问题已处理、Owner 明确授权 | 通过 PR 合并到 main，关联关闭 Issue；合并不自动发布版本 |

小型文档更正可直接用 PR 留痕。L1/L2/L3 控制文档深度，FAST/STANDARD/CRITICAL 控制检查强度，两者不自动对应。

## 文档与过程文件

- 规范、模板、设计说明和代码属于仓库；任务讨论结论按实际项目授权决定是否可提交。
- 本机任务文档、冻结版本、原始日志、审查输出和测试证据留在授权任务目录。GitHub 只写安全版本、Run ID、摘要和限制；本机路径不能冒充远程可访问证据。
- 原始商业策划、商业源码、认证、SQLite、artifacts/evidence、缓存和运行配置不得上传。不要为了填写 PR 而复制它们。
- 原生完整对话不自动归档。Context 记录观察和来源，关键决定记录理由；未确认内容保留为待确认。

## 分支、检查与合并

main 保存可恢复的集成结果。开发使用 feature/*、fix/*；文档使用 docs/*。提交采用 feat/fix/test/docs/chore/refactor 等前缀，描述最终变更。

根 GitHub Actions 的 Build and test 执行 scripts/Test-Unified.ps1，覆盖 Harness、SOP 与 Change Lens。导入目录中的 .github 文件作为来源快照保留，不是本仓库自动运行的工作流。

PR 说明真实独立审查及范围。原生 Reviewer 的结论写入 PR 摘要，不冒充另一 GitHub 账号审批；样例审查通过不等于整个集成 PR 已被独立审查。缺口、跳过和 PARTIAL 必须明示，交给 Owner 判断。

合并前核对最终 PR revision、全部必需检查、未解决问题和实际授权。Owner 明确说“合并”可作为本次授权，无需反复询问。禁止 force push、绕过检查和无人值守自动合并。使用普通 merge commit 保留历史；不因合并自动打 tag、release 或部署。

这些是执行规范，不能声称等同于已启用 GitHub 服务端分支保护。服务端设置以实际仓库状态为准。

## 完成与恢复

合并后检查远程 main 包含 merge commit，本地 main 只做 fast-forward 同步，记录 PR、commit、CI 和限制。合并授权仅用于对应 GitHub 变更，不能代替业务样例最终验收。

阻塞时保留失败原因，修复后重验；范围变化回到讨论并更新冻结文档。回滚使用新的 revert 提交和 PR，保留历史，不 reset/force push main。
