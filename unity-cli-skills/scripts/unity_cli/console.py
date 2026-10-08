from __future__ import annotations

import hashlib
import json
import pathlib
import time
from typing import Any

from .compile import CompileWorkflow, WorkflowRefusal
from .lifecycle import SessionRefusal
from .model import CommandResult
from .reports import ReportPathError, ReportTarget
from .transport import _redact_value


class WorkflowConsoleRefusal(RuntimeError):
    def __init__(self, code: str, message: str, details: dict[str, Any] | None = None):
        super().__init__(message)
        self.code = code
        self.details = details or {}

    def as_dict(self) -> dict[str, Any]:
        return {"ok": False, "error": {"code": self.code, "message": str(self), "details": self.details}}


class ConsoleWorkflow:
    CAPTURE_LIMIT = 2000
    GROUND_TRUTH_MAX_AGE_MS = 2000
    LEVELS = {"log", "warn", "error"}
    LOG_TYPES = {"Log": "log", "Warning": "warn", "Error": "error", "Assert": "error", "Exception": "error"}
    COUNTS = {"error": "consoleErrors", "warn": "consoleWarnings", "log": "consoleLogs"}

    def __init__(self, project: str | pathlib.Path, session, clock=time):
        self.project = pathlib.Path(project).resolve()
        self.session = session
        self.clock = clock

    @staticmethod
    def _target(project: pathlib.Path, output: str | pathlib.Path) -> ReportTarget:
        try:
            return ReportTarget(project, output)
        except ReportPathError as exc:
            code = "CONSOLE_OUTPUT_EXISTS" if exc.code in {"TEST_OUTPUT_EXISTS", "TEST_OUTPUT_RACE"} else "CONSOLE_OUTPUT_PATH_CHANGED" if exc.code == "TEST_OUTPUT_PATH_CHANGED" else "CONSOLE_OUTPUT_INVALID" if exc.code.startswith("TEST_OUTPUT") else "CONSOLE_REPORT_INVALID"
            raise WorkflowConsoleRefusal(code, str(exc), exc.details) from exc

    def run(self, *, output: str | pathlib.Path, level: str | None = None, tail: int = 100,
            since: int | None = None, since_session: str | None = None) -> dict[str, Any]:
        if level is not None and (not isinstance(level, str) or level not in self.LEVELS):
            raise WorkflowConsoleRefusal("INVALID_ARGUMENT", "level must be log, warn, or error")
        if not isinstance(tail, int) or isinstance(tail, bool) or not 1 <= tail <= 1000:
            raise WorkflowConsoleRefusal("INVALID_ARGUMENT", "tail must be an integer from 1 through 1000")
        if (since is None) != (since_session is None):
            raise WorkflowConsoleRefusal("INVALID_ARGUMENT", "since and since_session must be supplied together")
        if since is not None and (not isinstance(since, int) or isinstance(since, bool) or since < 0):
            raise WorkflowConsoleRefusal("INVALID_ARGUMENT", "since must be a nonnegative integer")
        if since_session is not None and (not isinstance(since_session, str) or not since_session.strip()):
            raise WorkflowConsoleRefusal("INVALID_ARGUMENT", "since_session must be a nonempty string")
        deadline = self.session._deadline()
        target = self._target(self.project, output)
        try:
            with self.session.observation_session(deadline=deadline) as lease:
                arguments: dict[str, str | int | bool] = {"tail": self.CAPTURE_LIMIT, "level": level or "log"}
                if since is not None:
                    arguments["since"] = since
                    arguments["since_session"] = since_session
                result = self.session.transport.invoke_command(str(self.project), "console", arguments, timeout=lease.remaining(), deadline=lease.transport_deadline)
                try:
                    lease.verify_current()
                    payload = self._outer_result(result, "console")["result"]
                    parsed = self._validate_payload(payload, level, since, since_session)
                except WorkflowConsoleRefusal as exc:
                    raise exc
                except SessionRefusal as exc:
                    raise WorkflowConsoleRefusal(exc.code, str(exc), exc.details) from exc
                except WorkflowRefusal as exc:
                    raise WorkflowConsoleRefusal(exc.code, str(exc), exc.details) from exc
                raw_artifact = {
                    "schemaVersion": 1,
                    "action": "console",
                    "project": str(self.project),
                    "source": "pipeline-buffer",
                    "mode": "follow" if since is not None else "snapshot",
                    "request": {"level": level, "captureTail": self.CAPTURE_LIMIT, "since": since, "sinceSession": since_session},
                    "effectiveLevel": level or "log",
                    "invocation": result.diagnostics.get("invocation"),
                    "nativeResponse": result.data,
                    "nativeError": None if result.success else {"code": result.code, "message": result.message, "diagnostics": result.diagnostics},
                }
                artifact_bytes = json.dumps(_redact_value(raw_artifact), sort_keys=True, separators=(",", ":")).encode("utf-8")
                self._publish(target, artifact_bytes)
                ground = parsed["groundTruth"]
                display_entries = parsed["entries"][-tail:]
                return {
                    "ok": True,
                    "action": "console",
                    "project": str(self.project),
                    "source": "pipeline-buffer",
                    "mode": "follow" if since is not None else "snapshot",
                    "artifactPath": str(target.path),
                    "artifactSha256": hashlib.sha256(artifact_bytes).hexdigest(),
                    "editorIdentity": lease.identity.as_dict(),
                    "sourceSession": parsed["session"],
                    "cursor": parsed["cursor"],
                    "since": since,
                    "sinceSession": since_session,
                    "level": level or "log",
                    "requestedLevel": level,
                    "returned": parsed["returned"],
                    "entries": display_entries,
                    "displayReturned": len(display_entries),
                    "counts": parsed["counts"],
                    "bufferCounts": parsed["counts"],
                    "dropped": parsed["dropped"],
                    "reset": parsed["reset"],
                    "bufferGap": parsed["dropped"],
                    "captureLimit": self.CAPTURE_LIMIT,
                    "displayLimit": tail,
                    "displayTruncated": len(parsed["entries"]) > tail,
                    "groundTruth": ground,
                    "groundTruthFreshness": parsed["groundTruthFreshness"],
                    "groundTruthCounts": parsed["groundTruthCounts"],
                    "groundTruthCountsMatchBuffer": parsed["groundTruthCountsMatchBuffer"],
                    "historyCompleteness": "bounded-callback-buffer; entries may be absent after overflow or reload",
                }
        except WorkflowConsoleRefusal:
            raise
        except SessionRefusal as exc:
            details = dict(exc.details)
            if target.published_identity is not None:
                details.update({"artifactPath": str(target.path), "artifactPreserved": True})
            raise WorkflowConsoleRefusal(exc.code, str(exc), details) from exc
        finally:
            target.close()

    @classmethod
    def _validate_payload(cls, payload: Any, requested_level: str | None, since: int | None, since_session: str | None) -> dict[str, Any]:
        if not isinstance(payload, dict):
            raise WorkflowConsoleRefusal("CONSOLE_RESPONSE_INVALID", "Native console result must be an object")
        required = {"entries", "cursor", "session", "returned", "dropped", "reset", "counts", "groundTruth"}
        if not required.issubset(payload):
            raise WorkflowConsoleRefusal("CONSOLE_RESPONSE_INVALID", "Native console result omitted required fields", {"missing": sorted(required - payload.keys())})
        entries, cursor, session, returned = payload["entries"], payload["cursor"], payload["session"], payload["returned"]
        if not isinstance(entries, list) or not cls._nonnegative_integer(cursor) or not isinstance(session, str) or not session.strip():
            raise WorkflowConsoleRefusal("CONSOLE_RESPONSE_INVALID", "Native console entries, cursor, or session have invalid types")
        if not cls._nonnegative_integer(returned) or returned != len(entries) or returned > cls.CAPTURE_LIMIT:
            raise WorkflowConsoleRefusal("CONSOLE_RESPONSE_INVALID", "Native returned count must match its bounded entries array", {"returned": returned, "entryCount": len(entries)})
        if not isinstance(payload["dropped"], bool) or not isinstance(payload["reset"], bool):
            raise WorkflowConsoleRefusal("CONSOLE_RESPONSE_INVALID", "Native dropped and reset values must be Boolean")
        if payload["reset"] and not payload["dropped"]:
            raise WorkflowConsoleRefusal("CONSOLE_CURSOR_INVALID", "A cursor reset must also report dropped=true")
        counts = payload["counts"]
        if not isinstance(counts, dict) or set(counts) != {"error", "warn", "log"} or any(not cls._nonnegative_integer(value) for value in counts.values()):
            raise WorkflowConsoleRefusal("CONSOLE_COUNTS_INVALID", "Native retained counts must contain nonnegative error, warn, and log integers")
        retained = sum(counts.values())
        if retained > cls.CAPTURE_LIMIT or returned > retained:
            raise WorkflowConsoleRefusal("CONSOLE_COUNTS_INVALID", "Native counts cannot explain the retained entries", {"counts": counts, "returned": returned})
        same_session_follow = since is not None and not payload["reset"] and session == since_session
        if since is not None:
            if session != since_session and not (payload["reset"] is True and payload["dropped"] is True):
                raise WorkflowConsoleRefusal("CONSOLE_CURSOR_INVALID", "A foreign source session must report both reset and dropped")
            if same_session_follow and cursor < since:
                raise WorkflowConsoleRefusal("CONSOLE_CURSOR_INVALID", "Native console cursor regressed within the same session", {"since": since, "cursor": cursor})
        if since is None and requested_level in (None, "log") and retained != returned:
            raise WorkflowConsoleRefusal("CONSOLE_COUNTS_INVALID", "An unfiltered snapshot must return every retained entry", {"retained": retained, "returned": returned})
        last_seq = -1
        entry_counts = {"error": 0, "warn": 0, "log": 0}
        for item in entries:
            if not isinstance(item, dict):
                raise WorkflowConsoleRefusal("CONSOLE_ENTRY_INVALID", "Each console entry must be an object")
            expected = {"seq", "timestampUtc", "level", "logType", "message", "stackTrace", "seeded"}
            if not expected.issubset(item):
                raise WorkflowConsoleRefusal("CONSOLE_ENTRY_INVALID", "A console entry omitted required fields", {"missing": sorted(expected - item.keys())})
            seq, entry_level, log_type = item["seq"], item["level"], item["logType"]
            if not cls._nonnegative_integer(seq) or seq <= last_seq:
                raise WorkflowConsoleRefusal("CONSOLE_ENTRY_INVALID", "Console sequence numbers must be nonnegative and strictly ascending")
            if not isinstance(item["timestampUtc"], str) or not item["timestampUtc"].strip():
                raise WorkflowConsoleRefusal("CONSOLE_ENTRY_INVALID", "Console timestampUtc must be a nonempty string")
            if not isinstance(entry_level, str) or entry_level not in cls.LEVELS or not isinstance(log_type, str) or cls.LOG_TYPES.get(log_type) != entry_level:
                raise WorkflowConsoleRefusal("CONSOLE_ENTRY_INVALID", "Console level and Unity logType disagree", {"level": entry_level, "logType": log_type})
            if not all(isinstance(item[name], str) for name in ("message", "stackTrace")) or not isinstance(item["seeded"], bool):
                raise WorkflowConsoleRefusal("CONSOLE_ENTRY_INVALID", "Console message, stackTrace, or seeded field has an invalid type")
            if requested_level == "error" and entry_level != "error" or requested_level == "warn" and entry_level not in {"warn", "error"}:
                raise WorkflowConsoleRefusal("CONSOLE_ENTRY_LEVEL_MISMATCH", "Native console returned an entry below the requested severity")
            if same_session_follow and seq <= since:
                raise WorkflowConsoleRefusal("CONSOLE_CURSOR_INVALID", "Native console returned an entry at or before the requested cursor", {"since": since, "seq": seq})
            if seq > cursor:
                raise WorkflowConsoleRefusal("CONSOLE_CURSOR_INVALID", "An entry sequence exceeds the returned cursor", {"cursor": cursor, "seq": seq})
            last_seq = seq
            entry_counts[entry_level] += 1
        if any(entry_counts[key] > counts[key] for key in entry_counts):
            raise WorkflowConsoleRefusal("CONSOLE_COUNTS_INVALID", "Returned entries exceed their retained severity counts", {"entries": entry_counts, "counts": counts})
        ground, freshness, ground_counts = cls._validate_ground_truth(payload["groundTruth"])
        comparison = None if freshness != "fresh" else all(counts[key] == ground_counts[cls.COUNTS[key]] for key in counts)
        return {"entries": entries, "cursor": cursor, "session": session, "returned": returned,
                "dropped": payload["dropped"], "reset": payload["reset"], "counts": counts,
                "groundTruth": ground, "groundTruthFreshness": freshness, "groundTruthCounts": ground_counts,
                "groundTruthCountsMatchBuffer": comparison}

    @classmethod
    def _validate_ground_truth(cls, value: Any) -> tuple[dict[str, Any] | None, str, dict[str, int] | None]:
        if value is None:
            return None, "unknown", None
        required = {"sampledUtc", "ageMs", "compilationFailed", "compiling", "consoleErrors", "consoleWarnings", "consoleLogs", "seeded"}
        if not isinstance(value, dict) or not required.issubset(value):
            raise WorkflowConsoleRefusal("CONSOLE_GROUND_TRUTH_INVALID", "Ground-truth sample is malformed or incomplete")
        if not isinstance(value["sampledUtc"], str) or not value["sampledUtc"].strip() or not cls._nonnegative_integer(value["ageMs"]):
            raise WorkflowConsoleRefusal("CONSOLE_GROUND_TRUTH_INVALID", "Ground-truth timestamp or age has an invalid type")
        if any(not isinstance(value[key], bool) for key in ("compilationFailed", "compiling", "seeded")):
            raise WorkflowConsoleRefusal("CONSOLE_GROUND_TRUTH_INVALID", "Ground-truth Boolean fields have invalid types")
        count_keys = ("consoleErrors", "consoleWarnings", "consoleLogs")
        if any(not cls._nonnegative_integer(value[key]) for key in count_keys):
            raise WorkflowConsoleRefusal("CONSOLE_GROUND_TRUTH_INVALID", "Ground-truth console counts must be nonnegative integers")
        counts = {key: value[key] for key in count_keys}
        freshness = "fresh" if value["ageMs"] <= cls.GROUND_TRUTH_MAX_AGE_MS else "stale"
        return value, freshness, counts

    @staticmethod
    def _nonnegative_integer(value: Any) -> bool:
        return isinstance(value, int) and not isinstance(value, bool) and value >= 0

    @staticmethod
    def _outer_result(result: CommandResult, command: str) -> dict[str, Any]:
        data = CompileWorkflow._outer_result(result, command)
        return data

    @staticmethod
    def _publish(target: ReportTarget, payload: bytes) -> None:
        try:
            target.publish(payload)
        except ReportPathError as exc:
            code = "CONSOLE_OUTPUT_EXISTS" if exc.code == "TEST_OUTPUT_RACE" else "CONSOLE_OUTPUT_PATH_CHANGED" if exc.code == "TEST_OUTPUT_PATH_CHANGED" else "CONSOLE_OUTPUT_INVALID"
            raise WorkflowConsoleRefusal(code, str(exc), exc.details) from exc
