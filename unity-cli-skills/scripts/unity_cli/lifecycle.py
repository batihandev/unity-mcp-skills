from __future__ import annotations

import time
import subprocess
import math
import hashlib
import json
import os
import pathlib
import tempfile
from contextlib import contextmanager
from dataclasses import dataclass
from typing import Any

from .model import Deadline, InventorySnapshot, ProcessRecord, RegistryObservation, SessionIdentity
from .transport import CliMissing


class SessionRefusal(RuntimeError):
    def __init__(self, code: str, message: str, details: dict[str, Any] | None = None):
        super().__init__(message)
        self.code = code
        self.details = details or {}

    def as_dict(self) -> dict[str, Any]:
        return {"ok": False, "error": {"code": self.code, "message": str(self), "details": self.details}}


@dataclass(frozen=True)
class _Discovery:
    target: ProcessRecord | None
    registry: dict[str, Any]
    auxiliaries: tuple[ProcessRecord, ...]


@dataclass(frozen=True)
class WorkflowSessionLease:
    """Exact Editor identity and shared project lock held for one workflow operation."""

    controller: Any
    identity: SessionIdentity
    deadline: float

    @property
    def transport_deadline(self) -> Deadline:
        return Deadline(self.deadline, self.controller.clock)

    def remaining(self) -> float:
        return self.controller._remaining(self.deadline)

    def verify_current(self) -> None:
        self.controller._require_identity_current(self.identity, self.deadline)


@dataclass(frozen=True)
class OfflineWorkflowLease:
    """Shared project lock and stopped-process proof for a native offline workflow."""

    controller: Any
    deadline: float

    @property
    def transport_deadline(self) -> Deadline:
        return Deadline(self.deadline, self.controller.clock)

    def remaining(self) -> float:
        return self.controller._remaining(self.deadline)

    def verify_stopped(self) -> None:
        self.controller._require_project_stopped(self.deadline)


class SessionController:
    def __init__(self, project: str, transport, platform, clock=time, timeout: float = 120, state_root: str | None = None):
        self.platform = platform
        self.project = platform.canonicalize_project(project)
        self.transport = transport
        self.clock = clock
        if not math.isfinite(timeout) or timeout <= 0:
            raise SessionRefusal("INVALID_TIMEOUT", "The action timeout must be a finite positive number")
        self.timeout = timeout
        self.state_root = pathlib.Path(state_root) if state_root else pathlib.Path(tempfile.gettempdir()) / f"unity-session-{os.getuid() if hasattr(os, 'getuid') else os.environ.get('USERNAME', 'user')}"

    @contextmanager
    def _launch_guard(self, deadline: float):
        try:
            self.state_root.mkdir(mode=0o700, parents=True, exist_ok=True)
            stem = hashlib.sha256(self.project.encode("utf-8")).hexdigest()
            lock_path = self.state_root / f"{stem}.lock"
            pending_path = self.state_root / f"{stem}.pending"
            with lock_path.open("a+b") as lock:
                if os.name == "nt" and lock_path.stat().st_size == 0:
                    lock.write(b"\0")
                    lock.flush()
                while True:
                    try:
                        if os.name == "nt":
                            import msvcrt
                            lock.seek(0)
                            msvcrt.locking(lock.fileno(), msvcrt.LK_NBLCK, 1)
                        else:
                            import fcntl
                            fcntl.flock(lock.fileno(), fcntl.LOCK_EX | fcntl.LOCK_NB)
                        break
                    except (BlockingIOError, OSError):
                        self.clock.sleep(min(0.05, self._remaining(deadline)))
                try:
                    yield pending_path
                finally:
                    if os.name == "nt":
                        lock.seek(0)
                        msvcrt.locking(lock.fileno(), msvcrt.LK_UNLCK, 1)
                    else:
                        fcntl.flock(lock.fileno(), fcntl.LOCK_UN)
        except OSError as exc:
            raise SessionRefusal("LAUNCH_STATE_UNAVAILABLE", "The project launch lock is unavailable", {"reason": str(exc)}) from exc

    def _deadline(self) -> float:
        return self.clock.monotonic() + self.timeout

    def _remaining(self, deadline: float) -> float:
        remaining = deadline - self.clock.monotonic()
        if remaining <= 0:
            raise SessionRefusal("ACTION_TIMEOUT", "The action reached its wall-clock deadline")
        return remaining

    def _ensure_cli(self, deadline: float) -> None:
        try:
            cli = self.transport.ensure_cli(timeout=self._remaining(deadline), deadline=Deadline(deadline, self.clock))
            self.platform.cli_executable = cli
        except subprocess.TimeoutExpired as exc:
            self._remaining(deadline)
            raise SessionRefusal("CLI_TIMEOUT", "Unity CLI discovery reached its local deadline") from exc
        except CliMissing as exc:
            raise SessionRefusal("CLI_NOT_FOUND", str(exc)) from exc

    def _snapshot(self, deadline: float | None = None) -> InventorySnapshot:
        snapshot = self.platform.inventory(timeout=self._remaining(deadline)) if deadline is not None else self.platform.inventory()
        if not snapshot.known:
            if snapshot.code == "PROCESS_INVENTORY_TIMEOUT" and deadline is not None:
                self._remaining(deadline)
            raise SessionRefusal(
                "PROCESS_INVENTORY_UNKNOWN",
                "The operating-system process inventory is unavailable; no lifecycle mutation was attempted",
                {"reason": snapshot.error},
            )
        return snapshot

    def _discovery(self, deadline: float | None = None) -> _Discovery:
        self._ensure_cli(deadline if deadline is not None else self._deadline())
        snapshot = self._snapshot(deadline)
        registry_result = self.transport.registry(self.project, timeout=self._remaining(deadline), deadline=Deadline(deadline, self.clock)) if deadline is not None else self.transport.registry(self.project)
        observations = registry_result.data if registry_result.success and isinstance(registry_result.data, list) else []
        unique: dict[tuple[str, int, str | None, int | None], RegistryObservation] = {}
        duplicates = 0
        for observation in observations:
            key = (self.platform.canonicalize_project(observation.project), observation.pid, observation.started_at, observation.pipeline_port)
            if key in unique:
                duplicates += 1
            unique[key] = observation

        primaries = [
            record for record in snapshot.records
            if record.role == "editor" and self.platform.canonicalize_project(record.project) == self.project
        ]
        if len(primaries) > 1:
            raise SessionRefusal(
                "MULTIPLE_PROJECT_EDITORS", "More than one primary Editor process claims the project",
                {"processes": [record.identity_dict() for record in primaries]},
            )
        target = primaries[0] if primaries else None
        stale = 0
        live: list[tuple[str, int, str | None, int | None]] = []
        for project, pid, started_at, port in unique:
            matched = any(
                record.role == "editor" and record.pid == pid
                and self.platform.canonicalize_project(record.project) == project
                and (started_at is None or record.started_at == started_at)
                for record in snapshot.records
            )
            if not matched:
                stale += 1
            else:
                live.append((project, pid, started_at, port))
        target_ports = {port for project, pid, started_at, port in live
                        if target is not None and project == self.project and pid == target.pid and port is not None}
        collisions = [{"project": project, "pid": pid, "startedAt": started_at, "port": port}
                      for project, pid, started_at, port in live
                      if project != self.project and port in target_ports]
        if collisions:
            raise SessionRefusal(
                "PIPELINE_ENDPOINT_COLLISION",
                "Live Editors for different projects advertise the same Pipeline port; inspect exact-project registry and native unity status before choosing recovery",
                {"process": target.identity_dict(), "collisions": collisions},
            )
        auxiliaries = tuple(
            record for record in snapshot.records
            if record.role != "editor" and target is not None and record.parent_pid == target.pid
        )
        return _Discovery(target, {
            "observations": len(unique), "duplicates": duplicates, "stale": stale,
            "collisions": collisions, "endpointUnknown": sum(port is None for _, _, _, port in unique),
            "available": int(registry_result.success),
            "diagnostic": None if registry_result.success else {"code": registry_result.code, "message": registry_result.message},
        }, auxiliaries)

    def discover(self) -> dict[str, Any]:
        discovery = self._discovery(self._deadline())
        return {
            "ok": True, "action": "discover", "project": self.project,
            "state": "running" if discovery.target else "stopped",
            "process": discovery.target.identity_dict() if discovery.target else None,
            "auxiliaryProcesses": len(discovery.auxiliaries), "registry": discovery.registry,
        }

    def open(self, editor_version: str | None = None, deadline: float | None = None, batch_mode: bool = False) -> dict[str, Any]:
        deadline = deadline if deadline is not None else self._deadline()
        with self._launch_guard(deadline) as pending_path:
            return self._open_locked(editor_version, deadline, batch_mode, pending_path)

    def _open_locked(self, editor_version: str | None, deadline: float, batch_mode: bool, pending_path: pathlib.Path) -> dict[str, Any]:
        discovery = self._discovery(deadline)
        if discovery.target:
            pending_path.unlink(missing_ok=True)
            return {
                "ok": True, "action": "open", "project": self.project, "state": "already-running",
                "process": discovery.target.identity_dict(), "responsive": "unknown",
            }
        if pending_path.exists():
            raise SessionRefusal("OPEN_PENDING", "A prior launch has no verified Editor handoff; inspect its diagnostic log before starting another", {"pendingState": str(pending_path)})
        with pending_path.open("x", encoding="utf-8") as state:
            os.chmod(pending_path, 0o600)
            json.dump({"state": "launching"}, state)
        result = self.transport.open(self.project, editor_version, timeout=self._remaining(deadline), batch_mode=batch_mode, deadline=Deadline(deadline, self.clock))
        if not result.success:
            pending_path.unlink(missing_ok=True)
            raise SessionRefusal(result.code or "OPEN_FAILED", result.message or "Unity CLI open failed", result.diagnostics)
        launcher_pid = result.data.get("launcherPid") if isinstance(result.data, dict) else None
        if not isinstance(launcher_pid, int) or launcher_pid <= 0:
            pending_path.write_text(json.dumps({"launcherPid": launcher_pid}), encoding="utf-8")
            raise SessionRefusal("OPEN_LAUNCH_INVALID", "The CLI launch returned no process identity; another launch was not attempted")
        with pending_path.open("w", encoding="utf-8") as state:
            json.dump({"launcherPid": launcher_pid, "diagnosticPath": result.data.get("diagnosticPath")}, state)
        while True:
            if self.clock.monotonic() >= deadline:
                raise SessionRefusal("OPEN_HANDOFF_TIMEOUT", "No exact Editor process appeared before the open deadline", {"launcherPid": launcher_pid, "pendingState": str(pending_path)})
            snapshot = self._snapshot(deadline)
            primaries = [record for record in snapshot.records if record.role == "editor" and self.platform.canonicalize_project(record.project) == self.project]
            if len(primaries) > 1:
                raise SessionRefusal("MULTIPLE_PROJECT_EDITORS", "More than one primary Editor process claims the project", {"processes": [record.identity_dict() for record in primaries]})
            if primaries:
                pending_path.unlink(missing_ok=True)
                return {"ok": True, "action": "open", "project": self.project, "state": "running", "process": primaries[0].identity_dict(), "detached": True, "responsive": "unknown"}
            status = self.transport.launch_status(launcher_pid, timeout=self._remaining(deadline), deadline=Deadline(deadline, self.clock))
            if not status.success:
                pending_path.unlink(missing_ok=True)
                raise SessionRefusal(status.code or "OPEN_FAILED", status.message or "Unity CLI rejected open", status.diagnostics)
            self.clock.sleep(min(0.25, self._remaining(deadline)))

    def _target(self, deadline: float) -> SessionIdentity:
        discovery = self._discovery(deadline)
        if not discovery.target:
            raise SessionRefusal("SESSION_NOT_RUNNING", "No running Editor process matches the exact project")
        return SessionIdentity.from_process(discovery.target)

    def _readiness_once(self, identity: SessionIdentity, deadline: float) -> bool:
        self._require_identity_current(identity, deadline)
        result = self.transport.readiness(identity, timeout=self._remaining(deadline), deadline=Deadline(deadline, self.clock))
        if result.code == "PIPELINE_AUTHENTICATION_FAILED":
            raise SessionRefusal(result.code, result.message or "Pipeline authentication failed", result.diagnostics)
        if not result.success or not isinstance(result.data, dict) or not result.data.get("ready"):
            return False
        project = result.data.get("project")
        pid = result.data.get("pid")
        ready = pid == identity.pid and result.data.get("startedAt") == identity.started_at and self.platform.canonicalize_project(project) == identity.project
        if ready:
            self._require_identity_current(identity, deadline)
        return ready

    def ready(self, expected_identity: SessionIdentity | None = None, *, deadline: float | None = None) -> dict[str, Any]:
        deadline = deadline if deadline is not None else self._deadline()
        identity = self._target(deadline)
        if expected_identity is not None:
            self._require_expected_identity(identity, expected_identity, deadline)
        while True:
            if self.clock.monotonic() >= deadline:
                raise SessionRefusal("READY_TIMEOUT", "The exact Editor did not become ready before the deadline", {"process": identity.as_dict()})
            if self._readiness_once(identity, deadline):
                return {"ok": True, "action": "ready", "project": self.project, "state": "ready", "process": identity.as_dict()}
            if self.clock.monotonic() >= deadline:
                raise SessionRefusal("READY_TIMEOUT", "The exact Editor did not become ready before the deadline", {"process": identity.as_dict()})
            self.clock.sleep(min(0.25, deadline - self.clock.monotonic()))

    def _wait_identity_absent(self, identity: SessionIdentity, deadline: float) -> None:
        while True:
            if self.clock.monotonic() >= deadline:
                raise SessionRefusal("EXIT_TIMEOUT", "The exact Editor did not exit before the deadline", {"phase": "exit-verification", "process": identity.as_dict()})
            try:
                snapshot = self._snapshot(deadline)
            except SessionRefusal as exc:
                if exc.code not in {"PROCESS_INVENTORY_UNKNOWN", "ACTION_TIMEOUT"}:
                    raise
                details = {"phase": "exit-verification", "process": identity.as_dict(), "inventoryReason": exc.details.get("reason")}
                if self.clock.monotonic() >= deadline:
                    raise SessionRefusal("EXIT_TIMEOUT", "The exact Editor exit could not be verified before the deadline", details) from exc
                raise SessionRefusal("PROCESS_INVENTORY_UNKNOWN", "The process inventory became unavailable after the close request; exit state is unknown", details) from exc
            present = any(
                record.role == "editor" and record.pid == identity.pid and record.started_at == identity.started_at
                and self.platform.canonicalize_project(record.project) == identity.project
                for record in snapshot.records
            )
            replacement = any(record.role == "editor" and self.platform.canonicalize_project(record.project) == identity.project and (record.pid != identity.pid or record.started_at != identity.started_at) for record in snapshot.records)
            if replacement:
                raise SessionRefusal("PROCESS_IDENTITY_CHANGED", "Another Editor now claims the project", {"process": identity.as_dict()})
            if not present:
                return
            if self.clock.monotonic() >= deadline:
                raise SessionRefusal("EXIT_TIMEOUT", "The exact Editor did not exit before the deadline", {"phase": "exit-verification", "process": identity.as_dict()})
            self.clock.sleep(min(0.25, deadline - self.clock.monotonic()))

    def _require_identity_current(self, identity: SessionIdentity, deadline: float) -> None:
        snapshot = self._snapshot(deadline)
        current = any(
            record.role == "editor" and record.pid == identity.pid and record.started_at == identity.started_at
            and self.platform.canonicalize_project(record.project) == identity.project
            for record in snapshot.records
        )
        if not current:
            raise SessionRefusal(
                "PROCESS_IDENTITY_CHANGED",
                "The Editor process identity changed during preflight; no close request was sent",
                {"process": identity.as_dict()},
            )

    @contextmanager
    def workflow_session(self, deadline: float | None = None):
        """Acquire the shared per-project lock and yield the exact current Editor identity."""
        deadline = deadline if deadline is not None else self._deadline()
        with self._launch_guard(deadline):
            identity = self._target(deadline)
            self._require_identity_current(identity, deadline)
            yield WorkflowSessionLease(self, identity, deadline)

    @contextmanager
    def observation_session(self, deadline: float | None = None):
        """Observe an exact Editor identity without waiting behind mutating project work."""
        deadline = deadline if deadline is not None else self._deadline()
        identity = self._target(deadline)
        self._require_identity_current(identity, deadline)
        try:
            yield WorkflowSessionLease(self, identity, deadline)
        finally:
            self._require_identity_current(identity, deadline)

    @contextmanager
    def offline_workflow_session(self, deadline: float | None = None):
        """Hold the same project lock while proving no Editor or importer owns the project."""
        deadline = deadline if deadline is not None else self._deadline()
        with self._launch_guard(deadline) as pending_path:
            if pending_path.exists():
                raise SessionRefusal("OPEN_PENDING", "A project launch is pending; offline Unity work is unsafe", {"pendingState": str(pending_path)})
            self._require_project_stopped(deadline)
            yield OfflineWorkflowLease(self, deadline)

    def _require_project_stopped(self, deadline: float) -> None:
        snapshot = self._snapshot(deadline)
        project_editors = [
            record for record in snapshot.records
            if record.role == "editor" and self.platform.canonicalize_project(record.project) == self.project
        ]
        editor_pids = {record.pid for record in project_editors}
        auxiliaries = [
            record for record in snapshot.records
            if record.role != "editor" and (
                self.platform.canonicalize_project(record.project) == self.project
                or record.parent_pid in editor_pids
            )
        ]
        if project_editors or auxiliaries:
            raise SessionRefusal(
                "PROJECT_NOT_STOPPED",
                "Offline Unity work requires the exact project Editor and its auxiliary workers to be stopped",
                {"editors": [record.identity_dict() for record in project_editors], "auxiliaryProcesses": [record.identity_dict() for record in auxiliaries]},
            )

    def _require_expected_identity(self, identity: SessionIdentity, expected: SessionIdentity, deadline: float) -> None:
        if identity.as_dict() != expected.as_dict():
            raise SessionRefusal("PROCESS_IDENTITY_CHANGED", "The current Editor differs from the expected owned identity; no lifecycle request was sent", {"expected": expected.as_dict(), "process": identity.as_dict()})
        self._require_identity_current(expected, deadline)

    def close(self, expected_identity: SessionIdentity | None = None) -> dict[str, Any]:
        deadline = self._deadline()
        with self._launch_guard(deadline):
            return self._close(deadline, expected_identity)

    def _close(self, deadline: float, expected_identity: SessionIdentity | None = None) -> dict[str, Any]:
        identity = self._target(deadline)
        if expected_identity is not None:
            self._require_expected_identity(identity, expected_identity, deadline)
        dirty = self.transport.dirty_scenes(identity, timeout=self._remaining(deadline), deadline=Deadline(deadline, self.clock))
        if not dirty.success:
            raise SessionRefusal(dirty.code or "DIRTY_STATE_UNKNOWN", dirty.message or "Dirty-scene state could not be inspected", dirty.diagnostics)
        if dirty.data:
            raise SessionRefusal("DIRTY_SCENES", "The Editor has dirty scenes; save or discard them explicitly before closing", {"scenes": dirty.data})
        self._require_identity_current(identity, deadline)
        requested = self.transport.request_editor_exit(identity, timeout=self._remaining(deadline), deadline=Deadline(deadline, self.clock))
        if not requested.success:
            raise SessionRefusal(requested.code or "CLOSE_FAILED", requested.message or "The Editor refused the close request", requested.diagnostics)
        self._wait_identity_absent(identity, deadline)
        return {
            "ok": True, "action": "close", "project": self.project, "state": "closed",
            "process": identity.as_dict(), "exitVerification": "identity-absent",
        }

    def restart(self, editor_version: str | None = None, batch_mode: bool = False) -> dict[str, Any]:
        deadline = self._deadline()
        with self._launch_guard(deadline) as pending_path:
            closed = self._close(deadline)
            opened = self._open_locked(editor_version, deadline, batch_mode, pending_path)
            return {
                "ok": True, "action": "restart", "project": self.project, "state": "restart-requested",
                "closedProcess": closed["process"], "open": opened,
            }

    def recover(self, graceful_close: bool = False) -> dict[str, Any]:
        deadline = self._deadline()
        with self._launch_guard(deadline):
            return self._recover_locked(graceful_close, deadline)

    def _recover_locked(self, graceful_close: bool, deadline: float) -> dict[str, Any]:
        identity = self._target(deadline)
        logs = self.transport.diagnose_logs(self.project, identity.log_file, timeout=self._remaining(deadline), deadline=Deadline(deadline, self.clock))
        if logs.get("code") == "CLI_TIMEOUT":
            self._remaining(deadline)
            raise SessionRefusal("CLI_TIMEOUT", "Editor log diagnosis reached its local deadline", {"logs": logs})
        modal = self.platform.inspect_modal(identity, min(self._remaining(deadline), 10))
        cancelled = None
        if modal.get("state") in {"scene-modal", "inspected"} and (
            modal.get("reason") == "scene-modal-found" or modal.get("title") == "Scene(s) Have Been Modified"
        ):
            cancelled = self.platform.cancel_safe_modal(identity, min(self._remaining(deadline), 10))
        if self._readiness_once(identity, deadline):
            return {
                "ok": True, "action": "recover", "project": self.project, "state": "ready",
                "process": identity.as_dict(), "logs": logs, "modal": modal, "modalCancellation": cancelled,
            }
        if not graceful_close:
            return {
                "ok": False, "action": "recover", "project": self.project, "state": "unresponsive",
                "process": identity.as_dict(), "logs": logs, "modal": modal, "modalCancellation": cancelled,
                "next": "Retry after resolving the reported condition, or pass --graceful-close to request an exact PID-targeted window close.",
            }
        request = self.platform.request_graceful_close(identity, min(self._remaining(deadline), 10))
        if request.get("state") != "requested":
            raise SessionRefusal("GRACEFUL_CLOSE_UNAVAILABLE", "A safe exact-target OS close could not be requested", {"process": identity.as_dict(), "request": request})
        self._wait_identity_absent(identity, deadline)
        return {
            "ok": True, "action": "recover", "project": self.project, "state": "closed",
            "process": identity.as_dict(), "exitVerification": "identity-absent",
            "logs": logs, "modal": modal, "modalCancellation": cancelled, "gracefulClose": request,
        }
