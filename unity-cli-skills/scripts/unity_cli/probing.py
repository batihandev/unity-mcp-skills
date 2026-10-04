from __future__ import annotations

import hashlib
from dataclasses import replace
import json
import pathlib
import re
import tempfile
import uuid
from typing import Any

from .compile import CompileWorkflow, WorkflowRefusal
from .console import ConsoleWorkflow, WorkflowConsoleRefusal
from .lifecycle import SessionRefusal, WorkflowSessionLease
from .reports import ReportPathError, ReportTarget
from .transport import _redact_value


class ProbeRefusal(WorkflowRefusal):
    pass


class ProbeWorkflow:
    def __init__(self, project: str | pathlib.Path, session):
        self.project = pathlib.Path(project).resolve()
        self.session = session

    def run(self, source, entry, completion_text, output, enter_play=False):
        return self._execute(source, output, entry=entry, completion_text=completion_text, enter_play=enter_play)

    def check(self, source, output):
        return self._execute(source, output, dry_run=True)

    def check_many(self, source_paths, output_dir):
        """Check independent sources in one exact-session native catalogue request."""
        import time
        started = time.monotonic()
        deadline = self.session._deadline()
        remaining = lambda: self.session._remaining(deadline)
        owner = CompileWorkflow(self.project, self.session)
        try:
            remaining()
            sources = owner._sources(source_paths)
            remaining()
            if not sources or any(p.suffix.lower() != '.cs' for p in sources):
                raise ProbeRefusal('INVALID_ARGUMENT', 'Select a nonempty set of C# sources')
            raw_dir = pathlib.Path(output_dir).expanduser().absolute()
            if raw_dir.is_symlink() or not raw_dir.is_dir() or any(raw_dir.iterdir()):
                raise ProbeRefusal('INVALID_ARGUMENT', 'Use an existing plain empty report directory')
            directory = raw_dir.resolve()
            outputs = [directory / f'{i+1:04d}-{p.stem}.json' for i,p in enumerate(sources)]
            for output in outputs:
                remaining()
                with ReportTarget(self.project, output):
                    pass
            target = ReportTarget(self.project, directory / 'summary.json')
        except (ReportPathError, WorkflowRefusal, SessionRefusal, ValueError) as error:
            if isinstance(error, ProbeRefusal): raise
            code = 'PROBE_TIMEOUT' if getattr(error, 'code', None) == 'ACTION_TIMEOUT' else getattr(error, 'code', 'INVALID_ARGUMENT')
            raise ProbeRefusal(code, str(error)) from error
        report = {'schemaVersion':1, 'action':'check-probes', 'ok':False, 'project':str(self.project),
                  'commands':[], 'checks':[], 'restoration':{'ownedPlayTransition':False}}
        error = None
        try:
            report['sources'] = owner._hash_sources(sources, check_budget=remaining)
            with tempfile.TemporaryDirectory(prefix='unity-probe-catalogue-') as temporary:
                stage = pathlib.Path(temporary)
                staged = []
                for index,(source,snapshot) in enumerate(zip(sources,report['sources'])):
                    remaining()
                    path = stage / f'{index:04d}' / source.name
                    path.parent.mkdir()
                    digest = hashlib.sha256()
                    with source.open('rb') as original, path.open('wb') as copy:
                        while True:
                            remaining()
                            block = original.read(1024 * 1024)
                            remaining()
                            if not block: break
                            copy.write(block)
                            digest.update(block)
                    remaining()
                    if digest.hexdigest() != snapshot['sha256']:
                        raise ProbeRefusal('PROBE_SOURCE_CHANGED', 'Source changed while staging the catalogue')
                    staged.append(path)
                with self.session.workflow_session(deadline=deadline) as lease:
                    report['editorIdentity'] = lease.identity.as_dict()
                    state = self._state(lease,report)
                    baseline = self._console(lease,report)
                    report['baseline'] = {'cursor':baseline['cursor'],'session':baseline['session']}
                    cli = self.session.transport.ensure_cli(timeout=lease.remaining(),deadline=lease.transport_deadline)
                    convert = self.session.platform.cli_project_path
                    manifest = {'schemaVersion':1,'sources':[{'id':str(i),'path':convert(str(path),cli,timeout=lease.remaining(),deadline=lease.transport_deadline),'sha256':snapshot['sha256']}
                        for i,(path,snapshot) in enumerate(zip(staged,report['sources']))]}
                    manifest_path = stage / 'manifest.json'
                    manifest_path.write_bytes(json.dumps(manifest,sort_keys=True).encode())
                    self._verify_sources(owner,sources,report,lease)
                    result = self._invoke(lease,report,'cli_compile_probes',{
                        'manifest':convert(str(manifest_path),cli,timeout=lease.remaining(),deadline=lease.transport_deadline),
                        'manifest_sha256':hashlib.sha256(manifest_path.read_bytes()).hexdigest(),
                        'time_budget_ms':max(1,int(lease.remaining()*1000))})
                    if not isinstance(result,dict) or result.get('schemaVersion') != 1:
                        raise ProbeRefusal('PROBE_BATCH_INVALID', 'Catalogue result schema is invalid')
                    backend = result.get('editorIdentity')
                    if not isinstance(backend,dict) or backend.get('pid') != lease.identity.pid or backend.get('startedAt') != lease.identity.started_at or self.session.platform.canonicalize_external_project(backend.get('project',''),deadline=lease.transport_deadline) != lease.identity.project:
                        raise ProbeRefusal('PROCESS_IDENTITY_CHANGED', 'Catalogue result belongs to another Editor')
                    items = result.get('items')
                    if not isinstance(items,list) or len(items) != len(sources):
                        raise ProbeRefusal('PROBE_BATCH_INVALID', 'Catalogue omitted source results')
                    outcomes = []
                    for index,(item,snapshot) in enumerate(zip(items,report['sources'])):
                        if not isinstance(item,dict) or item.get('id') != str(index) or item.get('sha256') != snapshot['sha256']:
                            raise ProbeRefusal('PROBE_BATCH_INVALID', 'Catalogue source identity is duplicated or mismatched')
                        outcome = {'ok':True,'nativeResult':item.get('result')}
                        try: self._script(item.get('result'),True)
                        except ProbeRefusal as refusal:
                            if refusal.code != 'PROBE_COMPILE_FAILED': raise
                            outcome.update(ok=False,error=refusal.as_dict()['error'])
                        outcomes.append(outcome)
                    after = self._console(lease,report,baseline['cursor'],baseline['session'])
                    if any(row['logType'] in {'Error','Assert','Exception'} for row in after['entries']):
                        raise ProbeRefusal('PROBE_RUNTIME_FAILED','Catalogue observation contains a current console error')
                    if after['groundTruth'] is not None and after['groundTruth']['compilationFailed']:
                        raise ProbeRefusal('PROBE_SCRIPT_FAILED','Editor reports compilation failure')
                    self._state(lease,report,expected=state['playMode'])
                    self._verify_sources(owner,sources,report,lease)
                    for source,snapshot,output,outcome in zip(sources,report['sources'],outputs,outcomes):
                        remaining()
                        per_file = dict(schemaVersion=1,action='probe-check',project=str(self.project),
                            editorIdentity=report['editorIdentity'],sources=[snapshot],baseline=report['baseline'],
                            restoration=report['restoration'],**outcome)
                        payload = json.dumps(_redact_value(per_file),sort_keys=True).encode()
                        with ReportTarget(self.project,output) as per_target:per_target.publish(payload)
                        report['checks'].append({'source':str(source.relative_to(self.project)),'ok':outcome['ok'],
                            'artifactPath':str(output),'artifactSha256':hashlib.sha256(payload).hexdigest()})
                    remaining()
                    report['ok'] = all(row['ok'] for row in report['checks'])
        except (WorkflowRefusal,WorkflowConsoleRefusal,SessionRefusal,ReportPathError,OSError,ValueError) as failure:
            code = 'PROBE_TIMEOUT' if getattr(failure,'code',None) in {'ACTION_TIMEOUT','READY_TIMEOUT'} else getattr(failure,'code','PROBE_BATCH_FAILED')
            error = ProbeRefusal(code,str(failure),getattr(failure,'details',{}))
            report['error'] = error.as_dict()['error']
        finally:
            report['wallSeconds'] = time.monotonic()-started
            payload = json.dumps(_redact_value(report),sort_keys=True).encode()
            try: target.publish(payload)
            finally: target.close()
        artifact = {'artifactPath':str(directory/'summary.json'),'artifactSha256':hashlib.sha256(payload).hexdigest()}
        if error is not None:
            error.details.update(artifact,artifactPreserved=True)
            raise error
        return {**{key:value for key,value in report.items() if key != 'commands'},**artifact}

    def _execute(self, source, output, *, entry=None, completion_text=None, enter_play=False, dry_run=False):
        try:
            target = ReportTarget(self.project, output)
        except ReportPathError as exc:
            raise ProbeRefusal(exc.code, str(exc), exc.details) from exc
        report: dict[str, Any] = {'schemaVersion': 1, 'ok': False, 'action': 'probe-check' if dry_run else 'probe',
                                  'project': str(self.project), 'commands': [], 'restoration': {'ownedPlayTransition': False}}
        deadline = self.session._deadline()
        error = None
        try:
            if not isinstance(enter_play, bool):
                raise ProbeRefusal('INVALID_ARGUMENT', 'enter_play must be Boolean')
            if not dry_run and (not isinstance(entry, str) or not entry.strip() or not isinstance(completion_text, str) or not completion_text.strip()):
                raise ProbeRefusal('INVALID_ARGUMENT', 'Supply a nonempty entry and exact completion message')
            compile_owner = CompileWorkflow(self.project, self.session)
            sources = compile_owner._sources(source)
            if len(sources) != 1 or sources[0].suffix.lower() != '.cs':
                raise ProbeRefusal('INVALID_ARGUMENT', 'Select exactly one C# source file')
            report['sources'] = compile_owner._hash_sources(sources)
            report['entry'] = entry
            run_id = uuid.uuid4().hex
            report['runId'] = run_id
            with tempfile.TemporaryDirectory(prefix='unity-probe-') as temporary:
                staged = pathlib.Path(temporary) / sources[0].name
                raw_source = sources[0].read_bytes()
                if hashlib.sha256(raw_source).hexdigest() != report['sources'][0]['sha256']:
                    raise ProbeRefusal('PROBE_SOURCE_CHANGED', 'Source changed while preparing the probe')
                if not dry_run:
                    marker = completion_text.replace('{run_id}', run_id)
                    report.update(completionText=marker, correlation='unique-run-id' if '{run_id}' in completion_text else 'bounded-current-sequence')
                    if '{run_id}' in completion_text:
                        encoded = completion_text.encode('utf-8')
                        if encoded not in raw_source:
                            raise ProbeRefusal('PROBE_TEMPLATE_MISSING', 'The exact completion template must occur literally in the source')
                        raw_source = raw_source.replace(encoded, marker.encode('utf-8'))
                staged.write_bytes(raw_source)
                report['stagedSha256'] = hashlib.sha256(raw_source).hexdigest()
                with self.session.workflow_session(deadline=deadline) as lease:
                    report['editorIdentity'] = lease.identity.as_dict()
                    # An owned transition needs time to restore after the probe's deadline.
                    reserve = min(5.0, lease.remaining() / 4) if enter_play else 0.0
                    action_lease = replace(lease, deadline=deadline - reserve) if isinstance(lease, WorkflowSessionLease) else lease
                    owned = False
                    try:
                        state = self._state(lease, report)
                        if not dry_run and state['playMode'] != 'playing':
                            if not enter_play or state['playMode'] != 'stopped':
                                raise ProbeRefusal('PROBE_PLAY_REQUIRED', 'Probe execution requires playing state; use enter_play to own a stopped-to-playing transition')
                            self._invoke(action_lease, report, 'editor_play', {})
                            owned = True
                            report['restoration']['ownedPlayTransition'] = True
                            self._wait_play(action_lease, report, 'playing')
                        self._verify_sources(compile_owner, sources, report, action_lease)
                        baseline = self._console(action_lease, report)
                        report['baseline'] = {'cursor': baseline['cursor'], 'session': baseline['session']}
                        cli = self.session.transport.ensure_cli(timeout=action_lease.remaining(), deadline=action_lease.transport_deadline)
                        native_file = self.session.platform.cli_project_path(str(staged), cli, timeout=action_lease.remaining(), deadline=action_lease.transport_deadline)
                        arguments = {'file': native_file, 'mode': 'ephemeral', 'dry_run': dry_run,
                                     'timeout_ms': max(1, int(action_lease.remaining() * 1000))}
                        if not dry_run: arguments['entry'] = entry
                        script = self._invoke(action_lease, report, 'run_script', arguments)
                        self._script(script, dry_run)
                        cursor = baseline['cursor']
                        while True:
                            self._verify_sources(compile_owner, sources, report, action_lease)
                            parsed = self._console(action_lease, report, cursor, baseline['session'])
                            if any(row['logType'] in {'Error', 'Assert', 'Exception'} for row in parsed['entries']):
                                raise ProbeRefusal('PROBE_RUNTIME_FAILED', 'Current probe console contains Error, Assert, or Exception')
                            ground = parsed['groundTruth']
                            if ground is not None and ground['compilationFailed']:
                                raise ProbeRefusal('PROBE_SCRIPT_FAILED', 'Editor reports compilation failure')
                            if dry_run:
                                report['ok'] = True
                                break
                            matches = [row for row in parsed['entries'] if row['message'] == marker and row['seeded'] is False]
                            if matches:
                                report['completion'] = matches[0]
                                report['ok'] = True
                                break
                            cursor = parsed['cursor']
                            self.session.clock.sleep(min(0.1, action_lease.remaining()))
                        self._state(action_lease, report, expected=state['playMode'] if dry_run else 'playing')
                        action_lease.verify_current()
                        self._verify_sources(compile_owner, sources, report, action_lease)
                    finally:
                        if owned:
                            try:
                                lease.verify_current()
                                self._invoke(lease, report, 'editor_stop', {})
                                self._wait_play(lease, report, 'stopped')
                                report['restoration']['restored'] = True
                            except (WorkflowRefusal, WorkflowConsoleRefusal, SessionRefusal) as exc:
                                report['restoration'].update(restored=False, error=exc.as_dict()['error'])
                                raise
            report['ok'] = True
        except (WorkflowRefusal, WorkflowConsoleRefusal, SessionRefusal) as exc:
            code = 'PROBE_TIMEOUT' if exc.code in {'ACTION_TIMEOUT', 'READY_TIMEOUT'} else exc.code
            error = ProbeRefusal(code, str(exc), exc.details)
        except (OSError, UnicodeError, ValueError) as exc:
            error = ProbeRefusal('PROBE_INPUT_FAILED', 'Probe source or staging could not be read or written', {'reason': str(exc)})
        except Exception as exc:
            error = ProbeRefusal('PROBE_INTERNAL_FAILED', 'Probe could not complete; inspect retained command evidence', {'reason': str(exc), 'type': type(exc).__name__})
        if error is not None:
            report.update(ok=False, error=error.as_dict()['error'])
        try:
            payload = json.dumps(_redact_value(report), sort_keys=True, separators=(',', ':')).encode('utf-8')
            target.publish(payload)
        except ReportPathError as exc:
            raise ProbeRefusal(exc.code, str(exc), exc.details) from exc
        finally:
            target.close()
        artifact = {'artifactPath': str(target.path), 'artifactSha256': hashlib.sha256(payload).hexdigest()}
        if error is not None:
            error.details.update(artifact, artifactPreserved=True)
            raise error
        return {**{key: value for key, value in report.items() if key != 'commands'}, **artifact}

    def _invoke(self, lease, report, command, arguments):
        lease.verify_current()
        result = self.session.transport.invoke_command(str(self.project), command, arguments, timeout=lease.remaining(), deadline=lease.transport_deadline)
        report['commands'].append({'command': command, 'arguments': arguments, 'nativeResponse': result.data,
                                   'transportSuccess': result.success, 'code': result.code, 'message': result.message, 'diagnostics': result.diagnostics})
        lease.verify_current()
        return CompileWorkflow._outer_result(result, command)['result']

    def _console(self, lease, report, since=None, source_session=None):
        arguments = {'tail': ConsoleWorkflow.CAPTURE_LIMIT, 'level': 'log'}
        if since is not None: arguments.update(since=since, since_session=source_session)
        payload = self._invoke(lease, report, 'console', arguments)
        parsed = ConsoleWorkflow._validate_payload(payload, None, since, source_session)
        if parsed['dropped'] or parsed['reset']:
            raise ProbeRefusal('PROBE_CONSOLE_GAP', 'Console history was reset or dropped; probe evidence is incomplete')
        return parsed

    def _state(self, lease, report, expected=None):
        state = self._invoke(lease, report, 'editor_status', {})
        if not isinstance(state, dict) or not isinstance(state.get('projectPath'), str):
            raise ProbeRefusal('EDITOR_STATUS_INVALID', 'Editor status must identify its project')
        project = self.session.platform.canonicalize_external_project(state['projectPath'], deadline=lease.transport_deadline)
        if project != lease.identity.project:
            raise ProbeRefusal('PROJECT_IDENTITY_MISMATCH', 'Editor status project differs from the leased project')
        if state.get('compiling') is not False or state.get('domainReloadInProgress') is not False or state.get('status') not in {'ready', 'playing'} or state.get('playMode') not in {'stopped', 'playing', 'paused'}:
            raise ProbeRefusal('EDITOR_NOT_READY', 'Editor must be idle and outside domain reload')
        if expected is not None and state['playMode'] != expected:
            raise ProbeRefusal('PROBE_PLAY_CHANGED', 'Editor play state changed during the probe')
        return state

    def _wait_play(self, lease, report, expected):
        while True:
            state = self._invoke(lease, report, 'editor_status', {})
            if isinstance(state, dict) and state.get('playMode') == expected:
                self._state(lease, report, expected=expected)
                return
            self.session.clock.sleep(min(0.1, lease.remaining()))

    @staticmethod
    def _script(value, dry_run):
        if not isinstance(value, dict) or type(value.get('success')) is not bool:
            raise ProbeRefusal('PROBE_SCRIPT_RESPONSE_INVALID', 'run_script omitted a Boolean success field')
        if not isinstance(value.get('diagnostics'), list) or any(not ConsoleWorkflow._nonnegative_integer(value.get(key)) for key in ('compileMs', 'executeMs')):
            raise ProbeRefusal('PROBE_SCRIPT_RESPONSE_INVALID', 'run_script omitted structured diagnostics or timing evidence')
        for diagnostic in value['diagnostics']:
            if not isinstance(diagnostic, dict) or diagnostic.get('severity') not in {'error', 'warning', 'info'}:
                raise ProbeRefusal('PROBE_SCRIPT_RESPONSE_INVALID', 'run_script returned an invalid compiler diagnostic')
        if dry_run:
            if 'assemblyName' not in value:
                raise ProbeRefusal('PROBE_SCRIPT_RESPONSE_INVALID', 'Compile-only response omitted assembly evidence')
            assembly = value['assemblyName']
            if value['executeMs'] != 0 or (value['success'] is True and assembly is not None):
                raise ProbeRefusal('PROBE_SCRIPT_RESPONSE_INVALID', 'Compile-only response reports loading or execution')
            if value['success'] is False and assembly is not None:
                # Native compile failures report the generated name before any assembly is loaded.
                generated = isinstance(assembly, str) and re.fullmatch(r'PipelineRunScript_.+_[0-9a-f]{32}', assembly)
                compiler_error = any(d['severity'] == 'error' for d in value['diagnostics'])
                if value.get('error') != 'Compilation Failed' or not generated or not compiler_error:
                    raise ProbeRefusal('PROBE_SCRIPT_RESPONSE_INVALID', 'Compile-only failure has invalid assembly evidence')
        if value['success'] is not True:
            categories = {'Compilation Failed': 'PROBE_COMPILE_FAILED',
                          'Entry Point Not Found': 'PROBE_ENTRY_FAILED',
                          'Runtime Error': 'PROBE_RUNTIME_FAILED'}
            code = categories.get(value.get('error'), 'PROBE_SCRIPT_FAILED')
            raise ProbeRefusal(code, 'run_script did not report successful compilation and execution', {'result': value})
        for diagnostic in value['diagnostics']:
            if diagnostic['severity'] == 'error':
                raise ProbeRefusal('PROBE_COMPILE_FAILED', 'run_script returned a compiler error diagnostic', {'diagnostic': diagnostic})
        if dry_run:
            if value.get('assemblyName') is not None or value['executeMs'] != 0:
                raise ProbeRefusal('PROBE_SCRIPT_RESPONSE_INVALID', 'Compile-only response must show no loaded assembly or execution')
        elif not isinstance(value.get('assemblyName'), str) or not value['assemblyName'].strip():
            raise ProbeRefusal('PROBE_SCRIPT_RESPONSE_INVALID', 'run_script must identify its compiled assembly')

    @staticmethod
    def _verify_sources(owner, sources, report, lease):
        if owner._hash_sources(sources, check_budget=lease.remaining) != report['sources']:
            raise ProbeRefusal('PROBE_SOURCE_CHANGED', 'Source hash changed during the probe')
