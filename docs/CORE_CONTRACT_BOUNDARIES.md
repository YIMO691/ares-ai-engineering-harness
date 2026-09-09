# C01–C10 — Frozen Boundaries for Phase 1

> 历史 Phase 1 设计快照：下文保留当时契约，不是当前新任务执行说明。当前行为见 [工作流](../WORKFLOW.md) 与 [文档导航](README.md)。

> 字段仅为 Phase 1 最小候选。实现时可做小幅命名调整，但不得改变责任归属。

# C01 Task

Purpose：研发目标。

Minimum:

```yaml
task_id
title
goal
acceptance[]
constraints[]
risk
workspace_id
lifecycle
created_at
updated_at
```

Core invariants：

- 不包含 current node。
- 不包含 rework_count。
- Agent/Backend 不能直接写 lifecycle。
- Delivered 只能由 Application 根据成功 WorkflowRun 完成。

---

# C02 WorkflowDefinition

Purpose：版本化可执行流程。

Minimum:

```yaml
workflow_id
version
risk
entry_node
nodes[]
routes[]
completion_conditions[]
rework_policy:
  max_reworks
```

Invariant：

- 一个 Run 固定 definition version。
- Route 是 Workflow 控制，不由 Agent 自由决定任意 next node。

---

# C03 WorkflowRun

Purpose：一次执行实例。

Minimum:

```yaml
run_id
task_id
workflow_id
workflow_version
state
current_node
rework_count
started_at
updated_at
result
```

Invariant：

- 正常 rework 保持同 run_id。
- backend 固定于 Run，不热切换。
- Deliver/Completed 必须满足 C02 completion conditions。
- process restart recovery Phase 1 不支持。

---

# C04 Node

Node types：

```text
DETERMINISTIC
AGENT
CODEX
HUMAN
```

Minimum definition：

```yaml
node_id
node_type
input_contract
result_contract
side_effect
timeout
retry_policy
```

Execution result：

```yaml
outcome
output
failure
artifact_refs[]
```

Agent 不直接返回 target node；它返回业务 outcome，Workflow route 决定 next。

---

# C05 Role

Purpose：职责配置，不是进程/模型。

Minimum：

```yaml
role_id
purpose
instruction_ref
capability_ids[]
```

Phase 1 可静态定义。

---

# C06 Capability

Minimum：

```yaml
capability_id
tool_ids[]
context_provider_ids[]
permission_policy
```

Phase 1 不实现动态 Registry 管理界面。

---

# C07 Tool

Minimum：

```yaml
tool_id
input_schema
output_schema
side_effect
timeout
permission
```

Failure 必须显式。

---

# C08 Workspace

Minimum：

```yaml
workspace_id
repo_root
git_ref
allowed_paths[]
artifact_root
isolation_mode
```

Core：
- no hard-coded D:/ in Domain
- host resolves actual paths
- Phase 1 local isolation only

---

# C09 ContextPackage

Minimum：

```yaml
task
workspace
selected_files[]
facts[]
previous_results[]
constraints[]
references[]
```

ContextPackage 是一次节点输入快照，不是 Knowledge Store。

---

# C10 Event

Minimum：

```yaml
event_id
event_type
occurred_at
task_id
run_id
node_id?
attempt?
payload
```

用途：
- timeline
- diagnostics
- future observability

Phase 1 JSONL event 是 export/record，不是 state truth。
