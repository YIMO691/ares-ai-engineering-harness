# Ares repository instructions

Preserve the thin boundary: Task/workflow/policy/verification/history/human gates belong here; native agent reasoning, conversation context, tool loop, search, edit, shell and sandbox belong to Codex. Do not add a custom agent/session runtime.

Use existing coordinator, store and native Codex adapter. Reviewer must remain independent and read-only. New Fusion behavior must preserve v0.2 tests and old stored records.

Keep all generated/cache/log/test outputs in an explicitly configured external task directory under D:/AgentWorkspace on the current Windows host. Never write process output to C: or a commercial target workspace. Resolve absolute output paths and reject reparse-point traversal.

Use feature branches; no force push or automatic merge. Before committing, inspect staged paths for target-project code, local configuration, auth and runtime data. Do not add a license without owner selection.
