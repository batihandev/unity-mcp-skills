import argparse
import base64
import hashlib
import json
import os
import shutil
import stat
import subprocess
import sys
import tempfile
import unicodedata
import uuid
from pathlib import Path

KEYWORDS = frozenset("abstract as base bool break byte case catch char checked class const continue decimal default delegate do double else enum event explicit extern false finally fixed float for foreach goto if implicit in int interface internal is lock long namespace new null object operator out override params private protected public readonly ref return sbyte sealed short sizeof stackalloc static string struct switch this throw true try typeof uint ulong unchecked unsafe ushort using virtual void volatile while".split())
IDENTIFIER_START = frozenset(("Lu", "Ll", "Lt", "Lm", "Lo", "Nl"))
IDENTIFIER_PART = IDENTIFIER_START | frozenset(("Nd", "Pc", "Mn", "Mc", "Cf"))


class Refusal(Exception):
    def __init__(self, code, message, details=None):
        super().__init__(message)
        self.code = code
        self.details = details or {}


def digest(data):
    return hashlib.sha256(data).hexdigest()


def snapshot(root):
    return [{"Path": path.relative_to(root).as_posix(), "Sha256": digest(path.read_bytes())}
            for path in sorted(root.rglob("*")) if path.is_file()]


def identifier(name):
    if not name or (name[0] != "_" and unicodedata.category(name[0]) not in IDENTIFIER_START):
        raise Refusal("CSHARP_IDENTIFIER_INVALID", "The class name is not a legal C# identifier.")
    if any(ch != "_" and unicodedata.category(ch) not in IDENTIFIER_PART for ch in name[1:]):
        raise Refusal("CSHARP_IDENTIFIER_INVALID", "The class name is not a legal C# identifier.")
    if name in KEYWORDS:
        raise Refusal("CSHARP_IDENTIFIER_KEYWORD", "The class name is a reserved C# keyword.")


def is_link(path):
    try:
        info = path.lstat()
    except FileNotFoundError:
        return False
    attributes = getattr(info, "st_file_attributes", 0)
    reparse = getattr(stat, "FILE_ATTRIBUTE_REPARSE_POINT", 0x400)
    return stat.S_ISLNK(info.st_mode) or bool(attributes & reparse)


def normalize(project, relative, allow_packages):
    if not isinstance(relative, str) or not relative.strip():
        raise Refusal("PATH_REQUIRED", "A project-relative authoring path is required.")
    text = relative.replace("\\", "/")
    if text.startswith("/") or ":" in text.split("/", 1)[0]:
        raise Refusal("PATH_OUTSIDE_ROOT", "The authoring path must be project-relative.")
    parts = [part for part in text.split("/") if part not in ("", ".")]
    if ".." in parts:
        raise Refusal("PATH_TRAVERSAL", "The authoring path must not contain '..'.")
    if len(parts) < 2 or parts[0] not in ("Assets", "Packages"):
        raise Refusal("PATH_OUTSIDE_ROOT", "The authoring path must be below Assets or an embedded package.")
    if parts[0] == "Packages" and (not allow_packages or len(parts) < 3):
        raise Refusal("PACKAGE_NOT_EMBEDDED", "An explicit embedded-package subpath is required.")
    relative = "/".join(parts)
    target = (project / Path(*parts)).resolve(strict=False)
    project_resolved = project.resolve(strict=True)
    try:
        target.relative_to(project_resolved)
    except ValueError as error:
        raise Refusal("PATH_OUTSIDE_ROOT", "The canonical path escapes the project.") from error
    return relative, target


def selected_root(project, relative):
    parts = relative.split("/")
    if parts[0] == "Assets":
        return project / "Assets"
    package = project / "Packages" / parts[1]
    if not package.is_dir() or not (package / "package.json").is_file():
        raise Refusal("PACKAGE_NOT_EMBEDDED", "The selected package is not embedded in this project.")
    return package


def inspect_no_follow(project, root, target):
    for ancestor in (project, *project.parents):
        if is_link(ancestor):
            raise Refusal("PATH_REPARSE_POINT", "The project root or an ancestor is linked.")
    cursor = project
    for part in target.relative_to(project).parts:
        cursor = cursor / part
        if cursor.exists() or is_link(cursor):
            if is_link(cursor):
                raise Refusal("PATH_REPARSE_POINT", "The authoring path crosses a link or reparse point.")
        else:
            break
    if not root.exists() or is_link(root):
        raise Refusal("PATH_REPARSE_POINT", "The selected authoring root is missing or linked.")


def read_json(path, code):
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, json.JSONDecodeError) as error:
        raise Refusal(code, "Assembly ownership metadata is malformed.", {"Owner": str(path)}) from error
    if not isinstance(value, dict):
        raise Refusal(code, "Assembly ownership metadata must be a JSON object.", {"Owner": str(path)})
    return value


def assembly_catalog(project):
    result = []
    for top in (project / "Assets", project / "Packages"):
        if not top.exists():
            continue
        for path in top.rglob("*.asmdef"):
            if any(is_link(parent) for parent in (path, *path.parents) if parent == top or top in parent.parents):
                continue
            document = read_json(path, "MALFORMED_ASMDEF")
            result.append((path, document))
    return result


def resolve_asmref(project, asmref, catalog):
    reference = read_json(asmref, "MALFORMED_ASMREF").get("reference")
    if not isinstance(reference, str) or not reference:
        raise Refusal("MALFORMED_ASMREF", "The asmref has no exact reference.", {"Owner": str(asmref)})
    matches = []
    if reference.startswith("GUID:"):
        wanted = reference[5:]
        for path, document in catalog:
            meta = Path(str(path) + ".meta")
            if meta.is_file() and any(line.strip() == "guid: " + wanted for line in meta.read_text(encoding="utf-8").splitlines()):
                matches.append((path, document))
    else:
        matches = [(path, document) for path, document in catalog if document.get("name") == reference]
    if len(matches) != 1:
        raise Refusal("ASMREF_UNRESOLVED" if not matches else "ASMREF_AMBIGUOUS",
                      "The asmref must resolve to exactly one asmdef.", {"Owner": str(asmref)})
    return matches[0]


def compatible(document, mode):
    optional = document.get("optionalUnityReferences", [])
    platforms = document.get("includePlatforms", [])
    if "TestAssemblies" not in optional or not isinstance(platforms, list):
        return False
    return platforms == ["Editor"] if mode == "EditMode" else platforms == []


def assembly_plan(project, root, folder, mode, generated_name):
    catalog = assembly_catalog(project)
    cursor = folder
    while True:
        if cursor.exists():
            owners = sorted((*cursor.glob("*.asmdef"), *cursor.glob("*.asmref")))
            if len(owners) > 1:
                raise Refusal("AMBIGUOUS_ASSEMBLY_OWNER", "Multiple assembly owners exist in one directory.",
                              {"Directory": str(cursor)})
            if owners:
                owner = owners[0]
                resolved = (owner, read_json(owner, "MALFORMED_ASMDEF")) if owner.suffix == ".asmdef" else resolve_asmref(project, owner, catalog)
                if not compatible(resolved[1], mode):
                    raise Refusal("INCOMPATIBLE_ASSEMBLY_OWNER", "The enclosing assembly is incompatible with the requested test mode.",
                                  {"Owner": resolved[0].relative_to(project).as_posix(), "OwnerSha256": digest(resolved[0].read_bytes())})
                return None
        if cursor == root:
            break
        if root not in cursor.parents:
            raise Refusal("PATH_OUTSIDE_ROOT", "The test folder escapes its authoring root.")
        cursor = cursor.parent
    collisions = [(path, document) for path, document in catalog if document.get("name") == generated_name]
    if collisions:
        raise Refusal("ASSEMBLY_NAME_COLLISION", "The generated assembly name is already used.",
                      {"Owners": [path.relative_to(project).as_posix() for path, _ in collisions]})
    return folder / (generated_name + ".asmdef")


def template_writes(request, project):
    mode = request.get("mode")
    if mode not in ("EditMode", "PlayMode"):
        raise Refusal("TEST_MODE_INVALID", "mode must be EditMode or PlayMode.")
    name = request.get("className")
    identifier(name)
    folder_relative, folder = normalize(project, request.get("folder"), request.get("allowEmbeddedPackages", False))
    root = selected_root(project, folder_relative)
    source_relative = folder_relative + "/" + name + ".cs"
    source = folder / (name + ".cs")
    if source.name != name + ".cs":
        raise Refusal("FILE_NAME_MISMATCH", "The class and file names must match.")
    assembly_name = "UnityCliEditModeTests" if mode == "EditMode" else "UnityCliPlayModeTests"
    asmdef = assembly_plan(project, root, folder, mode, assembly_name)
    if mode == "EditMode":
        body = "using NUnit.Framework;\n\npublic sealed class %s\n{\n    [Test]\n    public void GeneratedTemplatePasses()\n    {\n        Assert.Pass();\n    }\n}\n" % name
        platforms = ["Editor"]
    else:
        body = "using System.Collections;\nusing NUnit.Framework;\nusing UnityEngine.TestTools;\n\npublic sealed class %s\n{\n    [UnityTest]\n    public IEnumerator GeneratedTemplatePasses()\n    {\n        yield return null;\n        Assert.Pass();\n    }\n}\n" % name
        platforms = []
    writes = [(source_relative, body.encode("utf-8"))]
    if asmdef is not None:
        document = {"name": assembly_name, "references": [], "includePlatforms": platforms,
                    "optionalUnityReferences": ["TestAssemblies"], "autoReferenced": False}
        writes.append((asmdef.relative_to(project).as_posix(),
                       (json.dumps(document, indent=2) + "\n").encode("utf-8")))
    return writes


def decode_writes(request):
    writes = request.get("writes")
    if not isinstance(writes, list) or not writes:
        raise Refusal("WRITES_REQUIRED", "At least one write is required.")
    result = []
    for item in writes:
        if not isinstance(item, dict) or not isinstance(item.get("path"), str) or not isinstance(item.get("content"), str):
            raise Refusal("WRITE_INVALID", "Each write requires path and Base64 content strings.")
        try:
            data = base64.b64decode(item["content"], validate=True)
        except ValueError as error:
            raise Refusal("CONTENT_INVALID", "Write content is not valid Base64.") from error
        result.append((item["path"], data))
    return result


def valid_sha256(value):
    return isinstance(value, str) and len(value) == 64 and all(ch in "0123456789abcdefABCDEF" for ch in value)


def asset_import_write(request, source_reader):
    if "writes" in request or "content" in request:
        raise Refusal("ASSET_IMPORT_REQUEST_INVALID", "asset-import does not accept writes or content.")
    source_value = request.get("sourcePath")
    destination = request.get("destinationPath")
    expected_source = request.get("expectedSourceSha256")
    state = request.get("expectedDestinationState")
    if not isinstance(source_value, str) or not source_value or not Path(source_value).is_absolute():
        raise Refusal("SOURCE_PATH_INVALID", "sourcePath must be an absolute path to the exact external source file.")
    if not isinstance(destination, str) or not destination:
        raise Refusal("PATH_REQUIRED", "destinationPath must name one project-relative authoring path.")
    if not valid_sha256(expected_source):
        raise Refusal("SOURCE_SHA256_INVALID", "expectedSourceSha256 must be a 64-digit hexadecimal SHA-256.")
    if not isinstance(state, dict) or state.get("kind") not in ("absent", "existing"):
        raise Refusal("DESTINATION_STATE_INVALID", "expectedDestinationState must describe an absent or existing target.")
    if state["kind"] == "absent":
        if set(state) != {"kind"}:
            raise Refusal("DESTINATION_STATE_INVALID", "An absent destination state accepts only kind.")
        if request.get("replaceAuthorized", False):
            raise Refusal("REPLACE_AUTHORIZATION_INVALID", "An absent destination requires replaceAuthorized=false.")
    else:
        if set(state) != {"kind", "sha256"} or not valid_sha256(state.get("sha256")):
            raise Refusal("DESTINATION_STATE_INVALID", "An existing destination state requires one SHA-256.")
        if not request.get("replaceAuthorized", False):
            raise Refusal("REPLACE_AUTHORIZATION_INVALID", "An existing destination requires replaceAuthorized=true.")
    source = Path(source_value)
    try:
        data = source_reader(source)
    except FileNotFoundError as error:
        raise Refusal("SOURCE_NOT_FOUND", "The authorized external source does not exist.", {"SourcePath": source_value}) from error
    except IsADirectoryError as error:
        raise Refusal("SOURCE_NOT_FILE", "The authorized external source is not a file.", {"SourcePath": source_value}) from error
    actual_source = digest(data)
    if actual_source.lower() != expected_source.lower():
        raise Refusal("SOURCE_SHA256_MISMATCH", "The external source snapshot does not match expectedSourceSha256.",
                      {"SourcePath": source_value, "ActualSha256": actual_source})
    return destination, data, {"SourcePath": source_value, "SourceSha256": actual_source,
                               "ExpectedDestinationState": state}


def validator_argv(request, relative):
    argv = [request["unityCli"], "command", "--project-path", request["projectPath"],
            "--timeout", "60", "--format", "json", "foundation.path.validate", "--", "--path", relative]
    if request.get("allowEmbeddedPackages", False):
        argv.extend(("--allowEmbeddedPackages", "true"))
    return argv


def run_checked(argv):
    completed = subprocess.run(argv, stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=False)
    if completed.returncode != 0:
        raise Refusal("COMMAND_FAILED", "A required command failed.", {"Argv": argv, "ExitCode": completed.returncode})
    try:
        envelope = json.loads(completed.stdout.decode("utf-8-sig"))
    except (UnicodeError, json.JSONDecodeError) as error:
        raise Refusal("COMMAND_OUTPUT_INVALID", "A required command did not return JSON.", {"Argv": argv}) from error
    result = envelope.get("data", {}).get("result", {})
    if envelope.get("success") is not True or (isinstance(result, dict) and (result.get("Ok") is False or result.get("success") is False)):
        raise Refusal("COMMAND_REJECTED", "A required command rejected the request.", {"Argv": argv, "Envelope": envelope})
    return envelope


def make_parents(parent, stop, created_dirs):
    missing = []
    cursor = parent
    while cursor != stop and not cursor.exists():
        missing.append(cursor)
        cursor = cursor.parent
    for path in reversed(missing):
        path.mkdir()
        created_dirs.append(path)


def transact(request, source_reader=None, before_publish=None, *, validate_path=None, after_publish=None, rollback_guard=None, publication_observer=None):
    for field in ("replaceAuthorized", "allowEmbeddedPackages"):
        if type(request.get(field, False)) is not bool:
            raise Refusal("REQUEST_BOOLEAN_INVALID", field + " must be a JSON boolean.", {"Field": field})
    project = Path(request["projectRoot"]).resolve(strict=True)
    allow_packages = request.get("allowEmbeddedPackages", False)
    operation = request.get("operation")
    asset_import = None
    if operation == "test-template":
        raw_writes = template_writes(request, project)
    elif operation == "asset-import":
        if source_reader is None:
            def source_reader(path):
                with path.open("rb") as stream:
                    return stream.read()
        relative, data, asset_import = asset_import_write(request, source_reader)
        raw_writes = [(relative, data)]
    else:
        raw_writes = decode_writes(request)
    planned = []
    for relative, data in raw_writes:
        normalized, target = normalize(project, relative, allow_packages)
        root = selected_root(project, normalized)
        inspect_no_follow(project, root, target)
        planned.append((normalized, target, root, data))
    if len({target for _, target, _, _ in planned}) != len(planned):
        raise Refusal("DUPLICATE_TARGET", "A transaction target was repeated.")
    if asset_import is not None:
        relative, target, _, _ = planned[0]
        state = asset_import["ExpectedDestinationState"]
        if state["kind"] == "absent" and target.exists():
            raise Refusal("DESTINATION_STATE_MISMATCH", "The destination exists but approval requires it to be absent.",
                          {"Path": relative})
        if state["kind"] == "existing":
            if not target.exists():
                raise Refusal("DESTINATION_STATE_MISMATCH", "The destination is absent but approval requires it to exist.",
                              {"Path": relative})
            actual = digest(target.read_bytes())
            if actual.lower() != state["sha256"].lower():
                raise Refusal("DESTINATION_STATE_MISMATCH", "The destination does not match its approved SHA-256.",
                              {"Path": relative, "ActualSha256": actual})
    invocations = []
    for relative, _, _, _ in planned:
        if validate_path is None:
            argv = validator_argv(request, relative)
            invocations.append({"Argv": argv, "Result": run_checked(argv)})
        else:
            validate_path(relative)

    created_dirs = []
    stages = []
    backups = []
    published = []
    committed = False
    failure = None
    recovery = []
    retained_backups = set()

    def record_recovery(path, action, error):
        recovery.append({"Path": path.relative_to(project).as_posix(), "Action": action,
                         "ErrorType": type(error).__name__})

    try:
        for relative, target, root, data in planned:
            make_parents(target.parent, root, created_dirs)
            inspect_no_follow(project, root, target)
            stage = target.parent / (".unity-cli-stage-" + uuid.uuid4().hex)
            with stage.open("xb") as stream:
                stages.append(stage)
                stream.write(data)
                stream.flush()
                os.fsync(stream.fileno())
        if asset_import is not None and before_publish is not None:
            before_publish(planned[0][0], planned[0][1])
        for (relative, target, _, data), stage in zip(planned, stages):
            if validate_path is not None:
                validate_path(relative)
            asset_state = asset_import["ExpectedDestinationState"] if asset_import is not None else None
            if asset_state is not None and asset_state["kind"] == "absent":
                if target.exists():
                    raise Refusal("DESTINATION_STATE_CHANGED", "The destination appeared immediately before publication.",
                                  {"Path": relative})
                try:
                    os.link(stage, target)
                except FileExistsError as error:
                    raise Refusal("DESTINATION_STATE_CHANGED", "The destination appeared during publication.",
                                  {"Path": relative}) from error
                published.append(target)
                if publication_observer is not None: publication_observer(target)
                stage.unlink()
            elif asset_state is not None:
                if not target.exists():
                    raise Refusal("DESTINATION_STATE_CHANGED", "The destination disappeared immediately before publication.",
                                  {"Path": relative})
                prior_data = target.read_bytes()
                actual = digest(prior_data)
                if actual.lower() != asset_state["sha256"].lower():
                    raise Refusal("DESTINATION_STATE_CHANGED", "The destination changed immediately before publication.",
                                  {"Path": relative, "ActualSha256": actual})
                backup = target.parent / (".unity-cli-backup-" + uuid.uuid4().hex)
                with backup.open("xb") as stream:
                    backups.append((target, backup))
                    stream.write(prior_data)
                    stream.flush()
                    os.fsync(stream.fileno())
                if not target.exists() or digest(target.read_bytes()).lower() != asset_state["sha256"].lower():
                    raise Refusal("DESTINATION_STATE_CHANGED", "The destination changed during publication.",
                                  {"Path": relative})
                os.replace(stage, target)
                published.append(target)
                if publication_observer is not None: publication_observer(target)
            elif target.exists():
                expected = request.get("expectedSha256", {}).get(relative)
                if not request.get("replaceAuthorized", False) or not expected:
                    raise Refusal("TARGET_EXISTS", "Replacement requires authorization and an expected SHA-256.", {"Path": relative})
                actual = digest(target.read_bytes())
                if actual != expected:
                    raise Refusal("SHA256_MISMATCH", "The target changed before publication.", {"Path": relative, "ActualSha256": actual})
                backup = target.parent / (".unity-cli-backup-" + uuid.uuid4().hex)
                with backup.open("xb") as stream:
                    backups.append((target, backup))
                    stream.write(target.read_bytes())
                    stream.flush()
                    os.fsync(stream.fileno())
                if digest(target.read_bytes()) != expected:
                    raise Refusal("SHA256_MISMATCH", "The target changed immediately before publication.", {"Path": relative})
                os.replace(stage, target)
                published.append(target)
                if publication_observer is not None: publication_observer(target)
            else:
                os.link(stage, target)
                published.append(target)
                if publication_observer is not None: publication_observer(target)
                stage.unlink()
        for argv in request.get("postCommands", []):
            if not isinstance(argv, list) or not argv or not all(isinstance(value, str) for value in argv):
                raise Refusal("POST_COMMAND_INVALID", "Each post command must be a nonempty argv string array.")
            invocations.append({"Argv": argv, "Result": run_checked(argv)})
        if after_publish is not None:
            after_publish()
        committed = True
    except BaseException as error:
        failure = error
        for target in reversed(published):
            backup = next((item for item in backups if item[0] == target), None)
            try:
                if rollback_guard is not None:
                    rollback_guard(target)
                if backup is None:
                    target.unlink(missing_ok=True)
                else:
                    os.replace(backup[1], target)
            except BaseException as restore_error:
                if backup is not None:
                    retained_backups.add(backup[1])
                    record_recovery(backup[1], "restore-backup", restore_error)
                record_recovery(target, "restore-target", restore_error)
    for stage in stages:
        try:
            stage.unlink(missing_ok=True)
        except BaseException as error:
            record_recovery(stage, "remove-stage", error)
    for _, backup in backups:
        if backup in retained_backups:
            continue
        try:
            backup.unlink(missing_ok=True)
        except BaseException as error:
            record_recovery(backup, "remove-backup", error)
    if not committed:
        for directory in reversed(created_dirs):
            try:
                directory.rmdir()
            except FileNotFoundError:
                pass
            except BaseException as error:
                record_recovery(directory, "remove-directory", error)
    if recovery:
        raise Refusal("TRANSACTION_RECOVERY_REQUIRED", "Recovery or cleanup needs the retained paths.",
                      {"Committed": committed, "Recovery": recovery,
                       "Cause": getattr(failure, "code", type(failure).__name__) if failure else None}) from failure
    if failure is not None:
        raise failure
    result = {"Ok": True, "SourceSha256": request.get("sourceSha256"), "Invocations": invocations,
              "Writes": [{"Path": relative, "Sha256": digest(data)} for relative, _, _, data in planned]}
    if asset_import is not None:
        result["AssetImport"] = {"SourcePath": asset_import["SourcePath"],
                                 "SourceSha256": asset_import["SourceSha256"],
                                 "DestinationPath": planned[0][0],
                                 "DestinationSha256": digest(planned[0][3])}
    return result


def import_asset_lifecycle(request, *, validate_path, observe, import_asset, refresh,
                           expected_type, snapshot_directory):
    from contextlib import ExitStack
    from .reports import _PosixDirectoryGuard, _WindowsDirectoryGuard

    project = Path(request["projectRoot"]).resolve(strict=True)
    guards = {}
    with ExitStack() as held:
        def guarded_validate(relative):
            for guard in guards.values(): guard.verify()
            lexical = project / relative
            inspect_no_follow(project, project / "Assets", lexical)
            inspect_no_follow(project, project / "Assets", Path(str(lexical) + ".meta"))
            parent = lexical.parent
            while not parent.exists(): parent = parent.parent
            if parent not in guards:
                info = parent.stat()
                kind = _WindowsDirectoryGuard if os.name == "nt" else _PosixDirectoryGuard
                guard = kind(parent, (info.st_dev, info.st_ino))
                held.callback(guard.close, suppress=True)
                guards[parent] = guard
            validate_path(relative)
            for guard in guards.values(): guard.verify()
        return _import_asset_lifecycle(request, validate_path=guarded_validate, observe=observe,
                                      import_asset=import_asset, refresh=refresh,
                                      expected_type=expected_type, snapshot_directory=snapshot_directory)


def _import_asset_lifecycle(request, *, validate_path, observe, import_asset, refresh,
                            expected_type, snapshot_directory):
    """Publish one asset and verify its complete file, metadata and native lifecycle.

    Callbacks belong to one caller-held Editor lease. Recovery retains external
    snapshots when a current hash or the caller's lease no longer matches.
    """
    project = Path(request["projectRoot"]).resolve(strict=True)
    if request.get("operation") != "write" or request.get("postCommands"):
        raise Refusal("ASSET_LIFECYCLE_REQUEST_INVALID", "Use one write and the lifecycle-owned import callback.")
    writes = decode_writes(request)
    if len(writes) != 1:
        raise Refusal("ASSET_LIFECYCLE_REQUEST_INVALID", "The asset lifecycle requires exactly one target.")
    relative, data = writes[0]
    relative, target = normalize(project, relative, False)
    if not relative.startswith("Assets/"):
        raise Refusal("PATH_OUTSIDE_ROOT", "Imported assets must be below Assets.")
    lexical = project / relative
    inspect_no_follow(project, project / "Assets", lexical)
    meta = Path(str(lexical) + ".meta")
    inspect_no_follow(project, project / "Assets", meta)
    validate_path(relative)
    prior = observe(relative)
    before_file = target.read_bytes() if target.exists() else None
    before_meta = meta.read_bytes() if meta.exists() else None
    replacement = before_file is not None
    if replacement:
        if not request.get("replaceAuthorized") or request.get("expectedSha256", {}).get(relative) != digest(before_file):
            raise Refusal("DESTINATION_STATE_MISMATCH", "Replacement requires authorization and the exact prior file SHA-256.")
        if before_meta is None or prior.get("loadable") is not True or not prior.get("guid") or prior.get("path") != relative:
            raise Refusal("ASSET_PRIOR_STATE_INVALID", "Replacement requires a loadable asset and exact metadata/GUID snapshot.")
    elif before_meta is not None or prior.get("loadable") is not False:
        raise Refusal("ASSET_PRIOR_STATE_INVALID", "A new destination requires file/meta absence and an unloadable asset.")
    snapshot_root = Path(snapshot_directory).absolute()
    if snapshot_root.is_relative_to(project):
        raise Refusal("SNAPSHOT_PATH_INVALID", "Recovery snapshots must be outside the Unity project.")
    snapshot_root.mkdir(parents=True, exist_ok=True)
    snapshot_root = snapshot_root / uuid.uuid4().hex
    snapshot_root.mkdir()
    if before_file is not None: (snapshot_root / "file.before").write_bytes(before_file)
    if before_meta is not None: (snapshot_root / "meta.before").write_bytes(before_meta)
    (snapshot_root / "state.json").write_text(json.dumps(prior, sort_keys=True), encoding="utf-8")
    post_meta_hash = None
    imported = None
    published = False
    import_complete = False
    import_started = False

    def did_publish(path):
        nonlocal published
        published = True

    def current_meta():
        inspect_no_follow(project, project / "Assets", meta)
        return digest(meta.read_bytes()) if meta.exists() else None

    def verify_before_publication(path):
        validate_path(path)
        if current_meta() != (digest(before_meta) if before_meta is not None else None):
            raise Refusal("META_STATE_CHANGED", "Metadata changed before publication; preserve the external change.")
        if target.exists() != replacement or (replacement and digest(target.read_bytes()) != digest(before_file)):
            raise Refusal("DESTINATION_STATE_CHANGED", "The asset file changed before publication.")

    def verify_import():
        nonlocal post_meta_hash, imported, import_complete, import_started
        validate_path(relative)
        import_started = True
        try:
            imported = import_asset(relative)
        finally:
            post_meta_hash = current_meta()
        if digest(target.read_bytes()) != digest(data):
            raise Refusal("ASSET_READBACK_MISMATCH", "Imported file bytes differ from the published snapshot.")
        if not isinstance(imported, dict) or imported.get("loadable") is not True or imported.get("path") != relative or imported.get("type") != expected_type or not imported.get("guid") or post_meta_hash is None:
            raise Refusal("ASSET_READBACK_MISMATCH", "Native import must prove exact path, GUID, metadata, loadability and main type.")
        if replacement and imported["guid"] != prior["guid"]:
            raise Refusal("ASSET_GUID_CHANGED", "Replacement changed the captured asset GUID.")
        import_complete = True

    def guard_inverse(path):
        validate_path(relative)
        inspect_no_follow(project, project / "Assets", path)
        if not path.exists() or digest(path.read_bytes()) != digest(data):
            raise Refusal("ASSET_RECOVERY_REFUSED", "The published asset changed; preserve it and retain the prior snapshot.")

    try:
        publication = transact(request, validate_path=verify_before_publication,
                               after_publish=verify_import, rollback_guard=guard_inverse, publication_observer=did_publish)
    except BaseException as failure:
        recovery_errors = []
        recovered = None
        try:
            validate_path(relative)
            observed = current_meta()
            expected_meta = post_meta_hash if import_started else (digest(before_meta) if before_meta is not None else None)
            if observed != expected_meta or current_meta() != expected_meta:
                raise Refusal("ASSET_RECOVERY_REFUSED", "Metadata changed before recovery; preserve it and retain snapshots.")
            if before_meta is None:
                if meta.exists(): meta.unlink()
            elif observed != digest(before_meta):
                restore = meta.parent / (".unity-cli-meta-restore-" + uuid.uuid4().hex)
                with restore.open("xb") as stream:
                    stream.write(before_meta)
                    stream.flush()
                    os.fsync(stream.fileno())
                validate_path(relative)
                if current_meta() != expected_meta:
                    restore.unlink()
                    raise Refusal("ASSET_RECOVERY_REFUSED", "Metadata changed immediately before restoration.")
                os.replace(restore, meta)
            refresh()
            recovered = observe(relative)
            if replacement:
                if target.read_bytes() != before_file or meta.read_bytes() != before_meta or recovered.get("loadable") is not True or recovered.get("guid") != prior.get("guid") or recovered.get("type") != prior.get("type") or recovered.get("path") != relative:
                    raise Refusal("ASSET_RECOVERY_INCOMPLETE", "The complete prior file/meta/GUID/type state was not restored.")
            elif target.exists() or meta.exists() or recovered.get("loadable") is not False:
                raise Refusal("ASSET_RECOVERY_INCOMPLETE", "The new asset file/meta and loaded asset were not removed.")
        except BaseException as recovery_failure:
            recovery_errors.append({"code":getattr(recovery_failure,"code",type(recovery_failure).__name__),"message":str(recovery_failure)})
        partial = bool(recovery_errors) or getattr(failure,"code",None) == "TRANSACTION_RECOVERY_REQUIRED"
        retained = [str(path) for path in sorted(snapshot_root.iterdir())] if partial else []
        if partial:
            for entry in getattr(failure, "details", {}).get("Recovery", []):
                candidate = project / entry.get("Path", "")
                if candidate.is_file() and str(candidate) not in retained: retained.append(str(candidate))
        if not partial: shutil.rmtree(snapshot_root)
        return {"ok":False,"success":False,"imported":relative,"filePublished":published,"importComplete":import_complete,
                "partialFailure":partial,"retainedPaths":retained,
                "error":{"code":getattr(failure,"code",type(failure).__name__),"message":str(failure),"details":getattr(failure,"details",{})},
                "recoveryErrors":recovery_errors,"recoveryObservation":recovered,"priorObservation":prior}
    shutil.rmtree(snapshot_root)
    return {"ok":True,"success":True,"imported":relative,"filePublished":True,"importComplete":True,
            "destinationSha256":digest(data),"guid":imported["guid"],"mainType":imported["type"],
            "importedWidth":imported.get("width"),"importedHeight":imported.get("height"),
            "partialFailure":False,"retainedPaths":[],"publication":publication}


def self_test(source_sha):
    with tempfile.TemporaryDirectory(prefix="canonical-authoring-self-test-") as directory:
        root = Path(directory)
        (root / "Assets").mkdir()
        fake = root / "validator.py"
        fake.write_text("import json; print(json.dumps({'success':True,'data':{'result':{'Ok':True}}}))\n", encoding="utf-8", newline="\n")
        base = {"projectRoot": str(root), "projectPath": "SELF_TEST_PROJECT", "unityCli": sys.executable,
                "allowEmbeddedPackages": False, "sourceSha256": source_sha}
        content = base64.b64encode(b"first\n").decode("ascii")
        request = dict(base, operation="write", writes=[{"path": "Assets/New/item.txt", "content": content}])
        original_builder = validator_argv
        globals()["validator_argv"] = lambda _request, _relative: [sys.executable, str(fake)]
        before = snapshot(root)
        created = transact(request)
        after_create = snapshot(root)
        old = root / "Assets" / "Old.txt"
        old.write_bytes(b"old\n")
        replacement = dict(base, operation="write", replaceAuthorized=True,
                           expectedSha256={"Assets/Old.txt": digest(old.read_bytes())},
                           writes=[{"path": "Assets/Old.txt", "content": base64.b64encode(b"new\n").decode("ascii")}],
                           postCommands=[[sys.executable, "-c", "raise SystemExit(7)"]])
        refused = None
        try:
            transact(replacement)
        except Refusal as error:
            refused = error.code
        keyword = None
        try:
            transact(dict(base, operation="test-template", mode="EditMode", className="class", folder="Assets/Tests/Editor"))
        except Refusal as error:
            keyword = error.code
        globals()["validator_argv"] = original_builder
        if old.read_bytes() != b"old\n" or refused != "COMMAND_FAILED" or keyword != "CSHARP_IDENTIFIER_KEYWORD":
            raise SystemExit("canonical transaction self-test failed")
        return {"Schema": "host.canonical-authoring-self-test@1", "SourceSha256": source_sha,
                "Before": before, "AfterCreate": after_create, "Create": created,
                "RollbackCode": refused, "KeywordCode": keyword, "OldTargetPreserved": True}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--request", type=Path)
    parser.add_argument("--result", type=Path)
    parser.add_argument("--self-test", action="store_true")
    parser.add_argument("--source-sha256")
    args = parser.parse_args()
    try:
        if args.self_test:
            result = self_test(args.source_sha256)
        else:
            request = json.loads(args.request.read_text(encoding="utf-8"))
            request["sourceSha256"] = args.source_sha256 or request.get("sourceSha256")
            result = transact(request)
        output = json.dumps(result, indent=2) + "\n"
        if args.result:
            args.result.write_text(output, encoding="utf-8", newline="\n")
        else:
            sys.stdout.write(output)
    except PermissionError as error:
        output = json.dumps({"Ok": False, "Error": {"Code": "PATH_ACCESS_DENIED", "Message": "The operating system denied the owned staging operation.", "Details": {"ExceptionType": type(error).__name__}}}, indent=2) + "\n"
        if args.result:
            args.result.write_text(output, encoding="utf-8", newline="\n")
        else:
            sys.stdout.write(output)
        raise SystemExit(2)
    except OSError as error:
        output = json.dumps({"Ok": False, "Error": {"Code": "PATH_IO_ERROR", "Message": "The owned staging or publication operation failed.", "Details": {"ExceptionType": type(error).__name__}}}, indent=2) + "\n"
        if args.result:
            args.result.write_text(output, encoding="utf-8", newline="\n")
        else:
            sys.stdout.write(output)
        raise SystemExit(2)
    except Refusal as error:
        output = json.dumps({"Ok": False, "Error": {"Code": error.code, "Message": str(error), "Details": error.details}}, indent=2) + "\n"
        if args.result:
            args.result.write_text(output, encoding="utf-8", newline="\n")
        else:
            sys.stdout.write(output)
        raise SystemExit(2)


if __name__ == "__main__":
    main()
