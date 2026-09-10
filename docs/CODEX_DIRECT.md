# Codex direct collaboration

New tasks use the [integrated workflow](UNIFIED_WORKFLOW.md): document before agree, align before accept. This page preserves underlying commands and recovery.

The Owner talks directly to the existing native Codex Primary. Ares records business agreements and evidence and runs deterministic checks plus independent read-only review. Web is an optional observer. No native conversation/session/tool runtime is implemented here.

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

## Rework, changes and interruption

Tests or review requiring repairs return to the existing Primary. Ares never starts an implementation agent in direct mode. Begin, edit, submit and verify again; preserve the contract version. Each verification attempt is a separate historical Run.

Environment failures can retry verify with the same submitted source. Source edits invalidate prior submission/verification. Changed requirements, Git references, instructions, allowed paths or verification commands need reconciliation and a new agreement. To change project settings, first reopen unfinished direct tasks to Discussing.

A stopped CLI command preserves partial evidence; retry verification rather than claiming exactly-once native continuation. After a hard crash, check that any spawned test/reviewer processes have stopped before recover. Recovery holds the writer lease and does not replay edits. A newly opened native conversation reads the task record, inspects current source and records an explicit handoff; Ares never fabricates continuity.

The writer lease serializes CLI commands. A task holds the project's business write claim through implementation and acceptance. This does not prevent editors outside Ares from modifying files: fingerprints detect such changes before verification and acceptance. Native tool-level Primary telemetry is not collected; the observer labels that limitation and shows milestone timestamps.

## Extension boundary

Documents and Change Lens are integrated. Future Git/PR/CI, resource and build-result views can link by project/task/attempt ID and evidence references. Add a concrete second source before introducing a plugin framework. Keep runtime data, source snapshots and auth out of Git.

## Verification

DirectTests exercises workflow guards with controlled handlers. Real native review must be separately verified with CLI evidence; controlled handler tests do not prove native session behavior.

Source fingerprints cover Git-tracked and unignored files plus HEAD, branch and instruction hashes. Diff artifacts are the working tree against HEAD, including pre-existing uncommitted changes; baseline hashes distinguish what existed before the agreement. Native tool policy remains responsible for changes outside these observations.

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

After implementation, update the affected project records and cite actual evidence and limitations. Align retains its existing current-run artifact requirements and writes the task snapshot summary. A new snapshot records the imported SOP commit; old DocumentSet.SopVersion and frozen content remain unchanged. See [Ares profile](../workflow/ARES_PROFILE.md).
