#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import math
import sys
from typing import Sequence, TextIO

from unity_cli.lifecycle import SessionController, SessionRefusal
from unity_cli.platforms import current_platform
from unity_cli.probing import ProbeWorkflow, ProbeRefusal
from unity_cli.transport import CliTransport, _redact_value


class ProbeArgumentParser(argparse.ArgumentParser):
    def error(self, message):
        raise ProbeRefusal('INVALID_ARGUMENT', message)


def main(argv: Sequence[str] | None = None, *, stdout: TextIO = sys.stdout, platform=None) -> int:
    try:
        parser = ProbeArgumentParser(description='Run an exact-session C# probe with durable completion evidence.')
        parser.add_argument('--project', required=True)
        parser.add_argument('--source', required=True)
        parser.add_argument('--entry')
        parser.add_argument('--completion-text')
        parser.add_argument('--output', required=True)
        parser.add_argument('--timeout', type=float, default=120.0)
        parser.add_argument('--enter-play', action='store_true')
        parser.add_argument('--dry-run', action='store_true', help='Compile the source without loading an assembly or executing an entry point.')
        parser.add_argument('--cli')
        args = parser.parse_args(argv)
        if not math.isfinite(args.timeout) or args.timeout <= 0:
            raise ProbeRefusal('INVALID_TIMEOUT', '--timeout must be finite and positive')
        if args.dry_run and (args.entry is not None or args.completion_text is not None or args.enter_play):
            raise ProbeRefusal('INVALID_ARGUMENT', '--dry-run accepts no entry, completion text, or play transition')
        if not args.dry_run and (not args.entry or not args.completion_text):
            raise ProbeRefusal('INVALID_ARGUMENT', 'Execution requires --entry and --completion-text')
        adapter = platform or current_platform()
        transport = CliTransport(adapter, cli_override=args.cli, command_timeout=args.timeout)
        session = SessionController(args.project, transport, adapter, timeout=args.timeout)
        workflow = ProbeWorkflow(args.project, session)
        result = workflow.check(args.source, args.output) if args.dry_run else workflow.run(
            args.source, args.entry, args.completion_text, args.output, enter_play=args.enter_play)
        print(json.dumps(_redact_value(result), sort_keys=True), file=stdout)
        return 0
    except (ProbeRefusal, SessionRefusal) as exc:
        print(json.dumps(_redact_value(exc.as_dict()), sort_keys=True), file=stdout)
        return 5 if exc.code.endswith('TIMEOUT') else 2 if exc.code in {'INVALID_ARGUMENT', 'INVALID_TIMEOUT'} else 6


if __name__ == '__main__':
    raise SystemExit(main())
