# 0002: Freeze business decisions, continue native context

Status: implemented; retained for legacy Web Fusion. The new-task entry is superseded by [ADR 0003](0003-codex-direct-observer.md). Baseline v0.2.0 is unchanged.

## Context

The earlier Web entry coordinated native discussion and implementation. The following decision describes that compatibility path.

## Decision

Owner discussion and implementation share one Primary Codex native thread. The host captures the official thread.started ID and requires equality when resuming. Reviewer has a separate read-only native thread. No conversation reconstruction, hidden agent, new session runtime or tool/sandbox implementation is introduced.

Task stores a visible dialogue ledger, editable six-part Ready draft, immutable versioned snapshots and Owner decisions. Existing SQLite JSON records gain optional fields; old records retain their original lifecycle. A discussion invocation reuses the native adapter with an immutable input snapshot, and does not create a second execution Run.

Ready checks nonempty anchors/acceptance, empty unresolved questions, a successful discussion with a valid native binding, and current project configuration. A snapshot also freezes project policy and Git/instruction state. Implementation/rework checks the exact Primary reference. No prepare node exists in Fusion definitions.

Deterministic command failures with diagnostics and Reviewer rework follow the existing bounded rework mechanism. Environment faults remain Blocked. Owner final acceptance is separate from both automated success and CRITICAL approval.

## Consequences

The Windows allowed_paths check detects unauthorized source changes; it is not a per-file OS sandbox. Actual tool permissions remain with Codex's native sandbox. No stronger isolation claim is made.
