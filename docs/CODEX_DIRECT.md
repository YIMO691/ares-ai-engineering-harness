# CLI 参考：Codex Direct + Observer

本文只说明当前命令、字段、配置与恢复。项目最终目的和五部分职责见[最终 Harness v2](TARGET_HARNESS.md)，当前如何查证需求、控制复杂度、实现和验证见[现行流程](UNIFIED_WORKFLOW.md)。目标中的质量机制与评测设计不自动增加请求字段、状态或执行命令。

Owner 与现有原生 Primary 直接协作。CLI 登记约定、文档、授权、提交和证据；`verify` 执行配置的 Build/Test，FAST 跳过 Reviewer，STANDARD/CRITICAL 执行独立只读 Reviewer。新任务保持 document before agree、align before accept；Web 为可选观察入口。Context、Design、Verification 等内容由 Primary 维护，CLI 不自动检索或校准研究资料，也不实现原生会话/工具循环。


## Reviewer context

Reviewer 和旧模式角色的长文本输入使用外部内容引用：完整文档、grounding/plan、已有业务输出、Primary 报告和 diff 按 UTF-8 SHA-256 保存到运行 artifacts 的 `context` 子目录；提示词只带最多 1000 字符预览、路径、字节数与哈希。每次调用保留输入索引，并在执行前后校验引用。Direct 优先使用冻结约定中的文档版本。

原生角色应按需读取完整内容；关键来源无法读取时报告 blocked，不能依据预览推断通过。此机制不记录实际读取轨迹，不认证语义覆盖；上游命令已经截断的输出也不会自动恢复。缺失的旧 grounding/plan 明示为 null。所有引用仍是资料，不授予执行权限。CLI 请求字段和数据库结构保持不变。

## Local settings

复制 scripts/workbench.example.json 到仓库外的本地配置，保留已有 DataRoot、ScratchRoot、CodexHome、dotnet/git 路径，补充：

- DocumentsRoot：文档根目录，默认 D:/AgentWorkspace/Ares/10_WORK/active。
- PythonExecutable：现有 Python 3.11+ 的绝对路径。
- ChangeLensRoot：本仓库 tools/change-lens 的绝对路径。
- ChangeLensWorker：ScratchRoot/lens-build/bin/ChangeLens.Analyzer/release/ChangeLens.Analyzer.dll。

认证、数据库、缓存和构建输出均放在授权 D:/AgentWorkspace 目录。使用已配置的官方 CLI home，不把认证放进项目。Worker 基于 net8.0，集成调用允许使用已安装的更新 .NET 主版本。

~~~powershell
./scripts/Ares.ps1 -SettingsFile '<absolute-local-settings.json>' -Operation projects -Build -BuildLens
~~~

Change Lens 正常调用无需 pip 安装；契约测试另需 jsonschema 和 PyYAML。其原 MIT 许可保留，来源与许可见[适配说明](integrations/LOCAL_ADAPTATIONS.md)。

## Start locally

Keep local settings outside Git. Use the Workbench settings shape in scripts/workbench.example.json, with DataRoot, ScratchRoot, CodexHome and all output/cache paths under the authorized D:/AgentWorkspace area. CodexHome is the configured native CLI home with existing authentication and sandbox provisioning. Direct CLI does not copy or refresh credentials itself.

Build the command host once:

~~~powershell
./scripts/Ares.ps1 -SettingsFile "<absolute-local-settings.json>" -Operation projects -Build
~~~

Subsequent calls use -Operation and, when required, -RequestFile. The direct CLI does not need a Web server. Its JSON output is intended for the Primary; the Owner should not need to author request files.

Open the observer on demand:

~~~powershell
./scripts/Start-Workbench.ps1 -SettingsFile "<absolute-local-settings.json>" -Port 5271
~~~

Visit /Observe or /Observe/<task-id>. ObserverOnly defaults to true. It accepts GET/HEAD only, starts no RunWorker, performs no interruption recovery, and reads the same SQLite records while CLI commands execute. Stopping the Web host does not stop a CLI command. The startup script runs in the foreground; an agent launching it in the background must use a hidden process and put logs in its task directory.

Set Workbench:ObserverOnly=false only for legacy Web execution. Legacy host and direct CLI share an exclusive writer lease; stop legacy execution before direct commands. Existing history and old task formats remain readable.

## Command sequence

| Operation | Request fields | Meaning |
|---|---|---|
| projects / list | None | List registered projects / tasks |
| project | Project | Register project root, allowed paths, instruction files, exact build/test commands |
| create | ProjectId, Title, Risk, PrimaryLabel, optional NativeSessionRef | Record existing external Primary; starts Discussing |
| document | TaskId, Revision, Brief | Record a versioned discussion document set before Ready |
| status | TaskId | Read revision, runs, events, artifacts and approvals |
| agree | TaskId, Revision, Owner; optional Anchors/Checks | Derive intent from the recorded Brief and freeze documents, agreement and policy |
| begin | TaskId, Revision; Owner for CRITICAL | Record external implementation start; no Codex process launched |
| submit | TaskId, Revision, Message | Report actual changes and capture current source fingerprint |
| verify | TaskId, Revision | Run build/test and, except FAST, independent native Reviewer |
| feedback | TaskId, Revision, Message, optional Owner | Request defect repair under the same agreement |
| reopen | TaskId, Revision, Message, Owner | Reopen scope, reconcile document version, then agree again |
| align | TaskId, Revision, Alignment | Bind each AC to current evidence and reconcile delivery documents |
| lens | TaskId, Revision, UnityPath, Assembly, optional AllowSyntaxPartial | Attach a read-only change report with actual status |
| accept | TaskId, Revision, Owner | Record actual acceptance after current verification and required Align |
| recover | TaskId, Revision, Owner | Acknowledge a crashed verification command after checking surviving processes |

Every mutation uses the latest Revision. Exit codes: 0 success, 3 business blocker/rework, 2 invalid command or execution error. Do not interpret exit 0 from begin/submit as completed verification. A successfully recorded lens attempt may still have Status=FAILED; read the attached status.

Owner is {"Quote":"actual instruction","Source":"conversation/message reference"}. This is an attribution recorded by the Primary, not independent authentication of the human. Never invent acceptance or infer it from authorization to implement. NativeSessionRef is an optional observed reference; Ares does not attach to or resume that Primary.

Anchors uses Goal, Acceptance (array), NonGoals, Boundary, KeyDecisions, Verification and Unresolved (empty when agreed). Checks maps every one-based acceptance index once:

~~~json
[{"Criterion":1,"Kind":"automatic","Method":"test"},{"Criterion":2,"Kind":"manual","Method":"Owner checks layout in browser"}]
~~~

Automatic methods are build/test and reference the frozen project commands. A passing command is evidence of that command, not automatic proof of every mapped acceptance item. Reviewer checks the requirements against source and evidence; Owner confirms manual items.

## Document and alignment examples

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

## Feature records and task snapshots

The current SOP uses an existing feature entry linked to affected client, server and verification records. Maintain those project files in place. L1/L2/L3 still select the CLI's task snapshot layout; they do not mandate another project document package or migrate old records.

Use the existing Brief fields to cite the authoritative documents and their observed revision. For example, the following is an illustrative fragment to merge into a complete document request, not a runnable request or evidence of executed work:

```json
{
  "Source": "docs/features/favorites.md @ <observed revision>; issue <actual reference>",
  "Context": [
    {"Observation": "Feature entry links the affected implementation and acceptance records.", "Source": "docs/features/favorites.md @ <observed revision>"},
    {"Observation": "Client interaction and failure recovery relevant to this change: <observed facts>.", "Source": "docs/features/favorites-client.md @ <observed revision>"},
    {"Observation": "Server validation and idempotency relevant to this change: <observed facts>.", "Source": "docs/features/favorites-server.md @ <observed revision>"},
    {"Observation": "Existing scenarios and verification gaps: <observed facts, actual results only>.", "Source": "docs/features/favorites-verification.md @ <observed revision>"}
  ]
}
```

Source and Context are text fields. The CLI does not follow links, import their contents or hash external documents automatically. Include the necessary current intent, design and checks in the complete Brief; a link alone is insufficient. Project files within the configured source snapshot scope are subject to the existing source-fingerprint checks; files outside it are not. Primary must read and reconcile the actual documents, and state inaccessible or unverified material.

After implementation, update the affected project records and cite actual evidence and limitations. Align retains its existing current-run artifact requirements and writes the task snapshot summary. A new snapshot records the imported SOP commit; old DocumentSet.SopVersion and frozen content remain unchanged. See the [project record rules](UNIFIED_WORKFLOW.md#项目文档与任务快照).

## Rework, changes and interruption

Tests or review requiring repairs return to the existing Primary. Ares never starts an implementation agent in direct mode. Begin, edit, submit and verify again; preserve the contract version. Each verification attempt is a separate historical Run.

Environment failures can retry verify with the same submitted source. Source edits invalidate prior submission/verification. Changed requirements, Git references, instructions, allowed paths or verification commands need reconciliation and a new agreement. To change project settings, first reopen unfinished direct tasks to Discussing.

A stopped CLI command preserves partial evidence; retry verification rather than claiming exactly-once native continuation. After a hard crash, check that any spawned test/reviewer processes have stopped before recover. Recovery holds the writer lease and does not replay edits. A newly opened native conversation reads the task record, inspects current source and records an explicit handoff; Ares never fabricates continuity.

The writer lease serializes CLI commands. A task holds the project's business write claim through implementation and acceptance. This does not prevent editors outside Ares from modifying files: fingerprints detect such changes before verification and acceptance. Native tool-level Primary telemetry is not collected; the observer labels that limitation and shows milestone timestamps.

## Change Lens

lens 请求增加 UnityPath、Assembly、AllowSyntaxPartial（默认 false）。它使用冻结 Git HEAD → 提交 WORKTREE，可能包含 Ready 前已有未提交修改，报告明确说明比较范围。

严格历史编译基线缺失时保留 FAILED；任务明确允许时传 AllowSyntaxPartial=true，结果标为 PARTIAL。不会替目标项目生成编译清单或执行 Unity。分析前后核对源码，变化时拒绝挂接。

输出在 DataRoot/artifacts/<run>/lens/<attempt>/。来源输入使用现有 intent-evidence schema，task/run/source 关联由 Harness 保存。报告的验证步骤只是建议，不改变测试或审批。

Observer 展示讨论文档、阶段、对齐结果、实际证据与变化报告。文档/报告被更改返回 409；报告绑定检查快照，不宣称实时监控外部编辑。网页不是任务执行入口。

## Extension boundary

Documents and Change Lens are integrated. Future Git/PR/CI, resource and build-result views can link by project/task/attempt ID and evidence references. Add a concrete second source before introducing a plugin framework. Keep runtime data, source snapshots and auth out of Git.

## Verification

DirectTests exercises workflow guards with controlled handlers. Real native review must be separately verified with CLI evidence; controlled handler tests do not prove native session behavior.

Source fingerprints cover Git-tracked and unignored files plus HEAD, branch and instruction hashes. Diff artifacts are the working tree against HEAD, including pre-existing uncommitted changes; baseline hashes distinguish what existed before the agreement. Native tool policy remains responsible for changes outside these observations.
