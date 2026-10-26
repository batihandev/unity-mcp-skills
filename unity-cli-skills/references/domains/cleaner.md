# Cleaner reports and recoverable cleanup

Read [foundation](foundation.md), [assets](asset.md), and [editor Undo](editor.md) before invoking a route. Discover the selected project's live schema for every command used. Reports inspect ordinary loaded scenes or confined asset directories; filesystem scans permit the exact `Assets` root for reading, refuse links before traversal, and exclude `.meta` bytes. Reports do not establish runtime or Addressables unuse.

## Operation map

| Requested operation | Canonical owner and defaults |
| --- | --- |
| Find unused assets | `analysis.assets-unused`: `assetType=Material`, `searchPath=Assets`, `limit=100`; dependency sources remain inside the selected directory; Resources sources and candidates are excluded. |
| Find duplicate files | `analysis.assets-duplicates`: `assetType=Texture2D`, `searchPath=Assets`, `limit=50` groups; size/SHA256 candidates are confirmed by exact byte equality. |
| Find missing scripts and references | `analysis.scene-missing`: `includeInactive=true`; visible ordinary objects across all loaded scenes, every unresolved script and serialized object reference; legitimate null values are excluded. |
| Remove selected assets | Confirmed ordered `asset.trash` calls after complete preview and external recovery capture below. |
| Get asset usage | `analysis.asset-usage`: required exact existing `assetPath`, `limit=50`; direct reverse references inside Assets, total before cap. |
| Find empty folders | `analysis.asset-folders`: `searchPath=Assets`; ordinal siblings with postorder append, empty leaves and recursively empty parents. |
| Find large assets | `analysis.assets-large`: `searchPath=Assets`, `limit=20`, `minSizeBytes=0`, `inclusive=false`, `sort=true`; strict byte threshold, largest first. |
| Remove empty folders | `analysis.asset-folders`, exclude `Root=true`, then deepest-first confirmed `asset.trash`; restore ancestors before descendants. |
| Fix missing scripts | `validation.missing-scripts-fix`: `includeInactive=true`, `dryRun=true`; scene-only, explicit confirmation and one Undo group. |
| Get dependency tree | `analysis.asset-dependencies`: exact existing file or folder `assetPath`, `recursive=true`; source excluded. |

Missing reports and fixes use visible hierarchy objects by default (`includeInactive=true`). `includeInactive=false` uses native active GameObject discovery, including native-eligible hidden objects. The missing report supports `includeHidden=true` with inactive hierarchy collection for complete Perception/Context source graphs. Persistent assets, preview scenes and prefab-stage objects are excluded from scene scans.

The typed report is `data.result`, with `Ok`, `Result`, and `Error`. Asset-list reports expose `Assets`, `Total`, and `Truncated`; usage and dependency reports also echo the selected `Source` identity. Every imported row retains `Path`, `Guid`, `Type`, and raw `SizeBytes`. Duplicate reports cap groups and expose full `TotalWastedBytes`. Material equivalence belongs to [optimization](optimization.md); it does not imply equal files.

```bash
unity --json command --project-path "$PROJECT_PATH" --query analysis.assets-unused --detail full
unity --json command --project-path "$PROJECT_PATH" analysis.assets-unused -- \
  --searchPath Assets --assetType Material --limit 100
unity --json command --project-path "$PROJECT_PATH" analysis.asset-usage -- \
  --assetPath Assets/Materials/Rock.mat --limit 50
unity --json command --project-path "$PROJECT_PATH" analysis.assets-large -- \
  --searchPath Assets --minSizeBytes 1048575 --limit 20
unity --json command --project-path "$PROJECT_PATH" analysis.scene-missing
unity --json command --project-path "$PROJECT_PATH" validation.missing-scripts-fix -- \
  --includeInactive true --dryRun true
```

Review missing-script preview before invoking the same route with `dryRun=false,confirm=true`. Removal changes loaded scene objects, registers full hierarchy Undo, and leaves prefab asset contents unchanged. Save the affected scene only when persistence is requested. Missing asset-reference fields require an explicit reassignment; removing a script does not repair them.

## Ordered recoverable Trash

Use exact project-relative paths. Preview every selected path before the first removal. Reject duplicate paths and any parent/child overlap in an arbitrary asset plan. For a recursively empty-folder plan, use the reported postorder, exclude the scan root, and deliberately process descendant folders before their ancestors. Capture the complete pre-removal bytes, every relevant `.meta`, and each original GUID outside the project before removing anything. Both workflows are persistent and non-Undo.

This executable host composition uses the existing command owner. Save the body as a caller-owned script, supply an initially absent external `RECOVERY_DIR`, and set `PROJECT_ROOT` to the host-accessible filesystem root, `PROJECT_PATH` to the same project in the CLI's path form. Run the saved body with the installed skill’s `scripts` directory on `PYTHONPATH` so it can reuse the canonical authoring path guard. Run with `APPLY=false` to preview. After reviewing the exact saved plan, rerun with `APPLY=true` using that same recovery directory. `MODE=assets` selects the JSON `PATHS` array; `MODE=empty-folders` collects `SEARCH_PATH` (default `Assets`). It validates the complete set before capture or mutation. A failed preview performs no removals. An operational removal failure is attributed to its path, later independent paths continue, and the process exits nonzero.

```python
import hashlib, json, os, pathlib, stat, subprocess, sys
from unity_cli.authoring import inspect_no_follow, run_checked
from unity_cli.platforms import current_platform
cli = current_platform().discover_cli()
if not cli:
    raise SystemExit("Unity CLI was not found")
project = pathlib.Path(os.environ["PROJECT_ROOT"]).absolute()
cli_project = os.environ["PROJECT_PATH"]
recovery = pathlib.Path(os.environ["RECOVERY_DIR"]).resolve()
mode = os.environ.get("MODE", "assets")
apply = os.environ.get("APPLY", "false").lower() == "true"
if recovery == project or project in recovery.parents:
    raise SystemExit("RECOVERY_DIR must be outside the project")
def command(name, **args):
    argv = [cli, "command", "--project-path", cli_project, "--format", "json", name, "--"]
    for key, value in args.items():
        argv += ["--" + key, json.dumps(value) if isinstance(value, (bool, list, dict)) else str(value)]
    reply = run_checked(argv)
    return reply["data"]["result"]
if mode == "empty-folders":
    report = command("analysis.asset-folders", searchPath=os.environ.get("SEARCH_PATH", "Assets"))["Result"]
    paths = [row["Path"] for row in report["Folders"] if not row["Root"]]
    paths.sort(key=lambda p: (-p.count("/"), p))
elif mode == "assets":
    paths = json.loads(os.environ["PATHS"])
else:
    raise SystemExit("MODE must be assets or empty-folders")
if not isinstance(paths, list) or any(not isinstance(p, str) for p in paths):
    raise SystemExit("PATHS must be an exact path array")
normalized = []
for path in paths:
    p = pathlib.PurePosixPath(path.replace("\\", "/"))
    if p.is_absolute() or ".." in p.parts or not p.parts or p.parts[0] != "Assets" or len(p.parts) < 2:
        raise SystemExit("Every removal must be an exact child path under Assets")
    normalized.append(str(p))
comparison = [path.casefold() for path in normalized]
if len(set(comparison)) != len(comparison):
    raise SystemExit("Duplicate removal paths refused")
if mode == "assets" and any(a != b and b.startswith(a + "/") for a in comparison for b in comparison):
    raise SystemExit("Overlapping parent/child removal paths refused")
def capture_entries():
    pending = [entry for path in normalized
               for entry in (project / path, pathlib.Path(str(project / path) + ".meta"))]
    files, directories = {}, set()
    while pending:
        entry = pending.pop()
        inspect_no_follow(project, project / "Assets", entry)
        relative = entry.relative_to(project).as_posix()
        if entry.is_dir():
            directories.add(relative)
            pending.extend(entry.iterdir())
        elif entry.is_file():
            files[relative] = entry
        elif entry.exists():
            raise SystemExit("Unsupported capture entry: " + relative)
    return files, directories
plan_path = recovery / "plan.json"
if apply:
    plan = json.loads(plan_path.read_text(encoding="utf-8"))
    if plan["project"] != str(project) or plan["paths"] != normalized or plan["mode"] != mode:
        raise SystemExit("Current exact plan differs from the reviewed capture")
else:
    if recovery.exists():
        raise SystemExit("Preview requires an initially absent RECOVERY_DIR")
    previews = [command("asset.trash", asset=p, dryRun=True) for p in normalized]
    recovery.mkdir(parents=True)
    entries, directories = capture_entries()
    captured = {}
    for relative, entry in entries.items():
        content = entry.read_bytes()
        digest = hashlib.sha256(content).hexdigest()
        (recovery / digest).write_bytes(content)
        captured[relative] = digest
    plan = {"project": str(project), "mode": mode, "paths": normalized,
            "previews": previews, "files": captured, "directories": sorted(directories)}
    plan_path.write_text(json.dumps(plan, indent=2), encoding="utf-8")
    print(json.dumps({"preview": True, "count": len(normalized), "plan": str(plan_path)}))
    sys.exit(0)
current_files, current_directories = capture_entries()
if set(current_files) != set(plan["files"]) or current_directories != set(plan["directories"]):
    raise SystemExit("Captured directory contents changed before removal")
for relative, digest in plan["files"].items():
    if hashlib.sha256((project / relative).read_bytes()).hexdigest() != digest:
        raise SystemExit("Captured bytes changed before removal: " + relative)
if mode == "empty-folders":
    current = command("analysis.asset-folders", searchPath=os.environ.get("SEARCH_PATH", "Assets"))["Result"]
    current_paths = sorted([row["Path"] for row in current["Folders"] if not row["Root"]], key=lambda p: (-p.count("/"), p))
    if current_paths != normalized:
        raise SystemExit("Empty-folder plan changed before removal")
for index, path in enumerate(normalized):
    current = command("asset.trash", asset=path, dryRun=True)
    if current["Result"]["Guid"] != plan["previews"][index]["Result"]["Guid"]:
        raise SystemExit("Asset GUID changed before removal: " + path)
outcomes = []
for path in normalized:
    try:
        reply = command("asset.trash", asset=path, confirm=True)
        if (project / path).exists() or pathlib.Path(str(project / path) + ".meta").exists():
            raise RuntimeError("Asset or meta remains after Trash")
        outcomes.append({"path": path, "ok": True, "reply": reply})
    except Exception as error:
        outcomes.append({"path": path, "ok": False, "error": str(error)})
result = {"attempted": len(outcomes), "removed": sum(row["ok"] for row in outcomes),
          "failed": sum(not row["ok"] for row in outcomes), "outcomes": outcomes, "recovery": str(plan_path)}
(recovery / "outcomes.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
print(json.dumps(result))
sys.exit(1 if result["failed"] else 0)
```

Before any apply, require the same complete file and directory path sets and hashes. Before applying an empty-folder plan, rescan and require the same exact folder list. The scan root never enters removal. For recovery, read `outcomes.json` and restore only successful owned removals; preserve failed paths. Create captured owned directories in ascending depth, then restore each captured file/meta only after its absent destination or unchanged captured hash is verified. Retain snapshots if a destination contains different bytes. Import restored paths in ancestor-first order through discovered `asset.reimport` (`asset`, `confirm=true`), and compare each GUID with the corresponding `previews[].Result.Guid`. Confirm exact file and meta hashes, loadable main assets, and original folder GUIDs. Never overwrite a neighbor or claim restoration from a cached GUID alone.

This restoration body uses the captured plan and outcomes. Run only when restoration is requested. It refuses differing destination bytes and validates confined paths through the same Foundation owner before writing. It verifies every captured byte and each restored asset GUID after reimport.

```python
import hashlib, json, os, pathlib, stat, subprocess
from unity_cli.authoring import run_checked
from unity_cli.platforms import current_platform
cli = current_platform().discover_cli()
if not cli:
    raise SystemExit("Unity CLI was not found")
project = pathlib.Path(os.environ["PROJECT_ROOT"]).resolve()
cli_project = os.environ["PROJECT_PATH"]
recovery = pathlib.Path(os.environ["RECOVERY_DIR"]).resolve()
plan = json.loads((recovery / "plan.json").read_text(encoding="utf-8"))
outcomes = json.loads((recovery / "outcomes.json").read_text(encoding="utf-8"))["outcomes"]
if plan["project"] != str(project):
    raise SystemExit("Recovery project differs from captured project")
owned = [row["path"] for row in outcomes if row["ok"]]
for path in owned:
    if (project / path).exists() or pathlib.Path(str(project / path) + ".meta").exists():
        raise SystemExit("Owned recovery destination was recreated; inspect it before restoring: " + path)
def command(name, **args):
    argv = [cli, "command", "--project-path", cli_project, "--format", "json", name, "--"]
    for key, value in args.items():
        argv += ["--" + key, json.dumps(value) if isinstance(value, bool) else str(value)]
    reply = run_checked(argv)
    return reply["data"]["result"]["Result"]
def belongs(path):
    return any(path == p or path == p + ".meta" or path.startswith(p + "/") for p in owned)
directories = sorted([p for p in plan["directories"] if belongs(p)], key=lambda p: (p.count("/"), p))
files = {p: h for p, h in plan["files"].items() if belongs(p)}
for path in directories + list(files):
    command("foundation.path.validate", path=path)
    destination = project / path
    if destination.is_symlink() or (destination.exists() and getattr(os.lstat(destination), "st_file_attributes", 0) & getattr(stat, "FILE_ATTRIBUTE_REPARSE_POINT", 0)):
        raise SystemExit("Linked restoration destination refused: " + path)
for path, digest in files.items():
    destination = project / path
    source = recovery / digest
    if hashlib.sha256(source.read_bytes()).hexdigest() != digest:
        raise SystemExit("Recovery snapshot hash mismatch")
    if destination.exists() and (not destination.is_file() or hashlib.sha256(destination.read_bytes()).hexdigest() != digest):
        raise SystemExit("Restoration destination changed: " + path)
for path in directories:
    (project / path).mkdir(parents=True, exist_ok=True)
for path, digest in files.items():
    destination = project / path
    destination.parent.mkdir(parents=True, exist_ok=True)
    command("foundation.path.validate", path=path)
    destination.write_bytes((recovery / digest).read_bytes())
for path in sorted(owned, key=lambda p: (p.count("/"), p)):
    restored = command("asset.reimport", asset=path, confirm=True)
    expected = plan["previews"][plan["paths"].index(path)]["Result"]["Guid"]
    if restored["AfterGuid"] != expected:
        raise SystemExit("Restored GUID mismatch: " + path)
for path, digest in files.items():
    if hashlib.sha256((project / path).read_bytes()).hexdigest() != digest:
        raise SystemExit("Restored bytes differ: " + path)
print(json.dumps({"restored": len(owned), "verifiedFiles": len(files)}))
```
