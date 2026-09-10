# Ares repository instructions

Preserve the thin boundary: Task/workflow/policy/verification/history/human gates belong here; native agent reasoning, conversation context, tool loop, search, edit, shell and sandbox belong to Codex. Do not add a custom agent/session runtime.

The complete target reference architecture is [docs/TARGET_HARNESS.md](docs/TARGET_HARNESS.md), with artifact contracts and a fictional walkthrough under docs/target-harness. Consult it when evolving the Harness or mapping a project's document chain. Keep target capabilities distinct from current implementation: do not invent CLI fields/states, activate proposed gates, require every role to be a separate file, or treat illustrative evidence as real execution or Owner decisions. Current Direct operation remains governed by the execution documents below.

Use existing coordinator, store and native Codex adapter. Reviewer must remain independent and read-only. New Fusion behavior must preserve v0.2 tests and old stored records.

Keep all generated/cache/log/test outputs in an explicitly configured external task directory under D:/AgentWorkspace on the current Windows host. Never write process output to C: or a commercial target workspace. Resolve absolute output paths and reject reparse-point traversal.

Use feature branches and docs/GITHUB_WORKFLOW.md; no force push or unattended auto-merge. Explicit Owner instructions can authorize a PR merge after checks pass; do not ask again for the same authorization. Before committing or pushing, inspect all outgoing paths for target-project code, local configuration, auth and runtime data. Do not add a license without owner selection.


## Direct collaboration entry
When the Owner asks to use Ares, use the current native Codex conversation as Primary. Read workflow/docs/WORKFLOW.md, workflow/ARES_PROFILE.md and docs/UNIFIED_WORKFLOW.md; select applicable workflow templates/examples only. Maintain existing feature records in place: the feature entry links affected client/server implementation and verification records. L1/L2/L3 are Harness task snapshot formats, not a mandatory second project document set. Brief Source/Context reference authoritative project records and their observed versions; the CLI does not automatically ingest linked files. Invoke scripts/Ares.ps1 with task-local JSON request files. Do not start a replacement Primary or forward the conversation through Web.
Record source/version, observed context, requirements, decisions and verifiable increments through document before agree. Agree derives intent from the brief; do not ask the Owner to re-enter it. Preserve unresolved questions until answered. Record actual authorization, then begin, implement, submit and verify. For STANDARD/CRITICAL use the existing independent read-only Reviewer through verify. Return findings to the current Primary. Never invent Owner acceptance: leave AwaitingAcceptance until the Owner actually accepts.
Report source-changing feedback as rework under the existing agreement; reopen only for scope/policy changes. Read the latest revision before commands. Preserve evidence on interruption and inspect current source before retrying.
Web is optional and read-only by default. Start it on request; do not require the Owner to retype requirements or approvals in a form. Task milestones are reported observations, not full native process telemetry.

After verification, align every AC to current evidence and reconcile document/design drift before actual Owner acceptance. Use status artifact IDs, not SQLite edits. Optional lens is read-only; preserve PARTIAL/FAILED. Do not auto-load the separate ares-ai-software-engineering knowledge repository; use it only when the Owner requests it.
