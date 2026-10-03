# Editor session lifecycle

Reader: an operator who must identify, open, recover, or close one Unity project without affecting another Editor or losing unsaved work.

Run the dependency-free host helper from the installed skill:

```bash
python scripts/unity_session.py discover --project /path/to/Project
python scripts/unity_session.py open --project /path/to/Project --timeout 180
python scripts/unity_session.py ready --project /path/to/Project --timeout 180
python scripts/unity_session.py close --project /path/to/Project --timeout 60
python scripts/unity_session.py restart --project /path/to/Project --timeout 180
python scripts/unity_session.py recover --project /path/to/Project --timeout 30
```

Every invocation emits one JSON document. A refusal exits nonzero and places a stable code under
`error.code`. Supply `--cli PATH` when discovery cannot find the executable. `open` and `restart` read
`ProjectSettings/ProjectVersion.txt`; `--editor-version VERSION` overrides that value. Every poll uses the
`--timeout SECONDS` wall-clock bound.
For a headless Editor that still uses graphics initialization, pass `--batch-mode` to `open` or
`restart`; this forwards Unity's `-batchmode` argument through native `unity open --args`.

## Identity and discovery

The helper combines the CLI registry with an independent operating-system process inventory. A session is
identified by canonical project path, process ID, and process start identity. Duplicate registry rows are
collapsed. Stale rows do not prove that an Editor exists, and an empty CLI status response does not prove that
one is absent. If the OS inventory is unavailable, lifecycle mutations refuse.

The process scanner excludes CLI authentication brokers and classifies Asset Import workers separately from
the primary Editor. It extracts only the project and identity fields needed for targeting; raw Unity command
lines are never returned because Hub launch arguments can contain credentials.

`open` is idempotent. Any primary Editor process for the exact project blocks a second launch even when
Pipeline is starting, blocked, or unavailable. A new launch starts native `unity open` in a detached process
with private diagnostic output and a unique Editor `-logFile`. The helper reports `running` only after an
independent OS inventory finds one exact project Editor with PID and start identity. It does not wait for the
CLI launcher to exit; some CLI versions stay alive until the Editor exits. Use `ready` afterward when the next
action requires Pipeline.

CLI beta.12 emits a structured success result for native `unity open`; the helper still verifies the
Editor through the OS inventory. Its `ready` action checks the exact project, PID, and start identity
inside that Editor, then reads `editor_status` for compilation, reload, and play-mode state. Native
`unity status --until-ready --timeout SECONDS --project-path PROJECT` is available for a CLI readiness
wait, but does not replace the helper's identity checks. Native status and Pipeline listings can also
include Multiplayer Play Mode virtual-player rows; a status row alone does not identify the owned
primary Editor.

Cooperating helper invocations serialize each project's open handoff. If no exact Editor appears before the
deadline, the helper retains a private pending marker and diagnostic path and refuses another launch as
`OPEN_PENDING` until the target is observed. Inspect the reported marker and launcher output before clearing
an unresolved launch manually. This lock does not control Editors started by other applications.

## Close and restart

`close` queries the selected Editor's open scenes first. Any dirty scene refuses the operation and reports the
scene metadata returned by Unity; save or discard it explicitly, then retry. Its connected request checks
the selected PID, start identity, project, dirty scenes, prefab stage, and loaded project or package assets.
The single-shot Editor update callback unsubscribes before repeating those checks and expires at the action
deadline; newly dirty work or a delayed callback keeps the Editor open. The helper reports `closed` only after the original PID and start identity
disappear from the OS inventory and no replacement Editor claims the project.

`restart` uses the same close contract and does not launch a replacement until that exit verification passes.
If the close request fails, the process remains, or inventory becomes unknown, restart refuses before open.

## Recovery

`recover` reads filtered compile-error and modal signals from the Editor log named by its `-logFile` launch
argument, or from standard Editor log locations, without requiring a
responsive Pipeline. On Windows, including a Windows Editor reached from WSL, it inspects dialogs owned by the
exact Editor PID. It cancels only one visible dialog titled `Scene(s) Have Been Modified` with one exact
`Cancel` child, then checks readiness again. Cancel preserves the dirty scene. Other dialogs, missing or
ambiguous buttons, changed process identity, and changed dialog identity are reported without input.
Logs from a default per-user location have `attribution: unverified` because another Editor may write the
same file. A log selected from the target process's `-logFile` launch argument has
`attribution: launch-argument`.

Recovery does not close the Editor by default. If diagnosis and safe modal cancellation do not restore
readiness, the result names `--graceful-close` as an explicit next action:

```bash
python scripts/unity_session.py recover --project /path/to/Project --graceful-close --timeout 60
```

That option rechecks PID, start identity, and project before requesting a normal window close. Windows uses
`CloseMainWindow`; macOS requests normal termination through a PID-bound `NSRunningApplication` instance
after checking its native process start identity. Linux requires an
X11 window with one exact `_NET_WM_PID` match and uses `_NET_CLOSE_WINDOW` through `wmctrl`. Linux Wayland,
headless sessions, missing desktop tools, and ambiguous windows return `GRACEFUL_CLOSE_UNAVAILABLE` with an
actionable reason. The helper never substitutes `kill`, `taskkill`, `SIGTERM`, or a process-name-wide close.
Even after a supported request, success requires independent absence of the original process identity.

## Platform boundaries

Native Windows, macOS, and Linux keep project paths in their native form. WSL discovers the Windows CLI with
PowerShell and converts the project path only when invoking a Windows `.exe`; a native Linux CLI receives the
Linux path unchanged. Supply an explicit CLI path if automatic discovery selects the wrong installation.

PowerShell modal inspection and Cancel were live-checked against a disposable Windows Editor reached from WSL;
the exact process identity and dirty scene survived Cancel. Native Windows-host invocation has offline coverage
only. macOS and Linux modal inspection return an explicit unsupported result. The macOS native process-argument
parser and Linux X11 refusal path have offline tests; their window-close behavior still requires live acceptance
on each desktop environment. Linux cannot claim graceful window close support when the display server does not
expose a unique PID-bound window.

## Offline test lifecycle

Before an offline test run, preserve or explicitly revert authored work, then close
with this lifecycle helper and verify the exact project is stopped. Run the public
[test workflow](test.md) with its offline route. Reopen with the lifecycle helper
when continued Editor work is needed and verify readiness before the next command.
A dirty-scene refusal requires the project owner's save or revert decision.

## Bind readiness and close to an owned Editor

Pass `--expected-pid <pid> --expected-started-at <startedAt>` together on
`unity_session.py ready` or `close` to require the exact identity returned by the
owned `open`. The PID must be positive; retain the native nonempty `startedAt`
string verbatim. The expected project is the canonical `--project`. A mismatch
returns `PROCESS_IDENTITY_CHANGED` before readiness, dirty-scene inspection or
close requests. Clean-state checks and verified exit remain required. These
options apply only to `ready` and `close`; omitting both preserves normal
current-project behavior. Keep the owned identity through failure cleanup and
preserve dirty Editors when close refuses.
