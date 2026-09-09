# 0003: Direct Codex collaboration with an optional observer

Status: implemented and merged into main through PR #5. Current Direct entry; supersedes the Web-first entry of ADR 0002.

## Context

The Owner prefers direct native Codex discussion and execution. The Web forwarding conversation, mandatory Ready form and Web-owned worker introduced an unnecessary dependency into everyday tasks.

## Decision

Direct mode stores an additive DirectTask business record on the existing EngineeringTask. A CLI invokes business operations. The existing WorkflowCoordinator runs submitted-evidence, test, independent review and delivery gates; a repair outcome returns control to the external Primary. It does not invoke or resume a replacement Primary.

RuntimeSettings and RunHandlerFactory are shared in Adapters.Codex so the command host and legacy Web host use the same implementation. SQLite and historical records remain shared. An exclusive writer lease prevents direct commands and legacy execution from running together. The default Web observer does not take that lease or run startup recovery.

CRITICAL approval records the frozen write boundary before external implementation, while Owner acceptance confirms the final result. These are distinct attributed decisions. External native actions remain under the native host's permission policy; Ares cannot enforce arbitrary actions issued outside its commands.

## Consequences

The implementation provides business milestones, actual tests/review/diff and explicitly labeled unavailable native Primary telemetry. It makes no promise of cross-client session takeover, transparent crash recovery or full OS-process monitoring.

Supersedes the Web-first entry assumption of decision 0002 for new direct tasks. Legacy Fusion behavior remains available with ObserverOnly=false.
