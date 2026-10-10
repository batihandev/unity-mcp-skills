from __future__ import annotations

import ctypes
import errno
import os
import pathlib
import secrets
import stat
from ctypes import wintypes
from typing import Any


class ReportPathError(ValueError):
    def __init__(self, code: str, message: str, details: dict | None = None):
        super().__init__(message)
        self.code = code
        self.details = details or {}


class _WindowsDirectoryApi:
    """Narrow Win32 adapter for pinned directory handles."""

    GENERIC_READ = 0x80000000
    DELETE = 0x00010000
    FILE_SHARE_READ = 0x00000001
    FILE_SHARE_WRITE = 0x00000002  # Deliberately omit FILE_SHARE_DELETE.
    OPEN_EXISTING = 3
    FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000
    FILE_FLAG_BACKUP_SEMANTICS = 0x02000000
    FILE_ATTRIBUTE_DIRECTORY = 0x00000010
    FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400
    INVALID_HANDLE_VALUE = ctypes.c_void_p(-1).value

    class _Info(ctypes.Structure):
        _fields_ = [
            ("attributes", wintypes.DWORD),
            ("created", wintypes.FILETIME),
            ("accessed", wintypes.FILETIME),
            ("written", wintypes.FILETIME),
            ("volume", wintypes.DWORD),
            ("size_high", wintypes.DWORD),
            ("size_low", wintypes.DWORD),
            ("links", wintypes.DWORD),
            ("index_high", wintypes.DWORD),
            ("index_low", wintypes.DWORD),
        ]

    class _DispositionInfo(ctypes.Structure):
        _fields_ = [("delete_file", wintypes.BOOLEAN)]

    def __init__(self):
        if os.name != "nt":
            raise OSError("Win32 directory handles are available only on Windows")
        self.kernel = ctypes.WinDLL("kernel32", use_last_error=True)
        self.kernel.CreateFileW.argtypes = [
            wintypes.LPCWSTR, wintypes.DWORD, wintypes.DWORD, wintypes.LPVOID,
            wintypes.DWORD, wintypes.DWORD, wintypes.HANDLE,
        ]
        self.kernel.CreateFileW.restype = wintypes.HANDLE
        self.kernel.GetFileInformationByHandle.argtypes = [wintypes.HANDLE, ctypes.POINTER(self._Info)]
        self.kernel.GetFileInformationByHandle.restype = wintypes.BOOL
        self.kernel.CloseHandle.argtypes = [wintypes.HANDLE]
        self.kernel.CloseHandle.restype = wintypes.BOOL
        self.kernel.SetFileInformationByHandle.argtypes = [
            wintypes.HANDLE, ctypes.c_int, wintypes.LPVOID, wintypes.DWORD
        ]
        self.kernel.SetFileInformationByHandle.restype = wintypes.BOOL

    def open_directory(self, path: str, access: int, share: int, disposition: int, flags: int):
        handle = self.kernel.CreateFileW(path, access, share, None, disposition, flags, None)
        if handle == self.INVALID_HANDLE_VALUE:
            raise OSError(ctypes.get_last_error(), "Could not pin report directory", path)
        return handle

    def info(self, handle):
        info = self._Info()
        if not self.kernel.GetFileInformationByHandle(handle, ctypes.byref(info)):
            raise OSError(ctypes.get_last_error(), "Could not inspect report directory")
        return info

    def identity(self, handle) -> tuple[int, int]:
        info = self.info(handle)
        return int(info.volume), (int(info.index_high) << 32) | int(info.index_low)

    def is_directory(self, handle) -> bool:
        return bool(self.info(handle).attributes & self.FILE_ATTRIBUTE_DIRECTORY)

    def is_reparse(self, handle) -> bool:
        return bool(self.info(handle).attributes & self.FILE_ATTRIBUTE_REPARSE_POINT)

    @staticmethod
    def path_identity(path: str) -> tuple[int, int]:
        info = os.stat(path, follow_symlinks=False)
        return int(info.st_dev), int(info.st_ino)

    @staticmethod
    def path_is_reparse(path: str) -> bool:
        return bool(getattr(os.lstat(path), "st_file_attributes", 0) & _WindowsDirectoryApi.FILE_ATTRIBUTE_REPARSE_POINT)

    def close(self, handle) -> None:
        if handle and not self.kernel.CloseHandle(handle):
            raise OSError(ctypes.get_last_error(), "Could not close report directory")

    def delete_directory(self, handle) -> None:
        disposition = self._DispositionInfo(True)
        if not self.kernel.SetFileInformationByHandle(
            handle, 4, ctypes.byref(disposition), ctypes.sizeof(disposition)
        ):
            raise OSError(ctypes.get_last_error(), "Could not delete owned report stage directory")

    def delete_regular_file(self, path: pathlib.Path, expected_identity: tuple[int, int]) -> None:
        share = self.FILE_SHARE_READ | self.FILE_SHARE_WRITE
        handle = self.kernel.CreateFileW(
            str(path), self.GENERIC_READ | self.DELETE, share, None,
            self.OPEN_EXISTING, self.FILE_FLAG_OPEN_REPARSE_POINT, None,
        )
        if handle == self.INVALID_HANDLE_VALUE:
            raise OSError(ctypes.get_last_error(), "Could not open staged report for safe cleanup", str(path))
        try:
            info = self.info(handle)
            identity = int(info.volume), (int(info.index_high) << 32) | int(info.index_low)
            if info.attributes & (self.FILE_ATTRIBUTE_DIRECTORY | self.FILE_ATTRIBUTE_REPARSE_POINT):
                raise ReportPathError("TEST_OUTPUT_PATH_CHANGED", "The staged report is not a plain file")
            if identity != expected_identity:
                raise ReportPathError("TEST_OUTPUT_PATH_CHANGED", "The staged report changed before cleanup")
            disposition = self._DispositionInfo(True)
            if not self.kernel.SetFileInformationByHandle(
                handle, 4, ctypes.byref(disposition), ctypes.sizeof(disposition)
            ):
                raise OSError(ctypes.get_last_error(), "Could not delete owned staged report")
        finally:
            self.close(handle)

    def open_regular_file(self, path: pathlib.Path):
        handle = self.kernel.CreateFileW(
            str(path), self.GENERIC_READ, self.FILE_SHARE_READ | self.FILE_SHARE_WRITE,
            None, self.OPEN_EXISTING, self.FILE_FLAG_OPEN_REPARSE_POINT, None,
        )
        if handle == self.INVALID_HANDLE_VALUE:
            raise OSError(ctypes.get_last_error(), "Could not safely open staged report", str(path))
        try:
            info = self.info(handle)
            if info.attributes & (self.FILE_ATTRIBUTE_DIRECTORY | self.FILE_ATTRIBUTE_REPARSE_POINT):
                raise ReportPathError("TEST_OUTPUT_PATH_CHANGED", "The staged report is not a plain file")
            identity = int(info.volume), (int(info.index_high) << 32) | int(info.index_low)
            return handle, identity
        except BaseException:
            self.close(handle)
            raise


class _WindowsDirectoryGuard:
    """Hold each resolved ancestor without delete sharing for one path operation."""

    def __init__(self, path: str | pathlib.Path, expected_identity: tuple[int, int], api=None,
                 *, delete_target: bool = False):
        self.path = str(path)
        self.expected_identity = expected_identity
        self.api = api if api is not None else _WindowsDirectoryApi()
        self.delete_target = delete_target
        self.handles: list[tuple[str, Any, tuple[int, int]]] = []
        try:
            pure = pathlib.PureWindowsPath(self.path)
            if not pure.is_absolute():
                raise ReportPathError("TEST_OUTPUT_PATH_CHANGED", "The report directory must be absolute")
            parts = pure.parts
            ancestors = [str(pathlib.PureWindowsPath(parts[0]))]
            current = pathlib.PureWindowsPath(parts[0])
            for component in parts[1:]:
                current = current / component
                ancestors.append(str(current))
            for ancestor in ancestors:
                share = self.api.FILE_SHARE_READ | self.api.FILE_SHARE_WRITE
                flags = self.api.FILE_FLAG_BACKUP_SEMANTICS | self.api.FILE_FLAG_OPEN_REPARSE_POINT
                access = self.api.GENERIC_READ | (
                    self.api.DELETE if delete_target and ancestor == ancestors[-1] else 0
                )
                handle = self.api.open_directory(
                    ancestor, access, share, self.api.OPEN_EXISTING, flags
                )
                identity = self.api.identity(handle)
                self.handles.append((ancestor, handle, identity))
                if not self.api.is_directory(handle) or self.api.is_reparse(handle):
                    raise ReportPathError("TEST_OUTPUT_PATH_CHANGED", "A report path ancestor is not a plain directory")
            if self.handles[-1][2] != expected_identity:
                raise ReportPathError("TEST_OUTPUT_PATH_CHANGED", "The report parent identity changed during preflight")
            self.verify()
        except BaseException:
            self.close(suppress=True)
            raise

    def delete_held_directory(self) -> None:
        if not self.delete_target or not hasattr(self.api, "delete_directory"):
            raise ReportPathError("TEST_OUTPUT_GUARD_UNAVAILABLE", "Safe directory-handle deletion is unavailable")
        self.verify()
        self.api.delete_directory(self.handles[-1][1])

    def verify(self) -> None:
        for path, handle, identity in self.handles:
            if self.api.identity(handle) != identity:
                raise ReportPathError("TEST_OUTPUT_PATH_CHANGED", "A held report directory identity changed")
            try:
                if self.api.path_is_reparse(path) or self.api.path_identity(path) != identity:
                    raise ReportPathError("TEST_OUTPUT_PATH_CHANGED", "A report path no longer maps to its held directory")
            except (OSError, RuntimeError) as exc:
                raise ReportPathError("TEST_OUTPUT_PATH_CHANGED", "A held report directory became unavailable") from exc

    def close(self, *, suppress: bool = False) -> None:
        errors = []
        while self.handles:
            _, handle, _ = self.handles.pop()
            try:
                self.api.close(handle)
            except OSError as exc:
                errors.append(exc)
        if errors and not suppress:
            raise errors[0]


class _PosixDirectoryGuard:
    def __init__(self, path: pathlib.Path, expected_identity: tuple[int, int]):
        required = (os.open, os.mkdir, os.stat, os.rename, os.unlink, os.rmdir)
        if os.name == "nt" or not hasattr(os, "O_DIRECTORY") or not hasattr(os, "O_NOFOLLOW") \
                or any(operation not in os.supports_dir_fd for operation in required):
            raise ReportPathError("TEST_OUTPUT_GUARD_UNAVAILABLE", "Safe directory-relative report operations are unavailable")
        self.path = path
        self.expected_identity = expected_identity
        self.fds: list[int] = []
        try:
            flags = os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW
            current_fd = os.open(path.anchor, flags)
            self.fds.append(current_fd)
            for component in path.parts[1:]:
                current_fd = os.open(component, flags, dir_fd=current_fd)
                self.fds.append(current_fd)
            if self.identity != expected_identity:
                raise ReportPathError("TEST_OUTPUT_PATH_CHANGED", "The report parent changed during preflight")
            self.verify()
        except BaseException:
            self.close(suppress=True)
            raise

    @property
    def fd(self) -> int:
        if not self.fds:
            raise ReportPathError("TEST_OUTPUT_GUARD_CLOSED", "The report directory guard is closed")
        return self.fds[-1]

    @property
    def identity(self) -> tuple[int, int]:
        info = os.fstat(self.fd)
        return int(info.st_dev), int(info.st_ino)

    def verify(self) -> None:
        if self.identity != self.expected_identity:
            raise ReportPathError("TEST_OUTPUT_PATH_CHANGED", "The held report parent identity changed")
        try:
            resolved = self.path.resolve(strict=True)
            current = self.path.stat()
        except (OSError, RuntimeError) as exc:
            raise ReportPathError("TEST_OUTPUT_PATH_CHANGED", "The report parent path became unavailable") from exc
        if resolved != self.path or (current.st_dev, current.st_ino) != self.expected_identity:
            raise ReportPathError("TEST_OUTPUT_PATH_CHANGED", "The report parent path no longer maps to its held identity")

    def close(self, *, suppress: bool = False) -> None:
        errors = []
        while self.fds:
            fd = self.fds.pop()
            try:
                os.close(fd)
            except OSError as exc:
                errors.append(exc)
        if errors and not suppress:
            raise errors[0]


class ReportTarget:
    """Pin an absent caller-selected report to its preflight parent directory."""

    def __init__(self, project: pathlib.Path, output: str | pathlib.Path):
        self.project = project.resolve()
        raw = pathlib.Path(output).expanduser()
        if raw.exists() or raw.is_symlink():
            raise ReportPathError("TEST_OUTPUT_EXISTS", "The caller-selected report path already exists", {"path": str(raw.absolute())})
        try:
            self.lexical_path = raw.absolute()
            self.path = raw.resolve(strict=False)
        except (OSError, RuntimeError, ValueError) as exc:
            raise ReportPathError("TEST_OUTPUT_INVALID", "The report path could not be resolved", {"reason": str(exc)}) from exc
        if self.path.exists() or self.path.is_symlink():
            raise ReportPathError("TEST_OUTPUT_EXISTS", "The caller-selected report path already exists", {"path": str(self.path)})
        self.parent_path = self.path.parent
        self._parent_guard = None
        self._stage: ReportStage | None = None
        self._closed = False
        self.published_identity: tuple[int, int] | None = None
        blocked = [self.project / part for part in ("Assets", "Packages", "Library", "Temp")]
        if any(self._under(candidate, root.resolve(strict=False)) for candidate in (self.path, self.lexical_path) for root in blocked):
            raise ReportPathError("TEST_OUTPUT_INSIDE_PROJECT", "Test reports must be outside project-owned Unity data directories", {"path": str(self.path)})
        if not self.parent_path.is_dir():
            raise ReportPathError("TEST_OUTPUT_PARENT_MISSING", "The report parent directory must already exist", {"parent": str(self.parent_path)})
        try:
            info = self.parent_path.stat()
            identity = (info.st_dev, info.st_ino)
            self._parent_guard = (
                _WindowsDirectoryGuard(self.parent_path, identity)
                if os.name == "nt"
                else _PosixDirectoryGuard(self.parent_path, identity)
            )
            self._verify_parent()
        except ReportPathError:
            self.close(suppress=True)
            raise
        except OSError as exc:
            self.close(suppress=True)
            raise ReportPathError("TEST_OUTPUT_PARENT_MISSING", "The report parent could not be safely pinned", {"reason": str(exc)}) from exc

    @staticmethod
    def _under(path: pathlib.Path, root: pathlib.Path) -> bool:
        try:
            path.relative_to(root)
            return True
        except ValueError:
            return False

    def __enter__(self) -> "ReportTarget":
        self._ensure_open()
        return self

    def __exit__(self, exc_type, exc, traceback) -> None:
        self.close()

    def _ensure_open(self) -> None:
        if self._closed or self._parent_guard is None:
            raise ReportPathError("TEST_OUTPUT_GUARD_CLOSED", "The report target is closed")

    def close(self, *, suppress: bool = False) -> None:
        if self._closed:
            return
        self._closed = True
        errors = []
        if self._stage is not None:
            self._stage.close(suppress=True)
        if self._parent_guard is not None:
            try:
                self._parent_guard.close(suppress=suppress)
            except OSError as exc:
                errors.append(exc)
        self._parent_guard = None
        if errors and not suppress:
            raise errors[0]

    def _verify_parent(self) -> None:
        self._ensure_open()
        self._parent_guard.verify()
        try:
            resolved = self.parent_path.resolve(strict=True)
        except (OSError, RuntimeError) as exc:
            raise ReportPathError("TEST_OUTPUT_PATH_CHANGED", "The report parent changed or became unavailable") from exc
        if resolved != self.parent_path:
            raise ReportPathError("TEST_OUTPUT_PATH_CHANGED", "The report parent no longer resolves to its preflight path")
        blocked = [self.project / part for part in ("Assets", "Packages", "Library", "Temp")]
        if any(self._under(resolved, root.resolve(strict=False)) for root in blocked):
            raise ReportPathError("TEST_OUTPUT_INSIDE_PROJECT", "The report parent now resolves inside project-owned Unity data", {"parent": str(resolved)})

    def create_stage(self) -> "ReportStage":
        if self._stage is not None:
            raise ReportPathError("TEST_REPORT_STAGE_EXISTS", "This report target already owns a staging directory")
        self._verify_parent()
        for _ in range(10):
            name = ".unity-test-report-" + secrets.token_hex(16)
            directory = self.parent_path / name
            try:
                if os.name == "nt":
                    os.mkdir(directory, 0o700)
                else:
                    os.mkdir(name, 0o700, dir_fd=self._parent_guard.fd)
            except FileExistsError:
                continue
            except OSError as exc:
                raise ReportPathError("TEST_REPORT_STAGE_FAILED", "A private report staging directory could not be created", {"reason": str(exc)}) from exc
            self._verify_parent()
            stage = ReportStage(self, name)
            self._stage = stage
            stage.verify()
            return stage
        raise ReportPathError("TEST_REPORT_STAGE_FAILED", "Could not allocate a unique report staging directory")

    def publish(self, payload: bytes) -> None:
        self._verify_parent()
        flags = os.O_WRONLY | os.O_CREAT | os.O_EXCL
        flags |= getattr(os, "O_BINARY", 0) | getattr(os, "O_NOFOLLOW", 0)
        try:
            fd = os.open(self.path, flags, 0o600) if os.name == "nt" else os.open(
                self.path.name, flags, 0o600, dir_fd=self._parent_guard.fd
            )
        except FileExistsError as exc:
            raise ReportPathError("TEST_OUTPUT_RACE", "The report target appeared during execution; it was not overwritten", {"path": str(self.path)}) from exc
        except OSError as exc:
            raise ReportPathError("TEST_OUTPUT_PUBLISH_FAILED", "The report could not be exclusively published", {"path": str(self.path), "reason": str(exc)}) from exc
        file_identity = None
        try:
            info = os.fstat(fd)
            if not stat.S_ISREG(info.st_mode):
                raise ReportPathError("TEST_OUTPUT_PATH_CHANGED", "The exclusively created report is not a regular file")
            file_identity = (info.st_dev, info.st_ino)
            stream = os.fdopen(fd, "wb")
            fd = None
            with stream:
                stream.write(payload)
                stream.flush()
                os.fsync(stream.fileno())
            self._verify_parent()
            if os.name != "nt":
                current = os.stat(self.path.name, dir_fd=self._parent_guard.fd, follow_symlinks=False)
                if (current.st_dev, current.st_ino) != file_identity or not stat.S_ISREG(current.st_mode):
                    raise ReportPathError("TEST_OUTPUT_PATH_CHANGED", "The published report file identity changed")
        except ReportPathError:
            raise
        except OSError as exc:
            if file_identity is not None:
                try:
                    self._verify_parent()
                    current = os.stat(self.path.name, dir_fd=self._parent_guard.fd, follow_symlinks=False) if os.name != "nt" else self.path.lstat()
                    if (current.st_dev, current.st_ino) == file_identity and stat.S_ISREG(current.st_mode):
                        if os.name != "nt":
                            os.unlink(self.path.name, dir_fd=self._parent_guard.fd)
                        else:
                            self.path.unlink()
                except (OSError, ReportPathError):
                    pass
            raise ReportPathError("TEST_OUTPUT_PUBLISH_FAILED", "The report bytes could not be written", {"path": str(self.path), "reason": str(exc)}) from exc
        finally:
            if fd is not None:
                os.close(fd)
        self._verify_parent()
        self.published_identity = file_identity


class ReportStage:
    def __init__(self, target: ReportTarget, name: str):
        self.target = target
        self.name = name
        self.directory = target.parent_path / name
        self.path = self.directory / target.path.name
        self._stage_fd: int | None = None
        self._windows_guard: _WindowsDirectoryGuard | None = None
        self._closed = False
        self._report_identity: tuple[int, int] | None = None
        self.retained_path: str | None = None
        if os.name == "nt":
            info = self.directory.stat()
            self._windows_guard = _WindowsDirectoryGuard(
                self.directory, (info.st_dev, info.st_ino), delete_target=True
            )
        else:
            self._stage_fd = os.open(name, os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW, dir_fd=target._parent_guard.fd)
            info = os.fstat(self._stage_fd)
            if not stat.S_ISDIR(info.st_mode):
                self.close(suppress=True)
                raise ReportPathError("TEST_OUTPUT_PATH_CHANGED", "The report stage is not a plain directory")
            self._stage_identity = (info.st_dev, info.st_ino)

    def verify(self) -> None:
        self.target._verify_parent()
        if self._closed:
            raise ReportPathError("TEST_OUTPUT_GUARD_CLOSED", "The report stage is closed")
        try:
            if os.name == "nt":
                self._windows_guard.verify()
                info = self.directory.stat()
                if (info.st_dev, info.st_ino) != self._windows_guard.expected_identity or not stat.S_ISDIR(info.st_mode):
                    raise ReportPathError("TEST_OUTPUT_PATH_CHANGED", "The stage path no longer maps to its held directory")
            else:
                held = os.fstat(self._stage_fd)
                current = os.stat(self.name, dir_fd=self.target._parent_guard.fd, follow_symlinks=False)
                if (held.st_dev, held.st_ino) != self._stage_identity or (current.st_dev, current.st_ino) != self._stage_identity or not stat.S_ISDIR(current.st_mode):
                    raise ReportPathError("TEST_OUTPUT_PATH_CHANGED", "The stage path no longer maps to its held directory")
        except ReportPathError:
            raise
        except OSError as exc:
            raise ReportPathError("TEST_OUTPUT_PATH_CHANGED", "The report stage became unavailable") from exc

    def native_path(self) -> str:
        self.verify()
        return str(self.path)

    def read_bytes(self) -> bytes | None:
        self.verify()
        try:
            if os.name == "nt":
                handle, native_identity = self._windows_guard.api.open_regular_file(self.path)
                try:
                    if self._windows_guard.api.path_is_reparse(str(self.path)) \
                            or self._windows_guard.api.path_identity(str(self.path)) != native_identity:
                        raise ReportPathError("TEST_OUTPUT_PATH_CHANGED", "The staged report path no longer maps to its opened file")
                    with self.path.open("rb") as stream:
                        info = os.fstat(stream.fileno())
                        identity = (info.st_dev, info.st_ino)
                        if identity != native_identity or not stat.S_ISREG(info.st_mode):
                            raise ReportPathError("TEST_OUTPUT_PATH_CHANGED", "The staged report changed before reading")
                        payload = stream.read()
                        self._report_identity = identity
                finally:
                    self._windows_guard.api.close(handle)
            else:
                fd = os.open(self.path.name, os.O_RDONLY | os.O_NOFOLLOW, dir_fd=self._stage_fd)
                try:
                    info = os.fstat(fd)
                    if not stat.S_ISREG(info.st_mode):
                        return None
                    chunks = []
                    while True:
                        chunk = os.read(fd, 1024 * 1024)
                        if not chunk:
                            break
                        chunks.append(chunk)
                    payload = b"".join(chunks)
                    self._report_identity = (info.st_dev, info.st_ino)
                finally:
                    os.close(fd)
            self.verify()
            if os.name != "nt":
                current = os.stat(self.path.name, dir_fd=self._stage_fd, follow_symlinks=False)
                if (current.st_dev, current.st_ino) != self._report_identity or not stat.S_ISREG(current.st_mode):
                    raise ReportPathError("TEST_OUTPUT_PATH_CHANGED", "The staged report file identity changed")
            return payload
        except FileNotFoundError:
            return None
        except OSError as exc:
            if exc.errno == getattr(os, "ELOOP", None):
                raise ReportPathError("TEST_OUTPUT_PATH_CHANGED", "The staged report must not be a symbolic link") from exc
            raise ReportPathError("TEST_REPORT_STAGE_READ_FAILED", "The staged report could not be read safely", {"reason": str(exc)}) from exc

    def cleanup(self) -> bool:
        self.verify()
        if self._report_identity is not None:
            try:
                if os.name == "nt":
                    self._windows_guard.api.delete_regular_file(self.path, self._report_identity)
                else:
                    claimed = self._claim_posix_leaf(
                        self.path.name, self._stage_fd, self.directory, self._report_identity, directory=False
                    )
                    if claimed is not None:
                        self._remove_claimed_leaf(claimed, self._stage_fd, self._report_identity, directory=False)
            except FileNotFoundError:
                pass
        if os.name == "nt":
            try:
                self._windows_guard.delete_held_directory()
            except OSError as exc:
                if getattr(exc, "winerror", None) in {145, 183}:
                    self.retained_path = str(self.directory)
                    self.close()
                    return False
                raise ReportPathError("TEST_REPORT_STAGE_CLEANUP_FAILED", "The owned report stage could not be deleted safely", {"reason": str(exc), "retainedPath": str(self.directory)}) from exc
        else:
            self.verify()
            try:
                claimed = self._claim_posix_leaf(
                    self.name, self.target._parent_guard.fd, self.target.parent_path,
                    self._stage_identity, directory=True
                )
                if claimed is None:
                    self.close()
                    return True
                try:
                    self._remove_claimed_leaf(
                        claimed, self.target._parent_guard.fd, self._stage_identity, directory=True
                    )
                except OSError as exc:
                    if exc.errno in {errno.ENOTEMPTY, errno.EEXIST}:
                        self.close()
                        return False
                    raise
            except OSError as exc:
                if exc.errno in {errno.ENOTEMPTY, errno.EEXIST}:
                    self.close()
                    return False
                raise ReportPathError("TEST_REPORT_STAGE_CLEANUP_FAILED", "The owned report stage could not be deleted safely", {"reason": str(exc), "retainedPath": self.retained_path}) from exc
        self.close()
        return True

    def _claim_posix_leaf(self, name: str, parent_fd: int, parent_path: pathlib.Path,
                          expected_identity: tuple[int, int], *, directory: bool):
        """Move a leaf under an exclusive private directory before identity-checked cleanup.

        POSIX has no portable conditional unlink-by-inode operation. The private name closes the
        known-leaf stat/unlink race. The quarantine directory is reserved with mkdir and held by
        descriptor before the leaf moves. A same-user actor racing its private `claimed` entry remains
        outside this guarantee, so mismatches are retained and reported instead of deleted.
        """
        try:
            os.stat(name, dir_fd=parent_fd, follow_symlinks=False)
        except FileNotFoundError:
            return None
        for _ in range(10):
            quarantine_name = ".unity-test-report-retained-" + secrets.token_hex(16)
            try:
                os.mkdir(quarantine_name, 0o700, dir_fd=parent_fd)
                break
            except FileExistsError:
                continue
        else:
            retained = parent_path / name
            self.retained_path = str(retained)
            raise ReportPathError(
                "TEST_REPORT_STAGE_CLEANUP_FAILED", "Could not allocate a private report cleanup directory",
                {"retainedPath": str(retained)},
            )

        quarantine_path = parent_path / quarantine_name
        claimed_path = quarantine_path / "claimed"
        self.retained_path = str(claimed_path)
        try:
            quarantine_fd = os.open(
                quarantine_name, os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW, dir_fd=parent_fd
            )
        except OSError as exc:
            self.retained_path = str(quarantine_path)
            raise ReportPathError(
                "TEST_REPORT_STAGE_CLEANUP_FAILED", "Could not hold the private report cleanup directory",
                {"retainedPath": str(quarantine_path), "reason": str(exc)},
            ) from exc
        try:
            held = os.fstat(quarantine_fd)
            visible = os.stat(quarantine_name, dir_fd=parent_fd, follow_symlinks=False)
            if (held.st_dev, held.st_ino) != (visible.st_dev, visible.st_ino) or not stat.S_ISDIR(visible.st_mode):
                self.retained_path = str(quarantine_path)
                raise ReportPathError(
                    "TEST_OUTPUT_PATH_CHANGED", "The private report cleanup directory changed during creation",
                    {"retainedPath": str(quarantine_path)},
                )
            try:
                os.rename(name, "claimed", src_dir_fd=parent_fd, dst_dir_fd=quarantine_fd)
            except FileNotFoundError:
                os.close(quarantine_fd)
                try:
                    os.rmdir(quarantine_name, dir_fd=parent_fd)
                except OSError as exc:
                    self.retained_path = str(quarantine_path)
                    raise ReportPathError(
                        "TEST_REPORT_STAGE_CLEANUP_FAILED", "The unused private cleanup directory could not be removed",
                        {"retainedPath": str(quarantine_path), "reason": str(exc)},
                    ) from exc
                self.retained_path = None
                return None
            if directory and self._stage_fd is not None:
                source_fd = self._stage_fd
                self._stage_fd = None
                os.close(source_fd)
            info = os.stat("claimed", dir_fd=quarantine_fd, follow_symlinks=False)
        except BaseException:
            try:
                os.close(quarantine_fd)
            except OSError:
                pass
            raise
        identity = (info.st_dev, info.st_ino)
        expected_type = stat.S_ISDIR(info.st_mode) if directory else stat.S_ISREG(info.st_mode)
        if identity != expected_identity or not expected_type:
            os.close(quarantine_fd)
            self.retained_path = str(claimed_path)
            kind = "directory" if directory else "file"
            raise ReportPathError(
                "TEST_OUTPUT_PATH_CHANGED", f"A competing report {kind} was moved to a private retained path",
                {"retainedPath": str(claimed_path), "expectedIdentity": expected_identity,
                 "observedIdentity": identity},
            )
        return quarantine_name, quarantine_fd, claimed_path

    def _remove_claimed_leaf(self, claim, source_fd: int, expected_identity: tuple[int, int], *, directory: bool):
        quarantine_name, quarantine_fd, claimed_path = claim
        self.retained_path = str(claimed_path)
        try:
            info = os.stat("claimed", dir_fd=quarantine_fd, follow_symlinks=False)
            identity = (info.st_dev, info.st_ino)
            correct_type = stat.S_ISDIR(info.st_mode) if directory else stat.S_ISREG(info.st_mode)
            if identity != expected_identity or not correct_type:
                raise ReportPathError(
                    "TEST_OUTPUT_PATH_CHANGED", "The private claimed report changed before cleanup",
                    {"retainedPath": str(claimed_path), "expectedIdentity": expected_identity,
                     "observedIdentity": identity},
                )
            if directory:
                os.rmdir("claimed", dir_fd=quarantine_fd)
            else:
                os.unlink("claimed", dir_fd=quarantine_fd)
        except BaseException:
            try:
                os.close(quarantine_fd)
            except OSError:
                pass
            raise
        os.close(quarantine_fd)
        try:
            os.rmdir(quarantine_name, dir_fd=source_fd)
        except OSError as exc:
            quarantine_path = claimed_path.parent
            self.retained_path = str(quarantine_path)
            raise ReportPathError(
                "TEST_REPORT_STAGE_CLEANUP_FAILED", "The private report cleanup directory could not be removed",
                {"retainedPath": str(quarantine_path), "reason": str(exc)},
            ) from exc
        self.retained_path = None

    def close(self, *, suppress: bool = False) -> None:
        if self._closed:
            return
        self._closed = True
        errors = []
        if self._stage_fd is not None:
            try:
                os.close(self._stage_fd)
            except OSError as exc:
                errors.append(exc)
            self._stage_fd = None
        if self._windows_guard is not None:
            try:
                self._windows_guard.close(suppress=suppress)
            except OSError as exc:
                errors.append(exc)
            self._windows_guard = None
        if errors and not suppress:
            raise errors[0]
