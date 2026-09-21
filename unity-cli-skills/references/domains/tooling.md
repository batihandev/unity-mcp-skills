# Persistent Editor tooling

Persistent tools are project code, not one-shot eval bodies. Keep the legacy tooling entrypoint until the
later test/UI tooling slice migrates its current consumers.

Before adding a tool, search the project and canonical references for an existing owner. Author one exact
Editor script through the [script transaction](script.md#create-and-delete), wait for terminal compilation,
discover its exact `Tools/...` menu item, preview menu availability, and invoke it only under the
[menu safety contract](editor.md#menu-play-state-undo-and-redo). Verify a target-state postcondition rather
than the acknowledgement. Uninstall only the exact owned script with hash/confirmation, wait for compilation,
and prove both the menu item and owned artifacts are absent.

Test execution uses [the canonical test workflow](test.md), including clean-scene preflight and exact reports.
UI tooling remains in its later consumer slice. Do not create aliases, permanent callbacks, caches, or a
second test runner while those migrations are staged.
