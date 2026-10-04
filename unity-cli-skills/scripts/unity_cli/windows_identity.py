from __future__ import annotations

import json
import queue
import subprocess
import threading
import uuid
from typing import Any

from .model import Deadline, SessionIdentity


class IdentityObserverError(RuntimeError):
    def __init__(self, code: str, message: str):
        super().__init__(message)
        self.code = code


def close_owned_observer_process(process: Any, worker: threading.Thread | None = None) -> None:
    try:
        if process.poll() is None:
            try:
                process.terminate()
            except ProcessLookupError:
                pass
        try:
            process.wait(timeout=0.5)
        except subprocess.TimeoutExpired:
            process.kill()
            process.wait(timeout=0.5)
    finally:
        if worker is not None and worker.ident is not None:
            worker.join(timeout=0.5)
        for stream in (process.stdin, process.stdout):
            if stream is not None:
                stream.close()


def _unique_fields(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
    fields: dict[str, Any] = {}
    for key, value in pairs:
        if key in fields:
            raise ValueError('Duplicate observer response field')
        fields[key] = value
    return fields


class WindowsIdentityObserver:
    """Fresh lifetime observations from one workflow-owned OS helper and process handle."""

    def __init__(self, process: Any, identity: SessionIdentity, deadline: Deadline, validate_startup=None):
        self._process = process
        self._identity = identity
        self._deadline = deadline
        self._observer = uuid.uuid4().hex
        self._sequence = 0
        self._validate_startup = validate_startup
        self._closed = False
        self._requests: queue.Queue = queue.Queue()
        self._responses: queue.Queue = queue.Queue()
        self._exchange_lock = threading.Lock()
        self._worker = threading.Thread(target=self._exchange, name='unity-windows-identity', daemon=True)
        self._worker.start()

    def _exchange(self) -> None:
        while True:
            request = self._requests.get()
            if request is None:
                return
            try:
                self._process.stdin.write(json.dumps(request, ensure_ascii=True) + '\n')
                self._process.stdin.flush()
                line = self._process.stdout.readline(4097)
                self._responses.put(line)
            except (OSError, ValueError) as exc:
                self._responses.put(exc)
                return

    def verify_current(self, deadline: Deadline | None = None) -> None:
        try:
            with self._exchange_lock:
                self._verify_current(deadline if deadline is not None and deadline.expiry < self._deadline.expiry else self._deadline)
        except (IdentityObserverError, subprocess.TimeoutExpired):
            self.close()
            raise

    def _verify_current(self, deadline: Deadline) -> None:
        if self._closed or self._process.poll() is not None:
            raise IdentityObserverError('PROCESS_OBSERVER_UNAVAILABLE', 'The workflow identity observer is no longer running')
        deadline.require_remaining('Windows identity observation')
        self._sequence += 1
        request = dict(self._identity.as_dict(), observer=self._observer, sequence=self._sequence)
        self._requests.put(request)
        try:
            response = self._responses.get(timeout=deadline.require_remaining('Windows identity observation'))
        except queue.Empty as exc:
            raise IdentityObserverError('PROCESS_OBSERVER_TIMEOUT', 'The workflow identity observer reached its deadline') from exc
        deadline.require_remaining('Windows identity observation')
        if isinstance(response, Exception) or self._process.poll() is not None:
            raise IdentityObserverError('PROCESS_OBSERVER_UNAVAILABLE', 'The workflow identity observer failed')
        if not isinstance(response, str) or not response.endswith('\n') or len(response) > 4096:
            raise IdentityObserverError('PROCESS_OBSERVER_INVALID', 'The workflow identity observer returned an incomplete response')
        try:
            payload = json.loads(response, object_pairs_hook=_unique_fields)
        except (ValueError, TypeError) as exc:
            raise IdentityObserverError('PROCESS_OBSERVER_INVALID', 'The workflow identity observer returned malformed JSON') from exc
        fields = {*request, 'alive'}
        if self._sequence == 1:
            fields.update(('commandLine', 'executable'))
        if (not isinstance(payload, dict) or set(payload) != fields
                or any(type(payload.get(key)) is not type(value) or payload[key] != value for key, value in request.items())
                or type(payload.get('alive')) is not bool):
            raise IdentityObserverError('PROCESS_OBSERVER_INVALID', 'The workflow identity observer returned a mismatched identity or request sequence')
        if self._sequence == 1 and self._validate_startup is not None:
            self._validate_startup(payload)
        if not payload['alive']:
            raise IdentityObserverError('PROCESS_IDENTITY_CHANGED', 'The exact Editor process lifetime ended during the workflow')

    def close(self) -> None:
        if self._closed:
            return
        self._closed = True
        self._requests.put(None)
        close_owned_observer_process(self._process, self._worker)
