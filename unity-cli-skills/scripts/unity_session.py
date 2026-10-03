#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import pathlib
import re
import sys
import math
from typing import Any, Sequence, TextIO

from unity_cli.model import SessionIdentity
from unity_cli.lifecycle import SessionController, SessionRefusal
from unity_cli.platforms import PlatformAdapter, current_platform
from unity_cli.transport import CliTransport


_EDITOR_VERSION = re.compile(r"^m_EditorVersion:\s*(\S+)\s*$", re.MULTILINE)


class InputRefusal(RuntimeError):
    def __init__(self, code: str, message: str, details: dict[str, Any] | None = None):
        super().__init__(message)
        self.code = code
        self.details = details or {}

    def as_dict(self) -> dict[str, Any]:
        return {"ok": False, "error": {"code": self.code, "message": str(self), "details": self.details}}


class JsonArgumentParser(argparse.ArgumentParser):
    def error(self, message: str) -> None:
        raise InputRefusal("INVALID_ARGUMENT", message)


def project_editor_version(project: pathlib.Path) -> str:
    version_file = project / "ProjectSettings" / "ProjectVersion.txt"
    try:
        text = version_file.read_text(encoding="utf-8")
    except OSError as exc:
        raise InputRefusal(
            "EDITOR_VERSION_UNKNOWN",
            "The project Editor version is unavailable; pass --editor-version or restore ProjectSettings/ProjectVersion.txt",
            {"reason": str(exc)},
        ) from exc
    match = _EDITOR_VERSION.search(text)
    if not match:
        raise InputRefusal(
            "EDITOR_VERSION_UNKNOWN",
            "ProjectSettings/ProjectVersion.txt does not contain m_EditorVersion; pass --editor-version",
        )
    return match.group(1)


def _parser() -> argparse.ArgumentParser:
    parser = JsonArgumentParser(description="Discover and manage one exact Unity Editor project session.")
    parser.add_argument("action", choices=("discover", "open", "ready", "close", "restart", "recover"))
    parser.add_argument("--project", required=True, metavar="PATH")
    parser.add_argument("--expected-pid", type=int, metavar="PID")
    parser.add_argument("--expected-started-at", metavar="STARTED-AT")
    parser.add_argument("--cli", metavar="PATH")
    parser.add_argument("--editor-version", metavar="VERSION")
    parser.add_argument("--batch-mode", action="store_true", help="Pass -batchmode to the Editor on open or restart.")
    parser.add_argument("--timeout", type=float, default=120.0, metavar="SECONDS")
    parser.add_argument(
        "--graceful-close", action="store_true",
        help="For recover only, request an exact PID-targeted OS window close if readiness is not restored.",
    )
    return parser


def _validate(args: argparse.Namespace) -> pathlib.Path:
    if (args.expected_pid is None) != (args.expected_started_at is None):
        raise InputRefusal("INVALID_ARGUMENT", "--expected-pid and --expected-started-at must be provided together")
    if args.expected_pid is not None:
        if args.action not in {"ready", "close"} or args.expected_pid <= 0 or not args.expected_started_at:
            raise InputRefusal("INVALID_ARGUMENT", "Expected identity requires ready or close, a positive PID, and a nonempty started-at value")
    project = pathlib.Path(args.project).expanduser()
    if not project.is_dir():
        raise InputRefusal("PROJECT_NOT_FOUND", "The project directory does not exist", {"project": str(project)})
    if not math.isfinite(args.timeout) or args.timeout <= 0:
        raise InputRefusal("INVALID_TIMEOUT", "--timeout must be finite and greater than zero")
    if args.batch_mode and args.action not in {"open", "restart"}:
        raise InputRefusal("INVALID_ARGUMENT", "--batch-mode is valid only with open or restart")
    if args.graceful_close and args.action != "recover":
        raise InputRefusal("INVALID_ARGUMENT", "--graceful-close is valid only with recover")
    return project.resolve()


def _exit_code(code: str) -> int:
    if code in {"PROJECT_NOT_FOUND", "INVALID_TIMEOUT", "INVALID_ARGUMENT", "EDITOR_VERSION_UNKNOWN"}:
        return 2
    if code in {"CLI_NOT_FOUND", "PROCESS_INVENTORY_UNKNOWN", "GRACEFUL_CLOSE_UNAVAILABLE"}:
        return 3
    if code in {"DIRTY_SCENES", "MULTIPLE_PROJECT_EDITORS", "SESSION_NOT_RUNNING"}:
        return 4
    if code.endswith("TIMEOUT"):
        return 5
    return 6


def main(
    argv: Sequence[str] | None = None,
    *,
    stdout: TextIO = sys.stdout,
    platform: PlatformAdapter | None = None,
) -> int:
    try:
        args = _parser().parse_args(argv)
        project = _validate(args)
        adapter = platform or current_platform()
        if type(adapter) is PlatformAdapter:
            raise InputRefusal("PLATFORM_UNSUPPORTED", "This operating system has no session adapter")
        transport = CliTransport(adapter, cli_override=args.cli, command_timeout=args.timeout)
        controller = SessionController(str(project), transport, adapter, timeout=args.timeout)
        expected_identity = None if args.expected_pid is None else SessionIdentity(
            adapter.canonicalize_project(str(project)), args.expected_pid, args.expected_started_at, "")
        editor_version = args.editor_version
        if args.action in {"open", "restart"} and editor_version is None:
            editor_version = project_editor_version(project)
        if args.action == "discover":
            result = controller.discover()
        elif args.action == "open":
            result = controller.open(editor_version=editor_version, batch_mode=args.batch_mode)
        elif args.action == "ready":
            result = controller.ready(expected_identity=expected_identity)
        elif args.action == "close":
            result = controller.close(expected_identity=expected_identity)
        elif args.action == "restart":
            result = controller.restart(editor_version=editor_version, batch_mode=args.batch_mode)
        else:
            result = controller.recover(graceful_close=args.graceful_close)
        print(json.dumps(result, sort_keys=True), file=stdout)
        return 0 if result.get("ok", False) else 6
    except (InputRefusal, SessionRefusal) as exc:
        print(json.dumps(exc.as_dict(), sort_keys=True), file=stdout)
        return _exit_code(exc.code)


if __name__ == "__main__":
    raise SystemExit(main())
