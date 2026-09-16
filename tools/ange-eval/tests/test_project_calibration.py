import contextlib
import hashlib
import io
import json
import os
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import project_calibration as pc


class CalibrationTests(unittest.TestCase):
    def setUp(self):
        # Test-Unified configures tempfile outside the checkout. Standalone Windows
        # runs must explicitly opt into the same safe output boundary.
        if os.name == "nt":
            root = Path(os.environ.get("TEMP", ""))
            if not root.is_absolute() or ".." in root.parts or not root.resolve().is_relative_to(Path("D:/AgentWorkspace")):
                self.fail("Set TEMP to an external task directory under D:/AgentWorkspace before running tests")
            pc.reject_linked_path(root)
        else:
            root = Path(tempfile.gettempdir())
        self.directory = tempfile.TemporaryDirectory(dir=root)
        self.root = Path(self.directory.name).resolve()
        self.assertTrue(self.root.is_relative_to(root.resolve()))
        self.addCleanup(self.cleanup_directory)
        self.source = self.root / "rules.txt"
        self.source.write_bytes(b"rule-v1\n")
        self.profile = dict(schema_version=1, project_id="fixture", sources=[
            dict(id="rules", path="rules.txt", sha256=hashlib.sha256(b"rule-v1\n").hexdigest())], candidates=[
            dict(id="candidate-1", family="low-risk", source_ids=["rules"], success_criteria="Preserve rule",
                 validation_plan="Run a separately authorized oracle", unverified=["oracle not executed"])])

    def cleanup_directory(self):
        pc.reject_linked_path(self.root)
        self.directory.cleanup()

    def test_matches_without_claiming_trial_readiness_or_leaking_contents(self):
        result = pc.calibrate(self.root, self.profile)
        self.assertTrue(result["all_sources_matched"])
        self.assertEqual("NOT_ESTABLISHED", result["candidates"][0]["trial_readiness"])
        self.assertNotIn(str(self.root), json.dumps(result))
        self.assertNotIn("rule-v1", json.dumps(result))

    def test_shipped_fixture_is_pinned_but_not_trial_ready(self):
        root = Path(__file__).resolve().parents[1] / "examples" / "calibration"
        result = pc.calibrate(root, pc.load(root / "profile.json"))
        self.assertTrue(result["all_sources_matched"])
        self.assertTrue(all(c["trial_readiness"] == "NOT_ESTABLISHED" for c in result["candidates"]))

    def test_stale_source_marks_dependent_candidate_incomplete(self):
        self.source.write_bytes(b"new-rule")
        result = pc.calibrate(self.root, self.profile)
        self.assertFalse(result["all_sources_matched"])
        self.assertEqual("STALE", result["sources"][0]["status"])
        self.assertEqual(["rules"], result["candidates"][0]["sources_needing_attention"])

    def test_unpinned_source_is_not_a_verified_baseline(self):
        self.profile["sources"][0]["sha256"] = None
        result = pc.calibrate(self.root, self.profile)
        self.assertEqual("UNPINNED", result["sources"][0]["status"])
        self.assertFalse(result["all_sources_matched"])

    def test_missing_source(self):
        self.source.unlink()
        self.assertEqual("MISSING", pc.calibrate(self.root, self.profile)["sources"][0]["status"])

    def test_traversal_absolute_alias_and_device_paths_rejected(self):
        for path in ("../rules.txt", "/rules.txt", "C:/rules.txt", "a\\b", "a//b", "a/./b", "a/../b",
                     "rules.txt:stream", "a/*.cs", "a/CON", "CON/file.cs", "a/LPT¹.txt", "a/trailing. ", "a/\x00b"):
            with self.subTest(path=path), self.assertRaises(pc.InvalidRecord):
                pc.relative_source(path)

    def test_case_aliases_and_unknown_source_ids_rejected(self):
        self.profile["sources"].append(dict(id="other", path="RULES.TXT", sha256=None))
        with self.assertRaisesRegex(pc.InvalidRecord, "duplicate source path"):
            pc.validate_profile(self.profile)
        self.profile["sources"].pop()
        self.profile["candidates"][0]["source_ids"] = ["absent"]
        with self.assertRaisesRegex(pc.InvalidRecord, "unknown source"):
            pc.validate_profile(self.profile)

    def test_size_limit(self):
        with patch.object(pc, "MAX_SOURCE_BYTES", 2):
            self.assertEqual("TOO_LARGE", pc.calibrate(self.root, self.profile)["sources"][0]["status"])

    def test_source_changes_during_read_are_not_reported_as_matched(self):
        original = os.fstat
        calls = 0
        def observed(fd):
            nonlocal calls
            calls += 1
            if calls == 2:
                self.source.write_bytes(b"concurrent update")
            return original(fd)
        with patch.object(os, "fstat", observed):
            result = pc.calibrate(self.root, self.profile)
        self.assertEqual("UNSAFE_OR_CHANGED", result["sources"][0]["status"])
        self.assertIsNone(result["sources"][0]["observed_sha256"])

    def test_nonregular_source(self):
        self.source.unlink()
        self.source.mkdir()
        self.assertEqual("UNSAFE_OR_CHANGED", pc.calibrate(self.root, self.profile)["sources"][0]["status"])

    def test_reparse_ancestor_is_rejected_before_source_read(self):
        original = Path.lstat
        def observed(path, *args, **kwargs):
            result = original(path, *args, **kwargs)
            if path == self.source:
                from types import SimpleNamespace
                return SimpleNamespace(st_mode=result.st_mode, st_file_attributes=pc.stat.FILE_ATTRIBUTE_REPARSE_POINT)
            return result
        with patch.object(Path, "lstat", observed), patch.object(Path, "open", side_effect=AssertionError("must not open")):
            self.assertEqual("UNSAFE_OR_CHANGED", pc.calibrate(self.root, self.profile)["sources"][0]["status"])

    def test_root_reparse_is_rejected(self):
        original = Path.lstat
        def observed(path, *args, **kwargs):
            result = original(path, *args, **kwargs)
            if path == self.root:
                from types import SimpleNamespace
                return SimpleNamespace(st_mode=result.st_mode, st_file_attributes=pc.stat.FILE_ATTRIBUTE_REPARSE_POINT)
            return result
        with patch.object(Path, "lstat", observed), self.assertRaisesRegex(pc.InvalidRecord, "reparse"):
            pc.calibrate(self.root, self.profile)

    def test_unreadable_source_does_not_disclose_os_path(self):
        with patch.object(Path, "open", side_effect=PermissionError(str(self.root))):
            result = pc.calibrate(self.root, self.profile)
        self.assertEqual("UNREADABLE", result["sources"][0]["status"])
        self.assertNotIn(str(self.root), json.dumps(result))

    def test_cli_complete_incomplete_invalid_exit_codes(self):
        profile = self.root / "profile.json"
        profile.write_text(json.dumps(self.profile), encoding="utf-8")
        args = ["--project-root", str(self.root), "--profile", str(profile)]
        with contextlib.redirect_stdout(io.StringIO()):
            self.assertEqual(0, pc.main(args))
            self.source.write_bytes(b"changed")
            self.assertEqual(3, pc.main(args))
        with contextlib.redirect_stderr(io.StringIO()):
            self.assertEqual(2, pc.main(["--project-root", "relative", "--profile", str(profile)]))


if __name__ == "__main__":
    unittest.main()
