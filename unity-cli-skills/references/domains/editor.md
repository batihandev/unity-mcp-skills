# Editor state and control

Read [foundation routing and safety](foundation.md) first and discover each command against the exact project.
A successful request is not proof of the resulting Editor state; read it back with `editor_status`,
`get_selection`, hierarchy, or the named postcondition owner.

## State and context

Use `editor_status` for play mode (`stopped`, `playing`, or `paused`), compilation/reload state, project, and
Unity version. A bounded read-only `eval` may add `EditorApplication.timeSinceStartup` and
`Application.platform`. Use `get_selection`, `list_open_scenes`, and `get_scene_hierarchy` for selected
GameObject name/path, selected-asset GUID/path/main type, and active scene name/path/dirty state. Discover
the exact response schemas before joining those fields.

The optional `editor.context.details` command returns `FocusedWindow` (window class name or `None`),
`SelectedGameObjects`, and `SelectedAssets`. Each GameObject has exact string `InstanceId`, optional
`GlobalObjectId`, name, tag, layer index/name, `ActiveSelf`, and `ActiveInHierarchy`; each asset has GUID,
path, exact string `InstanceId`, and `IsFolder`. Its `includeComponents=false` and
`includeChildren=false` arguments independently add component type-name arrays and direct child
`{Name, InstanceId}` arrays. Missing-script null slots are omitted from component names. Direct children
include inactive children, preserve sibling order, and exclude descendants. Requested empty lists serialize
as arrays; unrequested lists are omitted. The command reads selection without modifying objects or dirty state.

Join GameObject observations by exact unsigned decimal string IDs or complete global IDs, and asset
observations by exact GUID plus path. Preserve the native path/type and active-scene fields with their source
observations. A duplicate name never identifies an object. Refuse a missing identity or inconsistent expected
name/path; reobserve when the selection, path, or scene changes during composition. Retain each observation's
source and time and label the combined result non-atomic: these calls observe different instants.

## Runtime-only command discovery

Some commands are hosted by the running application. First read and retain get_runtime_pipeline_settings.
For an explicitly authorized development build, enable the runtime with settings such as
{"enableInBuilds":true,"port":0,"requestTimeoutMs":30000,"enableAuditLogging":true,"autoStart":true,"maxWorkItemsPerFrame":10},
then read the settings back. Pass --runtime-path with the directory containing .unity-pipeline-runtime-port
when discovering or invoking runtime commands; runtime_status can confirm that route is ready. Restore the
captured settings when the runtime work is finished.

Check the nested command result's Success value, then verify the application-owned postcondition through its
authoritative state. A successful CLI envelope alone confirms only that the runtime command returned.

Simulated input can be acknowledged without reaching an InputAction callback. When that happens, use an
application-owned typed development action that calls the same domain operation as the callback, then verify
the exact target and changed state. Keep this seam in the project and scoped to development builds. Do not
publish test-specific actions as general Unity CLI commands, or treat one injection failure as proof that
runtime input is universally unavailable.

Read tags with `get_tags_layers.values.tags`. For layers, inspect indices 0 through 31 with the public
`LayerMask.LayerToName` API, omit blank names, and retain increasing index order.

## Exact selection and ping

For a saved scene object, require its complete `GlobalObjectId` and expected hierarchy path. Parse the ID, resolve it with
`GlobalObjectId.GlobalObjectIdentifierToObjectSlow`, and return `EntityId.ToULong(obj.GetEntityId()).ToString()`
as the exact unsigned-decimal string representation. Compare the resolved global ID and full hierarchy path with both caller observations
using exact ordinal equality before any selection mutation; refuse a mismatch with selection unchanged.
Recheck immediately, pass only that string to native `set_selection`,
and verify the selected global ID and hierarchy path. Never transport the rounded JSON number.

For an unsaved object in Unity 6, require the exact loaded `Scene.handle` value serialized with the public
`SceneHandle.ToString()` representation, an integer sibling-index path from scene root through every child,
and an expected-name array of equal length. Enumerate loaded scenes and compare that handle string exactly;
do not coerce it through a JSON number. Validate the handle, loaded state, every
index, and every exact ordinal name. Return the scene/path/name and exact unsigned ID string; refuse changed,
missing, stale, or ambiguous observations. Duplicate names never permit first-match selection. Asset
selection uses exact project paths; clearing uses the native setter's empty selection form.

After exact readback, ping the already-resolved object with a narrow `EditorGUIUtility.PingObject` eval when
the native setter has not visibly pinged it. Resolve the same ID again, revalidate the saved expected path or
the complete unsaved observation, and refuse a stale object; do not
repeat a name search.

## Menu, play state, Undo, and Redo

Call `menu` without a path to preview availability. Classify the exact menu action before invoking it.
Read-only UI inspection may execute. A reversible mutation requires existing caller authorization or an
explicit confirmation, a named expected postcondition, its Undo/inverse behavior, clean-scene/save handling,
and cleanup. Refuse unknown, destructive, overwriting, scene-replacing, and package-changing actions through
the generic route; use their guarded domain owners. A missing/disabled menu path or acknowledgement without
the postcondition is failure.

Use the exact discovered `Edit/Undo` and `Edit/Redo` menu paths for one owned Undo-recorded state change.
Verify the target state after each step and finish with the owned state removed. Preserve no-op availability
as a distinct case.

Before `editor_play` or connected tests, inspect every open scene. Save authorized owned changes or refuse;
never trigger a batch-mode save dialog. Observe `editor_status` until playing. `editor_pause` is applicable
only while playing; verify paused and unpaused states. Always call `editor_stop`, observe stopped state, and
disclose that unpersisted PlayMode changes are lost. Repeated play/stop acknowledgements remain distinct from
the observed state.

## Compile freshness gate

Use `python3 scripts/unity_workflow.py compile --project PATH --source FILE` for a native compile gate.
Pass every intended project source file with a repeated `--source`; the default is deliberately explicit and
does not guess a glob or hash `Library`. The workflow hashes those files, serializes cooperating workflows
with the shared project lock, verifies the exact Editor identity and stopped/ready state, calls native
`recompile`, then polls `recompile_status`. It accepts only a successful trigger followed by a fresh terminal
status-file rewrite, matching clean native status, unchanged source hashes, fresh Editor ground truth, and a
ready/stopped final Editor readback. `up_to_date` is a successful native no-op and reports `compiled: false`;
pass `--require-compiled` when the caller requires `compiling` followed by `completed`.

Use `--sources-file /path/to/sources.json` for a plain UTF-8 JSON array of explicit source paths,
for example `["Assets/Runtime/Player.cs", "Assets/Editor/Build tools.cs"]`. The list is bounded to
one MiB and can be combined with repeated `--source`. Paths still pass the same confinement,
existence, duplicate and hash checks. Keep deleted paths out of a compile input set; a Git rename
needs the current path. Preserve spaces when deriving a reviewed list from version control.

`--output /path/to/new-report.json` preserves the final compile verdict, source hashes, native
observations, wall time and last phase on refusal. Its existing parent must be a plain directory
outside the project's data trees; an existing report is never replaced. `--format summary`
emits concise JSON on stdout while a requested saved report keeps full evidence. The default
stdout format remains full JSON. A publication refusal retains the original workflow cause
and available evidence in the full response; it does not claim an artifact was saved.

The native status file has no run identifier. A changed pre/post file snapshot binds its result to the current
trigger only for cooperating workflow callers; manual edits or another uncooperative Editor/CLI process can
still race it. If file freshness or exact Editor identity cannot be established, rerun the gate after resolving
the competing operation. Compilation while playing is refused: a clean compile result does not prove that
runtime state survived the reload. In particular, serialized and nonserialized gameplay state can differ after
recompilation; stop and reenter PlayMode or initialize and read back the project-owned runtime state explicitly.

Treat compile flags and runtime diagnostics as separate observations. A live runtime exception can be present
while `compilationFailed` is false; an exception burst alone does not establish compiler failure, CLI
starvation, or Library corruption. Attribute entries with their Editor process and timestamps before choosing
a recovery action. Use the bounded session log and graceful-close routes; do not force-kill the Editor.

For Play transitions, inspect the actual domain- and scene-reload settings and read back the project's
initialization postconditions in PlayMode. A builder-to-Play transition and a save/reopen-to-Play transition
do not imply a universal need to reopen the project. When gameplay state needs a reset, verify the
project-owned initialization or stop and reenter PlayMode, then read the state back.
