import contextlib
import copy
import importlib.util
import io
import json
from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("ange_eval", ROOT / "ange_eval.py")
ae = importlib.util.module_from_spec(spec)
spec.loader.exec_module(ae)


class PilotTests(unittest.TestCase):
    def setUp(self):
        self.protocol = ae.load(ROOT / "examples/protocol.json")
        self.runs = [ae.load(p) for p in sorted((ROOT / "examples/runs").glob("*.json"))]

    def invalid_run(self, mutate, expected):
        run = copy.deepcopy(self.runs[0])
        mutate(run)
        with self.assertRaisesRegex(ae.InvalidRecord, expected):
            ae.report(self.protocol, [run])

    def test_complete_example_retains_failure_and_separates_task_strata(self):
        result = ae.report(self.protocol, self.runs)
        self.assertTrue(result["record_set_complete"])
        self.assertEqual("synthetic", result["kind"])
        self.assertEqual("descriptive_only", result["analysis"])
        self.assertEqual(9, len(result["groups"]))
        self.assertEqual(1, sum(g["status_counts"]["failed"] for g in result["groups"]))
        self.assertEqual(9, sum(g["reported_runs"] for g in result["groups"]))
        self.assertNotIn("winner", result)

    def test_omitted_failed_run_is_reported_not_silently_excluded(self):
        failed = next(r for r in self.runs if r["status"] == "failed")
        result = ae.report(self.protocol, [r for r in self.runs if r != failed])
        self.assertFalse(result["record_set_complete"])
        self.assertEqual([failed["run_id"]], result["missing_runs"])

    def test_null_metric_is_not_imputed_to_zero(self):
        run = self.runs[0]
        key = next(iter(run["metrics"]))
        run["metrics"][key] = dict(value=None, missing_reason="Timer unavailable", evidence_ids=[])
        result = ae.report(self.protocol, self.runs)
        self.assertEqual(1, len(result["missing_metrics"]))
        group = next(g for g in result["groups"] if g["task_id"] == "local-fix" and g["condition_id"] == "P0_BASELINE")
        self.assertIsNone(group["metrics"][key]["median"])
        self.assertEqual(0, group["metrics"][key]["observed_n"])

    def test_protocol_edits_invalidate_record_binding(self):
        self.protocol["hypothesis"] += " changed"
        with self.assertRaisesRegex(ae.InvalidRecord, "hash mismatch"):
            ae.report(self.protocol, self.runs)

    def test_hash_ignores_object_key_order(self):
        self.assertEqual(ae.fingerprint(self.protocol), ae.fingerprint(dict(reversed(list(self.protocol.items())))))

    def test_duplicate_runs_rejected(self):
        with self.assertRaisesRegex(ae.InvalidRecord, "duplicate run_id"):
            ae.report(self.protocol, self.runs + [self.runs[0]])

    def test_unknown_assignment_rejected(self):
        self.invalid_run(lambda r: r.update(run_id="unregistered"), "unregistered")

    def test_mixed_empirical_and_synthetic_rejected(self):
        self.invalid_run(lambda r: r.update(kind="empirical"), "kind mismatch")

    def test_configuration_drift_rejected(self):
        for key in self.runs[0]["observed"]:
            with self.subTest(key=key):
                self.invalid_run(lambda r: r["observed"].update({key: "different"}), "configuration drift")

    def test_invalid_values_rejected(self):
        key = next(iter(self.runs[0]["metrics"]))
        for value in (True, "12", float("inf"), float("nan"), -1):
            with self.subTest(value=value):
                self.invalid_run(lambda r: r["metrics"][key].update(value=value), "finite number|below minimum")

    def test_null_requires_reason_and_present_value_requires_evidence(self):
        key = next(iter(self.runs[0]["metrics"]))
        self.invalid_run(lambda r: r["metrics"][key].update(value=None), "missing_reason")
        self.invalid_run(lambda r: r["metrics"][key].update(evidence_ids=[]), "empty array")
        self.invalid_run(lambda r: r["metrics"][key].update(evidence_ids=["missing"]), "unknown evidence")

    def test_failure_reason_required(self):
        self.invalid_run(lambda r: r.update(status="timeout", notes=[]), "failure/abort reason")

    def test_deviation_kept_visible(self):
        self.runs[0]["protocol_deviations"] = ["Evaluator saw condition label"]
        result = ae.report(self.protocol, self.runs)
        self.assertFalse(result["record_set_complete"])
        self.assertEqual(1, len(result["protocol_deviations"]))

    def test_missing_unregistered_metrics_and_typos_rejected(self):
        key = next(iter(self.runs[0]["metrics"]))
        self.invalid_run(lambda r: r["metrics"].pop(key), "expected fields")
        self.invalid_run(lambda r: r.update(metric={}), "expected fields")

    def test_invalid_timestamps(self):
        self.invalid_run(lambda r: r.update(ended_at="2020-01-01T00:00:00Z"), "ended before")
        self.invalid_run(lambda r: r.update(ended_at="2026-09-16T01:00:00"), "timezone")

    def test_repeated_runs_do_not_inflate_participant_count(self):
        duplicate = copy.deepcopy(self.protocol["assignments"][0])
        duplicate.update(run_id="repeat-run", repeat=2)
        self.protocol["assignments"].append(duplicate)
        repeated = copy.deepcopy(self.runs[0])
        repeated["run_id"] = "repeat-run"
        self.runs.append(repeated)
        for run in self.runs:
            run["protocol_sha256"] = ae.fingerprint(self.protocol)
        result = ae.report(self.protocol, self.runs)
        group = next(g for g in result["groups"] if g["task_id"] == "local-fix" and g["condition_id"] == "P0_BASELINE")
        self.assertEqual(2, group["reported_runs"])
        self.assertEqual(1, group["participant_count"])

    def test_invalid_protocol(self):
        cases = (
            lambda p: p["assignments"].append(p["assignments"][0]),
            lambda p: p["assignments"][0].update(condition_id="missing"),
            lambda p: p["metrics"][0].update(id="bad id"),
            lambda p: p["metrics"][0].update(minimum=10, maximum=0),
            lambda p: p.update(schema_version=True),
            lambda p: [m.update(primary=False) for m in p["metrics"]],
        )
        for mutate in cases:
            protocol = copy.deepcopy(self.protocol)
            mutate(protocol)
            with self.subTest(protocol=protocol), self.assertRaises(ae.InvalidRecord):
                ae.validate_protocol(protocol)

    def test_new_run_id_cannot_duplicate_same_logical_trial(self):
        duplicate = dict(self.protocol["assignments"][0], run_id="duplicate-logical-run")
        self.protocol["assignments"].append(duplicate)
        with self.assertRaisesRegex(ae.InvalidRecord, "duplicate logical trial"):
            ae.validate_protocol(self.protocol)

    def test_json_duplicate_keys_and_nonfinite_rejected(self):
        for payload in ('{"id":1,"id":2}', '{"value":NaN}', '{"value":Infinity}'):
            with self.assertRaises(ae.InvalidRecord):
                json.loads(payload, object_pairs_hook=ae.unique_pairs, parse_constant=ae.reject_constant)

    def test_cli_exit_codes_and_machine_readable_stdout(self):
        stdout, stderr = io.StringIO(), io.StringIO()
        with contextlib.redirect_stdout(stdout), contextlib.redirect_stderr(stderr):
            code = ae.main(["report", "--protocol", str(ROOT / "examples/protocol.json")])
        self.assertEqual(3, code)
        self.assertEqual(9, len(json.loads(stdout.getvalue())["missing_runs"]))
        self.assertEqual("", stderr.getvalue())
        with contextlib.redirect_stderr(io.StringIO()):
            self.assertEqual(2, ae.main(["protocol", "--protocol", str(ROOT / "absent.json")]))


if __name__ == "__main__":
    unittest.main()
