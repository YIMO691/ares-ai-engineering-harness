# Mini Lab: retained prototype lessons

The sole maintained project is Ares AI Engineering Harness. The former
`YIMO691/agent-engineering-mini-lab` was a teaching prototype, not a runtime dependency.

## Provenance and recovery

The last Mini Lab main commit was
`367929cb279b2a356de6508e052b3114cff143ce`; its parent was
`c2cd04159ae2efbd3fafe1eb91e3b68c7ab5a33e`.
Before retiring the hosted repository, the maintainer created a full Git mirror
and self-contained `agent-engineering-mini-lab.bundle`. A separate restore
passed `git fsck --full`; refs and reachable object IDs matched. The backup
contains both commits, 79 files at the final commit, and the original LICENSE.
Backup paths, hashes and operational evidence are recorded in the local task
case, outside this repository.

This document preserves the useful design and evaluation reference. It does not
import the old Python runtime, provider, tool layer, execution outputs or license
policy into the current application.

## What the prototype established

Mini Lab used a fixed Todo CLI priority task and a Planner → Engineer → Test →
Reviewer → bounded rework flow. Its default provider was scripted; pytest really
executed, but scripted behavior did not demonstrate model quality. The optional
OpenAI Agents SDK route was marked REAL_AGENT_RUN: NOT_VERIFIED.

Retain these lessons:
- A model's completion claim is not an acceptance result.
- Tests and independent review use actual changes and evidence.
- Invalid handoffs, denied operations and changed task instructions stop progress.
- Each revision retains its own results, diff and review findings.
- Rework has an explicit limit and cannot erase earlier failures.
- Missing evidence is NOT_RUN, not an inferred pass.
- Deterministic fixture performance does not establish model success rate or ROI.

Current Workbench keeps these workflow concerns while native Codex owns its
reasoning, conversation, tools and sandbox. Workflow Fusion starts with Owner
discussion, freezes Ready anchors, and resumes the same Primary thread for
implementation. It does not restore Mini Lab's separate Planner dispatch.

## Historical evaluation catalogue

The following 15 cases retain the identifiers from the prototype's
`evals/eval_cases.yaml`. These are historical scenario definitions, not a claim
that the current Workbench runs a Todo benchmark or has newly passed these cases.

| Case | Scenario | Expected result |
|---|---|---|
| E-001 | Priority omitted | Use priority 3 |
| E-002 | Priority 1 | Accept the lower bound |
| E-003 | Priority 5 | Accept the upper bound |
| E-004 | Priority 0 | Reject without persisting |
| E-005 | Priority 6 | Reject without persisting |
| E-006 | Non-integer priority | Reject |
| E-007 | Existing record has no priority | Read it as priority 3 |
| E-008 | List priorities | Sort in descending order; preserve stable ordering |
| E-009 | Existing done operation | Keep prior behavior |
| H-001 | Invalid plan/handoff | Block the transition |
| H-002 | Tests fail | Do not advance to successful review/delivery |
| H-003 | Reviewer requests a fix | Return to implementation |
| H-004 | Revision limit reached | Stop instead of looping indefinitely |
| H-005 | Tool operation denied | Record the denial |
| H-006 | Frozen task changes | Fail closed |

Business cases E-001 through E-009 belong to the archived Todo example.
Harness cases H-001 through H-006 can inform current workflow regression design;
they must be adapted to the current Ready/session contracts, not treated as
reasons to add a separate Planner or custom tool runtime.

## Reproducible rework example

The original demo intentionally accepted priority 0 in the first scripted
implementation, despite the required range of 1 through 5. Independent acceptance
exposed the boundary error. Reviewer identified the violated requirement and
requested correction plus a regression test. A subsequent revision corrected the
lower bound, reran verification, and obtained a successful review.

Fault injection belongs in an explicitly labeled test fixture. Never alter the
normal production path or report scripted findings as spontaneous model behavior.
Workbench's own rework budget and final Owner acceptance remain authoritative;
the old prototype's default maximum of three implementation revisions does not
override current workflow policy.

## Where to continue

- [Current workflow](../UNIFIED_WORKFLOW.md)
- [Native execution boundary](../decisions/0001-native-execution-boundary.md)
- [Workflow Fusion](../decisions/0002-workflow-fusion.md)
- [Architecture](../architecture/ARCHITECTURE.md)

Original executable examples and detailed historical documents are recoverable
from the offline Git bundle when needed. They are not a second maintained product.
