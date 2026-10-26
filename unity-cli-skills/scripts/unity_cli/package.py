"""Exact UPM Embed and sample workflows under the shared project session lease."""
from __future__ import annotations

import hashlib
import json
import pathlib
import re
import time
import subprocess
import uuid

from . import authoring
from .compile import CompileWorkflow, WorkflowRefusal
from .lifecycle import SessionRefusal


class PackageRefusal(WorkflowRefusal):
    pass


def journal_path(project, operation_id):
    if not isinstance(operation_id, str) or not re.fullmatch(r'[a-f0-9]{32}', operation_id):
        raise PackageRefusal('OPERATION_ID_INVALID', 'Select a fresh 32-character lowercase hexadecimal operation ID')
    project = pathlib.Path(project).absolute()
    path = project / 'Library/unity-cli-package-operations' / (operation_id + '.json')
    try:
        authoring.inspect_no_follow(project, project / 'Library', path)
    except authoring.Refusal as exc:
        raise PackageRefusal(exc.code, str(exc), exc.details) from exc
    return path


def read_journal(project, operation_id, identity, command_hash, *, canonicalize=None, native_start_ticks=None):
    return read_journal_receipt(project, operation_id, identity, command_hash, canonicalize=canonicalize, native_start_ticks=native_start_ticks)[0]


def read_journal_receipt(project, operation_id, identity, command_hash, *, canonicalize=None, native_start_ticks=None):
    path = journal_path(project, operation_id)
    try:
        raw = path.read_bytes()
        value = json.loads(raw)
    except (OSError, ValueError, UnicodeError) as exc:
        raise PackageRefusal('JOURNAL_UNAVAILABLE', 'The exact package operation journal cannot be read', {'reason':str(exc)}) from exc
    if not isinstance(value, dict):
        raise PackageRefusal('JOURNAL_INVALID', 'The package operation journal must be an object')
    canonicalize = canonicalize or (lambda path: str(pathlib.Path(path).absolute()))
    try:
        exact_project = canonicalize(value.get('project', '')) == canonicalize(identity.project)
    except SessionRefusal:
        raise
    except (OSError, ValueError, RuntimeError, TypeError):
        exact_project = False
    valid = (value.get('schema') == 'unity.package.operation@1' and value.get('operationId') == operation_id
             and exact_project and value.get('editorPid') == identity.pid and not isinstance(value.get('editorPid'), bool)
             and value.get('editorStartedAt') == identity.started_at and value.get('commandSha256') == command_hash)
    if native_start_ticks is not None:
        valid = valid and value.get('nativeProcessStartTicks') == native_start_ticks
    if not valid:
        raise PackageRefusal('JOURNAL_IDENTITY_MISMATCH', 'The journal is not bound to this exact project, operation, Editor session and command source')
    return value, hashlib.sha256(raw).hexdigest()


def validate_sample_completion(journal, acknowledgement, approved_plan=None):
    if (journal.get("kind") != "sample" or journal.get("state") not in {"awaiting-readback", "imported"}
            or journal.get("acknowledged") is not True or journal.get("importAcknowledged") is not True
            or any(journal.get(key) != acknowledgement.get(key) for key in ("operationId", "name", "version", "sampleName"))
            or not isinstance(journal.get("samplePlan"), dict)
            or journal["samplePlan"].get("planSha256") != acknowledgement.get("planSha256")
            or any(journal["samplePlan"].get(key) != acknowledgement[key] for key in journal["samplePlan"] if key in acknowledgement)
            or approved_plan is not None and journal["samplePlan"] != approved_plan):
        raise PackageRefusal("SAMPLE_NOT_VERIFIED", "The exact sample journal lacks a matching acknowledged native request")


def validate_embed_terminal(journal):
    if journal.get('state') == 'failed':
        raise PackageRefusal('EMBED_NATIVE_FAILED', 'The public UPM Embed request failed', {'nativeStatus':journal.get('nativeStatus'), 'nativeError':journal.get('nativeError')})
    if (journal.get('kind') != 'embed' or journal.get('state') not in {'awaiting-readback', 'completed'} or journal.get('nativeStatus') != 'Success'
            or journal.get('acknowledged') is not True or journal.get('observerDetached') is not True
            or journal.get('requestUnresolved') is not False):
        raise PackageRefusal('EMBED_NOT_VERIFIED', 'Embed success requires an exact terminal SDK outcome and a detached observer', {'state':journal.get('state'), 'nativeStatus':journal.get('nativeStatus'), 'requestUnresolved':journal.get('requestUnresolved')})


def validate_embed_completion(journal, installed):
    validate_embed_terminal(journal)
    native = journal.get('nativeResult')
    if not isinstance(native, dict) or any(native.get(key) != journal.get(key) for key in ('name','version')) or native.get('source') != 'Embedded':
        raise PackageRefusal('EMBED_RESULT_MISMATCH', 'The SDK result is not the exact selected Embedded package')
    if not isinstance(installed, list):
        raise PackageRefusal('PACKAGE_LIST_INVALID', 'Native installed packages must be an array')
    selected = [item for item in installed if isinstance(item, dict) and item.get('name') == journal.get('name')]
    if len(selected) != 1 or selected[0].get('version') != journal.get('version') or selected[0].get('source') != 'Embedded':
        raise PackageRefusal('EMBED_READBACK_MISMATCH', 'Fresh native package_list must contain one exact Embedded name and version')
    return selected[0]


class PackageWorkflow:
    def __init__(self, project, session, *, source_paths=None, clock=time, poll_interval=0.25):
        self.project = pathlib.Path(project).absolute()
        self.session = session
        self.clock = clock
        if not isinstance(poll_interval, (int,float)) or isinstance(poll_interval,bool) or poll_interval <= 0:
            raise PackageRefusal('INVALID_POLL_INTERVAL','Select a positive package polling interval')
        self.poll_interval = poll_interval
        self.source_paths = source_paths or sorted((self.project / 'Packages/com.batihandev.unity-cli-commands/Editor/Packages').glob('*.cs'))
        self.compile = CompileWorkflow(self.project, session, clock=clock)

    def _invoke(self, lease, command, arguments):
        lease.verify_current()
        result = self.session.transport.invoke_command(str(self.project), command, arguments, timeout=lease.remaining(), deadline=lease.transport_deadline)
        lease.verify_current()
        data = CompileWorkflow._outer_result(result, command)['result']
        if isinstance(data, str):
            try: data = json.loads(data)
            except ValueError as exc: raise PackageRefusal('NATIVE_RESULT_INVALID','The native command result is malformed JSON') from exc
        if not isinstance(data, dict): raise PackageRefusal('NATIVE_RESULT_INVALID','The native command result must be an object')
        if 'Ok' in data:
            if data.get('Ok') is not True:
                error=data.get('Error') or {}
                raise PackageRefusal(error.get('Code','PACKAGE_COMMAND_FAILED'),error.get('Message','Package command refused'),error.get('Details') or {})
            data=data.get('Result')
        if not isinstance(data, dict): raise PackageRefusal('NATIVE_RESULT_INVALID','The typed command omitted its result object')
        return data

    def _preflight(self, lease):
        state=self._invoke(lease,'package_status',{})
        if state.get('status') not in {'idle','completed','failed'}:
            raise PackageRefusal('PACKAGE_NATIVE_BUSY','Wait for the native add/remove/resolve operation to reach a terminal state',{'nativeStatus':state})
        return state

    def _installed(self, lease):
        result=self._invoke(lease,'package_list',{})
        packages=result.get('packages')
        if result.get('success') is not True or result.get('scope') != 'installed' or not isinstance(packages,list) or result.get('count') != len(packages):
            raise PackageRefusal('PACKAGE_LIST_INVALID','Native package_list did not return a complete installed inventory',{'nativeResult':result})
        return packages

    def _identity_arguments(self, lease, preview):
        if preview.get('nativePid') != lease.identity.pid or not isinstance(preview.get('nativeProcessStartTicks'), str) or not re.fullmatch(r'[0-9]+',preview['nativeProcessStartTicks']):
            raise PackageRefusal('EDITOR_IDENTITY_MISMATCH','The command preview does not identify the leased Editor process')
        lease.verify_current()
        return {'expectedNativePid':preview['nativePid'],'expectedNativeProcessStartTicks':preview['nativeProcessStartTicks'], 'editorStartedAt':lease.identity.started_at}

    @staticmethod
    def _review(preview, expected):
        if expected is None:
            raise PackageRefusal('PLAN_HASH_REQUIRED','Supply the exact reviewed dry-run plan hash',{'preview':preview})
        if expected != preview.get('planSha256'):
            raise PackageRefusal('PLAN_HASH_MISMATCH','The current plan differs from the reviewed preview; inspect a fresh preview before confirming',{'preview':preview})

    def _read(self, lease, operation, preview):
        return self._read_receipt(lease, operation, preview)[0]

    def _read_receipt(self, lease, operation, preview):
        lease.verify_current()
        def canonicalize(path):
            lease.remaining()
            return self.session.platform.canonicalize_external_project(path, deadline=lease.transport_deadline)
        try:
            value=read_journal_receipt(self.project,operation,lease.identity,preview['commandSha256'],
                               canonicalize=canonicalize,
                               native_start_ticks=preview['nativeProcessStartTicks'])
        except subprocess.TimeoutExpired as exc:
            lease.remaining()
            raise PackageRefusal('CLI_TIMEOUT', 'Package journal path conversion reached its local deadline') from exc
        lease.verify_current()
        return value

    def embed(self, *, name, version, operation_id=None, expected_plan_sha256=None, dry_run=False, confirm=False):
        operation=operation_id or uuid.uuid4().hex
        arguments={'name':name,'version':version,'operationId':operation}
        acknowledged=False
        initial_identity=None
        try:
            with self.session.workflow_session() as lease:
                initial_identity=lease.identity
                preflight=self._preflight(lease)
                before=self._installed(lease)
                selected=[row for row in before if row.get('name')==name and row.get('version')==version]
                if len(selected)!=1: raise PackageRefusal('PACKAGE_IDENTITY_MISMATCH','Select one exact installed package name and version')
                preview=self._invoke(lease,'package_embed',dict(arguments,dryRun=True,confirm=True))
                identity=self._identity_arguments(lease,preview)
                if dry_run: return {'ok':True,'action':'embed','dryRun':True,'preview':preview,'preflight':preflight}
                if confirm is not True: raise PackageRefusal('CONFIRMATION_REQUIRED','Confirm the reviewed Embed preview before mutation',{'preview':preview})
                self._review(preview,expected_plan_sha256)
                self.compile.run_with_lease(self.source_paths,lease)
                self._preflight(lease)
                ack=self._invoke(lease,'package_embed',dict(arguments,**identity,expectedPlanSha256=preview['planSha256'],confirm=True,timeoutSeconds=max(1,int(lease.remaining()-1))))
                acknowledged=ack.get('acknowledged') is True
                if not acknowledged: raise PackageRefusal('EMBED_ACKNOWLEDGEMENT_MISSING','UPM Embed did not acknowledge the exact request',{'result':ack})
                while True:
                    journal=self._read(lease,operation,preview)
                    if journal.get('name')!=name or journal.get('version')!=version: raise PackageRefusal('JOURNAL_IDENTITY_MISMATCH','The journal selects another package')
                    state=journal.get('state')
                    if state not in {'starting','pending'}: break
                    self.clock.sleep(min(self.poll_interval,lease.remaining()))
                validate_embed_terminal(journal)
                self.session.ready(expected_identity=lease.identity,deadline=lease.deadline)
                package=validate_embed_completion(journal,self._installed(lease))
                compiled=self.compile.run_with_lease(self.source_paths,lease)
                final, receipt_hash=self._read_receipt(lease,operation,preview)
                package=validate_embed_completion(final,self._installed(lease))
                self._preflight(lease)
                readback_arguments={'operationId':operation,'expectedJournalSha256':receipt_hash,'mode':'completed-readback',
                    'expectedNativePid':identity['expectedNativePid'],'expectedNativeProcessStartTicks':identity['expectedNativeProcessStartTicks']}
                readback_preview=self._invoke(lease,'package_embed_reconcile',dict(readback_arguments,dryRun=True))
                self._invoke(lease,'package_embed_reconcile',dict(readback_arguments,
                    expectedPlanSha256=readback_preview['planSha256'],confirm=True))
                final=self._read(lease,operation,preview)
                if final.get('state') != 'completed': raise PackageRefusal('EMBED_NOT_VERIFIED','Native readback did not publish the completed seal')
                package=validate_embed_completion(final,self._installed(lease))
                path=journal_path(self.project,operation)
                return {'ok':True,'action':'embed','operationId':operation,'editorIdentity':lease.identity.as_dict(), 'nativeProcessStartTicks':preview['nativeProcessStartTicks'],
                        'package':package,'originalPackage':selected[0],'journal':final,'journalSha256':hashlib.sha256(path.read_bytes()).hexdigest(),'compile':compiled}
        except (SessionRefusal, WorkflowRefusal, KeyboardInterrupt) as exc:
            if acknowledged:
                try:
                    with self.session.workflow_session(deadline=self.clock.monotonic()+5) as cleanup_lease:
                        if cleanup_lease.identity == initial_identity:
                            self._invoke(cleanup_lease,'package_embed_detach',{'operationId':operation,'confirm':True})
                except Exception: pass
            if isinstance(exc,KeyboardInterrupt): raise
            if isinstance(exc,PackageRefusal): raise
            raise PackageRefusal(exc.code,str(exc),dict(exc.details,operationId=operation,sdkRequestCancelled=False)) from exc

    def complete_sample_with_lease(self, lease, acknowledgement):
        operation=acknowledgement.get('operationId')
        name=acknowledgement.get('name'); version=acknowledgement.get('version'); sample_name=acknowledgement.get('sampleName')
        if acknowledgement.get('acknowledged') is not True:
            raise PackageRefusal('SAMPLE_ACKNOWLEDGEMENT_MISSING','Sample import did not acknowledge its exact request')
        approved_plan=None
        approved_hash=None
        try:
            while True:
                journal, observed_hash=self._read_receipt(lease,operation,acknowledgement)
                validate_sample_completion(journal,acknowledgement,approved_plan)
                if approved_hash is not None and observed_hash!=approved_hash:
                    raise PackageRefusal('JOURNAL_HASH_MISMATCH','The pending journal changed between readback attempts; preserve its current bytes')
                approved_hash=observed_hash
                approved_plan=journal['samplePlan']
                self.session.ready(expected_identity=lease.identity,deadline=lease.deadline)
                compiled=self.compile.run_with_lease(self.source_paths,lease)
                current,current_hash=self._read_receipt(lease,operation,acknowledgement)
                if current_hash!=approved_hash:
                    raise PackageRefusal('JOURNAL_HASH_MISMATCH','The validated sample journal changed during compilation; preserve its current bytes')
                validate_sample_completion(current,acknowledgement,approved_plan)
                try:
                    readback=self._invoke(lease,'package_sample_readback',{'operationId':operation,'expectedJournalSha256':approved_hash})
                except PackageRefusal as exc:
                    if exc.code!='SAMPLE_IMPORT_PENDING': raise
                    self.clock.sleep(min(self.poll_interval,lease.remaining()))
                    continue
                final,final_hash=self._read_receipt(lease,operation,acknowledgement)
                validate_sample_completion(final,acknowledgement,approved_plan)
                terminal_fields={'state','finishedUtc','readback','destinationAfter'}
                if ({key:value for key,value in final.items() if key not in terminal_fields} !=
                        {key:value for key,value in journal.items() if key not in terminal_fields}
                        or final.get('readback')!=readback or final.get('destinationAfter')!=readback.get('inventory')):
                    raise PackageRefusal('SAMPLE_NOT_VERIFIED','Terminal publication changed the approved request or its exact readback inventory')
                if final.get('state')!='imported':
                    raise PackageRefusal('SAMPLE_NOT_VERIFIED','Native sample readback did not publish its imported journal')
                return {'ok':True,'action':'sample','operationId':operation,'editorIdentity':lease.identity.as_dict(),
                        'journalSha256':final_hash,'readback':readback,'compile':compiled}
        except (SessionRefusal, WorkflowRefusal) as exc:
            code='SAMPLE_IMPORT_TIMEOUT' if exc.code in {'ACTION_TIMEOUT','READY_TIMEOUT','COMPILE_TIMEOUT','CLI_TIMEOUT'} else exc.code
            raise PackageRefusal(code,str(exc),dict(exc.details,operationId=operation,sdkRequestCancelled=False,
                                 nativeImportMayStillRun=True,journal=str(journal_path(self.project,operation)))) from exc

    def sample(self, *, name, version, sample_name, operation_id=None, expected_plan_sha256=None, allow_overwrite=False, dry_run=False, confirm=False):
        operation=operation_id or uuid.uuid4().hex
        arguments={'name':name,'version':version,'sampleName':sample_name,'operationId':operation,'allowOverwrite':allow_overwrite}
        with self.session.workflow_session() as lease:
            self._preflight(lease)
            preview=self._invoke(lease,'package_sample',dict(arguments,dryRun=True,confirm=True))
            identity=self._identity_arguments(lease,preview)
            if dry_run: return {'ok':True,'action':'sample','dryRun':True,'preview':preview}
            if confirm is not True: raise PackageRefusal('CONFIRMATION_REQUIRED','Confirm the reviewed exact sample preview before mutation',{'preview':preview})
            self._review(preview,expected_plan_sha256)
            self.compile.run_with_lease(self.source_paths,lease); self._preflight(lease)
            try:
                result=self._invoke(lease,'package_sample',dict(arguments,**identity,expectedPlanSha256=preview['planSha256'],confirm=True))
            except (SessionRefusal, WorkflowRefusal) as exc:
                if exc.code not in {'ACTION_TIMEOUT','READY_TIMEOUT','COMPILE_TIMEOUT','CLI_TIMEOUT'}: raise
                raise PackageRefusal('SAMPLE_IMPORT_TIMEOUT',str(exc),dict(exc.details,operationId=operation,
                                     sdkRequestCancelled=False,nativeImportMayStillRun=True,
                                     journal=str(journal_path(self.project,operation)))) from exc
            if any(result.get(key)!=preview.get(key) for key in ('operationId','name','version','sampleName','planSha256','commandSha256','nativePid','nativeProcessStartTicks')):
                raise PackageRefusal('SAMPLE_ACKNOWLEDGEMENT_MISMATCH','Native sample acknowledgement differs from the exact reviewed request')
            return self.complete_sample_with_lease(lease,result)

    def recover(self, *, operation_id, expected_journal_sha256, dry_run=False, confirm=False):
        with self.session.workflow_session() as lease:
            self._preflight(lease)
            path=journal_path(self.project,operation_id)
            raw=path.read_bytes()
            if hashlib.sha256(raw).hexdigest()!=expected_journal_sha256: raise PackageRefusal('JOURNAL_HASH_MISMATCH','Use the exact captured journal hash for recovery')
            journal=json.loads(raw)
            arguments={'operationId':operation_id,'expectedJournalSha256':expected_journal_sha256}
            preview=self._invoke(lease,'package_operation_recover',dict(arguments,dryRun=True,confirm=True))
            identity=self._identity_arguments(lease,preview)
            identity.pop('editorStartedAt')
            if dry_run: return {'ok':True,'action':'recover','dryRun':True,'preview':preview}
            if confirm is not True: raise PackageRefusal('CONFIRMATION_REQUIRED','Confirm the hash-bound inverse before restoring project files')
            self.compile.run_with_lease(self.source_paths,lease); self._preflight(lease)
            restored=self._invoke(lease,'package_operation_recover',dict(arguments,**identity,confirm=True))
            if restored.get('restored') is not True: raise PackageRefusal('RECOVERY_NOT_VERIFIED','The exact operation did not report restoration')
            if restored.get('requiresNativeResolve') is True:
                resolve=self._invoke(lease,'package_resolve',{})
                if resolve.get('status')!='completed' or resolve.get('success') is not True: raise PackageRefusal('PACKAGE_RESOLVE_FAILED','Native resolve did not complete after inverse',{'nativeResult':resolve})
            self.session.ready(expected_identity=lease.identity,deadline=lease.deadline)
            compiled=self.compile.run_with_lease(self.source_paths,lease)
            packages=self._installed(lease)
            original=journal.get('originalPackage',journal.get('samplePlan',{}).get('originalPackage',{}))
            selected=[item for item in packages if item.get('name')==original.get('name')]
            keys=('name','version','source','isDirectDependency')
            if len(selected)!=1 or any(selected[0].get(key)!=original.get(key) for key in keys):
                raise PackageRefusal('RECOVERY_PACKAGE_MISMATCH','Fresh installed metadata differs from the captured original package',{'expected':original,'actual':selected})
            return dict(restored,ok=True,compile=compiled,package=selected[0])
