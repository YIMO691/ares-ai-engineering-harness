# ADR 0001 — Native execution boundary

Status: accepted baseline

Ares retains workflow state, verification scheduling, policy, recovery checks and human gates. Codex retains agent execution and native context. Primary and Reviewer use distinct observed thread IDs; each may resume its own thread.

Workflow Fusion will reuse that boundary rather than replaying a stored transcript as model memory. A missing or mismatched native ID must fail visibly, never silently create a replacement continuation.

Consequence: runtime and authentication data remain local and excluded from Git. Availability and latency depend on the native executor and environment.
