# 贡献指南

本仓库维护 Ares Harness、已导入的 SOP 和 Change Lens 集成。先阅读 [项目说明](README.md)、[GitHub 流程](docs/GITHUB_WORKFLOW.md) 和根 [AGENTS.md](AGENTS.md)。

## 提交问题与需求

使用 [Issue 模板](https://github.com/YIMO691/ares-ai-engineering-harness/issues/new/choose)：缺陷写复现、预期/实际结果和版本；功能建议写目标、范围及验收；工程任务记录讨论文档、Ready 和交付。Codex 根据实际对话整理记录，Owner 不必重复填表。

非简单任务关联一个 Issue；小型文档修正可直接提交 PR。安全问题按 [SECURITY.md](SECURITY.md) 私下反馈。

## 开发准备

按 [README 快速开始](README.md#快速开始) 配置本地环境，输出和认证放在仓库外授权目录。采用 `feature/*`、`fix/*`、`docs/*` 分支；不要直接改写 `main`。

讨论时记录真实来源和上下文。维护功能入口及受影响的实现、验证记录，复用已有格式；L1/L2/L3 仅选择现有 Harness 任务快照格式，FAST/STANDARD/CRITICAL 选择验证强度。按 [现行流程](docs/UNIFIED_WORKFLOW.md#项目文档与任务快照) 衔接来源与冻结摘要，不复制第二套项目文档。新增架构决定才写 ADR，不为小改动制造完整文档包。

## 实施与文档

复用现有 coordinator、store 和官方原生适配器，保持旧记录兼容。Owner 与当前 Primary 直接协作，Reviewer 独立；不新增 Agent/Session/Search/Edit/Shell/Sandbox Runtime。

行为或契约变化同步更新对应文档。当前入口、历史 ADR、导入来源要标明适用范围；避免多份文档重复维护同一套细节。提交使用 `feat`、`fix`、`test`、`docs`、`chore`、`refactor` 等前缀。

## 验证

```powershell
./scripts/Test-Unified.ps1 -ScratchRoot '<absolute-task-scratch>' -EvidenceRoot '<absolute-task-evidence>' -InstallTestDependencies
```

该入口覆盖 Harness、SOP 和 Change Lens。纯文档修改先检查链接、代码示例、术语和 Issue YAML；不为可逆文本修改新增运行时测试。PR 的现有 CI 仍按配置运行。记录真实跳过、失败与限制，不把控制替身或部分分析写成原生完整验收。

## Pull Request

按 [PR 模板](.github/pull_request_template.md) 填写问题与最终行为、范围、文档版本、验证、审查、Align 和回滚方式。STANDARD/CRITICAL 记录独立 Reviewer 的实际结论与所审 revision，说明审查后改动和验证；FAST 如实写“不要求独立 Reviewer”。

CI 不能代替独立审查或 Owner 验收。检查出站文件，禁止上传商业源码、认证、本地配置、SQLite、artifacts/evidence 和运行记录。只提交安全证据摘要。

## 合并与许可

最终 PR revision 的检查成功、阻断项已处理且 Owner 明确授权后才能合并；不 force push、不启用无人值守自动合并、不自动发布版本。当前修改授权不能冒充另一项工作的验收或合并授权。

主项目尚未选择开源许可证。导入组件保留各自来源和许可，不擅自新增或扩大授权。支持渠道见 [SUPPORT.md](SUPPORT.md)。
