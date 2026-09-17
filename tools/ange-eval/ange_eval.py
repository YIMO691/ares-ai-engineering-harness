"""Read-only validation and descriptive reporting for explicitly registered pilots.

No model execution, evidence dereferencing, acceptance decision or file output.
"""
from __future__ import annotations

import argparse
from collections import Counter
from datetime import datetime
import hashlib
import json
import math
from pathlib import Path
import re
import statistics
import sys


class InvalidRecord(ValueError):
    pass


def require(condition, message):
    if not condition:
        raise InvalidRecord(message)


def text(value, label):
    require(isinstance(value, str) and bool(value.strip()), f"{label}: non-empty string required")


def identifier(value, label):
    text(value, label)
    require(re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9_.-]*", value) is not None,
            f"{label}: use a stable ASCII identifier")


def fields(value, required, label):
    require(isinstance(value, dict), f"{label}: object required")
    require(set(value) == set(required.split()), f"{label}: expected fields {required}")


def items(value, label, nonempty=True):
    require(isinstance(value, list), f"{label}: array required")
    require(not nonempty or bool(value), f"{label}: empty array")
    return value


def strings(value, label, nonempty=False):
    for entry in items(value, label, nonempty):
        text(entry, label)


def indexed(value, label):
    result = {}
    for entry in items(value, label):
        require(isinstance(entry, dict), f"{label}: object entry required")
        identifier(entry.get("id"), f"{label}.id")
        require(entry["id"] not in result, f"{label}: duplicate id {entry['id']}")
        result[entry["id"]] = entry
    return result


def unique_pairs(pairs):
    result = {}
    for key, value in pairs:
        require(key not in result, f"JSON: duplicate key {key}")
        result[key] = value
    return result


def reject_constant(value):
    raise InvalidRecord(f"JSON: non-finite number {value}")


def load(path):
    return json.loads(Path(path).read_text(encoding="utf-8-sig"),
                      object_pairs_hook=unique_pairs, parse_constant=reject_constant)


def fingerprint(protocol):
    # Whitespace/key order do not change identity; any semantic protocol edit does.
    payload = json.dumps(protocol, sort_keys=True, ensure_ascii=False,
                         separators=(",", ":"), allow_nan=False).encode("utf-8")
    return hashlib.sha256(payload).hexdigest()


def validate_protocol(protocol):
    fields(protocol, "schema_version study_id kind hypothesis model environment comparison_controls "
           "evaluator missing_data_policy conditions tasks metrics assignments", "protocol")
    require(type(protocol["schema_version"]) is int and protocol["schema_version"] == 1,
            "protocol: unsupported schema_version")
    require(protocol["kind"] in ("synthetic", "empirical"), "protocol: invalid kind")
    for key in ("study_id", "hypothesis", "environment", "comparison_controls", "evaluator", "missing_data_policy"):
        text(protocol[key], key)
    fields(protocol["model"], "id configuration", "model")
    for key, value in protocol["model"].items():
        text(value, f"model.{key}")
    conditions = indexed(protocol["conditions"], "conditions")
    require(len(conditions) >= 2, "protocol: at least two conditions required")
    for condition in conditions.values():
        fields(condition, "id description harness_revision policy_ref", "condition")
        for key, value in condition.items():
            text(value, f"condition.{key}")
    tasks = indexed(protocol["tasks"], "tasks")
    for task in tasks.values():
        fields(task, "id family repo_revision success_criteria", "task")
        for key, value in task.items():
            text(value, f"task.{key}")
    metrics = indexed(protocol["metrics"], "metrics")
    for metric in metrics.values():
        fields(metric, "id unit direction primary minimum maximum", "metric")
        text(metric["unit"], "metric.unit")
        require(metric["direction"] in ("lower", "higher", "descriptive"), "metric: invalid direction")
        require(type(metric["primary"]) is bool, "metric.primary: boolean required")
        for bound in ("minimum", "maximum"):
            value = metric[bound]
            require(value is None or (type(value) in (int, float) and math.isfinite(value)),
                    f"metric.{bound}: finite number or null required")
        require(metric["minimum"] is None or metric["maximum"] is None or metric["minimum"] <= metric["maximum"],
                "metric: inverted bounds")
    require(any(m["primary"] for m in metrics.values()), "protocol: primary metric required")
    assignments = {}
    trials = set()
    for assignment in items(protocol["assignments"], "assignments"):
        fields(assignment, "run_id condition_id task_id participant_id repeat", "assignment")
        for key in ("run_id", "condition_id", "task_id", "participant_id"):
            identifier(assignment[key], f"assignment.{key}")
        require(assignment["condition_id"] in conditions, "assignment: unknown condition")
        require(assignment["task_id"] in tasks, "assignment: unknown task")
        require(type(assignment["repeat"]) is int and assignment["repeat"] > 0,
                "assignment.repeat: positive integer required")
        require(assignment["run_id"] not in assignments, "assignment: duplicate run_id")
        trial = tuple(assignment[key] for key in ("condition_id", "task_id", "participant_id", "repeat"))
        require(trial not in trials, "assignment: duplicate logical trial; use a new repeat number")
        trials.add(trial)
        assignments[assignment["run_id"]] = assignment
    require({a["condition_id"] for a in assignments.values()} == set(conditions),
            "protocol: condition without assignments")
    require({a["task_id"] for a in assignments.values()} == set(tasks),
            "protocol: task without assignments")
    return conditions, tasks, metrics, assignments


def timestamp(value, label):
    text(value, label)
    try:
        parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
    except ValueError as error:
        raise InvalidRecord(f"{label}: invalid ISO timestamp") from error
    require(parsed.utcoffset() is not None, f"{label}: timezone required")
    return parsed


STATUSES = ("success", "failed", "timeout", "aborted", "tool_failure")
EVIDENCE_KINDS = ("functional", "engineering", "experience", "telemetry", "recovery", "human_cost", "process")


def validate_run(run, protocol, catalog):
    conditions, tasks, metrics, assignments = catalog
    fields(run, "schema_version kind protocol_sha256 run_id status started_at ended_at observed "
           "metrics evidence notes protocol_deviations", "run")
    require(type(run["schema_version"]) is int and run["schema_version"] == 1,
            "run: unsupported schema_version")
    require(run["kind"] == protocol["kind"], "run: synthetic/empirical kind mismatch")
    require(run["protocol_sha256"] == fingerprint(protocol), "run: protocol hash mismatch")
    text(run["run_id"], "run_id")
    require(run["run_id"] in assignments, "run: unregistered run_id")
    require(run["status"] in STATUSES, "run: invalid status")
    require(timestamp(run["ended_at"], "ended_at") >= timestamp(run["started_at"], "started_at"),
            "run: ended before started")
    assignment = assignments[run["run_id"]]
    fields(run["observed"], "model_id model_configuration environment harness_revision repo_revision", "observed")
    expected = dict(model_id=protocol["model"]["id"], model_configuration=protocol["model"]["configuration"],
                    environment=protocol["environment"],
                    harness_revision=conditions[assignment["condition_id"]]["harness_revision"],
                    repo_revision=tasks[assignment["task_id"]]["repo_revision"])
    require(run["observed"] == expected, "run: observed configuration drift; register a separate protocol")
    fields(run["metrics"], " ".join(metrics), "run.metrics")
    for key, observation in run["metrics"].items():
        fields(observation, "value missing_reason evidence_ids", f"metrics.{key}")
        value = observation["value"]
        if value is None:
            text(observation["missing_reason"], f"metrics.{key}.missing_reason")
        else:
            require(type(value) in (int, float) and math.isfinite(value), f"metrics.{key}: finite number required")
            metric = metrics[key]
            require(metric["minimum"] is None or value >= metric["minimum"], f"metrics.{key}: below minimum")
            require(metric["maximum"] is None or value <= metric["maximum"], f"metrics.{key}: above maximum")
            require(observation["missing_reason"] is None, f"metrics.{key}: value cannot have missing_reason")
        strings(observation["evidence_ids"], f"metrics.{key}.evidence_ids", value is not None)
    evidence = indexed(run["evidence"], "evidence")
    for entry in evidence.values():
        fields(entry, "id kind ref claim limitations", "evidence")
        require(entry["kind"] in EVIDENCE_KINDS, "evidence: invalid kind")
        for key in ("ref", "claim", "limitations"):
            text(entry[key], f"evidence.{key}")
    for key, observation in run["metrics"].items():
        require(set(observation["evidence_ids"]) <= set(evidence), f"metrics.{key}: unknown evidence id")
    strings(run["notes"], "notes")
    strings(run["protocol_deviations"], "protocol_deviations")
    if run["status"] != "success":
        require(bool(run["notes"]), "unsuccessful run: failure/abort reason required in notes")


def report(protocol, runs):
    catalog = validate_protocol(protocol)
    conditions, tasks, metrics, assignments = catalog
    by_id = {}
    for run in runs:
        validate_run(run, protocol, catalog)
        require(run["run_id"] not in by_id, "report: duplicate run_id")
        by_id[run["run_id"]] = run
    missing = sorted(set(assignments) - set(by_id))
    missing_metrics = [{"run_id": r["run_id"], "metric_id": key,
                        "reason": value["missing_reason"]}
                       for r in runs for key, value in r["metrics"].items() if value["value"] is None]
    deviations = [{"run_id": r["run_id"], "deviations": r["protocol_deviations"]}
                  for r in runs if r["protocol_deviations"]]
    summaries = []
    # Keep task strata visible; repeated runs are not independent tasks or people.
    for task_id in tasks:
        for condition_id in conditions:
            planned = [a for a in assignments.values() if a["task_id"] == task_id and a["condition_id"] == condition_id]
            observed = [by_id[a["run_id"]] for a in planned if a["run_id"] in by_id]
            counts = Counter(r["status"] for r in observed)
            measurement = {}
            for key, metric in metrics.items():
                values = [r["metrics"][key]["value"] for r in observed if r["metrics"][key]["value"] is not None]
                measurement[key] = {"unit": metric["unit"], "primary": metric["primary"],
                                    "direction": metric["direction"], "observed_n": len(values),
                                    "values": values, "median": statistics.median(values) if values else None}
            summaries.append({"task_id": task_id, "condition_id": condition_id,
                              "planned_runs": len(planned), "reported_runs": len(observed),
                              "participant_count": len({assignments[r["run_id"]]["participant_id"] for r in observed}),
                              "status_counts": {status: counts[status] for status in STATUSES},
                              "metrics": measurement})
    return {"schema_version": 1, "study_id": protocol["study_id"], "kind": protocol["kind"],
            "protocol_sha256": fingerprint(protocol), "analysis": "descriptive_only",
            "record_set_complete": not missing and not missing_metrics and not deviations,
            "missing_runs": missing, "missing_metrics": missing_metrics, "protocol_deviations": deviations,
            "groups": summaries,
            "limitations": ["Configuration and evidence references are recorded assertions, not independently verified facts.",
                            "All observed metric values, including unsuccessful runs, remain visible; null is never zero.",
                            "Runs are not independent tasks/participants. No pooled effect, causal claim or significance test is computed.",
                            "Protocol hashing detects edits, but does not prove registration occurred before execution.",
                            "No automatic empirical validation, human acceptance, merge or release decision."]}


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("operation", choices=("protocol", "report"))
    parser.add_argument("--protocol", required=True, type=Path)
    parser.add_argument("--runs", nargs="*", type=Path, default=[])
    args = parser.parse_args(argv)
    try:
        protocol = load(args.protocol)
        validate_protocol(protocol)
        if args.operation == "protocol":
            require(not args.runs, "protocol operation does not accept runs")
            result = {"schema_version": 1, "study_id": protocol["study_id"], "kind": protocol["kind"],
                      "protocol_sha256": fingerprint(protocol), "validation": "structure_only"}
        else:
            result = report(protocol, [load(path) for path in args.runs])
        print(json.dumps(result, ensure_ascii=False, indent=2, allow_nan=False))
        return 3 if args.operation == "report" and not result["record_set_complete"] else 0
    except (InvalidRecord, OSError, UnicodeError, ValueError, TypeError, OverflowError) as error:
        print(json.dumps({"error": str(error)}, ensure_ascii=False), file=sys.stderr)
        return 2


if __name__ == "__main__":
    sys.exit(main())
