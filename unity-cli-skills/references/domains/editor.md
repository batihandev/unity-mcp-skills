# Editor state and control

Read [foundation routing and safety](foundation.md) first and discover each command against the exact project.
A successful request is not proof of the resulting Editor state; read it back with `editor_status`,
`get_selection`, hierarchy, or the named postcondition owner.

## State and context

Use `editor_status` for play mode (`stopped`, `playing`, or `paused`), compilation/reload state, project, and
Unity version. A bounded read-only `eval` may add `EditorApplication.timeSinceStartup` and
`Application.platform`. Use `get_selection`, `list_open_scenes`, and `get_scene_hierarchy` for the complete
objects they own. The optional `editor.context.details` command supplies only focused-window class or `None`,
selected GameObject tag/layer/active state, and selected-asset folder status. Exact IDs are strings. Multiple
requests are observations at different instants, not one atomic snapshot. Components and direct children are
optional projections from hierarchy data.

Read tags with `get_tags_layers.values.tags`. For layers, inspect indices 0 through 31 with the public
`LayerMask.LayerToName` API, omit blank names, and retain increasing index order.

## Exact selection and ping

For a saved scene object, require its complete `GlobalObjectId` and expected hierarchy path. Parse the ID, resolve it with
`GlobalObjectId.GlobalObjectIdentifierToObjectSlow`, and return `GetInstanceID()` as the exact unsigned-decimal
string representation. Compare the resolved global ID and full hierarchy path with both caller observations
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
