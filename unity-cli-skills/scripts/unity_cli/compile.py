from __future__ import annotations

import hashlib
import json
import math
import pathlib
import time
import subprocess
from typing import Any

from .lifecycle import SessionRefusal


STATUS_RELATIVE_PATH = pathlib.Path("Temp/pipeline_recompile_status.json")
GROUND_TRUTH_MAX_AGE_MS = 2000
RETRYABLE_READ_CODES = {"CLI_TIMEOUT", "CLI_EXEC_FAILED", "CLI_COMMAND_FAILED", "CONNECTION_RESET", "CONNECTION_REFUSED"}


class WorkflowRefusal(RuntimeError):
    def __init__(self, code: str, message: str, details: dict[str, Any] | None = None):
        super().__init__(message)
        self.code = code
        self.details = details or {}

    def as_dict(self) -> dict[str, Any]:
        return {"ok": False, "error": {"code": self.code, "message": str(self), "details": self.details}}


class CompileWorkflow:
    """Conservative host-side gate around Pipeline's native recompilation commands."""

    def __init__(self, project: str | pathlib.Path, session, poll_interval: float = 0.25, clock=time):
        self.project = pathlib.Path(project).resolve()
        self.session = session
        if isinstance(poll_interval, bool) or not isinstance(poll_interval, (int, float)) or not math.isfinite(poll_interval) or poll_interval <= 0:
            raise ValueError("poll_interval must be finite and positive")
        self.poll_interval = poll_interval
        self.clock = clock

    def run(self, source_paths, require_compiled: bool = False) -> dict[str, Any]:
        if not isinstance(require_compiled, bool):
            raise WorkflowRefusal("INVALID_ARGUMENT", "require_compiled must be Boolean")
        try:
            with self.session.workflow_session() as lease:
                return self.run_with_lease(source_paths, lease, require_compiled=require_compiled)
        except WorkflowRefusal:
            raise
        except SessionRefusal as exc:
            code = "COMPILE_TIMEOUT" if exc.code in {"ACTION_TIMEOUT", "READY_TIMEOUT"} else exc.code
            raise WorkflowRefusal(code, str(exc), exc.details) from exc

    def run_with_lease(self, source_paths, lease, require_compiled: bool = False) -> dict[str, Any]:
        """Run the compile gate under a caller-owned project workflow lease."""
        if not isinstance(require_compiled, bool):
            raise WorkflowRefusal("INVALID_ARGUMENT", "require_compiled must be Boolean")
        try:
            sources = self._sources(source_paths)
            before_sources = self._hash_sources(sources, check_budget=lease.remaining)
            status_path = self.project / STATUS_RELATIVE_PATH
            before_status = self._fingerprint(status_path)
            preflight, preflight_invocation, preflight_attempts = self._editor_state(lease)
            trigger = self._invoke(lease, "recompile", {"focus": False}, trigger=True)
            lease.verify_current()
            native = self._outer_result(trigger, "recompile")
            trigger_result = native["result"]
            if not isinstance(trigger_result, dict):
                raise WorkflowRefusal("NATIVE_RESULT_INVALID", "Recompile trigger result must be an object", {"nativeEnvelope": trigger.data, "invocation": trigger.diagnostics.get("invocation")})
            trigger_status = trigger_result.get("status")
            trigger_evidence = self._command_evidence(trigger, trigger_result)
            if not isinstance(trigger_status, str):
                raise WorkflowRefusal("COMPILE_TRIGGER_INVALID", "Pipeline recompile trigger status must be a string", {"trigger": trigger_evidence, "statusType": type(trigger_status).__name__})
            if trigger_status == "failed":
                raise WorkflowRefusal("COMPILE_TRIGGER_FAILED", "Pipeline rejected the compile request", {"trigger": trigger_evidence})
            if trigger_status not in {"compiling", "up_to_date"}:
                raise WorkflowRefusal("COMPILE_TRIGGER_INVALID", "Pipeline returned an unsupported recompile trigger status", {"trigger": trigger_evidence, "status": trigger_status})
            if require_compiled and trigger_status == "up_to_date":
                raise WorkflowRefusal("COMPILATION_NOT_RUN", "Pipeline reports a fresh no-op; this request requires an actual compile cycle", {"trigger": trigger_evidence})
            if trigger_status == "compiling":
                self._wait_terminal_marker(lease, status_path, before_status)
                self.session.ready(expected_identity=lease.identity, deadline=lease.deadline)
            polls = []
            terminal = None
            saw_compiling = trigger_status == "compiling"
            while True:
                lease.verify_current()
                status_result = self._invoke(lease, "recompile_status", {})
                if not status_result.success:
                    polls.append({"ok": False, "code": status_result.code, "message": status_result.message, "invocation": status_result.diagnostics.get("invocation")})
                    if status_result.code not in RETRYABLE_READ_CODES:
                        raise WorkflowRefusal("COMPILE_STATUS_UNAVAILABLE", "Could not read Pipeline compile status", {"polls": polls})
                    self._sleep(lease)
                    continue
                payload = self._outer_result(status_result, "recompile_status")["result"]
                state = self._validate_status(payload)
                polls.append({"ok": True, "status": state, "invocation": status_result.diagnostics.get("invocation")})
                if state["status"] == "compiling":
                    saw_compiling = True
                elif state["status"] in {"completed", "up_to_date"}:
                    terminal = state
                    break
                elif state["status"] in {"failed", "error"}:
                    raise WorkflowRefusal("COMPILATION_FAILED", "Pipeline reports a failed compilation", {"status": state})
                elif state["status"] not in {"triggered", "idle"}:
                    raise WorkflowRefusal("COMPILE_STATUS_INVALID", "Pipeline returned an unsupported compile status", {"status": state["status"]})
                self._sleep(lease)
            expected_terminal = "completed" if trigger_status == "compiling" else "up_to_date"
            if terminal["failed"] or terminal["errors"] or terminal["compilationFailed"]:
                raise WorkflowRefusal("COMPILATION_FAILED", "Fresh terminal compile status is not clean", {"status": terminal})
            after_status = self._fingerprint(status_path)
            if not after_status["exists"]:
                raise WorkflowRefusal("COMPILE_STATUS_MISSING", "Pipeline compile status file is missing after the trigger")
            if not self._changed(before_status, after_status):
                raise WorkflowRefusal("COMPILE_STATUS_NOT_FRESH", "Pipeline compile status file did not change after this trigger", {"before": before_status, "after": after_status})
            file_status = self._read_status_file(status_path)
            if file_status != terminal:
                raise WorkflowRefusal("COMPILE_STATUS_MISMATCH", "Native status command and status file disagree", {"commandStatus": terminal, "fileStatus": file_status})
            after_sources = self._hash_sources(sources, check_budget=lease.remaining)
            if before_sources != after_sources:
                raise WorkflowRefusal("SOURCE_INPUT_CHANGED", "An explicit source input changed during the compile workflow", {"before": before_sources, "after": after_sources})
            if terminal["status"] != expected_terminal or (trigger_status == "compiling" and not saw_compiling):
                raise WorkflowRefusal("COMPILE_STATUS_MISMATCH", "Trigger and terminal compile states do not form the required lifecycle", {"triggerStatus": trigger_status, "terminalStatus": terminal["status"], "sawCompiling": saw_compiling})
            console, ground_truth, console_invocation, console_attempts = self._fresh_ground_truth(lease)
            final_editor, final_editor_invocation, final_editor_attempts = self._editor_state(lease)
            lease.verify_current()
            return {
                "ok": True,
                "action": "compile",
                "project": str(self.project),
                "editorIdentity": lease.identity.as_dict(),
                "compiled": trigger_status == "compiling",
                "noCompilationReason": "fresh-native-up-to-date" if trigger_status == "up_to_date" else None,
                "sourceInputs": before_sources,
                "trigger": {"invocation": trigger.diagnostics.get("invocation"), "result": trigger_result, "nativeEnvelope": trigger.data},
                "polls": polls,
                "freshness": {
                    "statusFile": {"before": before_status, "after": after_status},
                    "terminalStatus": terminal["status"],
                    "statusFileMatchesCommand": True,
                    "groundTruth": ground_truth,
                    "editorBefore": {"result": preflight, "invocation": preflight_invocation, "retries": preflight_attempts},
                    "editorAfter": {"result": final_editor, "invocation": final_editor_invocation, "retries": final_editor_attempts},
                    "writerLimitation": "Freshness is bound by the observed status-file rewrite and cooperating project lock; uncooperative external writers cannot be excluded.",
                },
                "consoleObservation": {"result": console, "invocation": console_invocation, "groundTruth": ground_truth, "retries": console_attempts},
            }
        except WorkflowRefusal:
            raise
        except SessionRefusal as exc:
            code = "COMPILE_TIMEOUT" if exc.code in {"ACTION_TIMEOUT", "READY_TIMEOUT"} else exc.code
            raise WorkflowRefusal(code, str(exc), exc.details) from exc

    def _wait_terminal_marker(self, lease, status_path, before_status):
        while True:
            lease.verify_current()
            marker = self._fingerprint(status_path)
            if marker["exists"] and self._changed(before_status, marker):
                state = self._read_status_file(status_path)
                if state["failed"] or state["errors"] or state["compilationFailed"] or state["status"] in {"failed", "error"}:
                    raise WorkflowRefusal("COMPILATION_FAILED", "Fresh compile marker reports a failed compilation", {"status": state})
                if state["status"] == "completed":
                    return
                if state["status"] not in {"compiling", "triggered", "idle"}:
                    raise WorkflowRefusal("COMPILE_STATUS_MISMATCH", "Actual compile requires a completed marker before native status polling", {"status": state})
            self._sleep(lease)

    def _sources(self, source_paths) -> list[pathlib.Path]:
        if isinstance(source_paths, (str, pathlib.Path)) or source_paths is None:
            source_paths = [] if source_paths is None else [source_paths]
        elif not isinstance(source_paths, (list, tuple)):
            raise WorkflowRefusal("SOURCE_INPUT_INVALID", "Source inputs must be a list of explicit paths")
        paths = list(source_paths)
        if not paths:
            raise WorkflowRefusal("SOURCE_INPUT_REQUIRED", "At least one explicit source input is required")
        resolved = []
        for item in paths:
            if not isinstance(item, (str, pathlib.Path)):
                raise WorkflowRefusal("SOURCE_INPUT_INVALID", "Each source input must be a path string", {"valueType": type(item).__name__})
            try:
                path = pathlib.Path(item)
                path = (self.project / path).resolve() if not path.is_absolute() else path.resolve()
            except (OSError, RuntimeError, ValueError) as exc:
                raise WorkflowRefusal("SOURCE_INPUT_INVALID", "A source input path could not be resolved", {"reason": str(exc)}) from exc
            try:
                path.relative_to(self.project)
            except ValueError as exc:
                raise WorkflowRefusal("SOURCE_INPUT_OUTSIDE_PROJECT", "Source inputs must remain inside the explicit project", {"path": str(path)}) from exc
            if not path.is_file():
                raise WorkflowRefusal("SOURCE_INPUT_MISSING", "An explicit source input is not a regular file", {"path": str(path)})
            resolved.append(path)
        if len(set(resolved)) != len(resolved):
            raise WorkflowRefusal("SOURCE_INPUT_DUPLICATE", "Source inputs must not contain duplicate paths")
        return sorted(resolved)

    @staticmethod
    def _hash_sources(paths, *, check_budget=None):
        values = []
        budget = check_budget if check_budget is not None else lambda: None
        for path in paths:
            budget()
            digest = hashlib.sha256()
            try:
                with path.open("rb") as stream:
                    while True:
                        budget()
                        block = stream.read(1024 * 1024)
                        budget()
                        if not block:
                            break
                        digest.update(block)
            except OSError as exc:
                raise WorkflowRefusal("SOURCE_INPUT_UNREADABLE", "A source input could not be hashed", {"path": str(path), "reason": str(exc)}) from exc
            values.append({"path": str(path), "sha256": digest.hexdigest()})
        return values

    @staticmethod
    def _fingerprint(path):
        try:
            stat = path.stat()
            content = path.read_bytes()
            return {"exists": True, "size": stat.st_size, "mtimeNs": stat.st_mtime_ns, "sha256": hashlib.sha256(content).hexdigest()}
        except FileNotFoundError:
            return {"exists": False, "size": None, "mtimeNs": None, "sha256": None}
        except OSError as exc:
            raise WorkflowRefusal("COMPILE_STATUS_FILE_INVALID", "Pipeline status file could not be inspected", {"path": str(path), "reason": str(exc)}) from exc

    @staticmethod
    def _changed(before, after):
        return before != after

    @staticmethod
    def _read_status_file(path):
        try:
            payload = json.loads(path.read_text(encoding="utf-8"))
        except (OSError, UnicodeError, json.JSONDecodeError) as exc:
            raise WorkflowRefusal("COMPILE_STATUS_FILE_INVALID", "Pipeline status file is not valid UTF-8 JSON", {"reason": str(exc)}) from exc
        return CompileWorkflow._validate_status(payload)

    @staticmethod
    def _validate_status(payload):
        if not isinstance(payload, dict):
            raise WorkflowRefusal("COMPILE_STATUS_INVALID", "Compile status payload must be an object")
        required = {"status": str, "failed": bool, "errors": list, "compilationFailed": bool}
        for key, kind in required.items():
            if key not in payload or not isinstance(payload[key], kind) or (kind is list and any(not isinstance(x, str) for x in payload[key])):
                raise WorkflowRefusal("COMPILE_STATUS_INVALID", "Compile status payload is missing a correctly typed required field", {"field": key})
        return {key: payload[key] for key in required}

    @staticmethod
    def _command_evidence(result, native_result=None):
        evidence = {
            "invocation": result.diagnostics.get("invocation"),
            "nativeEnvelope": result.data,
        }
        if native_result is not None:
            evidence["result"] = native_result
        return evidence

    @staticmethod
    def _outer_result(result, expected_command):
        if not result.success:
            raise WorkflowRefusal("NATIVE_COMMAND_FAILED", result.message or "Unity Pipeline command failed", {"code": result.code, "invocation": result.diagnostics.get("invocation"), "diagnostics": result.diagnostics})
        envelope = result.data
        if not isinstance(envelope, dict) or envelope.get("success") is not True or not isinstance(envelope.get("data"), dict):
            raise WorkflowRefusal("NATIVE_ENVELOPE_INVALID", "Unity Pipeline returned a malformed success envelope", {"nativeEnvelope": envelope})
        data = envelope["data"]
        if data.get("command") != expected_command:
            raise WorkflowRefusal("NATIVE_ENVELOPE_INVALID", "Unity Pipeline response command did not match the invocation", {"expected": expected_command, "actual": data.get("command"), "nativeEnvelope": envelope})
        if "result" not in data:
            raise WorkflowRefusal("NATIVE_RESULT_MISSING", "Unity Pipeline response omitted data.result", {"nativeEnvelope": envelope})
        return data

    def _invoke(self, lease, command, arguments, trigger=False):
        lease.verify_current()
        result = self.session.transport.invoke_command(str(self.project), command, arguments, timeout=lease.remaining(), deadline=lease.transport_deadline)
        if trigger and not result.success:
            raise WorkflowRefusal("COMPILE_TRIGGER_FAILED", "Compile trigger did not return a successful CLI response; it was not retried", {"code": result.code, "message": result.message, "invocation": result.diagnostics.get("invocation")})
        lease.verify_current()
        return result

    def _editor_state(self, lease):
        result, retries = self._invoke_readonly(lease, "editor_status", {})
        data = self._outer_result(result, "editor_status")
        state = data["result"]
        if not isinstance(state, dict):
            raise WorkflowRefusal("EDITOR_STATUS_INVALID", "Editor status result must be an object")
        project = state.get("projectPath")
        if not isinstance(project, str):
            raise WorkflowRefusal("EDITOR_STATUS_INVALID", "Editor status omitted its project path")
        lease.remaining()
        try:
            canonical = self.session.platform.canonicalize_external_project(project, deadline=lease.transport_deadline)
        except subprocess.TimeoutExpired as exc:
            lease.remaining()
            raise WorkflowRefusal("CLI_TIMEOUT", "Editor status path conversion reached its local deadline") from exc
        except (OSError, RuntimeError, TypeError, ValueError):
            canonical = None
        valid = (
            state.get("status") == "ready"
            and state.get("compiling") is False
            and state.get("domainReloadInProgress") is False
            and state.get("playMode") == "stopped"
            and canonical == lease.identity.project
        )
        if not valid:
            raise WorkflowRefusal("EDITOR_NOT_READY", "Exact Editor must be ready, stopped, and outside domain reload", {"editorStatus": state})
        return state, result.diagnostics.get("invocation"), retries

    def _invoke_readonly(self, lease, command, arguments):
        retries = []
        while True:
            result = self._invoke(lease, command, arguments)
            if result.success:
                return result, retries
            retries.append({"code": result.code, "message": result.message, "invocation": result.diagnostics.get("invocation")})
            if result.code not in RETRYABLE_READ_CODES:
                raise WorkflowRefusal("NATIVE_COMMAND_UNAVAILABLE", f"Could not read Pipeline {command} state", {"retries": retries})
            try:
                self._sleep(lease)
            except SessionRefusal as exc:
                raise WorkflowRefusal("COMPILE_TIMEOUT", f"Could not read Pipeline {command} state before the workflow deadline", {"retries": retries}) from exc

    def _fresh_ground_truth(self, lease):
        retries = []
        while True:
            try:
                result, attempt_retries = self._invoke_readonly(lease, "console_status", {})
                retries.extend(attempt_retries)
            except SessionRefusal as exc:
                raise WorkflowRefusal("GROUND_TRUTH_STALE", "A fresh, idle Editor groundTruth sample did not arrive before the workflow deadline", {"retries": retries}) from exc
            data = self._outer_result(result, "console_status")["result"]
            if not isinstance(data, dict):
                raise WorkflowRefusal("GROUND_TRUTH_INVALID", "Console status result must be an object")
            ground_truth = data.get("groundTruth")
            if "groundTruth" in data and ground_truth is None:
                retries.append({"code": "GROUND_TRUTH_PENDING", "message": "Editor groundTruth sample is pending after assembly reload",
                                "invocation": result.diagnostics.get("invocation")})
                try:
                    self._sleep(lease)
                except SessionRefusal as exc:
                    raise WorkflowRefusal("GROUND_TRUTH_STALE", "A fresh, idle Editor groundTruth sample did not arrive before the workflow deadline",
                                          {"retries": retries}) from exc
                continue
            if not isinstance(ground_truth, dict):
                raise WorkflowRefusal("GROUND_TRUTH_INVALID", "Console status omitted its Editor groundTruth sample")
            age = ground_truth.get("ageMs")
            if not isinstance(age, int) or isinstance(age, bool) or age < 0:
                raise WorkflowRefusal("GROUND_TRUTH_INVALID", "Editor groundTruth ageMs must be a nonnegative integer")
            if not isinstance(ground_truth.get("compiling"), bool) or not isinstance(ground_truth.get("compilationFailed"), bool):
                raise WorkflowRefusal("GROUND_TRUTH_INVALID", "Editor groundTruth compiling and compilationFailed must be Boolean fields")
            if ground_truth.get("compilationFailed") is True:
                raise WorkflowRefusal("COMPILATION_FAILED", "Fresh Editor groundTruth reports a compile failure")
            if age <= GROUND_TRUTH_MAX_AGE_MS and ground_truth.get("compiling") is False:
                if ground_truth.get("compilationFailed") is not False:
                    raise WorkflowRefusal("GROUND_TRUTH_INVALID", "Editor groundTruth compilationFailed must be Boolean false")
                return data, ground_truth, result.diagnostics.get("invocation"), retries
            try:
                self._sleep(lease)
            except SessionRefusal as exc:
                code = "GROUND_TRUTH_STALE" if age > GROUND_TRUTH_MAX_AGE_MS else "EDITOR_NOT_READY"
                raise WorkflowRefusal(code, "A fresh, idle Editor groundTruth sample did not arrive before the workflow deadline", {"ageMs": age, "compiling": ground_truth.get("compiling")}) from exc

    def _sleep(self, lease):
        remaining = lease.remaining()
        self.clock.sleep(min(self.poll_interval, remaining))
