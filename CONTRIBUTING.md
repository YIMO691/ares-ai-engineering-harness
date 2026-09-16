# 贡献指南

本仓库围绕[最终 Harness v2](docs/TARGET_HARNESS.md)维护目标设计、当前工程控制实现和适用方法，目标是减少人的协调负担并提高交付可靠性与代码可维护性。先阅读 [项目说明](README.md)，按需使用[现行流程](docs/UNIFIED_WORKFLOW.md)、[CLI 参考](docs/CODEX_DIRECT.md)和 [GitHub 协作规范](docs/GITHUB_WORKFLOW.md)；维护指令见 [AGENTS.md](AGENTS.md)。

## 提交问题与需求

使用 [Issue 模板](https://github.com/YIMO691/ares-ai-engineering-harness/issues/new/choose)：缺陷写复现、预期/实际结果和版本；功能建议写目标、范围及验收；工程任务记录讨论文档、Ready 和交付。Codex 根据实际对话整理记录，Owner 不必重复填表。

非简单任务关联一个 Issue；小型文档修正可直接提交 PR。安全问题按 [SECURITY.md](SECURITY.md) 私下反馈。

## 开发准备

按 [README 快速开始](README.md#快速开始) 配置本地环境，输出和认证放在仓库外授权目录。采用 `feature/*`、`fix/*`、`docs/*` 分支；不要直接改写 `main`。

讨论时记录真实来源和上下文。维护功能入口及受影响的实现、验证记录，复用已有格式；L1/L2/L3 仅选择现有 Harness 任务快照格式，FAST/STANDARD/CRITICAL 选择验证强度。按 [现行流程](docs/UNIFIED_WORKFLOW.md#项目文档与任务快照) 衔接来源与冻结摘要，不复制第二套项目文档。新增架构决定才写 ADR，不为小改动制造完整文档包。

## 实施与文档

复用现有 coordinator、store 和官方原生适配器，保持旧记录兼容。Owner 与当前 Primary 直接协作，Reviewer 独立；不新增 Agent/Session/Search/Edit/Shell/Sandbox Runtime。

先查证项目事实，保留关键未知，使用当前需要的最小方案。新增抽象、依赖或明显扩大的变更面说明必要性；引用模式说明适用范围与依据；质量检查按风险选取并写入已有设计、验证和交付记录。领域 profile 未校准前只作为候选参考，不直接激活为门禁。

目标变化同时核对 README 的项目定位、文档导航、现行流程、架构与 Agent 维护指令；仅在相应职责受影响时修改。目标设计归总纲，已实现操作归现行流程，字段/命令归 CLI，来源与限制归研究页；历史正文保持当时语义，上游适配另行登记。检查正文的职责、能力状态和读者路径，链接正确本身不代表已经对齐。提交使用 `feat`、`fix`、`test`、`docs`、`chore`、`refactor` 等前缀。

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
