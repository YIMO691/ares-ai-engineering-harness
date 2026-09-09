# ARES AGENT ENGINEERING WORKBENCH BASELINE v1.0

> 状态：`FROZEN_FOR_PHASE1_IMPLEMENTATION`
> 基于 2026-09-07 Architecture Alignment Candidate，并应用本工作包 D1–D6 修订。

# 1. Mission

构建一套：

> **Agent-native Software Engineering Workbench**

把：

```text
Task
Workflow
Project Context
Agents
Codex
Tools
Tests
Review
Human Decision
Trace
Knowledge
```

组织成可以承担真实研发工作的系统。

# 2. Core Principles

1. Workflow owns control.
2. Agent owns reasoning.
3. Codex owns coding execution.
4. Deterministic code owns deterministic checks.
5. Human owns irreversible/high-risk approval.
6. Context is assembled, not dumped.
7. Knowledge is distilled, not chat history.
8. Assurance is risk-driven.
9. Framework is replaceable infrastructure.
10. Workbench contracts remain Ares-owned.

# 3. Core Architecture

```text
Owner / CLI / future UI
        ↓
Task Intake
        ↓
Risk Router
        ↓
Workflow Runtime
   ┌────┼────┬─────┐
   │    │    │     │
Det   Agent Codex Human
   └────┼────┴─────┘
        ↓
Workspace / ACI
        ↓
Project Intelligence (later)
        ↓
Risk-driven Assurance
        ↓
Events / Trace / Learning
```

# 4. Ten Core Contracts

```text
C01 Task
C02 WorkflowDefinition
C03 WorkflowRun
C04 Node
C05 Role
C06 Capability
C07 Tool
C08 Workspace
C09 ContextPackage
C10 Event
```

详细 Phase 1 boundary 见 `specs/CORE_CONTRACT_BOUNDARIES.md`。

# 5. Routes

## FAST

```text
Task → Codex → Build/Test → Diff → Deliver
```

## STANDARD

```text
Task → Grounding → Plan → Codex → Test → Review → Deliver
```

Review 可：

```text
REWORK_REQUIRED
→ same WorkflowRun
→ Codex
```

## CRITICAL

```text
Task
→ Grounding
→ Architecture/Risk
→ Plan
→ Codex
→ Test
→ Independent Verify
→ Review
→ Human Approval
→ Deliver
```

Phase 1 只验证 Human WAITING/approval contract，不实现 durable resume。

# 6. Rework

```text
definition.max_reworks = 3
run.rework_count
```

Task 无执行返工计数。

# 7. Runtime

主实现：

```text
C# / .NET 10
```

Microsoft Agent Framework：

```text
ADAPTER
```

Mini Lab：

```text
REFERENCE PROTOTYPE ONLY
```

Codex：

```text
DEFAULT ENGINEERING EXECUTOR
```

# 8. State / Events

Phase 1：

```text
InMemoryRunStore = state truth
JsonlEventSink = observability
```

不得把 JSONL 回读成 authoritative runtime state。

# 9. Assurance

```yaml
FAST:
  deterministic_checks: required
  review: optional

STANDARD:
  deterministic_checks: required
  review: required

CRITICAL:
  deterministic_checks: required
  independent_verify: required
  review: required
  human_approval: required
```

# 10. Phase Roadmap

```text
Phase 1 Workflow Core
Phase 2 Workspace / GameDev ACI
Phase 3 Project Context / Grounding
Phase 4 Role / Capability Registry
Phase 5 Assurance / selected AEH
Phase 6 Observability / Eval / Knowledge
Phase 7 Workbench UI
Phase 8 Durable / Automation
```

Phase 1 开始后不因后续阶段未设计完整而阻塞。
