# Persistent Editor tooling

Persistent tools are project code. Use a bounded one-shot eval for an operation that does not require a
reusable installed Editor tool.

Before adding a tool, search the project and canonical references for an existing owner. Author one exact
Editor script through the [script transaction](script.md#create-and-delete), wait for terminal compilation,
discover its exact `Tools/...` menu item, preview menu availability, and invoke it only under the
[menu safety contract](editor.md#menu-play-state-undo-and-redo). Verify a target-state postcondition rather
than the acknowledgement. Uninstall only the exact owned script through hash-bound `asset.trash` under the
[script deletion and recovery contract](script.md#create-and-delete). Preserve source/meta/GUID snapshots,
wait for terminal compilation, and prove both the exact menu item and owned file/meta artifacts are absent.
A menu invocation must change its named postcondition; an available path or successful acknowledgement is
not completion. Retain exact script path/hash and menu path throughout the install/invoke/uninstall lifecycle.

Test execution uses [the canonical test workflow](test.md), including clean-scene preflight and exact reports.
UI capture uses the [UI workflow](ui.md) and its camera pixel and asset metadata recovery owners.
Keep temporary lifecycle fixtures outside shipped tooling; install persistent helpers only for a reusable
project requirement.

For camera PNGs and isolated UGUI panel screenshots, use the shipped
[host camera workflow](scene.md#screenshot-workflow) and [panel composition](ui.md#isolated-panel-captures).
Each panel is one temporary eval using the portable pixel source and complete asset metadata lifecycle.
