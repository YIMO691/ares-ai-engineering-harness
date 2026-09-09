# Engineering workflow

## Workflow Fusion
New tasks begin DISCUSSING. Owner and Primary discuss in the official Codex native session. Discussion calls are read-only; the visible user/assistant ledger is an audit record, never a model context replay.

Primary maintains a concise Ready draft. Owner can edit it and freezes Goal, Acceptance, Non-goals, Boundary, Key Decisions and Verification. Unresolved must be empty, the latest discussion must have succeeded, and a native session must be confirmed. Ready binds a version, project policy fingerprint, Git/instruction stamp and Primary reference.

Ready queues implementation directly into the same Primary thread through `codex exec resume <thread-id>`. No Grounding, Planner or prepare agent is dispatched.

- FAST: implement → deterministic build/test → delivery summary → Owner acceptance.
- STANDARD: implement → build/test → independent read-only Reviewer → delivery summary → Owner acceptance.
- CRITICAL: STANDARD verification plus the existing Owner approval gate before the delivery summary.

Verification failures with actual command diagnostics and actionable Reviewer findings return to Primary, then retest/review. The existing two-rework budget applies. Environment failures and unknown effects remain visible Blocked outcomes.

Stop interrupts the active process tree and preserves edits/checkpoints. Resume uses the same Run and native Primary reference. Cancel is terminal for that Run. Changed frozen policy or Git/instruction state prevents silent continuation. Return to discussion creates a new Ready revision before another Run; earlier snapshots remain immutable.

A completed Run places a Fusion Task in AwaitingAcceptance. The Task page presents acceptance criteria, Primary changes/limitations, deterministic results, Reviewer conclusions and links to actual diff/artifacts. Owner Accept alone marks Done. Reject requires a reason and returns to discussion; it cannot mark Done.

## Compatibility and persistence
Existing v0.2 Tasks without Fusion metadata continue their original workflow: STANDARD/CRITICAL prepare → implement → test → Reviewer; FAST omits prepare/review. Their completion still uses Delivered. No historical records are rewritten.

SQLite stores optional business discussion/snapshot fields on Task, using the existing store, queue, coordinator, native adapter and checkpoints. Ares does not implement agent/session execution or tool loops. Only the current Owner message, current draft/frozen decisions, current policy and latest relevant business evidence are passed at each native continuation.

Run execution/approval recovery after application restart retains the existing v0.2 limitation: in-memory continuation is unavailable. Interrupted discussion records become Blocked; a subsequent message can resume the previously confirmed native thread. If the first call was interrupted before its thread ID was captured, continuity cannot be claimed.
