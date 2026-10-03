from __future__ import annotations

import json
import ntpath
import os
import pathlib
import platform as host_platform
import re
import shlex
import shutil
import subprocess
import ctypes
import struct
import time
from typing import Any, Callable, Iterable

from .model import Deadline, InventorySnapshot, ProcessRecord, SessionIdentity
from .transport import _bounded


Runner = Callable[..., Any]
_PROJECT_FLAGS = {"-projectpath", "--project-path"}
_WORKER_MARKERS = {"-parentpid", "-adb2"}


def _windows_helper_response(completed: Any, action: str) -> dict[str, Any]:
    invalid = {
        "supported": True, "state": "refused", "reason": "Windows helper returned invalid output",
        "exitCode": completed.returncode, "stderr": _bounded(completed.stderr), "stdout": _bounded(completed.stdout),
    }
    try:
        payload = json.loads(completed.stdout)
    except (json.JSONDecodeError, TypeError):
        return invalid
    expected = {"Inspect": "inspected", "Cancel": "cancelled", "Close": "requested"}[action]
    if not isinstance(payload, dict) or payload.get("supported") is not True:
        return invalid
    state, reason = payload.get("state"), payload.get("reason")
    if not isinstance(state, str) or not isinstance(reason, str) or not reason:
        return invalid
    if state == "refused":
        return payload
    if state != expected:
        return invalid
    if action == "Inspect" and reason in {"modal-absent", "scene-modal-found"}:
        return payload
    if action == "Inspect" and reason == "other-dialog" and isinstance(payload.get("title"), str):
        return payload
    if action == "Cancel" and reason == "modal-dismissed":
        return payload
    if action == "Close" and reason == "close-main-window" and payload.get("method") == "CloseMainWindow":
        return payload
    return invalid


def _project_from_argv(argv: list[str]) -> str | None:
    for index, value in enumerate(argv):
        lower = value.lower()
        if lower in _PROJECT_FLAGS and index + 1 < len(argv):
            return argv[index + 1]
        if lower.startswith("-projectpath=") or lower.startswith("--project-path="):
            return value.split("=", 1)[1]
    return None


def _log_from_argv(argv: list[str]) -> str | None:
    for index, value in enumerate(argv[:-1]):
        if value.lower() == "-logfile":
            return argv[index + 1]
    return None


def _worker_parent(argv: list[str]) -> int | None:
    for index, value in enumerate(argv):
        if value.lower() == "-parentpid" and index + 1 < len(argv):
            try:
                return int(argv[index + 1])
            except ValueError:
                return None
    return None


def _process_role(argv: list[str]) -> str:
    lowered = [part.lower() for part in argv]
    if any(marker in lowered for marker in _WORKER_MARKERS):
        return "worker"
    for index, value in enumerate(lowered[:-1]):
        if value == "-name" and lowered[index + 1].startswith("assetimport"):
            return "worker"
    if any("assetimportworker" in part for part in lowered):
        return "worker"
    return "editor"


def _cli_invocation(argv: list[str]) -> bool:
    lowered = [part.lower() for part in argv]
    return (len(lowered) > 1 and lowered[1] in {"command", "open", "editors", "status", "auth"}) or "-ipc-key" in lowered


def _safe_argv(command_line: str | None, windows: bool = False) -> list[str]:
    if not command_line:
        return []
    try:
        if windows:
            # CommandLineToArgvW-compatible quote and backslash handling.
            args, i, n = [], 0, len(command_line)
            while i < n:
                while i < n and command_line[i] in " \t": i += 1
                if i == n: break
                part, quoted = [], False
                while i < n and (quoted or command_line[i] not in " \t"):
                    if command_line[i] == "\\":
                        start = i
                        while i < n and command_line[i] == "\\": i += 1
                        count = i - start
                        if i < n and command_line[i] == '"':
                            part.extend("\\" * (count // 2))
                            if count % 2: part.append('"'); i += 1
                            else: quoted = not quoted; i += 1
                        else: part.extend("\\" * count)
                    elif command_line[i] == '"': quoted = not quoted; i += 1
                    else: part.append(command_line[i]); i += 1
                args.append("".join(part))
            return args
        return shlex.split(command_line, posix=True)
    except ValueError:
        return []


def _linux_starttime(stat: str) -> str:
    end = stat.rfind(")")
    if end < 0:
        raise ValueError("malformed process stat")
    fields = stat[end + 1:].split()
    return fields[19]  # field 22, after pid and parenthesized comm


class _MacBsdInfo(ctypes.Structure):
    # Apple XNU proc_info.h, struct proc_bsdinfo (PROC_PIDTBSDINFO).
    _fields_ = [
        (name, ctypes.c_uint32) for name in (
            "pbi_flags", "pbi_status", "pbi_xstatus", "pbi_pid", "pbi_ppid",
            "pbi_uid", "pbi_gid", "pbi_ruid", "pbi_rgid", "pbi_svuid", "pbi_svgid", "rfu_1",
        )
    ] + [
        ("pbi_comm", ctypes.c_char * 16), ("pbi_name", ctypes.c_char * 32),
    ] + [
        (name, ctypes.c_uint32) for name in (
            "pbi_nfiles", "pbi_pgid", "pbi_pjobc", "e_tdev", "e_tpgid", "pbi_nice",
        )
    ] + [
        ("pbi_start_tvsec", ctypes.c_uint64), ("pbi_start_tvusec", ctypes.c_uint64),
    ]


def _mac_starttime(pid: int) -> str:
    libproc = ctypes.CDLL("/usr/lib/libproc.dylib")
    info = _MacBsdInfo()
    size = libproc.proc_pidinfo(pid, 3, 0, ctypes.byref(info), ctypes.sizeof(info))
    if size != ctypes.sizeof(info) or info.pbi_pid != pid:
        raise OSError("native process start identity unavailable")
    seconds, micros = info.pbi_start_tvsec, info.pbi_start_tvusec
    if not seconds or micros >= 1_000_000:
        raise OSError("invalid native process start identity")
    return f"{seconds}.{micros:06d}"


def _parse_mac_procargs(buffer: bytes) -> list[str]:
    if len(buffer) < 5:
        raise ValueError("process arguments unavailable")
    count = struct.unpack_from("=i", buffer)[0]
    if count < 1 or count > 4096:
        raise ValueError("invalid process argument count")
    position = buffer.find(b"\0", 4)
    if position < 0:
        raise ValueError("process executable path missing")
    position += 1
    while position < len(buffer) and buffer[position] == 0:
        position += 1
    argv = []
    for _ in range(count):
        end = buffer.find(b"\0", position)
        if end < 0:
            raise ValueError("incomplete process arguments")
        argv.append(buffer[position:end].decode("utf-8", "replace"))
        position = end + 1
    return argv


def _mac_argv(pid: int) -> list[str]:
    libc = ctypes.CDLL("/usr/lib/libSystem.B.dylib", use_errno=True)
    libc.sysctl.argtypes = [ctypes.POINTER(ctypes.c_int), ctypes.c_uint, ctypes.c_void_p, ctypes.POINTER(ctypes.c_size_t), ctypes.c_void_p, ctypes.c_size_t]
    mib = (ctypes.c_int * 3)(1, 49, pid)  # CTL_KERN, KERN_PROCARGS2, PID
    buffer = ctypes.create_string_buffer(262144)
    length = ctypes.c_size_t(ctypes.sizeof(buffer))
    if libc.sysctl(mib, 3, buffer, ctypes.byref(length), None, 0) != 0:
        raise OSError(ctypes.get_errno(), "native process arguments unavailable")
    return _parse_mac_procargs(buffer.raw[:length.value])


def _mac_running_application(pid: int) -> Callable[[], bool] | None:
    # NSRunningApplication represents one process instance; terminate requests a normal quit.
    ctypes.CDLL("/System/Library/Frameworks/AppKit.framework/AppKit")
    objc = ctypes.CDLL("/usr/lib/libobjc.A.dylib")
    objc.objc_getClass.restype = ctypes.c_void_p
    objc.objc_getClass.argtypes = [ctypes.c_char_p]
    objc.sel_registerName.restype = ctypes.c_void_p
    objc.sel_registerName.argtypes = [ctypes.c_char_p]
    send = objc.objc_msgSend
    app_class = objc.objc_getClass(b"NSRunningApplication")
    select = objc.sel_registerName
    send.restype = ctypes.c_void_p
    send.argtypes = [ctypes.c_void_p, ctypes.c_void_p, ctypes.c_int]
    app = send(app_class, select(b"runningApplicationWithProcessIdentifier:"), pid)
    if not app:
        return None
    def terminate() -> bool:
        send.argtypes = [ctypes.c_void_p, ctypes.c_void_p]
        send.restype = ctypes.c_bool
        return bool(send(app, select(b"terminate")))
    return terminate


def _mac_request_close(pid: int, started_at: str) -> bool:
    terminate = _mac_running_application(pid)
    if terminate is None or _mac_starttime(pid) != started_at:
        return False
    return terminate()


class PlatformAdapter:
    name = "unknown"

    def __init__(self, command_runner: Runner = subprocess.run):
        self._run = command_runner
        self.cli_executable: str | None = None

    def is_cli_process(self, argv: list[str], executable: str) -> bool:
        if self.cli_executable and os.path.normcase(os.path.realpath(executable)) == os.path.normcase(os.path.realpath(self.cli_executable)):
            return True
        return _cli_invocation(argv)

    def canonicalize_project(self, project: str) -> str:
        return os.path.normcase(os.path.realpath(os.path.abspath(project)))

    def canonicalize_external_project(self, project: str, *, deadline: Deadline | None = None) -> str:
        return self.canonicalize_project(project)

    def cli_project_path(self, project: str, cli_path: str, timeout: float | None = None, *, deadline: Deadline | None = None) -> str:
        return project

    def discover_cli(self, timeout: float | None = None, *, deadline: Deadline | None = None) -> str | None:
        return shutil.which("unity")

    def inventory(self, timeout: float | None = None) -> InventorySnapshot:
        raise NotImplementedError

    def _identity_is_current(self, identity: SessionIdentity | ProcessRecord, timeout: float | None = None) -> bool:
        snapshot = self.inventory(timeout=timeout)
        return snapshot.known and any(
            record.role == "editor"
            and record.pid == identity.pid
            and record.started_at == identity.started_at
            and self.canonicalize_project(record.project) == self.canonicalize_project(identity.project)
            for record in snapshot.records
        )

    def inspect_modal(self, identity: SessionIdentity, timeout: float) -> dict[str, Any]:
        return {"supported": False, "state": "unsupported", "reason": f"modal inspection is unavailable on {self.name}"}

    def cancel_safe_modal(self, identity: SessionIdentity, timeout: float) -> dict[str, Any]:
        return {"supported": False, "state": "unsupported", "reason": f"modal cancellation is unavailable on {self.name}"}

    def request_graceful_close(self, identity: SessionIdentity, timeout: float) -> dict[str, Any]:
        return {"supported": False, "state": "unsupported", "reason": f"graceful window close is unavailable on {self.name}"}


class LinuxAdapter(PlatformAdapter):
    name = "linux"

    def inventory(self, timeout: float | None = None) -> InventorySnapshot:
        records: list[ProcessRecord] = []
        try:
            for proc_dir in pathlib.Path("/proc").iterdir():
                if not proc_dir.name.isdigit():
                    continue
                try:
                    argv = (proc_dir / "cmdline").read_bytes().decode("utf-8", "replace").split("\0")
                    argv = [item for item in argv if item]
                    project = _project_from_argv(argv)
                    if not project:
                        continue
                    executable = os.path.basename(os.readlink(proc_dir / "exe"))
                    if executable.lower() not in {"unity", "unity.exe"}:
                        continue
                    if self.is_cli_process(argv, os.readlink(proc_dir / "exe")):
                        continue
                    stat = _linux_starttime((proc_dir / "stat").read_text(encoding="utf-8"))
                    records.append(ProcessRecord(
                        self.canonicalize_project(project), int(proc_dir.name), stat, executable,
                        _process_role(argv), _worker_parent(argv), _log_from_argv(argv),
                    ))
                except PermissionError as exc:
                    return InventorySnapshot(False, (), f"incomplete process inventory: {exc}")
                except (FileNotFoundError, ProcessLookupError, OSError, ValueError, IndexError):
                    continue
            return InventorySnapshot(True, tuple(records))
        except (PermissionError, OSError) as exc:
            return InventorySnapshot(False, (), f"process inventory failed: {exc}")

    def request_graceful_close(self, identity: SessionIdentity, timeout: float) -> dict[str, Any]:
        deadline = time.monotonic() + timeout
        remaining = lambda: max(0.001, deadline - time.monotonic())
        if not self._identity_is_current(identity, remaining()):
            return {"supported": True, "state": "refused", "reason": "process-identity-changed"}
        if not shutil.which("xprop") or not shutil.which("wmctrl"):
            return {"supported": False, "state": "unsupported", "reason": "xprop and wmctrl are required for PID-bound window close"}
        root = self._run(["xprop", "-root", "_NET_CLIENT_LIST"], capture_output=True, text=True, timeout=remaining(), check=False)
        if root.returncode != 0:
            return {"supported": False, "state": "unsupported", "reason": "the display does not expose _NET_CLIENT_LIST"}
        windows = re.findall(r"0x[0-9a-fA-F]+", root.stdout)
        matches: list[str] = []
        for window in windows:
            prop = self._run(["xprop", "-id", window, "_NET_WM_PID"], capture_output=True, text=True, timeout=remaining(), check=False)
            if prop.returncode == 0 and re.search(rf"=\s*{identity.pid}\s*$", prop.stdout):
                matches.append(window)
        if len(matches) != 1:
            return {"supported": False, "state": "unsupported", "reason": "an exact PID-bound Editor window was not found"}
        if not self._identity_is_current(identity, remaining()):
            return {"supported": True, "state": "refused", "reason": "process-identity-changed"}
        final = self._run(["xprop", "-id", matches[0], "_NET_WM_PID"], capture_output=True, text=True, timeout=remaining(), check=False)
        if final.returncode != 0 or not re.search(rf"=\s*{identity.pid}\s*$", final.stdout):
            return {"supported": True, "state": "refused", "reason": "window-owner-changed"}
        if time.monotonic() >= deadline:
            return {"supported": True, "state": "refused", "reason": "deadline-expired"}
        completed = self._run(["wmctrl", "-ic", matches[0]], capture_output=True, text=True, timeout=remaining(), check=False)
        if completed.returncode != 0:
            return {"supported": True, "state": "refused", "reason": "window close request failed"}
        return {"supported": True, "state": "requested", "method": "_NET_CLOSE_WINDOW"}


class MacOSAdapter(PlatformAdapter):
    name = "macos"

    def inventory(self, timeout: float | None = None) -> InventorySnapshot:
        try:
            completed = self._run(["ps", "-axo", "pid=,comm="], capture_output=True, text=True, timeout=min(timeout or 10, 10), check=False)
            if completed.returncode != 0:
                return InventorySnapshot(False, (), "ps process inventory failed")
            records: list[ProcessRecord] = []
            for line in completed.stdout.splitlines():
                parts = line.strip().split(None, 1)
                if len(parts) < 2:
                    continue
                if pathlib.Path(parts[1]).name != "Unity":
                    continue
                argv = _mac_argv(int(parts[0]))
                if self.is_cli_process(argv, argv[0] if argv else ""):
                    continue
                project = _project_from_argv(argv)
                if not project or not argv or pathlib.Path(argv[0]).name != "Unity":
                    continue
                records.append(ProcessRecord(
                    self.canonicalize_project(project), int(parts[0]), _mac_starttime(int(parts[0])), "Unity",
                    _process_role(argv), _worker_parent(argv), _log_from_argv(argv),
                ))
            return InventorySnapshot(True, tuple(records))
        except subprocess.TimeoutExpired as exc:
            return InventorySnapshot(False, (), f"process inventory failed: {exc}", code="PROCESS_INVENTORY_TIMEOUT")
        except (OSError, subprocess.SubprocessError, ValueError) as exc:
            return InventorySnapshot(False, (), f"process inventory failed: {exc}")

    def request_graceful_close(self, identity: SessionIdentity, timeout: float) -> dict[str, Any]:
        if not self._identity_is_current(identity, timeout):
            return {"supported": True, "state": "refused", "reason": "process-identity-changed"}
        if not _mac_request_close(identity.pid, identity.started_at):
            return {"supported": True, "state": "refused", "reason": "PID-bound terminate request failed"}
        return {"supported": True, "state": "requested", "method": "NSRunningApplication.terminate"}


class WindowsAdapter(PlatformAdapter):
    name = "windows"

    def canonicalize_project(self, project: str) -> str:
        return ntpath.normcase(ntpath.normpath(project))

    def is_cli_process(self, argv: list[str], executable: str) -> bool:
        if self.cli_executable and ntpath.normcase(ntpath.normpath(executable)) == ntpath.normcase(ntpath.normpath(self.cli_executable)):
            return True
        return super().is_cli_process(argv, executable)

    def _powershell(self) -> str:
        return shutil.which("pwsh") or shutil.which("powershell") or "powershell.exe"

    def inventory(self, timeout: float | None = None) -> InventorySnapshot:
        script = (
            "Get-CimInstance Win32_Process -Filter \"Name='Unity.exe'\" -ErrorAction Stop | "
            "ForEach-Object { $row=$_; "
            "if($row.CommandLine -match '(?i)^\\s*(?:\"[^\"]+\"|\\S+)\\s+(?:command|open|editors|status|auth)(?=\\s|$)' "
            "-or $row.CommandLine -match '(?i)(?:^|\\s)-ipc-key(?:\\s|$)'){return}; "
            "try { $p=Get-Process -Id $row.ProcessId -ErrorAction Stop; $ticks=$p.StartTime.ToUniversalTime().Ticks } "
            "catch { if($_.FullyQualifiedErrorId -like 'NoProcessFoundForGivenId,*'){return}; throw }; "
            "[pscustomobject]@{ProcessId=$row.ProcessId; StartTicks=$ticks; "
            "ExecutablePath=$row.ExecutablePath; CommandLine=$row.CommandLine} } | ConvertTo-Json -Compress"
        )
        try:
            completed = self._run([self._powershell(), "-NoProfile", "-NonInteractive", "-Command", script], capture_output=True, text=True, timeout=min(timeout or 15, 15), check=False)
            if completed.returncode != 0:
                return InventorySnapshot(False, (), f"Windows process inventory failed: {_bounded(completed.stderr)}")
            payload = json.loads(completed.stdout) if completed.stdout.strip() else []
            rows = payload if isinstance(payload, list) else [payload]
            records: list[ProcessRecord] = []
            for row in rows:
                if not isinstance(row, dict) or not row.get("StartTicks"):
                    return InventorySnapshot(False, (), "Windows process inventory omitted precise start identity")
                argv = _safe_argv(row.get("CommandLine"), windows=True)
                project = _project_from_argv(argv)
                executable = row.get("ExecutablePath") or ""
                if not project or ntpath.basename(executable).lower() != "unity.exe":
                    continue
                if self.is_cli_process(argv, executable):
                    continue
                records.append(ProcessRecord(
                    self.canonicalize_project(project), int(row["ProcessId"]), str(row["StartTicks"]), executable,
                    _process_role(argv), _worker_parent(argv), _log_from_argv(argv),
                ))
            return InventorySnapshot(True, tuple(records))
        except subprocess.TimeoutExpired as exc:
            return InventorySnapshot(False, (), f"Windows process inventory failed: {exc}", code="PROCESS_INVENTORY_TIMEOUT")
        except (OSError, subprocess.SubprocessError, ValueError, TypeError, json.JSONDecodeError) as exc:
            return InventorySnapshot(False, (), f"Windows process inventory failed: {exc}")

    def _modal(self, identity: SessionIdentity, action: str, timeout: float) -> dict[str, Any]:
        deadline = time.monotonic() + timeout
        if not self._identity_is_current(identity, timeout):
            return {"supported": True, "state": "refused", "reason": "process-identity-changed"}
        remaining = deadline - time.monotonic()
        if remaining <= 0:
            return {"supported": True, "state": "refused", "reason": "deadline-expired"}
        script_path = pathlib.Path(__file__).with_name("windows_modal.ps1")
        try:
            completed = self._run([
            self._powershell(), "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", str(script_path),
            "-Action", action, "-ExpectedProcessId", str(identity.pid), "-ExpectedStart", identity.started_at,
            "-ExpectedProject", identity.project, "-DeadlineUtcTicks", str(621355968000000000 + time.time_ns() // 100 + int(remaining * 10_000_000)),
            ], capture_output=True, text=True, timeout=remaining, check=False)
        except subprocess.TimeoutExpired:
            return {"supported": True, "state": "refused", "reason": "Windows helper reached its deadline"}
        if completed.returncode != 0:
            return {"supported": True, "state": "refused", "reason": "Windows helper failed", "exitCode": completed.returncode, "stderr": _bounded(completed.stderr)}
        return _windows_helper_response(completed, action)

    def inspect_modal(self, identity: SessionIdentity, timeout: float) -> dict[str, Any]:
        return self._modal(identity, "Inspect", timeout)

    def cancel_safe_modal(self, identity: SessionIdentity, timeout: float) -> dict[str, Any]:
        return self._modal(identity, "Cancel", timeout)

    def request_graceful_close(self, identity: SessionIdentity, timeout: float) -> dict[str, Any]:
        return self._modal(identity, "Close", timeout)


class WslAdapter(WindowsAdapter):
    name = "wsl"

    def canonicalize_project(self, project: str) -> str:
        if re.match(r"^[A-Za-z]:[\\/]", project) or project.startswith("\\\\"):
            converted = self._convert_path("-u", project)
            return os.path.normcase(os.path.realpath(converted))
        return os.path.normcase(os.path.realpath(os.path.abspath(project)))

    def canonicalize_external_project(self, project: str, *, deadline: Deadline | None = None) -> str:
        if re.match(r"^[A-Za-z]:[\\/]", project) or project.startswith("\\\\"):
            return os.path.normcase(os.path.realpath(self._convert_path("-u", project, deadline=deadline)))
        return self.canonicalize_project(project)

    def is_cli_process(self, argv: list[str], executable: str) -> bool:
        cli = self.cli_executable
        if cli and not re.match(r"^[A-Za-z]:[\\/]", cli) and not cli.startswith("\\\\"):
            if getattr(self, "_cached_cli_source", None) != cli:
                try:
                    self._cached_cli_native = self._convert_path("-w", cli)
                except RuntimeError:
                    self._cached_cli_native = None
                self._cached_cli_source = cli
            cli = self._cached_cli_native
        if cli and ntpath.normcase(ntpath.normpath(executable)) == ntpath.normcase(ntpath.normpath(cli)):
            return True
        return _cli_invocation(argv)

    def _convert_path(self, direction: str, value: str, timeout: float | None = None, *, deadline: Deadline | None = None) -> str:
        deadline = deadline if deadline is not None else Deadline.after(5 if timeout is None else min(timeout, 5))
        argv = ["wslpath", direction, value]
        limit = deadline.require_remaining("wslpath", 5)
        completed = self._run(argv, capture_output=True, text=True, timeout=limit, check=False)
        if completed.returncode != 0 or not completed.stdout.strip():
            raise RuntimeError("WSL path conversion failed")
        return completed.stdout.strip()

    def cli_project_path(self, project: str, cli_path: str, timeout: float | None = None, *, deadline: Deadline | None = None) -> str:
        if cli_path.lower().endswith(".exe"):
            return self._convert_path("-w", project, timeout, deadline=deadline)
        return project

    def discover_cli(self, timeout: float | None = None, *, deadline: Deadline | None = None) -> str | None:
        deadline = deadline if deadline is not None else Deadline.after(10 if timeout is None else timeout)
        script = "Get-Command unity -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty Source"
        try:
            argv = [self._powershell(), "-NoProfile", "-NonInteractive", "-Command", script]
            remaining = deadline.require_remaining("Unity CLI discovery", 10)
            completed = self._run(argv, capture_output=True, text=True, timeout=remaining, check=False)
            if completed.returncode == 0 and completed.stdout.strip():
                return self._convert_path("-u", completed.stdout.strip(), deadline=deadline)
        except subprocess.TimeoutExpired:
            raise
        except (OSError, subprocess.SubprocessError, RuntimeError):
            pass
        deadline.require_remaining("Unity CLI discovery")
        return shutil.which("unity")

    def default_editor_log(self, timeout: float | None = None, *, deadline: Deadline | None = None) -> str | None:
        deadline = deadline if deadline is not None else Deadline.after(10 if timeout is None else timeout)
        script = "[IO.Path]::Combine([Environment]::GetFolderPath('LocalApplicationData'), 'Unity', 'Editor', 'Editor.log')"
        try:
            argv = [self._powershell(), "-NoProfile", "-NonInteractive", "-Command", script]
            remaining = deadline.require_remaining("Editor log discovery", 10)
            completed = self._run(argv, capture_output=True, text=True, timeout=remaining, check=False)
            if completed.returncode != 0 or not ntpath.isabs(completed.stdout.strip()):
                return None
            return self._convert_path("-u", completed.stdout.strip(), deadline=deadline)
        except subprocess.TimeoutExpired:
            raise
        except (OSError, subprocess.SubprocessError, RuntimeError):
            return None

    def _modal(self, identity: SessionIdentity, action: str, timeout: float) -> dict[str, Any]:
        deadline = time.monotonic() + timeout
        if not self._identity_is_current(identity, timeout):
            return {"supported": True, "state": "refused", "reason": "process-identity-changed"}
        remaining = deadline - time.monotonic()
        if remaining <= 0:
            return {"supported": True, "state": "refused", "reason": "deadline-expired"}
        try:
            script_path = self._convert_path("-w", str(pathlib.Path(__file__).with_name("windows_modal.ps1")), remaining)
            remaining = deadline - time.monotonic()
            if remaining <= 0:
                return {"supported": True, "state": "refused", "reason": "deadline-expired"}
            project_path = self._convert_path("-w", identity.project, remaining)
            remaining = deadline - time.monotonic()
            if remaining <= 0:
                return {"supported": True, "state": "refused", "reason": "deadline-expired"}
        except (RuntimeError, subprocess.TimeoutExpired):
            return {"supported": True, "state": "refused", "reason": "Windows path conversion failed"}
        try:
            completed = self._run([
            self._powershell(), "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", script_path,
            "-Action", action, "-ExpectedProcessId", str(identity.pid), "-ExpectedStart", identity.started_at,
            "-ExpectedProject", project_path, "-DeadlineUtcTicks", str(621355968000000000 + time.time_ns() // 100 + int(remaining * 10_000_000)),
            ], capture_output=True, text=True, timeout=remaining, check=False)
        except subprocess.TimeoutExpired:
            return {"supported": True, "state": "refused", "reason": "Windows helper reached its deadline"}
        if completed.returncode != 0:
            return {"supported": True, "state": "refused", "reason": "Windows helper failed", "exitCode": completed.returncode, "stderr": _bounded(completed.stderr)}
        return _windows_helper_response(completed, action)


def current_platform() -> PlatformAdapter:
    system = host_platform.system().lower()
    if system == "linux" and "microsoft" in host_platform.release().lower():
        return WslAdapter()
    if system == "windows":
        return WindowsAdapter()
    if system == "darwin":
        return MacOSAdapter()
    if system == "linux":
        return LinuxAdapter()
    return PlatformAdapter()
