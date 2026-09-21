# Console capture, settings, and export

Read [foundation routing and safety](foundation.md) first. Pipeline capture and the live Editor Console are
different data owners. Preserve their distinct counts and never substitute one for the other.

## Capture, logging, and statistics

`console_status` establishes the current Pipeline session/cursor. Treat that cursor as the caller's follow
boundary, poll `console(reset=false)`, and stop by ending caller polling. There is no start/stop preference or
listener to allocate; stopping does not clear either buffer. Call `clear_console` only on an explicit clear
request. Use captured counts and Editor `groundTruth` from `console_status` as separate observations; do not
invent exception/assert categories.

For a bounded log request, map case-insensitive `log`, `warning`, and `error` to the corresponding
`Debug.Log*` call in a narrow `eval`; unknown type preserves the baseline log fallback. Generate only a
quoted literal, including Unicode and multiline text, and verify the unique message through the retained
session/cursor. Never interpolate message text as C# code.

## Effective Console settings

The optional `console.settings` command reads or patches `collapse`, `clearOnPlay`, and `errorPause` through
Unity's effective Console flags. No supplied nullable Boolean means read. `dryRun=true` previews and wins over
`confirm=true`; every supplied patch, including an idempotent one, otherwise requires `confirm=true`.
The typed result contains `Before`, `Proposed`, `After`, `Applied`, `DryRun`, and `Undoable=false`. The command
preserves unrelated flag bits and reads the effective flags back. Capture the original snapshot and restore
it with the same guarded route.

## Live entries and export

The optional read-only `console.entries(limit=1000)` command reads the live Editor Console in Editor index
order. Valid limits are 0 through 1000. It returns `Source="editor-console"`, `TotalAvailable`, `Count`,
`Truncated`, and ordered entries with index, full original message, integer mode, and tested `Log`, `Warning`,
or `Error` classification. It always ends the LogEntries read session in `finally`; any failed entry read is
an error with no successful prefix. Collapse state can change the Editor's visible entry set, so retain it
with the result.

Export the returned prefix as UTF-8 without BOM with one LF-terminated line per entry:
`[Log|Warning|Error] <first physical message line>`. Strip a CR only when it immediately precedes the first
stripped LF; preserve an empty first line. Limit zero creates an empty file. Report the verified project-
relative path, exported count, `editor-console` source, and import completion.

Validate the exact destination immediately before writing. `Assets/...` is the default; an existing,
project-contained embedded `Packages/<package>/...` target requires `allowEmbeddedPackages=true`.
`foundation.path.validate` reports confinement, link/reparse-point, and read-only-attribute diagnostics. It is
read-only and does not prove that the operating system will allow a later write. The transaction below checks
existing parent components without following links and proves create permission by staging in the destination
directory before it publishes any target. Existing output refuses unless replacement was explicitly authorized
and its current SHA-256 still matches immediately before atomic replacement. On failure it attempts to restore
preexisting targets and remove only files and directories the transaction created. If the OS prevents recovery,
`TRANSACTION_RECOVERY_REQUIRED` identifies retained paths and preserves any needed old-content backup;
`Committed` distinguishes cleanup after completed commands from failed publication or rollback. Import an asset destination
and wait for completion; delete only the owned export during cleanup.

## Executable host authoring transaction

Save the following block byte-for-byte as a temporary Python file and invoke it with `--request <json>`. This
is the canonical host transaction for console export, test templates, script writes, and one external asset
publication. The request names the
exact project root, Windows project path, resolved Unity CLI application, operation, and post-publication CLI
commands. Each post command is an argv array; no shell evaluates caller text. `test-template` accepts `mode`,
`className`, and `folder`. `write` accepts `writes` with project-relative `path` and Base64 `content`, plus
optional replacement authorization and an expected SHA-256 per existing target. `replaceAuthorized` and
`allowEmbeddedPackages` accept JSON booleans only; malformed values refuse before any diagnostic or write.

`asset-import` publishes one exact external byte snapshot. It requires an absolute `sourcePath`,
`destinationPath`, a
64-digit hexadecimal `expectedSourceSha256`, and `expectedDestinationState`. The destination state is either
`{"kind":"absent"}` with `replaceAuthorized=false`, or
`{"kind":"existing","sha256":"<64-digit hexadecimal SHA-256>"}` with `replaceAuthorized=true`. The
operation does not accept `writes` or `content`. It opens and reads the source once, verifies that snapshot,
then passes the same bytes through this transaction's normal confined staging and recovery. It checks the
approved destination state before staging and immediately before publication; appearance, disappearance, or
hash change refuses. The result retains `Writes` and adds `AssetImport` with the source path, destination path,
and verified source and destination hashes. This host result proves file publication only. A caller that needs
a Unity asset must separately run and verify the AssetDatabase import lifecycle; this transaction does not
report import completion or an asset GUID.

```python
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


def transact(request, source_reader=None, before_publish=None):
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
        argv = validator_argv(request, relative)
        invocations.append({"Argv": argv, "Result": run_checked(argv)})

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
            else:
                os.link(stage, target)
                published.append(target)
                stage.unlink()
        for argv in request.get("postCommands", []):
            if not isinstance(argv, list) or not argv or not all(isinstance(value, str) for value in argv):
                raise Refusal("POST_COMMAND_INVALID", "Each post command must be a nonempty argv string array.")
            invocations.append({"Argv": argv, "Result": run_checked(argv)})
        committed = True
    except BaseException as error:
        failure = error
        for target in reversed(published):
            backup = next((item for item in backups if item[0] == target), None)
            try:
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
```

## Defines and recompilation

Use [project defines](project.md#scripting-defines) for exact-group define reads/replacement. For a forced
reload, call `recompile(focus=false)`, then poll `recompile_status`; `up_to_date` is terminal, otherwise require
completed status, `failed:false`, empty errors, and `compilationFailed:false`. A request acknowledgement is
not compilation completion.
