---
name: unity-package
description: "Use when installing, removing, inspecting, or refreshing Unity packages."
---

# Package Management

**Do not use `Unity_RunCommand` for package operations.** Unity's MCP server exposes first-class package tools that cover every operation in this domain directly.

## Routing

| Operation | Tool | Typical payload |
|---|---|---|
| List installed packages | `Unity_PackageManager_GetData` | `{ "installedOnly": true }` (no `packageID`) |
| Check a specific package | `Unity_PackageManager_GetData` | `{ "packageID": "com.unity.cinemachine", "installedOnly": true }` |
| List a package's versions | `Unity_PackageManager_GetData` | `{ "packageID": "com.unity.cinemachine", "installedOnly": false }` |
| List a package's dependencies | `Unity_PackageManager_GetData` | `{ "packageID": "com.unity.timeline", "installedOnly": false }` |
| Install a package | `Unity_PackageManager_ExecuteAction` | `{ "action": "Add", "packageID": "com.unity.cinemachine" }` (optional `version`) |
| Remove a package | `Unity_PackageManager_ExecuteAction` | `{ "action": "Remove", "packageID": "com.unity.splines" }` |
| Refresh / resolve | `Unity_PackageManager_ExecuteAction` | `{ "action": "Resolve" }` |
| Embed a local copy | `Unity_PackageManager_ExecuteAction` | `{ "action": "Embed", "packageID": "com.unity.cinemachine" }` |
| Import a package sample | `Unity_PackageManager_ExecuteAction` | `{ "action": "Sample", "packageID": "...", "sampleName": "..." }` |

## Notes

- Install / remove operations are asynchronous on Unity's side. If the tool returns a "pending" status, poll `Unity_PackageManager_GetData` with `installedOnly: true` a few seconds later to observe the final state.
- Package operations can trigger a Domain Reload. Expect transient server unavailability afterward; retry once.
- For older `package_*` recipe filenames under `recipes/package/`, see the tombstone files — each points here.

## Retired recipes (tombstoned, 2026-04-21)

The following recipe filenames are preserved as tombstones that point back to this skill:

- `package_check`, `package_get_cinemachine_status`, `package_get_dependencies`, `package_get_versions`, `package_list`, `package_search` → `Unity_PackageManager_GetData`
- `package_install`, `package_install_cinemachine`, `package_install_splines`, `package_remove`, `package_refresh` → `Unity_PackageManager_ExecuteAction`
