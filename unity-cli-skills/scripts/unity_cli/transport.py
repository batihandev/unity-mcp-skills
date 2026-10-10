from __future__ import annotations

import json
import math
import os
import pathlib
import re
import subprocess
import tempfile
import shlex
import time
from typing import Any

from .model import Deadline, CommandResult, RegistryObservation, SessionIdentity


NATIVE_CLEANUP_RESERVE_SECONDS = 15


class CliMissing(RuntimeError):
    pass


class CliTransport:
    def __init__(self, platform, cli_override: str | None = None, command_runner=subprocess.run, command_timeout: float = 30, launcher=subprocess.Popen, diagnostic_root: str | None = None):
        self.platform = platform
        self.cli_override = cli_override
        self._run = command_runner
        self._launch = launcher
        self.diagnostic_root = diagnostic_root
        self._launchers: dict[int, tuple[Any, pathlib.Path]] = {}
        self.command_timeout = command_timeout
        self.cli_path: str | None = None
        self._project_paths: dict[tuple[str, str], str] = {}

    def _budget(self, timeout: float | None, deadline: Deadline | None) -> Deadline:
        if deadline is not None:
            return deadline.capped(self.command_timeout if timeout is None else min(timeout, self.command_timeout))
        limit = self.command_timeout if timeout is None else min(timeout, self.command_timeout)
        return Deadline.after(limit)

    def ensure_cli(self, timeout: float | None = None, *, deadline: Deadline | None = None) -> str:
        deadline = self._budget(timeout, deadline)
        deadline.require_remaining("Unity CLI discovery")
        if self.cli_path:
            return self.cli_path
        candidate = self.cli_override or self.platform.discover_cli(timeout=deadline.remaining(), deadline=deadline)
        if not candidate:
            raise CliMissing("Unity CLI was not found; install it or pass --cli PATH")
        if self.cli_override and not pathlib.Path(candidate).is_file():
            raise CliMissing(f"Unity CLI override does not exist: {candidate}")
        self.cli_path = candidate
        return candidate

    def _invoke(self, args: list[str], timeout: float | None = None, *, deadline: Deadline | None = None) -> CommandResult:
        deadline = self._budget(timeout, deadline)
        try:
            cli = self.ensure_cli(deadline=deadline)
            argv = [cli, *args]
            framework_args = args[:args.index("--")] if "--" in args else args
            timeout_index = argv.index("--timeout") + 1 if "--timeout" in framework_args else None
            requested = float(argv[timeout_index]) if timeout_index is not None else None
            limit = deadline.require_remaining("Unity CLI command")
            if args and args[0] == "command" and timeout_index is not None:
                milliseconds = int(limit * 1000)
                if milliseconds < 1:
                    raise subprocess.TimeoutExpired("Unity CLI command", limit)
                argv[timeout_index] = str(min(requested, milliseconds / 1000))
                args[timeout_index - 1] = argv[timeout_index]
            elif args and args[0] == "test" and timeout_index is not None:
                native_timeout = int(limit) - NATIVE_CLEANUP_RESERVE_SECONDS
                if native_timeout < 1:
                    return CommandResult.failed("TEST_TIMEOUT_BUDGET_TOO_SMALL", "The host deadline cannot reserve time for Unity's native Editor cleanup")
                argv[timeout_index] = str(min(int(requested), native_timeout))
                args[timeout_index - 1] = argv[timeout_index]
            completed = self._run(
                argv, capture_output=True, text=True,
                timeout=limit, check=False,
            )
        except subprocess.TimeoutExpired as exc:
            return CommandResult.failed("CLI_TIMEOUT", "Unity CLI command reached its deadline", stderr=_bounded(exc.stderr))
        except OSError as exc:
            return CommandResult.failed("CLI_EXEC_FAILED", f"Unity CLI could not be executed: {exc}")
        payload = None
        if completed.stdout.strip():
            try:
                payload = json.loads(completed.stdout)
            except (ValueError, RecursionError):
                return CommandResult.failed(
                    "CLI_INVALID_JSON", "Unity CLI returned invalid JSON",
                    exitCode=completed.returncode, stdout=_bounded(completed.stdout), stderr=_bounded(completed.stderr),
                )
        diagnostics = {
            "exitCode": completed.returncode,
            "stderr": _bounded(completed.stderr),
        }
        if not isinstance(payload, dict) or not isinstance(payload.get("success"), bool) or (payload["success"] and not isinstance(payload.get("data"), dict)):
            return CommandResult.failed("CLI_INVALID_ENVELOPE", "Unity CLI returned a malformed command envelope", stdout=_bounded(completed.stdout), **diagnostics)
        if completed.returncode != 0 or (isinstance(payload, dict) and payload.get("success") is False):
            code, message = _envelope_error(payload)
            return CommandResult.failed(_bounded(code or "CLI_COMMAND_FAILED", 500), _bounded(message or "Unity CLI command failed", 500), payload=_redact_value(payload), **diagnostics)
        return CommandResult.ok(payload, **diagnostics)

    def _project(self, project: str, timeout: float | None = None, *, deadline: Deadline | None = None, cache: bool = True) -> str:
        deadline = self._budget(timeout, deadline)
        cli = self.ensure_cli(deadline=deadline)
        remaining = deadline.require_remaining("Unity CLI preparation")
        if not cache:
            target = self.platform.cli_project_path(project, cli, timeout=remaining, deadline=deadline)
            deadline.require_remaining("Unity CLI preparation")
            return target
        canonicalize = getattr(self.platform, "canonicalize_project", None)
        canonical = canonicalize(project) if canonicalize is not None else project
        key = (canonical, cli)
        if key not in self._project_paths:
            target = self.platform.cli_project_path(project, cli, timeout=remaining, deadline=deadline)
            deadline.require_remaining("Unity CLI preparation")
            self._project_paths[key] = target
        target = self._project_paths[key]
        deadline.require_remaining("Unity CLI preparation")
        return target

    def _project_budget(self, project: str, timeout: float | None, *, deadline: Deadline | None = None) -> tuple[str, float]:
        deadline = self._budget(timeout, deadline)
        try:
            target = self._project(project, deadline=deadline)
        except subprocess.TimeoutExpired:
            return project, 0
        return target, deadline.remaining()

    def invoke_command(self, project: str, command: str, arguments: dict[str, str | int | bool] | None = None, timeout: float | None = None, *, deadline: Deadline | None = None) -> CommandResult:
        """Invoke one structured Pipeline command without shell evaluation."""
        if not isinstance(command, str) or not re.fullmatch(r"[A-Za-z][A-Za-z0-9_-]*(?:\.[A-Za-z][A-Za-z0-9_-]*)*", command):
            return CommandResult.failed("INVALID_COMMAND", "The Pipeline command name is invalid")
        if arguments is None:
            arguments = {}
        if not isinstance(arguments, dict):
            return CommandResult.failed("INVALID_ARGUMENTS", "Pipeline command arguments must be an object")

        args = []
        normalized: dict[str, str | int | bool] = {}
        for key, value in arguments.items():
            if not isinstance(key, str) or not re.fullmatch(r"[A-Za-z][A-Za-z0-9_-]*", key):
                return CommandResult.failed("INVALID_ARGUMENTS", "A Pipeline argument name is invalid")
            if isinstance(value, bool):
                normalized[key] = value
                if value:
                    args.append(f"--{key}")
            elif isinstance(value, (str, int)):
                normalized[key] = value
                args.extend((f"--{key}", str(value)))
            else:
                return CommandResult.failed("INVALID_ARGUMENTS", f"Pipeline argument {key!r} must be a string, integer, or Boolean")

        budget = self.command_timeout if timeout is None else min(timeout, self.command_timeout)
        if not 0 < budget < float("inf") or not math.isfinite(budget * 1000):
            return CommandResult.failed("CLI_TIMEOUT", "Unity CLI command reached its deadline")
        deadline = self._budget(timeout, deadline)
        try:
            target_project = self._project(project, budget, deadline=deadline)
            cli = self.ensure_cli(deadline=deadline)
            budget = deadline.remaining()
            cli_timeout_ms = int(budget * 1000)
            if cli_timeout_ms < 1:
                return CommandResult.failed("CLI_TIMEOUT", "Unity CLI command cannot fit a positive timeout within its deadline",
                                            invocation={"command": command, "arguments": normalized, "project": project})
            args = ["command", "--project-path", target_project, "--format", "json", "--timeout", str(cli_timeout_ms / 1000), command, "--", *args]
        except subprocess.TimeoutExpired as exc:
            return CommandResult.failed("CLI_TIMEOUT", "Unity CLI preparation reached its deadline", stderr=_bounded(exc.stderr))
        except (CliMissing, OSError, RuntimeError, subprocess.SubprocessError) as exc:
            code = "CLI_NOT_FOUND" if isinstance(exc, CliMissing) else "CLI_EXEC_FAILED"
            return CommandResult.failed(code, str(exc), invocation={"command": command, "arguments": normalized, "project": project})

        result = self._invoke(args, timeout=budget, deadline=deadline)
        argv = [cli, *args]
        invocation = {"argv": argv, "command": command, "arguments": normalized, "project": project}
        diagnostics = dict(result.diagnostics)
        diagnostics["invocation"] = invocation
        if result.success:
            return CommandResult.ok(result.data, **diagnostics)
        return CommandResult.failed(result.code or "CLI_COMMAND_FAILED", result.message or "Unity CLI command failed", **diagnostics)

    def invoke_native_test(self, project: str, mode: str, filter_value: str | None, output: str, editor_version: str, timeout: float, *, assembly: str | None = None, deadline: Deadline | None = None) -> CommandResult:
        """Run a top-level native suite and retain invocation evidence."""
        if mode not in {"EditMode", "PlayMode"}:
            return CommandResult.failed("INVALID_ARGUMENT", "Test mode must be EditMode or PlayMode")
        if filter_value is not None and (not isinstance(filter_value, str) or not filter_value):
            return CommandResult.failed("INVALID_ARGUMENT", "Test filter must be omitted or nonempty")
        if assembly is not None and (not isinstance(assembly, str) or not assembly or ";" in assembly or assembly.startswith("-")):
            return CommandResult.failed("INVALID_ARGUMENT", "One exact assembly name is required")
        try:
            deadline = self._budget(timeout, deadline)
            initial_budget = deadline.remaining()
            target_project = self._project(project, initial_budget, deadline=deadline)
            remaining = deadline.remaining()
            target_output = self._project(output, remaining, deadline=deadline, cache=False)
            remaining = deadline.remaining()
            host_budget = min(remaining, self.command_timeout)
            native_timeout = int(host_budget) - NATIVE_CLEANUP_RESERVE_SECONDS
            if native_timeout < 1:
                return CommandResult.failed("TEST_TIMEOUT_BUDGET_TOO_SMALL", "The host deadline cannot reserve time for Unity's native Editor cleanup")
            args = ["test", target_project, "--mode", mode, "--output", target_output,
                    "--editor-version", editor_version, "--timeout", str(native_timeout), "--format", "json"]
            if filter_value is not None:
                args.extend(("--filter", filter_value))
            if assembly is not None:
                args.extend(("--", "-assemblyNames", assembly))
            cli = self.ensure_cli(deadline=deadline)
        except subprocess.TimeoutExpired as exc:
            return CommandResult.failed("CLI_TIMEOUT", "Unity CLI preparation reached its deadline", stderr=_bounded(exc.stderr))
        except (CliMissing, OSError, RuntimeError, subprocess.SubprocessError) as exc:
            code = "CLI_NOT_FOUND" if isinstance(exc, CliMissing) else "CLI_EXEC_FAILED"
            return CommandResult.failed(code, str(exc), invocation={"command": "test", "project": project, "output": output})
        result = self._invoke(args, deadline=deadline)
        argv = [cli, *args]
        invocation = {"argv": argv, "command": "test", "mode": mode, "filter": filter_value, "project": project, "output": output, "editorVersion": editor_version, "assembly": assembly}
        diagnostics = dict(result.diagnostics)
        diagnostics["invocation"] = invocation
        if result.success:
            return CommandResult.ok(result.data, **diagnostics)
        return CommandResult.failed(result.code or "CLI_COMMAND_FAILED", result.message or "Unity test command failed", **diagnostics)

    def registry(self, project: str, timeout: float | None = None, *, deadline: Deadline | None = None) -> CommandResult:
        deadline = self._budget(timeout, deadline)
        result = self._invoke(["editors", "running", "--format", "json"], timeout, deadline=deadline)
        if not result.success:
            return result
        envelope = result.data.get("data") if isinstance(result.data, dict) else None
        instances = envelope.get("instances") if isinstance(envelope, dict) else None
        if not isinstance(instances, list):
            return CommandResult.failed("REGISTRY_INVALID", "Unity CLI editor inventory omitted data.instances")
        observations: list[RegistryObservation] = []
        for instance in instances:
            if not isinstance(instance, dict):
                return CommandResult.failed("REGISTRY_INVALID", "Unity CLI editor inventory contained a malformed instance")
            raw_project = instance.get("project") or instance.get("projectPath")
            pid = instance.get("pid") or instance.get("processId")
            if not isinstance(raw_project, str) or not raw_project or not isinstance(pid, int) or isinstance(pid, bool) or pid <= 0:
                return CommandResult.failed("REGISTRY_INVALID", "Unity CLI editor inventory contained a malformed instance")
            try:
                canonical = self.platform.canonicalize_external_project(raw_project, deadline=deadline)
            except subprocess.TimeoutExpired as exc:
                return CommandResult.failed("CLI_TIMEOUT", "Editor registry path conversion reached its deadline", stderr=_bounded(exc.stderr))
            port = instance.get("port")
            pipeline_port = port if isinstance(port, int) and not isinstance(port, bool) and 1 <= port <= 65535 else None
            observations.append(RegistryObservation(
                canonical, pid,
                _optional_string(instance.get("startedAt") or instance.get("startTime")), "editors-running", pipeline_port,
            ))
        return CommandResult.ok(observations, **result.diagnostics)

    def readiness(self, identity: SessionIdentity, timeout: float | None = None, *, deadline: Deadline | None = None) -> CommandResult:
        deadline = self._budget(timeout, deadline)
        target_project, timeout = self._project_budget(identity.project, timeout, deadline=deadline)
        code = self._identity_code(identity, target_project) + "return new { ready = exact, pid = current.Id, project = currentProject, startedAt = start };"
        result = self._invoke(["command", "eval", code, "--project-path", target_project, "--format", "json"], timeout, deadline=deadline)
        if result.code == "CLI_TIMEOUT":
            return result
        authentication = _authentication_refusal(result, identity, "identity")
        if authentication is not None:
            return authentication
        value = _pipeline_result(result.data) if result.success else None
        if not isinstance(value, dict):
            return CommandResult.ok({"ready": False, "pid": identity.pid, "project": identity.project})
        exact = value.get("ready") is True and value.get("pid") == identity.pid and value.get("startedAt") == identity.started_at
        project = value.get("project")
        try:
            exact = exact and isinstance(project, str) and self.platform.canonicalize_external_project(project, deadline=deadline) == identity.project
        except subprocess.TimeoutExpired as exc:
            return CommandResult.failed("CLI_TIMEOUT", "Editor readiness path conversion reached its deadline", stderr=_bounded(exc.stderr))
        identity_observation = value
        if not exact:
            return CommandResult.ok({"ready": False, "pid": identity.pid, "project": identity.project,
                                     "startedAt": identity.started_at}, identityObservation=identity_observation,
                                    identityTransport=result.diagnostics)
        status_result = self.invoke_command(identity.project, "editor_status", {}, deadline=deadline)
        authentication = _authentication_refusal(status_result, identity, "editor_status")
        if authentication is not None:
            return authentication
        diagnostics = {"identityObservation": identity_observation, "identityTransport": result.diagnostics,
                       "nativeEnvelope": status_result.data, "nativeTransport": status_result.diagnostics}
        if status_result.code == "CLI_TIMEOUT":
            return CommandResult.failed("CLI_TIMEOUT", "Editor native readiness reached its deadline", **diagnostics)
        status = _pipeline_result(status_result.data) if status_result.success else None
        envelope = status_result.data.get("data") if status_result.success and isinstance(status_result.data, dict) else None
        if isinstance(envelope, dict) and envelope.get("command") == "editor_status":
            if status is None:
                status = envelope.get("result")
        else:
            status = None
        diagnostics["nativeStatus"] = status
        idle = (isinstance(status, dict) and status.get("compiling") is False
                and status.get("domainReloadInProgress") is False
                and isinstance(status.get("status"), str)
                and status.get("status") in {"ready", "playing"}
                and isinstance(status.get("playMode"), str)
                and status.get("playMode") in {"stopped", "playing", "paused"})
        native_project = status.get("projectPath") if isinstance(status, dict) else None
        try:
            deadline.require_remaining("Editor native readiness")
            idle = idle and isinstance(native_project, str) and self.platform.canonicalize_external_project(native_project, deadline=deadline) == identity.project
            deadline.require_remaining("Editor native readiness")
        except subprocess.TimeoutExpired as exc:
            return CommandResult.failed("CLI_TIMEOUT", "Editor native readiness reached its deadline", stderr=_bounded(exc.stderr), **diagnostics)
        return CommandResult.ok({"ready": bool(idle), "pid": identity.pid, "project": identity.project,
                                 "startedAt": identity.started_at}, **diagnostics)

    def _identity_code(self, identity: SessionIdentity, target_project: str) -> str:
        project = json.dumps(target_project)
        started = json.dumps(identity.started_at)
        if self.platform.name == "linux":
            source = 'var stat = System.IO.File.ReadAllText("/proc/self/stat"); var fields = stat.Substring(stat.LastIndexOf(\')\') + 1).Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries); var start = fields[19];'
        elif self.platform.name == "macos":
            # .NET StartTime preserves sub-second precision; native inventory token uses microseconds.
            source = 'var epoch = new System.DateTime(1970,1,1,0,0,0,System.DateTimeKind.Utc); var elapsed = current.StartTime.ToUniversalTime() - epoch; var start = ((long)elapsed.TotalSeconds).ToString() + "." + ((elapsed.Ticks % 10000000) / 10).ToString("D6");'
        else:
            source = 'var start = current.StartTime.ToUniversalTime().Ticks.ToString();'
        comparison = "System.StringComparison.OrdinalIgnoreCase" if self.platform.name in {"windows", "wsl"} else "System.StringComparison.Ordinal"
        return (
            "var current = System.Diagnostics.Process.GetCurrentProcess();"
            'var currentProject = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath,"..")).TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);'
            + source + f"var exact = current.Id == {identity.pid} && start == {started} && System.String.Equals(currentProject, {project}, {comparison});"
        )

    def dirty_scenes(self, identity: SessionIdentity, timeout: float | None = None, *, deadline: Deadline | None = None) -> CommandResult:
        code = (
            "return System.Linq.Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount)"
            ".Select(i => UnityEngine.SceneManagement.SceneManager.GetSceneAt(i))"
            ".Where(s => s.isDirty).Select(s => new { path = s.path, name = s.name, handle = s.handle.ToString(), saved = !string.IsNullOrEmpty(s.path) }).ToArray();"
        )
        deadline = self._budget(timeout, deadline)
        target_project, timeout = self._project_budget(identity.project, timeout, deadline=deadline)
        result = self._invoke([
            "command", "eval", code, "--project-path", target_project, "--format", "json",
        ], timeout, deadline=deadline)
        if not result.success:
            return _authentication_refusal(result, identity, "dirty_scenes") or result
        value = _pipeline_result(result.data)
        if not isinstance(value, list):
            return CommandResult.failed("DIRTY_STATE_INVALID", "Dirty-scene inspection returned an unexpected result", payload=_redact_value(result.data))
        return CommandResult.ok(value)

    def request_editor_exit(self, identity: SessionIdentity, timeout: float | None = None, *, deadline: Deadline | None = None) -> CommandResult:
        deadline = self._budget(timeout, deadline)
        budget = deadline.remaining()
        if not 0 < budget < float("inf"):
            return CommandResult.failed("CLI_TIMEOUT", "The close request reached its deadline")
        expires_utc_ticks = 621355968000000000 + time.time_ns() // 100 + int(budget * 10_000_000)
        deadline = self._budget(timeout, deadline)
        target_project, timeout = self._project_budget(identity.project, timeout, deadline=deadline)
        guard = self._identity_code(identity, target_project)
        inspection = (
            "(string reason, object[] scenes, object prefabStage, object[] assets) InspectExit(){" + guard +
            "if(!exact) return (\"identity\", new object[0], null, new object[0]);"
            "var scenes = System.Linq.Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount)"
            ".Select(i => UnityEngine.SceneManagement.SceneManager.GetSceneAt(i)).Where(s => s.isDirty)"
            ".Select(s => (object)new { path = s.path, name = s.name, handle = s.handle.ToString(), saved = !string.IsNullOrEmpty(s.path) }).ToArray();"
            "var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();"
            "object prefabStage = stage != null && stage.scene.isDirty ? (object)new { path = stage.assetPath, name = stage.scene.name, handle = stage.scene.handle.ToString() } : null;"
            "var assets = UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.Object>().Where(o => {"
            "if(!UnityEditor.EditorUtility.IsPersistent(o) || !UnityEditor.EditorUtility.IsDirty(o)) return false;"
            "var path = UnityEditor.AssetDatabase.GetAssetPath(o);"
            "if(path.StartsWith(\"Assets/\")) return true;"
            "if(!path.StartsWith(\"Packages/\")) return false;"
            "var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(path);"
            "return info != null && (info.source == UnityEditor.PackageManager.PackageSource.Embedded || info.source == UnityEditor.PackageManager.PackageSource.Local);"
            "}).Select(o => (object)new { path = UnityEditor.AssetDatabase.GetAssetPath(o), name = o.name, globalObjectId = UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(o).ToString(), type = o.GetType().FullName }).ToArray();"
            "return (scenes.Length > 0 || prefabStage != null || assets.Length > 0 ? \"dirty-state\" : null, scenes, prefabStage, assets); }"
        )
        code = inspection + "bool SafeToExit(){ return InspectExit().reason == null; }" + (
            f"var expires = new System.DateTime({expires_utc_ticks}, System.DateTimeKind.Utc);"
            "if(System.DateTime.UtcNow >= expires) return new { requested = false, reason = \"deadline\" };"
            "var refusal = InspectExit();"
            "if(refusal.reason != null) return new { requested = false, reason = refusal.reason, scenes = refusal.scenes, prefabStage = refusal.prefabStage, assets = refusal.assets };"
            "if(System.DateTime.UtcNow >= expires) return new { requested = false, reason = \"deadline\" };"
            "UnityEditor.EditorApplication.CallbackFunction callback = null;"
            "callback = () => { UnityEditor.EditorApplication.update -= callback;"
            "if(System.DateTime.UtcNow < expires && SafeToExit() && System.DateTime.UtcNow < expires) UnityEditor.EditorApplication.Exit(0); };"
            "UnityEditor.EditorApplication.update += callback;"
            "return new { requested = true };"
        )
        result = self._invoke([
            "command", "eval", code, "--project-path", target_project, "--format", "json",
        ], timeout, deadline=deadline)
        if not result.success:
            return _authentication_refusal(result, identity, "editor_exit") or result
        value = _pipeline_result(result.data)
        if not isinstance(value, dict) or value.get("requested") is not True:
            reasons = {"dirty-state": "DIRTY_EDITOR_STATE", "identity": "PROCESS_IDENTITY_CHANGED", "deadline": "CLI_TIMEOUT"}
            reason = value.get("reason") if isinstance(value, dict) else None
            return CommandResult.failed(reasons.get(reason, "CLOSE_NOT_ACCEPTED"), "The exact Editor refused the guarded close request", refusal=_redact_value(value), payload=_redact_value(result.data))
        return CommandResult.ok({"requested": True}, transport=result.diagnostics)

    def open(self, project: str, editor_version: str | None, timeout: float | None = None, batch_mode: bool = False, *, deadline: Deadline | None = None) -> CommandResult:
        deadline = self._budget(timeout, deadline)
        target_project, timeout = self._project_budget(project, timeout, deadline=deadline)
        if timeout is not None and timeout <= 0:
            return CommandResult.failed("CLI_TIMEOUT", "The open action reached its deadline before launch")
        args = ["open", target_project, "--format", "json"]
        if editor_version:
            args.extend(["--editor-version", editor_version])
        owned_root: pathlib.Path | None = None
        try:
            if self.diagnostic_root:
                root = pathlib.Path(self.diagnostic_root)
            else:
                owned_root = pathlib.Path(tempfile.mkdtemp(prefix="unity-session-launch-"))
                root = owned_root
            root.mkdir(mode=0o700, parents=True, exist_ok=True)
            fd, log_path = tempfile.mkstemp(prefix="open-", suffix=".log", dir=root)
        except OSError as exc:
            if owned_root is not None:
                try:
                    owned_root.rmdir()
                except OSError:
                    pass
            return CommandResult.failed("DIAGNOSTIC_STORAGE_FAILED", _bounded(f"The CLI launch diagnostic file could not be created: {exc}", 500))
        editor_log_path = root / f"editor-{pathlib.Path(log_path).stem}.log"
        try:
            native_editor_log = self._project(str(editor_log_path), deadline=deadline, cache=False)
        except subprocess.TimeoutExpired:
            os.close(fd)
            pathlib.Path(log_path).unlink(missing_ok=True)
            return CommandResult.failed("CLI_TIMEOUT", "The open action reached its deadline before launch")
        except (OSError, RuntimeError, subprocess.SubprocessError) as exc:
            os.close(fd)
            pathlib.Path(log_path).unlink(missing_ok=True)
            return CommandResult.failed("EDITOR_LOG_PATH_UNAVAILABLE", _bounded(f"The Editor log path could not be converted: {exc}", 500))
        if deadline.remaining() <= 0:
            os.close(fd)
            pathlib.Path(log_path).unlink(missing_ok=True)
            return CommandResult.failed("CLI_TIMEOUT", "The open action reached its deadline before launch")
        editor_args = ["-logFile", native_editor_log]
        if batch_mode:
            editor_args.insert(0, "-batchmode")
        args.extend(["--args", subprocess.list2cmdline(editor_args) if self.platform.name in {"windows", "wsl"} else shlex.join(editor_args)])
        try:
            with os.fdopen(fd, "wb") as log:
                options = {"stdin": subprocess.DEVNULL, "stdout": log, "stderr": subprocess.STDOUT, "close_fds": True}
                if os.name == "nt":
                    options["creationflags"] = subprocess.DETACHED_PROCESS | subprocess.CREATE_NEW_PROCESS_GROUP
                else:
                    options["start_new_session"] = True
                cli = self.ensure_cli(deadline=deadline)
                argv = [cli, *args]
                deadline.require_remaining("Unity CLI open")
                process = self._launch(argv, **options)
        except subprocess.TimeoutExpired:
            pathlib.Path(log_path).unlink(missing_ok=True)
            return CommandResult.failed("CLI_TIMEOUT", "The open action reached its deadline before launch")
        except OSError as exc:
            pathlib.Path(log_path).unlink(missing_ok=True)
            return CommandResult.failed("CLI_EXEC_FAILED", _bounded(f"Unity CLI could not be launched: {exc}", 500))
        self._launchers[process.pid] = (process, pathlib.Path(log_path))
        return CommandResult.ok({"launcherPid": process.pid, "diagnosticPath": log_path, "editorLogPath": str(editor_log_path)}, detached=True)

    def launch_status(self, launcher_pid: int, timeout: float | None = None, *, deadline: Deadline | None = None) -> CommandResult:
        launcher = self._launchers.get(launcher_pid)
        if launcher is None:
            return CommandResult.failed("LAUNCHER_UNKNOWN", "The launcher is not owned by this helper process")
        process, log_path = launcher
        exit_code = process.poll()
        try:
            with log_path.open("rb") as log:
                log.seek(max(0, log.seek(0, os.SEEK_END) - 8192))
                output = log.read().decode("utf-8", "replace")
        except OSError:
            output = ""
        payload = None
        for line in reversed(output.splitlines()):
            try:
                parsed = json.loads(line)
            except (ValueError, RecursionError):
                continue
            if isinstance(parsed, dict) and "success" in parsed:
                payload = parsed
                break
        if isinstance(payload, dict) and payload.get("success") is False:
            code, message = _envelope_error(payload)
            return CommandResult.failed(_bounded(code or "CLI_COMMAND_FAILED", 500), _bounded(message or "Unity CLI rejected open", 500), exitCode=exit_code, output=_bounded(output), diagnosticPath=str(log_path))
        if exit_code is not None and exit_code != 0:
            return CommandResult.failed("CLI_COMMAND_FAILED", "Unity CLI open launcher exited before the Editor appeared", exitCode=exit_code, output=_bounded(output), diagnosticPath=str(log_path))
        return CommandResult.ok({"state": "running" if exit_code is None else "completed", "exitCode": exit_code}, diagnosticPath=str(log_path))

    def diagnose_logs(self, project: str, log_file: str | None = None, timeout: float | None = None, *, deadline: Deadline | None = None) -> dict[str, Any]:
        deadline = self._budget(timeout, deadline)
        candidates: list[tuple[pathlib.Path, str]] = []
        if log_file and log_file != "-":
            if self.platform.name == "wsl" and (re.match(r"^[A-Za-z]:[\\/]", log_file) or log_file.startswith("\\\\")):
                try:
                    log_file = self.platform.canonicalize_external_project(log_file, deadline=deadline)
                except subprocess.TimeoutExpired:
                    return {"source": "editor-log", "code": "CLI_TIMEOUT", "error": "Editor log path conversion reached its deadline", "matches": []}
            custom = pathlib.Path(log_file)
            candidates.append((custom if custom.is_absolute() else pathlib.Path(project) / custom, "launch-argument"))
        candidates.append((pathlib.Path(project) / "Logs" / "Editor.log", "unverified"))
        if self.platform.name == "macos":
            candidates.append((pathlib.Path.home() / "Library" / "Logs" / "Unity" / "Editor.log", "unverified"))
        elif self.platform.name == "linux":
            candidates.append((pathlib.Path.home() / ".config" / "unity3d" / "Editor.log", "unverified"))
        elif self.platform.name == "wsl":
            finder = getattr(self.platform, "default_editor_log", None)
            try:
                default = finder(timeout=timeout, deadline=deadline) if finder else None
            except subprocess.TimeoutExpired:
                return {"source": "editor-log", "code": "CLI_TIMEOUT", "error": "Default Editor log discovery reached its deadline", "matches": []}
            if default:
                candidates.append((pathlib.Path(default), "unverified"))
        elif self.platform.name == "windows" and os.environ.get("LOCALAPPDATA"):
            candidates.append((pathlib.Path(os.environ["LOCALAPPDATA"]) / "Unity" / "Editor" / "Editor.log", "unverified"))
        for path, attribution in candidates:
            try:
                if not path.is_file():
                    continue
                matches: list[dict[str, Any]] = []
                for number, line in enumerate(path.read_text(encoding="utf-8", errors="replace").splitlines(), 1):
                    categories = []
                    if re.search(r"error CS\d{4}|Scripts have compiler errors", line, re.IGNORECASE):
                        categories.append("compile-error")
                    if re.search(r"Scene\(s\) Have Been Modified|modal|dialog", line, re.IGNORECASE):
                        categories.append("modal")
                    if categories:
                        matches.append({"line": number, "categories": categories, "text": _bounded(line, 500)})
                return {"source": "editor-log", "attribution": attribution, "matches": matches[-40:]}
            except OSError as exc:
                return {"source": "project-log", "error": str(exc), "matches": []}
        return {"source": "project-log", "error": "project Editor log was not found", "matches": []}


def _pipeline_result(payload: Any) -> Any:
    if not isinstance(payload, dict):
        return None
    data = payload.get("data")
    if not isinstance(data, dict):
        return None
    value = data.get("result")
    if isinstance(value, dict) and "Result" in value:
        return value.get("Result") if value.get("Ok") is True else None
    if isinstance(value, dict) and "result" in value:
        return value.get("result") if value.get("success") is True else None
    return None


def _envelope_error(payload: Any) -> tuple[str | None, str | None]:
    if not isinstance(payload, dict):
        return None, None
    errors = payload.get("errors")
    if isinstance(errors, list) and errors and isinstance(errors[0], dict):
        code = _optional_string(errors[0].get("code"))
        message = _optional_string(errors[0].get("message"))
        if code == "COMMAND_FAILED" and message is not None and len(message) <= 500 and re.fullmatch(
            r"Failed to execute command '[A-Za-z][A-Za-z0-9_-]*': Network error: An error occurred while sending the request\.",
            message,
        ):
            code = "CLI_COMMAND_FAILED"
        return code, message
    return None, None


def _optional_string(value: Any) -> str | None:
    return str(value) if value is not None else None


def _bounded(value: Any, limit: int = 4000) -> str:
    if value is None:
        return ""
    redacted = _redact(str(value))
    sanitized = "".join(character for character in redacted if character == "\t" or ord(character) >= 32)
    return sanitized[:limit]


def _redact(value: str) -> str:
    value = re.sub(r"(?i)(--?access[-_]?token|accessToken|licensingIpc|hubIPC)(?:=|\s+)\S+", r"\1=<redacted>", value)
    value = re.sub(r"(?i)(Bearer\s+)\S+", r"\1<redacted>", value)
    return value


def _redact_value(value: Any) -> Any:
    if isinstance(value, str): return _redact(value)
    if isinstance(value, list): return [_redact_value(item) for item in value]
    if isinstance(value, dict):
        redacted = {}
        for key, item in value.items():
            sensitive_key = isinstance(key, str) and re.search(r"(?i)(token|secret|password|authorization|api[_-]?key|licensingipc|hubipc)", key)
            safe_key = _redact(key) if isinstance(key, str) else key
            redacted[safe_key] = "<redacted>" if sensitive_key else _redact_value(item)
        return redacted
    return value


def _authentication_refusal(result: CommandResult, identity: SessionIdentity, phase: str) -> CommandResult | None:
    if result.success or not (result.code in {"HTTP_401", "UNAUTHORIZED"} or re.search(
        r"(?i)\b(?:HTTP(?:/\d(?:\.\d)?)?\s+401\b|401\s*\(Unauthorized\)|status(?:\s+code)?\s*[:=]\s*401\b)",
        result.message or "",
    )):
        return None
    return CommandResult.failed(
        "PIPELINE_AUTHENTICATION_FAILED",
        "Pipeline rejected authentication for the exact project; inspect its registry endpoint and native unity status before choosing recovery",
        process=identity.as_dict(), httpStatus=401, phase=phase,
        transport=_redact_value(result.diagnostics),
    )
