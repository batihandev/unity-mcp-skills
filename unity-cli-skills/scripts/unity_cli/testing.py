from __future__ import annotations

import hashlib
import json
import math
import os
import pathlib
import re
import stat
import time
import subprocess
import xml.etree.ElementTree as ET
from typing import Any

from .authoring import is_link
from .compile import CompileWorkflow, WorkflowRefusal
from .lifecycle import SessionRefusal
from .model import CommandResult
from .reports import ReportPathError, ReportTarget, _PosixDirectoryGuard, _WindowsDirectoryGuard
from .transport import _redact_value


class WorkflowTestRefusal(RuntimeError):
    def __init__(self, code: str, message: str, details: dict[str, Any] | None = None, *, report: dict[str, Any] | None = None):
        super().__init__(message)
        self.code = code
        self.details = details or {}
        self.report = report

    def as_dict(self) -> dict[str, Any]:
        return {**(self.report or {}), "ok": False, "error": {"code": self.code, "message": str(self), "details": self.details}}


def exact_offline_filter(full_name: str) -> str:
    """Return a .NET regex that selects this exact native test fullname."""
    # CommandLineOption.Split treats semicolon as a list separator even inside a quoted value.
    literal = re.escape(full_name).replace(";", r"\u003B")
    return rf"\A{literal}\z"


class TestWorkflow:
    STATUS_PATH = pathlib.Path("Temp/pipeline_test_status.json")
    REQUEST_PATH = pathlib.Path("Temp/pipeline_test_request.json")

    def __init__(self, project: str | pathlib.Path, session, poll_interval: float = 0.25, clock=time):
        self.project = pathlib.Path(project).resolve()
        self.session = session
        self.poll_interval = poll_interval
        self.clock = clock
        self.compile = CompileWorkflow(self.project, session, poll_interval=poll_interval, clock=clock)

    @staticmethod
    def _select_connected(full_names: list[str], expected: str) -> str:
        if not isinstance(expected, str) or not expected:
            raise WorkflowTestRefusal("TEST_IDENTITY_INVALID", "An exact nonempty test identity is required")
        if any(not isinstance(name, str) for name in full_names):
            raise WorkflowTestRefusal("TEST_DISCOVERY_INVALID", "Test discovery contained a missing or malformed FullName")
        candidates = [name for name in full_names if expected.casefold() in name.casefold()]
        exact = [name for name in full_names if name == expected]
        if len(candidates) > 1:
            raise WorkflowTestRefusal("TEST_FILTER_AMBIGUOUS", "The native case-insensitive substring filter would select multiple tests", {"expectedFullName": expected, "candidates": candidates})
        if len(exact) != 1 or len(candidates) != 1:
            raise WorkflowTestRefusal("TEST_EXPECTED_IDENTITY_MISSING", "The exact expected test was not uniquely discovered", {"expectedFullName": expected, "candidates": candidates})
        return exact[0]

    @staticmethod
    def _selectors(test_name=None, test_class=None, assembly=None):
        for key, value in (("test_name", test_name), ("test_class", test_class), ("assembly", assembly)):
            if value is not None and (not isinstance(value, str) or not value.strip()):
                raise WorkflowTestRefusal("INVALID_ARGUMENT", f"{key} must be a nonempty exact identity")
        if test_name is not None and test_class is not None:
            raise WorkflowTestRefusal("INVALID_ARGUMENT", "Choose one exact test name or class")
        return {"testName": test_name, "testClass": test_class, "assembly": assembly}

    @staticmethod
    def _test_class(full_name):
        depth = 0
        quote = None
        escaped = False
        separator = -1
        for index, char in enumerate(full_name):
            if quote is not None:
                if escaped:
                    escaped = False
                elif char == "\\":
                    escaped = True
                elif char == quote:
                    quote = None
            elif char in {"\"", "'"} and depth:
                quote = char
            elif char == "(":
                depth += 1
            elif char == ")":
                depth -= 1
            elif char == "." and depth == 0:
                separator = index
        return full_name[:separator].split("(", 1)[0] if separator >= 0 else ""

    @staticmethod
    def _selection(discovery, mode, selectors, include_explicit=False):
        if not isinstance(discovery, dict) or discovery.get("success") is not True or discovery.get("Mode") != mode or not isinstance(discovery.get("Tests"), list):
            raise WorkflowTestRefusal("TEST_DISCOVERY_INVALID", "Native discovery must contain the requested mode and Tests")
        rows = discovery["Tests"]
        count = discovery.get("Count")
        if not isinstance(count, int) or isinstance(count, bool) or count != len(rows):
            raise WorkflowTestRefusal("TEST_DISCOVERY_INVALID", "Native discovery Count does not match Tests")
        seen = set()
        for row in rows:
            if (not isinstance(row, dict) or not isinstance(row.get("FullName"), str) or not row["FullName"]
                    or row.get("Mode") != mode or not isinstance(row.get("Assembly"), str) or not row["Assembly"]
                    or not isinstance(row.get("Explicit"), bool) or not isinstance(row.get("Categories"), list)
                    or any(not isinstance(c, str) for c in row["Categories"])):
                raise WorkflowTestRefusal("TEST_DISCOVERY_INVALID", "Native discovery contained malformed test metadata")
            pair = (row["Assembly"], row["FullName"])
            if pair in seen:
                raise WorkflowTestRefusal("TEST_DISCOVERY_INVALID", "Native discovery contains a duplicate assembly/fullname identity")
            seen.add(pair)
        selected = [row for row in rows
                    if (selectors["assembly"] is None or row["Assembly"] == selectors["assembly"])
                    and (selectors["testName"] is None or row["FullName"] == selectors["testName"])
                    and (selectors["testClass"] is None or TestWorkflow._test_class(row["FullName"]) == selectors["testClass"])]
        if selectors["testName"] and any(row["Explicit"] for row in selected) and not include_explicit:
            raise WorkflowTestRefusal("TEST_EXPLICIT_REQUIRES_OPT_IN", "The selected exact test is explicit; pass --include-explicit")
        selected = [row for row in selected if include_explicit or not row["Explicit"]]
        if not selected:
            raise WorkflowTestRefusal("TEST_EXPECTED_IDENTITY_MISSING", "The requested selection discovered no eligible tests")
        return json.loads(json.dumps(selected))

    @staticmethod
    def _connected_filter(rows, expected, selectors, include_explicit):
        eligible = [row for row in rows if include_explicit or not row["Explicit"]]
        wanted = {(r["Assembly"], r["FullName"]) for r in expected}
        options = []
        if selectors["testName"] is not None:
            options.append(("testName", selectors["testName"]))
        if selectors["testClass"] is not None:
            options.append(("testName", selectors["testClass"] + "."))
            options.append(("testName", selectors["testClass"] + "("))
            options.append(("testName", selectors["testClass"]))
        if selectors["assembly"] is not None:
            options.append(("assembly", selectors["assembly"]))
        if not options:
            options.append((None, None))
        for kind, value in options:
            native = [r for r in eligible if value is None or value.casefold() in r["Assembly" if kind == "assembly" else "FullName"].casefold()]
            # The SDK submits fullnames to TestRunnerApi, losing assembly ownership.
            names = {r["FullName"] for r in native}
            actual = {(r["Assembly"], r["FullName"]) for r in rows if r["FullName"] in names}
            if actual == wanted and len(names) == len(expected):
                return {} if value is None else {"filter": value, "filter_type": kind}
        code = "TEST_FILTER_AMBIGUOUS" if selectors["testName"] and not selectors["assembly"] else "TEST_FILTER_UNREPRESENTABLE"
        raise WorkflowTestRefusal(code, "One native connected filter cannot select these exact identities; prepare a test-plan and use the offline route", {"expectedTests": expected})

    @staticmethod
    def _validate_leaves(results, summary, expected, mode, *, assembly_bound=False):
        wanted = [{"FullName": expected}] if isinstance(expected, str) else expected
        if not isinstance(wanted, list) or not wanted:
            raise WorkflowTestRefusal("TEST_EXPECTED_IDENTITY_MISSING", "A nonzero frozen expected test set is required")
        expected_pairs = [(r.get("Assembly") if assembly_bound else None, r["FullName"]) for r in wanted]
        actual_pairs = []
        counts = {"Passed": 0, "Failed": 0, "Skipped": 0, "Inconclusive": 0}
        for item in results:
            if item.get("Mode", mode) != mode or item.get("Status") not in counts:
                raise WorkflowTestRefusal("TEST_RESULT_INVALID", "A native leaf has an unexpected mode or status")
            counts[item["Status"]] += 1
            if assembly_bound and (not isinstance(item.get("Assembly"), str) or not item["Assembly"]):
                raise WorkflowTestRefusal("TEST_RESULT_INVALID", "NUnit leaf omitted its parent Assembly identity")
            actual_pairs.append((item.get("Assembly") if assembly_bound else None, item["FullName"]))
        if len(actual_pairs) != len(set(actual_pairs)):
            raise WorkflowTestRefusal("TEST_RESULT_DUPLICATE", "Native report contains duplicate test identities")
        if not actual_pairs or sorted(actual_pairs) != sorted(expected_pairs):
            raise WorkflowTestRefusal("TEST_EXPECTED_IDENTITY_MISSING", "Native report does not contain the complete frozen expected set", {"expectedTests": wanted, "results": results})
        if any(summary[key.lower()] != value for key, value in counts.items()):
            raise WorkflowTestRefusal("TEST_SUMMARY_INVALID", "Native summary status counts disagree with leaf outcomes")
        return counts["Passed"] == len(wanted)

    @staticmethod
    def _require_physical(path):
        if any(is_link(parent) for parent in (path, *path.parents)):
            raise WorkflowTestRefusal("SOURCE_INPUT_OUTSIDE_PROJECT", "Assembly inputs must use physical paths without symlink traversal", {"path": str(path)})

    def _registered_packages(self, lease):
        native = self.session.transport.invoke_command(str(self.project), "package_list", {"scope": "installed", "include_indirect": True}, timeout=lease.remaining(), deadline=lease.transport_deadline)
        lease.verify_current()
        value = self._outer_result(native, "package_list")["result"]
        if (not isinstance(value, dict) or value.get("success") is not True or value.get("scope") != "installed"
                or not isinstance(value.get("packages"), list) or type(value.get("count")) is not int
                or value["count"] != len(value["packages"])):
            raise WorkflowTestRefusal("TEST_PACKAGE_INPUT_INVALID", "Installed package discovery must contain a complete registered package set")
        packages = []
        seen = set()
        for item in value["packages"]:
            if (not isinstance(item, dict) or any(not isinstance(item.get(key), str) or not item[key] for key in ("name", "version", "source", "resolvedPath"))
                    or item.get("isInstalled") is not True or item["name"] in seen):
                raise WorkflowTestRefusal("TEST_PACKAGE_INPUT_INVALID", "Installed package metadata is malformed or duplicated")
            seen.add(item["name"])
            raw = item["resolvedPath"]
            platform = getattr(self.session.transport, "platform", None)
            if platform is not None and platform.name == "wsl" and (re.match(r"^[A-Za-z]:[\\/]", raw) or raw.startswith("\\\\")):
                try:
                    raw = platform._convert_path("-u", raw, lease.remaining(), deadline=lease.transport_deadline)
                except subprocess.TimeoutExpired as exc:
                    lease.remaining()
                    raise WorkflowTestRefusal("CLI_TIMEOUT", "Registered package path conversion reached its local deadline") from exc
            path = pathlib.Path(raw)
            if not path.is_absolute():
                raise WorkflowTestRefusal("TEST_PACKAGE_INPUT_INVALID", "Registered package resolvedPath must be absolute")
            # Built-in Editor packages outside the project are bound by the exact Editor version.
            outside = not path.is_relative_to(self.project)
            if outside and item["source"] == "BuiltIn":
                host_path = None
            else:
                self._require_physical(path)
                if not path.is_dir():
                    raise WorkflowTestRefusal("SOURCE_INPUT_MISSING", "A registered package source root is missing", {"package": item["name"]})
                host_path = str(path)
            packages.append({**{key: item[key] for key in ("name", "version", "source", "resolvedPath", "isInstalled")}, "hostPath": host_path})
        return sorted(packages, key=lambda item: item["name"]), native.data

    def _assembly_inputs(self, packages=(), *, lease=None):
        check_budget = lease.remaining if lease is not None else lambda: None
        check_budget()
        roots = [(self.project / "Assets", "source", False), (self.project / "Packages", "source", False)]
        for item in packages:
            check_budget()
            if (not isinstance(item, dict) or any(not isinstance(item.get(key), str) or not item[key] for key in ("name", "version", "source", "resolvedPath"))
                    or item.get("isInstalled") is not True):
                raise WorkflowTestRefusal("TEST_PLAN_INVALID", "Frozen registered package metadata is invalid")
            root = item.get("hostPath")
            if root is None:
                if item["source"] != "BuiltIn":
                    raise WorkflowTestRefusal("TEST_PLAN_INVALID", "Only version-bound built-in packages can omit a source root")
            elif not isinstance(root, str) or not pathlib.Path(root).is_absolute() or ".." in pathlib.Path(root).parts:
                raise WorkflowTestRefusal("TEST_PLAN_INVALID", "Frozen registered package source root is invalid")
            else:
                roots.append((pathlib.Path(root), "source", True))
        roots.extend([(self.project / "Library/ScriptAssemblies", "compiled", False),
                      (self.project / "ProjectSettings", "settings", False)])
        suffixes = {".cs", ".asmdef", ".asmref", ".rsp", ".meta", ".dll"}
        inventory = {}

        def selected(path, kind):
            if kind == "compiled":
                return path.name.endswith(".dll") or path.name.endswith(".dll.meta")
            if kind == "settings":
                return path.name == "ProjectVersion.txt"
            return (path.suffix.lower() in suffixes or path.name == "package.json"
                    or path in {self.project / "Packages/manifest.json", self.project / "Packages/packages-lock.json"})

        def signature(info):
            return info.st_dev, info.st_ino, info.st_mode, info.st_size, info.st_mtime_ns, info.st_ctime_ns

        def changed(path):
            raise WorkflowTestRefusal("SOURCE_INPUT_OUTSIDE_PROJECT", "An assembly input path changed or traversed a link during inventory", {"path": str(path)})

        def hash_fd(fd, path, before):
            check_budget()
            opened = os.fstat(fd)
            if not stat.S_ISREG(opened.st_mode) or signature(opened) != signature(before):
                changed(path)
            digest = hashlib.sha256()
            while True:
                check_budget()
                block = os.read(fd, 1024 * 1024)
                check_budget()
                if not block:
                    break
                digest.update(block)
            if signature(os.fstat(fd)) != signature(opened):
                changed(path)
            inventory[path] = {"path": str(path), "sha256": digest.hexdigest()}

        def walk_posix(fd, directory, kind):
            check_budget()
            before = os.fstat(fd)
            with os.scandir(fd) as entries:
                for entry in entries:
                    check_budget()
                    path = directory / entry.name
                    info = entry.stat(follow_symlinks=False)
                    if stat.S_ISLNK(info.st_mode) or getattr(info, "st_file_attributes", 0) & 0x400:
                        changed(path)
                    if stat.S_ISDIR(info.st_mode) and kind == "source":
                        child = os.open(entry.name, os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW, dir_fd=fd)
                        try:
                            if signature(os.fstat(child)) != signature(info):
                                changed(path)
                            walk_posix(child, path, kind)
                            if signature(os.stat(entry.name, dir_fd=fd, follow_symlinks=False)) != signature(info):
                                changed(path)
                        finally:
                            os.close(child)
                    elif stat.S_ISREG(info.st_mode) and selected(path, kind):
                        child = os.open(entry.name, os.O_RDONLY | os.O_NOFOLLOW | getattr(os, "O_NONBLOCK", 0), dir_fd=fd)
                        try:
                            hash_fd(child, path, info)
                            if signature(os.stat(entry.name, dir_fd=fd, follow_symlinks=False)) != signature(info):
                                changed(path)
                        finally:
                            os.close(child)
            if signature(os.fstat(fd)) != signature(before):
                changed(directory)
            check_budget()

        def walk_windows(directory, kind):
            check_budget()
            before = directory.lstat()
            guard = _WindowsDirectoryGuard(directory, (before.st_dev, before.st_ino))
            try:
                with os.scandir(directory) as entries:
                    for entry in entries:
                        check_budget()
                        path = directory / entry.name
                        info = entry.stat(follow_symlinks=False)
                        if stat.S_ISLNK(info.st_mode) or getattr(info, "st_file_attributes", 0) & 0x400:
                            changed(path)
                        if stat.S_ISDIR(info.st_mode) and kind == "source":
                            walk_windows(path, kind)
                        elif stat.S_ISREG(info.st_mode) and selected(path, kind):
                            handle, identity = guard.api.open_regular_file(path)
                            try:
                                if identity != (info.st_dev, info.st_ino):
                                    changed(path)
                                # The no-delete-sharing handle pins this file during the CRT open.
                                with path.open("rb") as stream:
                                    hash_fd(stream.fileno(), path, info)
                                if signature(path.lstat()) != signature(info):
                                    changed(path)
                            finally:
                                guard.api.close(handle)
                guard.verify()
                if signature(directory.lstat()) != signature(before):
                    changed(directory)
                check_budget()
            finally:
                guard.close(suppress=True)

        for root, kind, required in dict.fromkeys(roots):
            check_budget()
            self._require_physical(root)
            try:
                before = root.lstat()
            except FileNotFoundError:
                if required:
                    raise WorkflowTestRefusal("SOURCE_INPUT_MISSING", "A registered package source root is missing", {"path": str(root)})
                continue
            if not stat.S_ISDIR(before.st_mode):
                changed(root)
            try:
                if os.name == "nt":
                    walk_windows(root, kind)
                else:
                    guard = _PosixDirectoryGuard(root, (before.st_dev, before.st_ino))
                    try:
                        walk_posix(guard.fd, root, kind)
                        check_budget()
                        self._require_physical(root)
                        guard.verify()
                        # Check the pinned ancestor chain once per root, never once per entry.
                        for ancestor, fd in zip(reversed((root, *root.parents)), guard.fds):
                            check_budget()
                            held = os.fstat(fd)
                            visible = ancestor.lstat()
                            if (visible.st_dev, visible.st_ino) != (held.st_dev, held.st_ino):
                                changed(ancestor)
                    finally:
                        guard.close(suppress=True)
            except ReportPathError as exc:
                raise WorkflowTestRefusal("SOURCE_INPUT_OUTSIDE_PROJECT", "Assembly inventory requires stable physical paths", {"path": str(root), "reason": str(exc)}) from exc
            except OSError as exc:
                raise WorkflowTestRefusal("SOURCE_INPUT_OUTSIDE_PROJECT", "An assembly input became unavailable or changed during inventory", {"path": str(root), "reason": str(exc)}) from exc
            check_budget()
        return [inventory[path] for path in sorted(inventory)]

    def plan(self, *, source_paths, output, mode="EditMode", test_name=None, test_class=None, assembly=None, include_explicit=False):
        selectors = self._selectors(test_name, test_class, assembly)
        if mode not in {"EditMode", "PlayMode"} or not isinstance(include_explicit, bool):
            raise WorkflowTestRefusal("INVALID_ARGUMENT", "Plan requires a supported mode and Boolean include_explicit")
        target = self._validate_output(self.project, output)
        try:
            with self.session.workflow_session(deadline=self.session._deadline()) as lease:
                compiled = self.compile.run_with_lease(source_paths, lease)
                sources = self.compile._sources(source_paths)
                source_inputs = self.compile._hash_sources(sources, check_budget=lease.remaining)
                self._clean_scenes(lease)
                native = self.session.transport.invoke_command(str(self.project), "list_tests", {"mode": "editor" if mode == "EditMode" else "playmode"}, timeout=lease.remaining(), deadline=lease.transport_deadline)
                discovery = self._outer_result(native, "list_tests")["result"]
                expected = self._selection(discovery, mode, selectors, include_explicit)
                packages, package_envelope = self._registered_packages(lease)
                assembly_inputs = self._assembly_inputs(packages, lease=lease)
                self._postflight(lease)
                final_packages, _ = self._registered_packages(lease)
                if final_packages != packages:
                    raise WorkflowTestRefusal("TEST_PLAN_STALE", "Registered package identities changed during test discovery")
                if source_inputs != self.compile._hash_sources(sources, check_budget=lease.remaining) or assembly_inputs != self._assembly_inputs(packages, lease=lease):
                    raise WorkflowTestRefusal("TEST_PLAN_STALE", "Source or assembly inputs changed during test discovery")
                from unity_session import project_editor_version
                plan = {"schemaVersion": 1, "kind": "unity-native-test-plan", "project": str(self.project), "mode": mode,
                        "selectors": selectors, "includeExplicit": include_explicit, "expectedTests": expected,
                        "nativeDiscovery": discovery, "nativeDiscoveryEnvelope": native.data,
                        "editorIdentity": lease.identity.as_dict(), "editorVersion": project_editor_version(self.project),
                        "sourceInputs": source_inputs, "assemblyInputs": assembly_inputs, "registeredPackages": packages,
                        "nativePackageEnvelope": package_envelope, "compile": compiled,
                        "createdAtUnix": self.clock.time()}
                payload = json.dumps(plan, sort_keys=True, separators=(",", ":")).encode("utf-8")
                self._publish_target(target, payload)
                return {"ok": True, "action": "test-plan", "reportPath": str(target.path), "reportSha256": hashlib.sha256(payload).hexdigest(), "expectedTests": expected}
        except (WorkflowRefusal, SessionRefusal) as exc:
            raise WorkflowTestRefusal(exc.code, str(exc), exc.details) from exc
        finally:
            target.close()

    def _clean_scenes(self, lease):
        dirty = self.session.transport.dirty_scenes(lease.identity, timeout=lease.remaining(), deadline=lease.transport_deadline)
        lease.verify_current()
        if not dirty.success or not isinstance(dirty.data, list):
            raise WorkflowTestRefusal("DIRTY_SCENE_STATE_UNKNOWN", "Clean-scene state could not be established before tests")
        if dirty.data:
            raise WorkflowTestRefusal("DIRTY_SCENES", "Save or discard dirty scenes explicitly before tests", {"scenes": dirty.data})

    def _bound_plan(self, discovery, mode, selectors, include_explicit, source_inputs, version, *, lease):
        if discovery is None:
            raise WorkflowTestRefusal("TEST_PLAN_REQUIRED", "Offline suite selection requires --discovery from test-plan")
        plan = self._read_json_file(pathlib.Path(discovery))
        if (not isinstance(plan, dict) or plan.get("schemaVersion") != 1 or plan.get("kind") != "unity-native-test-plan"
                or plan.get("project") != str(self.project) or plan.get("mode") != mode
                or plan.get("selectors") != selectors or plan.get("includeExplicit") is not include_explicit
                or plan.get("editorVersion") != version or not isinstance(plan.get("editorIdentity"), dict)
                or not isinstance(plan.get("createdAtUnix"), (int, float)) or not isinstance(plan.get("registeredPackages"), list)):
            raise WorkflowTestRefusal("TEST_PLAN_INVALID", "Discovery plan does not match the exact project, mode, selectors, and Editor version")
        if plan.get("sourceInputs") != source_inputs or plan.get("assemblyInputs") != self._assembly_inputs(plan.get("registeredPackages", ()), lease=lease):
            raise WorkflowTestRefusal("TEST_PLAN_STALE", "Source or compiled assembly inventory differs from the discovery plan; prepare a fresh test-plan")
        expected = self._selection(plan.get("nativeDiscovery"), mode, selectors, include_explicit)
        if plan.get("expectedTests") != expected:
            raise WorkflowTestRefusal("TEST_PLAN_INVALID", "Frozen expected identities disagree with native discovery")
        return expected, plan["registeredPackages"]

    @staticmethod
    def _validate_output(project: pathlib.Path, output: str | pathlib.Path) -> ReportTarget:
        try:
            return ReportTarget(project, output)
        except ReportPathError as exc:
            raise WorkflowTestRefusal(exc.code, str(exc), exc.details) from exc

    @staticmethod
    def _parse_nunit(payload: bytes, expected: str, mode: str, *, allow_nonpassing: bool = False) -> dict[str, Any]:
        try:
            root = ET.fromstring(payload)
        except (ET.ParseError, ValueError) as exc:
            raise WorkflowTestRefusal("TEST_REPORT_INVALID", "Native NUnit XML is malformed", {"reason": str(exc)}) from exc
        if root.tag != "test-run":
            raise WorkflowTestRefusal("TEST_REPORT_INVALID", "Native NUnit XML root must be test-run", {"root": root.tag})
        names = ("total", "passed", "failed", "inconclusive", "skipped")
        summary = {}
        for name in names:
            raw = root.attrib.get(name)
            if raw is None or not raw.isdecimal():
                raise WorkflowTestRefusal("TEST_SUMMARY_INVALID", "NUnit summary counts must be nonnegative integers", {"field": name, "value": raw})
            summary[name] = int(raw)
        if summary["total"] != sum(summary[name] for name in ("passed", "failed", "inconclusive", "skipped")):
            raise WorkflowTestRefusal("TEST_SUMMARY_INVALID", "NUnit summary counts do not add up", {"summary": summary})
        start_time, end_time, raw_duration = (root.attrib.get("start-time"), root.attrib.get("end-time"), root.attrib.get("duration"))
        if not isinstance(start_time, str) or not start_time or not isinstance(end_time, str) or not end_time:
            raise WorkflowTestRefusal("TEST_REPORT_INVALID", "NUnit test-run must retain its start-time and end-time")
        try:
            duration = float(raw_duration)
        except (TypeError, ValueError) as exc:
            raise WorkflowTestRefusal("TEST_REPORT_INVALID", "NUnit test-run duration must be numeric", {"duration": raw_duration}) from exc
        if not math.isfinite(duration) or duration < 0:
            raise WorkflowTestRefusal("TEST_REPORT_INVALID", "NUnit test-run duration must be finite and nonnegative", {"duration": raw_duration})
        results = []
        parents = {child: parent for parent in root.iter() for child in parent}
        for leaf in root.iter("test-case"):
            fullname = leaf.attrib.get("fullname")
            status = leaf.attrib.get("result")
            if not fullname or not status:
                raise WorkflowTestRefusal("TEST_RESULT_INVALID", "NUnit test-case omitted fullname or result")
            item = {"FullName": fullname, "Status": status}
            ancestor = parents.get(leaf)
            while ancestor is not None:
                if ancestor.tag == "test-suite" and ancestor.attrib.get("type") == "Assembly":
                    owner = ancestor.attrib.get("name") or ancestor.attrib.get("fullname")
                    if not owner:
                        raise WorkflowTestRefusal("TEST_RESULT_INVALID", "NUnit Assembly suite omitted its name")
                    item["Assembly"] = pathlib.PureWindowsPath(owner).name.removesuffix(".dll")
                    break
                ancestor = parents.get(ancestor)
            raw_leaf_duration = leaf.attrib.get("duration")
            if raw_leaf_duration is not None:
                try:
                    leaf_duration = float(raw_leaf_duration)
                except ValueError as exc:
                    raise WorkflowTestRefusal("TEST_RESULT_INVALID", "NUnit test-case duration must be numeric", {"fullName": fullname}) from exc
                if not math.isfinite(leaf_duration) or leaf_duration < 0:
                    raise WorkflowTestRefusal("TEST_RESULT_INVALID", "NUnit test-case duration must be finite and nonnegative", {"fullName": fullname})
                item["Duration"] = leaf_duration
            failure = leaf.find("failure")
            if failure is not None:
                message = failure.findtext("message")
                stack = failure.findtext("stack-trace")
                item["Message"] = "" if message is None else message
                item["StackTrace"] = "" if stack is None else stack
            results.append(item)
        if len(results) != summary["total"]:
            raise WorkflowTestRefusal("TEST_SUMMARY_INVALID", "NUnit leaf result count differs from its summary")
        passed_leaves = TestWorkflow._validate_leaves(results, summary, expected, mode, assembly_bound=not isinstance(expected, str))
        native_result = root.attrib.get("result")
        passed = passed_leaves and native_result == "Passed"
        if not allow_nonpassing and not passed:
            raise WorkflowTestRefusal("TEST_RESULT_NOT_PASSED", "Every selected NUnit leaf must pass", {"expectedFullName": expected, "summary": summary, "results": results})
        return {"mode": mode, "summary": summary, "results": results, "nativeResult": native_result,
                "startTime": start_time, "endTime": end_time, "duration": duration, "passed": passed}

    def run(self, *, route: str, mode: str = "EditMode", test_name: str | None = None, test_class: str | None = None,
            assembly: str | None = None, discovery: str | pathlib.Path | None = None, source_paths, output: str | pathlib.Path,
            include_explicit: bool = False, editor_version: str | None = None) -> dict[str, Any]:
        if route not in {"connected", "offline"}:
            raise WorkflowTestRefusal("INVALID_ARGUMENT", "Route must be connected or offline")
        if mode not in {"EditMode", "PlayMode"}:
            raise WorkflowTestRefusal("INVALID_ARGUMENT", "Mode must be EditMode or PlayMode")
        selectors = self._selectors(test_name, test_class, assembly)
        if route == "connected" and discovery is not None:
            raise WorkflowTestRefusal("INVALID_ARGUMENT", "--discovery applies only to offline tests")
        if editor_version is not None and (not isinstance(editor_version, str) or not editor_version.strip()):
            raise WorkflowTestRefusal("INVALID_ARGUMENT", "editor_version must be omitted or a nonempty string")
        if not isinstance(include_explicit, bool):
            raise WorkflowTestRefusal("INVALID_ARGUMENT", "include_explicit must be Boolean")
        if route == "offline" and include_explicit:
            raise WorkflowTestRefusal("OFFLINE_EXPLICIT_UNSUPPORTED", "The installed top-level Unity test command exposes no explicit-test inclusion switch")
        deadline = self.session._deadline()
        target = self._validate_output(self.project, output)
        try:
            if route == "connected":
                return self._run_connected(target, mode, selectors, source_paths, include_explicit, deadline)
            return self._run_offline(target, mode, selectors, source_paths, editor_version, deadline, discovery)
        except WorkflowTestRefusal:
            raise
        except WorkflowRefusal as exc:
            raise WorkflowTestRefusal(exc.code, str(exc), exc.details) from exc
        except SessionRefusal as exc:
            raise WorkflowTestRefusal(exc.code, str(exc), exc.details) from exc
        finally:
            target.close()

    def _run_connected(self, target: ReportTarget, mode: str, selectors, source_paths, include_explicit: bool, deadline: float) -> dict[str, Any]:
        report = {"schemaVersion": 1, "action": "test", "ok": False, "route": "connected",
                  "project": str(self.project), "mode": mode, "selectors": selectors,
                  "expectedFullName": selectors["testName"], "expectedTests": None,
                  "editorIdentity": None, "sourceInputs": None, "nativeCommands": [],
                  "polls": [], "terminal": None, "statusFile": None, "freshness": {}}
        error = None
        result = None
        try:
            with self.session.workflow_session(deadline=deadline) as lease:
                report["editorIdentity"] = lease.identity.as_dict()
                sources = self.compile._sources(source_paths)
                before_sources = self.compile._hash_sources(sources, check_budget=lease.remaining)
                report["sourceInputs"] = before_sources
                compile_result = self.compile.run_with_lease(source_paths, lease)
                report["compile"] = compile_result
                dirty = self.session.transport.dirty_scenes(lease.identity, timeout=lease.remaining(), deadline=lease.transport_deadline)
                lease.verify_current()
                if not dirty.success or not isinstance(dirty.data, list):
                    raise WorkflowTestRefusal("DIRTY_SCENE_STATE_UNKNOWN", "Clean-scene state could not be established before tests", {"code": dirty.code, "message": dirty.message})
                if dirty.data:
                    raise WorkflowTestRefusal("DIRTY_SCENES", "Save or discard dirty scenes explicitly before running tests", {"scenes": dirty.data})
                mode_arg = "editor" if mode == "EditMode" else "playmode"
                discovery_result = self.session.transport.invoke_command(str(self.project), "list_tests", {"mode": mode_arg}, timeout=lease.remaining(), deadline=lease.transport_deadline)
                report["nativeCommands"].append({"command": "list_tests", **vars(discovery_result)})
                discovery_data = self._outer_result(discovery_result, "list_tests")["result"]
                report["discovery"] = discovery_data
                if not isinstance(discovery_data, dict) or discovery_data.get("success") is not True or discovery_data.get("Mode") != mode or not isinstance(discovery_data.get("Tests"), list):
                    raise WorkflowTestRefusal("TEST_DISCOVERY_INVALID", "Native test discovery response has an unexpected mode or shape")
                expected = self._selection(discovery_data, mode, selectors, include_explicit)
                report["expectedTests"] = expected
                native_filter = self._connected_filter(discovery_data["Tests"], expected, selectors, include_explicit)
                status_path = self.project / self.STATUS_PATH
                request_path = self.project / self.REQUEST_PATH
                if request_path.exists():
                    raise WorkflowTestRefusal("TEST_REQUEST_ALREADY_RUNNING", "A native test request file already exists; inspect the current run before submitting another")
                before_status = self._fingerprint(status_path)
                report["freshness"]["statusFileBefore"] = before_status
                args = {"mode": mode_arg, **native_filter, "async_tests": True, "include_explicit": include_explicit}
                trigger = self.session.transport.invoke_command(str(self.project), "run_tests", args, timeout=lease.remaining(), deadline=lease.transport_deadline)
                report["triggerEnvelope"] = trigger.data
                report["nativeCommands"].append({"command": "run_tests", **vars(trigger)})
                lease.verify_current()
                trigger_data = self._outer_result(trigger, "run_tests")["result"]
                report["trigger"] = trigger_data
                if not isinstance(trigger_data, dict) or trigger_data.get("success") is not True or trigger_data.get("result") != "running" or trigger_data.get("Mode") != mode:
                    raise WorkflowTestRefusal("TEST_TRIGGER_INVALID", "Native test acknowledgement did not confirm the expected asynchronous run", {"acknowledgement": trigger.data})
                expected_status_path = str(self.STATUS_PATH).replace("\\", "/")
                if trigger_data.get("StatusPath") != expected_status_path:
                    raise WorkflowTestRefusal("TEST_TRIGGER_INVALID", "Native test acknowledgement returned an unexpected status path", {"statusPath": trigger_data.get("StatusPath")})
                polls = report["polls"]
                terminal = None
                while True:
                    lease.verify_current()
                    poll = self.session.transport.invoke_command(str(self.project), "test_status", {}, timeout=lease.remaining(), deadline=lease.transport_deadline)
                    report["nativeCommands"].append({"command": "test_status", **vars(poll)})
                    if not poll.success:
                        if poll.code not in {"CLI_TIMEOUT", "CLI_EXEC_FAILED", "CLI_COMMAND_FAILED", "CONNECTION_RESET", "CONNECTION_REFUSED"}:
                            raise WorkflowTestRefusal("TEST_STATUS_UNAVAILABLE", "Native test status could not be read", {"code": poll.code, "message": poll.message})
                        polls.append({"ok": False, "code": poll.code})
                        self._sleep(lease)
                        continue
                    data = self._outer_result(poll, "test_status")["result"]
                    state = data
                    report["lastStatus"] = state
                    if not isinstance(state, dict) or not isinstance(state.get("status"), str):
                        raise WorkflowTestRefusal("TEST_STATUS_INVALID", "Native test status omitted a string status")
                    polls.append({"ok": True, "status": state.get("status"), "invocation": poll.diagnostics.get("invocation")})
                    if state["status"] == "completed":
                        terminal = state
                        report["terminal"] = terminal
                        report["summary"] = terminal.get("summary")
                        report["results"] = terminal.get("results")
                        report["duration"] = terminal.get("duration")
                        break
                    if state["status"] in {"error", "no_tests"}:
                        raise WorkflowTestRefusal("TEST_RUN_FAILED", "Native test workflow ended without a completed run", {"status": state})
                    if state["status"] != "running":
                        raise WorkflowTestRefusal("TEST_STATUS_INVALID", "Native test workflow returned an unsupported status", {"status": state["status"]})
                    self._sleep(lease)
                after_status = self._fingerprint(status_path)
                report["freshness"]["statusFileAfter"] = after_status
                if after_status["exists"]:
                    report["statusFile"] = self._read_json_file(status_path)
                if not after_status["exists"] or after_status == before_status:
                    raise WorkflowTestRefusal("TEST_STATUS_NOT_FRESH", "Native test status file was not rewritten by this run", {"before": before_status, "after": after_status})
                disagreements = self._compare_status_views(report["statusFile"], terminal)
                report["freshness"].update(terminalMatchesStatusFile=report["statusFile"] == terminal,
                    terminalProtocolMatchesStatusFile=True, durationDisagreements=disagreements,
                    writerLimitation="Freshness is bound by the observed status rewrite and cooperating project lock; the native protocol has no run ID, so uncooperative external writers cannot be excluded.")
                parsed = self._validate_connected_terminal(terminal, expected, mode)
                report["postflight"] = {}
                self._postflight(lease, observations=report["postflight"], native_commands=report["nativeCommands"])
                after_sources = self.compile._hash_sources(sources, check_budget=lease.remaining)
                report["sourceInputsAfter"] = after_sources
                if before_sources != after_sources:
                    raise WorkflowTestRefusal("SOURCE_INPUT_CHANGED", "An explicit source changed during the test workflow", {"before": before_sources, "after": after_sources})
                result = {"ok": True, "action": "test", "route": "connected", "mode": mode, "expectedFullName": selectors["testName"], "expectedTests": expected,
                        "reportPath": str(target.path), "summary": parsed["summary"],
                        "results": parsed["results"], "duration": terminal.get("duration"),
                        "sourceInputs": before_sources, "freshness": report["freshness"]}
        except (WorkflowTestRefusal, WorkflowRefusal, SessionRefusal) as exc:
            if isinstance(exc, SessionRefusal) and exc.code in {"ACTION_TIMEOUT", "READY_TIMEOUT"}:
                error = WorkflowTestRefusal("TEST_RUN_TIMEOUT", "The connected test observation reached its deadline; the native run may still be active and was not cancelled", {"requestPath": str(self.project / self.REQUEST_PATH), "nativeMayStillBeRunning": True})
            else:
                error = WorkflowTestRefusal(exc.code, str(exc), exc.details)
        if error is not None and report["statusFile"] is None:
            try:
                fingerprint = self._fingerprint(self.project / self.STATUS_PATH)
                report["freshness"]["statusFileAfter"] = fingerprint
                if error.code == "TEST_RUN_TIMEOUT":
                    error.details["statusFingerprint"] = fingerprint
                if fingerprint["exists"]:
                    report["statusFile"] = self._read_json_file(self.project / self.STATUS_PATH)
            except WorkflowTestRefusal as inspection_error:
                report["statusFileInspectionError"] = inspection_error.as_dict()["error"]
        report["ok"] = error is None
        if error is not None:
            report["error"] = error.as_dict()["error"]
        payload = json.dumps(_redact_value(report), sort_keys=True, separators=(",", ":")).encode("utf-8")
        try:
            self._publish_target(target, payload)
        except WorkflowTestRefusal as publication_error:
            details = dict(publication_error.details)
            if error is not None:
                details["originalWorkflowError"] = report["error"]
            raise WorkflowTestRefusal(publication_error.code, str(publication_error), details, report=report) from publication_error
        report_sha256 = hashlib.sha256(payload).hexdigest()
        if error is not None:
            report["reportSha256"] = report_sha256
            raise WorkflowTestRefusal(error.code, str(error), {**error.details, "reportPath": str(target.path), "reportSha256": report_sha256, "reportPreserved": True}, report=report) from error
        result["reportSha256"] = report_sha256
        return result

    @staticmethod
    def _valid_connected_duration(value):
        if type(value) not in {int, float} or value < 0:
            return False
        try:
            return math.isfinite(value)
        except OverflowError:
            return False

    @staticmethod
    def _compare_status_views(file_state, terminal):
        disagreements = []
        def duration(value, path, code):
            if not TestWorkflow._valid_connected_duration(value):
                raise WorkflowTestRefusal(code, "Connected duration must be finite and nonnegative", {"field": path})
        for state in (file_state, terminal):
            if not isinstance(state, dict):
                raise WorkflowTestRefusal("TEST_STATUS_INVALID", "Native status must be an object")
            if "duration" in state:
                duration(state["duration"], "duration", "TEST_STATUS_INVALID")
            for index, item in enumerate(state.get("results", []) if isinstance(state.get("results"), list) else []):
                if isinstance(item, dict) and "Duration" in item:
                    duration(item["Duration"], f"results[{index}].Duration", "TEST_RESULT_INVALID")
        def compare(left, right, path=""):
            metric = path == "duration" or re.fullmatch(r"results\[\d+\]\.Duration", path) is not None
            if metric:
                if left != right:
                    disagreements.append({"path": path, "statusFile": left, "transport": right})
                return
            if type(left) is not type(right):
                raise WorkflowTestRefusal("TEST_STATUS_MISMATCH", "Native test_status response does not match the fresh status file", {"field": path})
            if isinstance(left, dict) and left.keys() == right.keys():
                for key in left:
                    compare(left[key], right[key], f"{path}.{key}" if path else key)
                return
            if isinstance(left, list) and len(left) == len(right):
                for index, (a, b) in enumerate(zip(left, right)):
                    compare(a, b, f"{path}[{index}]")
                return
            if left != right:
                raise WorkflowTestRefusal("TEST_STATUS_MISMATCH", "Native test_status response does not match the fresh status file", {"field": path})
        compare(file_state, terminal)
        return disagreements

    def _run_offline(self, target: ReportTarget, mode: str, selectors, source_paths, editor_version: str | None, deadline: float, discovery) -> dict[str, Any]:
        try:
            from unity_session import InputRefusal, project_editor_version
        except ImportError as exc:
            raise WorkflowTestRefusal("EDITOR_VERSION_UNKNOWN", "The project version reader is unavailable", {"reason": str(exc)}) from exc
        try:
            version = editor_version or project_editor_version(self.project)
        except InputRefusal as exc:
            raise WorkflowTestRefusal(getattr(exc, "code", "EDITOR_VERSION_UNKNOWN"), str(exc), getattr(exc, "details", {})) from exc
        stage = None
        stage_file = None
        try:
            with self.session.offline_workflow_session(deadline=deadline) as lease:
                lease.verify_stopped()
                sources = self.compile._sources(source_paths)
                before_sources = self.compile._hash_sources(sources, check_budget=lease.remaining)
                legacy_exact = selectors["testName"] is not None and selectors["assembly"] is None and discovery is None
                if legacy_exact:
                    expected, packages = selectors["testName"], []
                else:
                    expected, packages = self._bound_plan(discovery, mode, selectors, False, before_sources, version, lease=lease)
                bound_assembly_inputs = None if legacy_exact else self._assembly_inputs(packages, lease=lease)
                stage = target.create_stage()
                stage_file = stage.path
                if selectors["testName"] is not None:
                    test_filter = exact_offline_filter(selectors["testName"])
                elif selectors["testClass"] is not None:
                    test_filter = "|".join(exact_offline_filter(row["FullName"]) for row in expected)
                else:
                    test_filter = None
                stage.verify()
                try:
                    extra = {"assembly": selectors["assembly"]} if selectors["assembly"] is not None else {}
                    result = self.session.transport.invoke_native_test(str(self.project), mode, test_filter, stage.native_path(), version, lease.remaining(), deadline=lease.transport_deadline, **extra)
                finally:
                    stage.verify()
                if not result.success and result.code == "CLI_TIMEOUT":
                    raise WorkflowTestRefusal("OFFLINE_TEST_TIMEOUT", "The host timed out while Unity may still be cleaning up; report presence is uncertain", {"invocation": result.diagnostics.get("invocation"), "stagePath": str(stage_file)})
                lease.verify_stopped()
                report_bytes = stage.read_bytes()
                if report_bytes is None:
                    raise WorkflowTestRefusal("TEST_REPORT_MISSING", "Unity did not produce the fresh persistent NUnit report", {"nativeSuccess": result.success, "code": result.code, "invocation": result.diagnostics.get("invocation"), "nativeCommandDiagnostics": result.diagnostics, "stagePath": str(stage_file)})
                try:
                    parsed = self._parse_nunit(report_bytes, expected, mode, allow_nonpassing=True)
                except WorkflowTestRefusal as exc:
                    raise WorkflowTestRefusal(exc.code, str(exc), {**exc.details, "stagePath": str(stage_file), "reportPreserved": True}) from exc
                after_sources = self.compile._hash_sources(sources, check_budget=lease.remaining)
                if bound_assembly_inputs is not None and bound_assembly_inputs != self._assembly_inputs(packages, lease=lease):
                    raise WorkflowTestRefusal("TEST_PLAN_STALE", "Source or assembly inventory changed during the offline test run", {"stagePath": str(stage_file), "reportPreserved": True})
                if before_sources != after_sources:
                    raise WorkflowTestRefusal("SOURCE_INPUT_CHANGED", "An explicit source changed during the offline test workflow", {"before": before_sources, "after": after_sources, "stagePath": str(stage_file), "reportPreserved": True})
                self._publish_target(target, report_bytes)
                stage_cleaned = stage.cleanup()
                stage_retained = not stage_cleaned
                if not parsed["passed"] or not result.success:
                    raise WorkflowTestRefusal("OFFLINE_TEST_FAILED", "The native Unity test command or selected test did not pass", {"code": result.code, "message": result.message, "invocation": result.diagnostics.get("invocation"), "nativeCommandDiagnostics": result.diagnostics, "reportPath": str(target.path), "reportPreserved": True, "summary": parsed["summary"], "results": parsed["results"], "startTime": parsed["startTime"], "endTime": parsed["endTime"], "duration": parsed["duration"], "stagingDirectoryRetained": stage_retained, **({"stagingDirectoryPath": stage.retained_path} if stage_retained else {})})
                return {"ok": True, "action": "test", "route": "offline", "mode": mode, "expectedFullName": selectors["testName"], "expectedTests": expected,
                        "reportPath": str(target.path), "reportSha256": hashlib.sha256(report_bytes).hexdigest(),
                        "nativeInvocation": result.diagnostics.get("invocation"), "editorVersion": version,
                        "sourceInputs": before_sources, "summary": parsed["summary"], "results": parsed["results"],
                        "startTime": parsed["startTime"], "endTime": parsed["endTime"], "duration": parsed["duration"],
                        "stagingDirectoryRetained": stage_retained,
                        **({"stagingDirectoryPath": stage.retained_path} if stage_retained else {}),
                        "freshness": {"sourceInputsStable": True, "projectStoppedBeforeAndAfter": True,
                                      "compileProof": "native-top-level-test-command-performed-compilation; no separate connected compile status is claimed"}}
        except WorkflowTestRefusal:
            raise
        except (SessionRefusal, ReportPathError) as exc:
            details = dict(getattr(exc, "details", {}))
            if stage_file is not None:
                details.setdefault("stagePath", str(stage_file))
                if stage is not None and stage._report_identity is not None:
                    details.setdefault("reportPreserved", True)
            raise WorkflowTestRefusal(getattr(exc, "code", "TEST_REPORT_PUBLISH_FAILED"), str(exc), details) from exc

    def _validate_connected_terminal(self, payload: dict[str, Any], expected: str, mode: str) -> dict[str, Any]:
        summary = payload.get("summary")
        results = payload.get("results")
        if not isinstance(summary, dict) or not isinstance(results, list):
            raise WorkflowTestRefusal("TEST_STATUS_INVALID", "Terminal connected test status omitted summary or results")
        keys = ("total", "passed", "failed", "skipped", "inconclusive")
        clean = {}
        for key in keys:
            value = summary.get(key)
            if not isinstance(value, int) or isinstance(value, bool) or value < 0:
                raise WorkflowTestRefusal("TEST_SUMMARY_INVALID", "Connected test summary counts must be nonnegative integers", {"field": key})
            clean[key] = value
        if clean["total"] != sum(clean[key] for key in keys[1:]) or clean["total"] != len(results):
            raise WorkflowTestRefusal("TEST_SUMMARY_INVALID", "Connected test summary does not match the result array", {"summary": clean, "resultCount": len(results)})
        duration = payload.get("duration")
        if duration is not None and not self._valid_connected_duration(duration):
            raise WorkflowTestRefusal("TEST_STATUS_INVALID", "Connected test duration must be finite and nonnegative")
        names = []
        for item in results:
            if not isinstance(item, dict) or not isinstance(item.get("FullName"), str) or not isinstance(item.get("Status"), str):
                raise WorkflowTestRefusal("TEST_RESULT_INVALID", "Connected test result omitted FullName or Status")
            item_duration = item.get("Duration")
            if item_duration is not None and not self._valid_connected_duration(item_duration):
                raise WorkflowTestRefusal("TEST_RESULT_INVALID", "Connected result duration must be finite and nonnegative", {"fullName": item["FullName"]})
            for field in ("Message", "StackTrace"):
                if field in item and item[field] is not None and not isinstance(item[field], str):
                    raise WorkflowTestRefusal("TEST_RESULT_INVALID", f"Connected result {field} must be string or null", {"fullName": item["FullName"]})
            names.append(item["FullName"])
        if payload.get("Mode", payload.get("mode", mode)) != mode:
            raise WorkflowTestRefusal("TEST_RESULT_INVALID", "Native terminal mode differs from the requested mode")
        passed = self._validate_leaves(results, clean, expected, mode)
        if not passed:
            raise WorkflowTestRefusal("TEST_RESULT_NOT_PASSED", "Every selected native test must pass without skipped or inconclusive results", {"summary": clean, "results": results})
        return {"summary": clean, "results": results}

    def _postflight(self, lease, *, observations=None, native_commands=None):
        observations = {} if observations is None else observations
        try:
            observations["lastPhase"] = "compile"
            final_compile = self._invoke_result(lease, "recompile_status", observations=observations, native_commands=native_commands)
            observations["compile"] = final_compile
            state = CompileWorkflow._validate_status(final_compile)
            if state["status"] not in {"completed", "up_to_date"} or state["failed"] or state["errors"] or state["compilationFailed"]:
                raise WorkflowTestRefusal("COMPILATION_FAILED", "Post-test native compile status is not clean", {"status": state})
            observations["lastPhase"] = "groundTruth"
            _, ground_truth, _, _ = self.compile._fresh_ground_truth(lease, observations=observations)
            observations["groundTruth"] = ground_truth
            observations["lastPhase"] = "editor"
            editor = self.compile._editor_state(lease, observations=observations, observation_key="editorObservation")[0]
            observations["editor"] = editor
            observations["lastPhase"] = "identity"
            lease.verify_current()
            observations["lastPhase"] = "completed"
            return state, ground_truth, editor
        except (WorkflowRefusal, WorkflowTestRefusal, SessionRefusal) as exc:
            observations["error"] = {"code": exc.code, "message": str(exc), "details": exc.details}
            if isinstance(exc, WorkflowRefusal):
                raise WorkflowTestRefusal(exc.code, str(exc), exc.details) from exc
            raise

    def _invoke_result(self, lease, command: str, *, observations=None, native_commands=None) -> dict[str, Any]:
        lease.verify_current()
        result = self.session.transport.invoke_command(str(self.project), command, {}, timeout=lease.remaining(), deadline=lease.transport_deadline)
        if native_commands is not None:
            native_commands.append({"command": command, **vars(result)})
        if observations is not None:
            observations["compileObservation"] = {"nativeEnvelope": result.data, "invocation": result.diagnostics.get("invocation")}
        data = CompileWorkflow._outer_result(result, command)["result"]
        return data

    @staticmethod
    def _outer_result(result: CommandResult, command: str) -> dict[str, Any]:
        return CompileWorkflow._outer_result(result, command)

    @staticmethod
    def _fingerprint(path: pathlib.Path) -> dict[str, Any]:
        try:
            raw = path.read_bytes()
            stat = path.stat()
            return {"exists": True, "size": len(raw), "mtimeNs": stat.st_mtime_ns, "sha256": hashlib.sha256(raw).hexdigest()}
        except FileNotFoundError:
            return {"exists": False, "size": None, "mtimeNs": None, "sha256": None}
        except OSError as exc:
            raise WorkflowTestRefusal("TEST_STATUS_FILE_INVALID", "Native test status file could not be inspected", {"reason": str(exc)}) from exc

    @staticmethod
    def _read_json_file(path: pathlib.Path) -> Any:
        try:
            return json.loads(path.read_text(encoding="utf-8"))
        except (OSError, UnicodeError, json.JSONDecodeError) as exc:
            raise WorkflowTestRefusal("TEST_STATUS_FILE_INVALID", "Native test status file is not valid UTF-8 JSON", {"reason": str(exc)}) from exc

    def _sleep(self, lease) -> None:
        self.clock.sleep(min(self.poll_interval, lease.remaining()))

    @staticmethod
    def _publish_target(target: ReportTarget, payload: bytes) -> None:
        try:
            target.publish(payload)
        except ReportPathError as exc:
            raise WorkflowTestRefusal(exc.code, str(exc), exc.details) from exc
