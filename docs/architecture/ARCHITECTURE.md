# Architecture

Domain defines immutable engineering contracts. Application owns task/run lifecycle, workflow routing, checkpoint policy and human decisions. Infrastructure implements SQLite and bounded OS command adapters. The Microsoft Agent Framework adapter drives workflows. The Codex adapter invokes the official CLI. Razor Pages presents the local surface.

Primary prepare and implementation use one native thread; Reviewer is a separate read-only thread. Explicit native IDs are observed from CLI JSONL, never synthesized or selected with --last. Business checkpoints are not model memory.

The v0.2 definitions and pipeline remain the baseline. Fusion will start the Primary during discussion, freeze intent at Ready and enter implementation directly. A visible conversation ledger is audit/display only; native Codex retains internal context.

SQLite uses additive JSON snapshots; older records remain readable. Run restarts retain the existing v0.2 limits. Historical Phase 1 documents in docs describe earlier contracts and are not claims that their legacy role sequence is the current UI.

No custom agent runtime, conversation engine, editor/search/shell/sandbox runtime is introduced.
