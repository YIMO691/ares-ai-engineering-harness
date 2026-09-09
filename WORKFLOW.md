# Engineering workflow

## Current v0.2 baseline
Owner defines a task → Primary prepares (STANDARD/CRITICAL) → Primary implements in the same native thread → deterministic build/test → independent Reviewer → bounded rework → optional CRITICAL approval → delivery.

FAST omits separate preparation and Reviewer. Stop/Cancel and recoverable Blocked/Resume preserve evidence and existing edits. A cancelled run is terminal.

## Planned Workflow Fusion
DISCUSSING with Primary → Owner freezes Goal, Acceptance, Non-goals, Boundary, Key Decisions and Verification → same native Primary implements → existing verification/review/rework pipeline → acceptance summary → Owner Accept or Reject.

Unresolved questions must be empty before Ready. The discussion ledger is a visible audit record; it must never become a replacement model context/session runtime. Reviewer stays independent. Final Owner Accept will be required for Done.
