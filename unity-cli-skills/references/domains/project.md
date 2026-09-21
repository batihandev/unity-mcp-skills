# Project configuration and rendering

Read [foundation routing and safety](foundation.md) first and discover every built-in against the exact
project. Settings and package writes are non-Undo mutations: capture before state, use their native
dry-run/confirmation gate, read back effective state, restore it, and wait for reloads where applicable.

## Project, tags, layers, and build scenes

Compose project/product/platform/play/version data from `get_project_info`, `editor_status`, and
`get_graphics_settings`; report actual rendering state. Use `get_tags_layers.values.tags` for tags. For
layers, inspect `LayerMask.LayerToName` at exactly indices 0 through 31, omit blanks, and return ordered
`Names`, matching `Count`, and `Layers` index/name pairs. The shared editor layers workflow uses the same read.

Use native tag/layer mutations only with their discovered guard. Verify before/after, duplicate/invalid
refusal, `Undoable=false`, explicit removal, and restoration. `get_build_settings` retains scene order,
enabled state, path, target/group, development/debug flags, and the project identity.

## Player and quality settings

Report `EditorUserBuildSettings.selectedBuildTargetGroup`, `activeBuildTarget`, and the settings target used
for every field separately. Scripting backend and API compatibility belong to the selected group; read only
that missing delta through public APIs when built-in ownership differs. Do not switch the ambient target.
Resolution/fullscreen fields may use a bounded read-only eval.

Read native quality data, then add only missing shadow and LOD fields through a bounded read-only eval.
Quality mutation resolves one exact name/index, refuses conflicts and invalid values, requires dry-run or
confirmation, reads both runtime and persisted state, returns non-Undo status, and restores the original
level.

## Rendering and shaders

Read `get_graphics_settings`, including default, quality override, effective/current asset, active pipeline
instance, and actual pipeline type. Do not infer a pipeline or shader from labels.

For shader listing, request native `list_shaders` with its largest supported limit, then compare its exact
names and count with an independent read-only complete-set observation from the current Editor. Equality
establishes completeness only for that observed project state; a large requested limit or asserted boolean
alone is insufficient. Then apply, in order: case-insensitive substring filter; exact-name de-duplication;
ordinal name sort; caller limit (default 50, zero empty, negative refused). Return the post-limit `Count`,
requested filter, and each retained row's builtin/asset/support metadata. If the current complete set cannot
be established, report the concrete gap rather than sorting a potentially truncated prefix.

## Scripting defines

The optional `project.defines` command reads or fully replaces one target's semicolon string. An omitted
`defines` reads and may default to the Editor-selected group. Empty explicitly clears. Replacement requires
an exact valid explicit `group` resolved to `NamedBuildTarget`; it does not change the selected group or
active target. `dryRun=true` wins over confirmation, and every replacement otherwise needs `confirm=true`.

The typed result separates `Group`, `NamedBuildTarget`, `SelectedGroup`, and `ActiveBuildTarget`, and returns
`Before`, `Requested`, `After`, `Applied`, `DryRun`, `RequiresRecompile`, and `Undoable=false`. Preserve the
full requested string deliberately, require effective readback, then poll native `recompile_status` to
terminal success before another mutation. Restore the original define string through the same route.

## Packages

Use native `package_list` for exact installed-package metadata. For the project manifest contract, validate
the confined `Packages/manifest.json` path, read its actual UTF-8 bytes on the host, retain the raw manifest
text, and parse the complete object. Return the dependency map without dropping sibling keys such as
`scopedRegistries`, `testables`, or registry settings. The raw file remains the source of truth; never
reconstruct a full manifest from `dependencies`. Use [the package lifecycle reference](package.md) for search,
dependencies, guarded mutation, terminal package status, and manifest/readback ownership.
