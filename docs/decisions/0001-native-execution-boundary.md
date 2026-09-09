# ADR 0001 — Native execution boundary

Status: accepted baseline; Direct mode clarification is recorded in [ADR 0003](0003-codex-direct-observer.md).

## Context

Ares needs workflow control without duplicating the native agent runtime.

## Decision

Ares retains workflow state, verification scheduling, policy, recovery checks and human gates. Codex retains agent execution and native context. The legacy hosted Primary and Reviewer use distinct observed thread IDs. In Direct mode Ares does not launch or resume the external Primary; its optional reference is attribution only.

The legacy Workflow Fusion path reuses that boundary rather than replaying a stored transcript as model memory. A missing or mismatched native ID must fail visibly, never silently create a replacement continuation.

## Consequences

Runtime and authentication data remain local and excluded from Git. Availability and latency depend on the native executor and environment.
