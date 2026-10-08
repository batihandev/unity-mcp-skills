#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import math
import pathlib
import sys
from typing import Sequence, TextIO

from unity_cli.compile import CompileWorkflow, WorkflowRefusal
from unity_cli.lifecycle import SessionController, SessionRefusal
from unity_cli.platforms import PlatformAdapter, current_platform
from unity_cli.transport import CliTransport, _redact_value
from unity_cli.testing import TestWorkflow, WorkflowTestRefusal
from unity_cli.console import ConsoleWorkflow, WorkflowConsoleRefusal
from unity_cli.importer import ImportWorkflow, ImportWorkflowRefusal
from unity_cli.capture import CaptureWorkflow, CaptureRefusal


class InputRefusal(RuntimeError):
    def __init__(self, code: str, message: str, details: dict | None = None):
        super().__init__(message)
        self.code = code
        self.details = details or {}

    def as_dict(self):
        return {"ok": False, "error": {"code": self.code, "message": str(self), "details": self.details}}


class JsonArgumentParser(argparse.ArgumentParser):
    def error(self, message: str) -> None:
        raise InputRefusal("INVALID_ARGUMENT", message)


def _parser() -> argparse.ArgumentParser:
    parser = JsonArgumentParser(description="Run native Unity compile, exact-test, console, and guarded importer workflows.")
    parser.add_argument("action", choices=("compile", "test", "test-plan", "console", "importer", "importer-restore", "importer-batch", "camera-capture", "ui-capture", "ui-capture-batch"))
    parser.add_argument("--project", required=True, metavar="PATH")
    parser.add_argument("--source", action="append", metavar="FILE", help="Explicit project-relative or absolute source file to include in the stable input snapshot; repeat as needed.")
    parser.add_argument("--require-compiled", action="store_true", help="Refuse a fresh native up_to_date no-op; require compiling then completed.")
    parser.add_argument("--route", choices=("connected", "offline"), help="Test route: connected exact Editor or top-level offline Unity CLI.")
    parser.add_argument("--mode", choices=("EditMode", "PlayMode"))
    parser.add_argument("--test-name", metavar="FULLNAME", help="Exact NUnit FullName.")
    parser.add_argument("--test-class", metavar="CLASS", help="Exact namespace-qualified test class.")
    parser.add_argument("--assembly", metavar="ASSEMBLY", help="Exact test assembly; can combine with a name or class.")
    parser.add_argument("--discovery", metavar="PLAN-JSON", help="Bound test-plan report required for offline suite selection.")
    parser.add_argument("--output", metavar="FILE", help="Initially absent persistent report path outside project-owned data directories.")
    parser.add_argument("--editor-version", metavar="VERSION", help="Offline Editor version override; defaults to ProjectVersion.txt.")
    parser.add_argument("--include-explicit", action="store_true", help="Deliberately include an explicit connected test.")
    parser.add_argument("--level", choices=("log", "warn", "error"), help="Minimum native console severity.")
    parser.add_argument("--tail", type=int, default=100, metavar="1..1000", help="Maximum number of most recent entries returned to stdout (default 100).")
    parser.add_argument("--since", type=int, help="Native console cursor from a prior response; supply with --since-session.")
    parser.add_argument("--since-session", metavar="SESSION", help="Native source session belonging to --since.")
    parser.add_argument("--asset", metavar="EXACT-ASSET", help="Exact importer asset path, GUID, or global ID.")
    parser.add_argument("--kind", choices=("audio", "texture", "model"))
    parser.add_argument("--settings", metavar="JSON", help="Importer settings object for the guarded importer action.")
    parser.add_argument("--capture", metavar="JSON", help="Complete capture JSON returned by an earlier importer action, used only for explicit restoration.")
    parser.add_argument("--items", metavar="JSON", help="Ordered array of independent importer requests for importer-batch.")
    parser.add_argument("--platform", default="Default", metavar="PLATFORM")
    parser.add_argument("--dry-run", action="store_true", help="Run host preflight and native dry run without writing importer settings.")
    parser.add_argument("--quality-unit", choices=("normalized", "percent"), default="normalized", help="Audio quality input unit; percent is accepted only from 0 through 100.")
    parser.add_argument("--profile", choices=("minimal", "rich"), default="rich", help="Accepted field profile for the importer request.")
    parser.add_argument("--camera", metavar="ENTITY-ID")
    parser.add_argument("--panel", metavar="NAME")
    parser.add_argument("--canvas", default="Canvas")
    parser.add_argument("--root", default="UIRoot")
    parser.add_argument("--canvas-id", metavar="ENTITY-ID")
    parser.add_argument("--root-id", metavar="ENTITY-ID")
    parser.add_argument("--panel-id", metavar="ENTITY-ID")
    parser.add_argument("--width", type=int)
    parser.add_argument("--height", type=int)
    parser.add_argument("--filename", metavar="PNG-BASENAME")
    parser.add_argument("--save-path", metavar="ASSETS-PATH")
    parser.add_argument("--replace-sha256", metavar="AUTHORIZED-SHA256")
    parser.add_argument("--cli", metavar="PATH")
    parser.add_argument("--timeout", type=float, default=120.0, metavar="SECONDS")
    return parser


def _validate(args: argparse.Namespace) -> pathlib.Path:
    project = pathlib.Path(args.project).expanduser()
    if not project.is_dir():
        raise InputRefusal("PROJECT_NOT_FOUND", "The project directory does not exist", {"project": str(project)})
    if not math.isfinite(args.timeout) or args.timeout <= 0:
        raise InputRefusal("INVALID_TIMEOUT", "--timeout must be finite and greater than zero")
    if args.action == "compile" and not args.source:
        raise InputRefusal("INVALID_ARGUMENT", "Compile requires at least one explicit --source FILE")
    if args.action not in {"test", "test-plan"} and (args.test_class or args.assembly or args.discovery):
        raise InputRefusal("INVALID_ARGUMENT", "Test selection options apply only to test and test-plan")
    if args.test_class and args.test_name:
        raise InputRefusal("INVALID_ARGUMENT", "Choose --test-name or --test-class")
    if args.action == "test-plan" and (args.route or args.discovery):
        raise InputRefusal("INVALID_ARGUMENT", "test-plan uses connected discovery and accepts no --route or --discovery")
    if args.action in {"test", "test-plan"}:
        required = [("--output", args.output)]
        if args.action == "test":
            required.append(("--route", args.route))
        missing = [name for name, value in required if not value]
        if missing:
            raise InputRefusal("INVALID_ARGUMENT", "Test action is missing required arguments", {"missing": missing})
        if not args.source:
            raise InputRefusal("SOURCE_INPUT_REQUIRED", "Test requires at least one explicit --source FILE")
        if args.require_compiled:
            raise InputRefusal("INVALID_ARGUMENT", "--require-compiled applies only to compile")
    if args.action == "console":
        if not args.output:
            raise InputRefusal("INVALID_ARGUMENT", "Console requires an explicit --output FILE")
        if args.source or args.require_compiled or args.route or args.mode or args.test_name or args.include_explicit or args.editor_version:
            raise InputRefusal("INVALID_ARGUMENT", "Compile and test arguments cannot be combined with console")
    if args.action == "importer":
        missing = [name for name, value in (("--asset", args.asset), ("--kind", args.kind), ("--settings", args.settings)) if value is None]
        if missing:
            raise InputRefusal("INVALID_ARGUMENT", "Importer action is missing required arguments", {"missing": missing})
        if args.capture or args.items or args.source or args.require_compiled or args.route or args.mode or args.test_name or args.output or args.level or args.since is not None or args.since_session:
            raise InputRefusal("INVALID_ARGUMENT", "Arguments from another workflow cannot be combined with importer")
        try:
            args.settings = json.loads(args.settings)
        except (TypeError, json.JSONDecodeError) as exc:
            raise InputRefusal("INVALID_ARGUMENT", "--settings must contain a JSON object") from exc
        if not isinstance(args.settings, dict):
            raise InputRefusal("INVALID_ARGUMENT", "--settings must contain a JSON object")
    if args.action == "importer-restore":
        if args.asset or args.kind or args.settings or args.items or args.source or args.require_compiled or args.route or args.mode or args.test_name or args.output or args.level or args.since is not None or args.since_session or args.platform != "Default" or args.quality_unit != "normalized" or args.profile != "rich":
            raise InputRefusal("INVALID_ARGUMENT", "Arguments from another workflow cannot be combined with importer-restore")
        if not args.capture:
            raise InputRefusal("INVALID_ARGUMENT", "Importer restore requires --capture JSON")
        try:
            args.capture = json.loads(args.capture)
        except (TypeError, json.JSONDecodeError) as exc:
            raise InputRefusal("INVALID_ARGUMENT", "--capture must contain a JSON object") from exc
        if not isinstance(args.capture, dict):
            raise InputRefusal("INVALID_ARGUMENT", "--capture must contain a JSON object")
    if args.action == "importer-batch":
        if args.asset or args.kind or args.settings or args.capture or args.source or args.require_compiled or args.route or args.mode or args.test_name or args.output or args.level or args.since is not None or args.since_session or args.dry_run:
            raise InputRefusal("INVALID_ARGUMENT", "Arguments from another workflow cannot be combined with importer-batch")
        if not args.items:
            raise InputRefusal("INVALID_ARGUMENT", "Importer batch requires --items JSON")
        try:
            args.items = json.loads(args.items)
        except (TypeError, json.JSONDecodeError) as exc:
            raise InputRefusal("INVALID_ARGUMENT", "--items must contain a JSON array") from exc
        if not isinstance(args.items, list):
            raise InputRefusal("INVALID_ARGUMENT", "--items must contain a JSON array")
    if args.action not in {"camera-capture", "ui-capture", "ui-capture-batch"}:
        if any(value is not None for value in (args.camera,args.panel,args.canvas_id,args.root_id,args.panel_id,args.width,args.height,args.filename,args.save_path,args.replace_sha256)) or args.canvas != "Canvas" or args.root != "UIRoot":
            raise InputRefusal("INVALID_ARGUMENT", "Capture arguments require a capture action")
    if args.action in {"camera-capture", "ui-capture", "ui-capture-batch"}:
        if args.source or args.require_compiled or args.route or args.mode or args.test_name or args.output or args.asset or args.kind or args.settings or args.capture or args.dry_run:
            raise InputRefusal("INVALID_ARGUMENT", "Arguments from another workflow cannot be combined with capture")
        if args.action == "camera-capture" and (not args.camera or args.panel or args.items or args.filename):
            raise InputRefusal("INVALID_ARGUMENT", "Camera capture requires --camera and an optional --save-path")
        if args.action == "ui-capture" and (not args.panel or args.camera or args.items):
            raise InputRefusal("INVALID_ARGUMENT", "UI capture requires --panel")
        if args.action == "ui-capture-batch":
            if not args.items or args.camera or args.panel or args.filename or args.save_path or args.replace_sha256 or args.panel_id:
                raise InputRefusal("INVALID_ARGUMENT", "UI capture batch requires --items JSON")
            try: args.items = json.loads(args.items)
            except (TypeError, json.JSONDecodeError) as exc:
                raise InputRefusal("INVALID_ARGUMENT", "--items must contain a JSON panel array") from exc
    return project.resolve()


def _exit_code(code: str) -> int:
    if code in {"PROJECT_NOT_FOUND", "INVALID_TIMEOUT", "INVALID_ARGUMENT", "SOURCE_INPUT_REQUIRED", "SOURCE_INPUT_OUTSIDE_PROJECT", "SOURCE_INPUT_MISSING"}:
        return 2
    if code.endswith("TIMEOUT"):
        return 5
    if code in {"CLI_NOT_FOUND", "SESSION_NOT_RUNNING", "PROCESS_INVENTORY_UNKNOWN"}:
        return 3
    return 6


def main(argv: Sequence[str] | None = None, *, stdout: TextIO = sys.stdout, platform: PlatformAdapter | None = None) -> int:
    try:
        args = _parser().parse_args(argv)
        project = _validate(args)
        adapter = platform or current_platform()
        if type(adapter) is PlatformAdapter:
            raise InputRefusal("PLATFORM_UNSUPPORTED", "This operating system has no session adapter")
        transport = CliTransport(adapter, cli_override=args.cli, command_timeout=args.timeout)
        session = SessionController(str(project), transport, adapter, timeout=args.timeout)
        if args.action == "compile":
            workflow = CompileWorkflow(project, session)
            result = workflow.run(args.source, require_compiled=args.require_compiled)
        else:
            if args.action in {"test", "test-plan"}:
                workflow = TestWorkflow(project, session)
                selection = {"mode": args.mode or "EditMode", "test_name": args.test_name,
                             "source_paths": args.source, "output": args.output, "include_explicit": args.include_explicit}
                if args.test_class is not None:
                    selection["test_class"] = args.test_class
                if args.assembly is not None:
                    selection["assembly"] = args.assembly
                if args.action == "test-plan":
                    result = workflow.plan(**selection)
                else:
                    if args.discovery is not None:
                        selection["discovery"] = args.discovery
                    result = workflow.run(route=args.route, editor_version=args.editor_version, **selection)
            elif args.action == "console":
                workflow = ConsoleWorkflow(project, session)
                result = workflow.run(output=args.output, level=args.level, tail=args.tail, since=args.since, since_session=args.since_session)
            elif args.action in {"camera-capture", "ui-capture", "ui-capture-batch"}:
                workflow = CaptureWorkflow(project, session)
                if args.action == "camera-capture":
                    result = workflow.camera(camera=args.camera,width=args.width if args.width is not None else 1920,
                                             height=args.height if args.height is not None else 1080,
                                             path=args.save_path or "Assets/screenshot.png",replace_sha256=args.replace_sha256)
                elif args.action == "ui-capture":
                    result = workflow.panel(panel=args.panel,width=args.width if args.width is not None else 390,
                                            height=args.height if args.height is not None else 844,filename=args.filename,
                                            path=args.save_path,canvas=args.canvas,root=args.root,
                                            canvas_id=args.canvas_id,root_id=args.root_id,panelId=args.panel_id,
                                            replace_sha256=args.replace_sha256)
                else:
                    result = workflow.panels(items=args.items,canvas=args.canvas,root=args.root,
                                             canvas_id=args.canvas_id,root_id=args.root_id)
            else:
                workflow = ImportWorkflow(project, session)
                if args.action == "importer":
                    result = workflow.run(asset=args.asset, kind=args.kind, settings=args.settings, platform=args.platform,
                                         dry_run=args.dry_run, quality_unit=args.quality_unit, profile=args.profile)
                elif args.action == "importer-restore":
                    result = workflow.restore(capture=args.capture, dry_run=args.dry_run)
                else:
                    result = workflow.run_batch(items=args.items, platform=args.platform, quality_unit=args.quality_unit, profile=args.profile)
        print(json.dumps(_redact_value(result), sort_keys=True), file=stdout)
        if result.get("ok") is False:
            return _exit_code("IMPORT_BATCH_FAILED")
        return 0
    except (InputRefusal, SessionRefusal, WorkflowRefusal, WorkflowTestRefusal, WorkflowConsoleRefusal, ImportWorkflowRefusal, CaptureRefusal) as exc:
        print(json.dumps(_redact_value(exc.as_dict()), sort_keys=True), file=stdout)
        return _exit_code(exc.code)


if __name__ == "__main__":
    raise SystemExit(main())
