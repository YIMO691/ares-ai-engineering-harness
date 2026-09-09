# 统一研发流程

在当前 Codex 对话提出需求，Codex 使用同一仓库的 SOP 和 Harness 推进。Web 按需查看，Change Lens 辅助理解变化。

## 使用顺序与产物

| 顺序 | CLI 操作 | 产物 |
|---|---|---|
| 讨论 | project → create → document | 原始来源、上下文、决定与 TASK/SPEC/正式文档包 |
| Ready | agree → begin | 文档版本、冻结约定与实际授权 |
| 实施 | 按行为增量修改及验证 → submit | 项目改动、提交说明与源码指纹 |
| 验证 | verify；缺陷后 begin → 修复 → submit → verify | 构建、测试、独立审查及历次证据 |
| 对齐 | align | L1/L2 更新 TASK/SPEC；L3 生成 DELIVERY |
| 理解变化 | 可选 lens | 分析与解释 JSON、离线 HTML |
| 验收 | 实际 Owner 接受后 accept | 验收决定；不自动 push/merge/release |

L1 最少 TASK；L2 一份 SPEC；L3 PRD、SDD、TEST-PLAN，交付时 DELIVERY；重大决定才单列 ADR。工程上下文与讨论结论进入对应文档，原生完整对话不自动复制。生成的 Markdown 是 Brief 的可读投影，不能编辑文件后假定授权也已经更新。

生成器覆盖最低共同结构，正式领域内容仍按相关模板填入真实需求、设计、失败/兼容和验证安排。执行规范见 [Ares 规则](../workflow/ARES_PROFILE.md) 和 [SOP](../workflow/SOP.md)。

## 一次性配置

复制 scripts/workbench.example.json 到仓库外的本地配置，保留已有 DataRoot、ScratchRoot、CodexHome、dotnet/git 路径，补充：

- DocumentsRoot：文档根目录，默认 D:/AgentWorkspace/Ares/10_WORK/active。
- PythonExecutable：现有 Python 3.11+ 的绝对路径。
- ChangeLensRoot：本仓库 tools/change-lens 的绝对路径。
- ChangeLensWorker：ScratchRoot/lens-build/bin/ChangeLens.Analyzer/release/ChangeLens.Analyzer.dll。

认证、数据库、缓存和构建输出均放在授权 D:/AgentWorkspace 目录。使用已配置的官方 CLI home，不把认证放进项目。Worker 基于 net8.0，集成调用允许使用已安装的更新 .NET 主版本。

~~~powershell
./scripts/Ares.ps1 -SettingsFile '<absolute-local-settings.json>' -Operation projects -Build -BuildLens
~~~

Change Lens 正常调用无需 pip 安装；契约测试另需 jsonschema 和 PyYAML。其原 MIT 许可保留，主项目与 SOP 仍为私有内容。

## Codex 的结构化输入

所有写操作使用最新 Revision。JSON 由 Codex 根据真实讨论准备，Owner 不直接操作。document 输入：

~~~json
{
  "TaskId": "<task-id>",
  "Revision": 0,
  "Brief": {
    "Level": "L2",
    "LevelReason": "可独立验证的局部行为",
    "Source": "策划需求的真实引用 @ 版本",
    "Context": [{"Observation":"当前行为和约束","Source":"检查过的文件或证据"}],
    "Anchors": {
      "Goal":"预期结果",
      "Acceptance":["可观察验收项"],
      "NonGoals":"本轮不做",
      "Boundary":"授权边界",
      "KeyDecisions":"确认的方案和理由",
      "Verification":"构建、测试、人工验证方法",
      "Unresolved":[]
    },
    "Checks":[{"Criterion":1,"Kind":"automatic","Method":"test"}],
    "Design":"关键设计、数据与流程；适用时说明失败、兼容、回滚和取舍",
    "Increments":["可独立验证的行为与验证方式"]
  }
}
~~~

agree 只需 TaskId、Revision、实际 Owner Quote/Source，省略 Anchors/Checks 时从 Brief 读取。准备文档不等于授权。

align 的 Alignment 字段示例：

~~~json
{
  "Criteria":[{"Criterion":1,"Result":"实际验收结果","ArtifactIds":["<current-run-test-artifact-id>"]}],
  "DesignConformance":"实现与设计、契约的实际核对",
  "Deviations":"已解决的偏移或无偏移的说明",
  "Cleanup":"清理结果与遗留",
  "Unresolved":[]
}
~~~

status 返回任务、运行、事件、artifact ID 和审批。人工项另传 ManualConfirmation（真实 Quote/Source），ArtifactIds 可为空。不得引用其他 Run 或把任意报告当作成功测试。Harness 校验证据归属、成功节点和哈希，Primary 的逐项判断仍需 Reviewer/Owner 评估。

## 文档版本与恢复

新 CLI 任务要求文档和 Align；旧记录保留兼容行为。新增版本写入 DocumentsRoot/TASK-<task-id>/v<version>-<id>/；Agreement 保留冻结文本。L1/L2 交付追加在同一文档，L3 DELIVERY 单独汇总，不重复生成实施报告。

文件发生变化会阻止相关操作。重新协调后 reopen → document → agree，旧版本保留。源码变化使用现有指纹保护，证据变化也会阻止 Align/accept。

关闭 Web 不影响 CLI；硬崩溃先核对残留进程，再 recover 和重验，不承诺会话透明接管或重放。

## Change Lens 与 Web

lens 请求增加 UnityPath、Assembly、AllowSyntaxPartial（默认 false）。它使用冻结 Git HEAD → 提交 WORKTREE，可能包含 Ready 前已有未提交修改，报告明确说明比较范围。

严格历史编译基线缺失时保留 FAILED；任务明确允许时传 AllowSyntaxPartial=true，结果标为 PARTIAL。不会替目标项目生成编译清单或执行 Unity。分析前后核对源码，变化时拒绝挂接。

输出在 DataRoot/artifacts/<run>/lens/<attempt>/。来源输入使用现有 intent-evidence schema，task/run/source 关联由 Harness 保存。报告的验证步骤只是建议，不改变测试或审批。

Observer 展示讨论文档、阶段、对齐结果、实际证据与变化报告。文档/报告被更改返回 409；报告绑定检查快照，不宣称实时监控外部编辑。网页不是任务执行入口。

## 来源与验证

[导入清单](integrations/UPSTREAM_IMPORTS.json) 保留原仓库、固定 commit 和原文件哈希。导入采用源码快照，历史可从固定来源追溯；不是将旧提交伪造成当前开发记录。

现有 Test-Workbench.ps1 覆盖兼容及文档/Align 约束；导入分析器保留 unittest。原生 Reviewer 和 UI 另用隔离真实样例验证。知识库未接入，仅 Owner 指定时手动参考。

## 统一验证入口

在已授权的外部目录运行 scripts/Test-Unified.ps1 -ScratchRoot <path> -EvidenceRoot <path> -InstallTestDependencies。该入口依次执行 Harness 回归、SOP 文档检查、Roslyn 构建与 Change Lens 测试；不会启用真实 Unity 项目测试。首次需要安装的 Python 测试依赖写入 ScratchRoot/python-libs。GitHub CI 使用同一入口，远程结果需实际运行后才能确认。
