"""Check selected project-source identities without building or executing a project."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import stat
import sys

from ange_eval import fields, fingerprint, indexed, load, require, strings, text, InvalidRecord


MAX_SOURCE_BYTES = 8 * 1024 * 1024


def relative_source(value):
    text(value, "source.path")
    parts = value.split("/")
    require(not value.startswith("/") and "\\" not in value and ":" not in value,
            "source.path: portable relative path required")
    require(all(p not in ("", ".", "..") and not p.endswith((" ", ".")) for p in parts),
            "source.path: empty, traversal or ambiguous component")
    require(not any(ord(c) < 32 for c in value) and not any(c in value for c in '*?<>|"'),
            "source.path: control characters and wildcards are forbidden")
    require(not any(re.fullmatch(r"CON|PRN|AUX|NUL|COM[1-9¹²³]|LPT[1-9¹²³]", p.split(".")[0].rstrip(" "), re.I)
                    for p in parts), "source.path: reserved device")
    return parts


def validate_profile(profile):
    fields(profile, "schema_version project_id sources candidates", "profile")
    require(type(profile["schema_version"]) is int and profile["schema_version"] == 1,
            "profile: unsupported schema_version")
    text(profile["project_id"], "project_id")
    sources = indexed(profile["sources"], "sources")
    require(len(sources) <= 256, "profile: at most 256 selected sources")
    paths = set()
    for source in sources.values():
        fields(source, "id path sha256", "source")
        relative_source(source["path"])
        # Portable profiles must not ambiguously identify one Windows file twice.
        key = source["path"].casefold()
        require(key not in paths, "profile: duplicate source path")
        paths.add(key)
        digest = source["sha256"]
        require(digest is None or (isinstance(digest, str) and re.fullmatch(r"[0-9a-f]{64}", digest)),
                "source.sha256: lowercase SHA-256 or null required")
    candidates = indexed(profile["candidates"], "candidates")
    for candidate in candidates.values():
        fields(candidate, "id family source_ids success_criteria validation_plan unverified", "candidate")
        for key in ("family", "success_criteria", "validation_plan"):
            text(candidate[key], f"candidate.{key}")
        strings(candidate["source_ids"], "candidate.source_ids", True)
        require(len(set(candidate["source_ids"])) == len(candidate["source_ids"]), "candidate: duplicate source id")
        require(set(candidate["source_ids"]) <= set(sources), "candidate: unknown source id")
        strings(candidate["unverified"], "candidate.unverified")
    return sources, candidates


def reject_linked_path(path):
    # Inspect lexical ancestors before resolving, including the supplied root.
    for part in reversed([path, *path.parents]):
        observed = part.lstat()
        require(not stat.S_ISLNK(observed.st_mode) and
                not (getattr(observed, "st_file_attributes", 0) & stat.FILE_ATTRIBUTE_REPARSE_POINT),
                "linked/reparse path is not allowed")


def stamp(info):
    # Windows path.stat and fstat can expose different legacy ctime semantics
    # (creation vs metadata-change time). Compare identity, size and mtime there.
    return (info.st_dev, info.st_ino, info.st_size, info.st_mtime_ns) + (() if os.name == "nt" else (info.st_ctime_ns,))


def inspect_source(root, source):
    result = {"id": source["id"], "path": source["path"], "expected_sha256": source["sha256"],
              "observed_sha256": None, "bytes": None}
    path = root.joinpath(*relative_source(source["path"]))
    try:
        reject_linked_path(path)
        before = path.stat()
        require(stat.S_ISREG(before.st_mode), "source is not a regular file")
        if before.st_size > MAX_SOURCE_BYTES:
            return dict(result, status="TOO_LARGE")
        digest, size = hashlib.sha256(), 0
        with path.open("rb") as stream:
            require(stamp(os.fstat(stream.fileno())) == stamp(before), "source changed before read")
            while block := stream.read(64 * 1024):
                size += len(block)
                require(size <= MAX_SOURCE_BYTES, "source grew beyond size limit")
                digest.update(block)
            require(stamp(os.fstat(stream.fileno())) == stamp(before), "source changed during read")
        reject_linked_path(path)
        require(stamp(path.stat()) == stamp(before), "source replaced during read")
        observed = digest.hexdigest()
        status = "UNPINNED" if source["sha256"] is None else ("MATCHED" if source["sha256"] == observed else "STALE")
        return dict(result, status=status, observed_sha256=observed, bytes=size)
    except FileNotFoundError:
        return dict(result, status="MISSING")
    except InvalidRecord as error:
        return dict(result, status="UNSAFE_OR_CHANGED", reason=str(error))
    except OSError:
        # Do not leak absolute paths or OS messages into a potentially shared report.
        return dict(result, status="UNREADABLE")


def calibrate(root, profile):
    sources, candidates = validate_profile(profile)
    root = Path(root)
    require(root.is_absolute() and ".." not in root.parts, "project root must be an absolute path without traversal")
    reject_linked_path(root)
    require(root.is_dir(), "project root must be a directory")
    observed = {key: inspect_source(root, source) for key, source in sources.items()}
    results = []
    for candidate in candidates.values():
        mismatches = [key for key in candidate["source_ids"] if observed[key]["status"] != "MATCHED"]
        results.append({"id": candidate["id"], "family": candidate["family"],
                        "source_status": "INCOMPLETE" if mismatches else "MATCHED",
                        "sources_needing_attention": mismatches,
                        "unverified": candidate["unverified"], "trial_readiness": "NOT_ESTABLISHED"})
    return {"schema_version": 1, "project_id": profile["project_id"],
            "profile_sha256": fingerprint(profile), "analysis": "source_identity_only",
            "all_sources_matched": all(s["status"] == "MATCHED" for s in observed.values()),
            "sources": list(observed.values()), "candidates": results,
            "limitations": ["Only explicitly selected file bytes are checked; no full dependency closure or VCS cleanliness claim.",
                            "Sources are read sequentially, not as an atomic whole-project snapshot; concurrent writers must be controlled externally.",
                            "Matching hashes do not establish a recoverable baseline, working oracle, isolated answers or trial readiness.",
                            "No project command, model, game, database or external service was executed.",
                            "No file contents or project-root path are included. Relative paths and identifiers may still be private."]}


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project-root", required=True, type=Path)
    parser.add_argument("--profile", required=True, type=Path)
    args = parser.parse_args(argv)
    try:
        result = calibrate(args.project_root, load(args.profile))
        print(json.dumps(result, ensure_ascii=False, indent=2, allow_nan=False))
        return 0 if result["all_sources_matched"] else 3
    except (InvalidRecord, OSError, UnicodeError, ValueError, TypeError) as error:
        message = "Cannot read calibration inputs or project root" if isinstance(error, OSError) else str(error)
        print(json.dumps({"error": message}, ensure_ascii=False), file=sys.stderr)
        return 2


if __name__ == "__main__":
    sys.exit(main())
