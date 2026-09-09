# Ares repository instructions

Preserve the thin boundary: Task/workflow/policy/verification/history/human gates belong here; native agent reasoning, conversation context, tool loop, search, edit, shell and sandbox belong to Codex. Do not add a custom agent/session runtime.

Use existing coordinator, store and native Codex adapter. Reviewer must remain independent and read-only. New Fusion behavior must preserve v0.2 tests and old stored records.

Keep all generated/cache/log/test outputs in an explicitly configured external task directory under D:/AgentWorkspace on the current Windows host. Never write process output to C: or a commercial target workspace. Resolve absolute output paths and reject reparse-point traversal.

Use feature branches; no force push or automatic merge. Before committing, inspect staged paths for target-project code, local configuration, auth and runtime data. Do not add a license without owner selection.


## Direct collaboration entry
When the Owner asks to use Ares, use the current native Codex conversation as Primary. Read docs/CODEX_DIRECT.md and invoke scripts/Ares.ps1 with task-local JSON request files. Do not start a replacement Primary or forward the conversation through Web.
Record the agreed scope and actual Owner authorization, then begin, implement with native tools, submit and verify. For STANDARD/CRITICAL use the existing independent read-only Reviewer through verify. Return findings to the current Primary. Never invent Owner acceptance: leave AwaitingAcceptance until the Owner actually accepts.
Report source-changing feedback as rework under the existing agreement; reopen only for scope/policy changes. Read the latest revision before commands. Preserve evidence on interruption and inspect current source before retrying.
Web is optional and read-only by default. Start it on request; do not require the Owner to retype requirements or approvals in a form. Task milestones are reported observations, not full native process telemetry.
