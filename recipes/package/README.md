# Package Recipes

Per-command recipes for the `unity-package` skill. Each file corresponds to one command ID.

## Native Tool First

All package operations have dedicated MCP tools. Always prefer them over `Unity_RunCommand`:
- **Query operations** (`package_list`, `package_check`, `package_search`, `package_get_dependencies`, `package_get_versions`, `package_get_cinemachine_status`) — use `Unity_PackageManager_GetData`.
- **Mutating operations** (`package_install`, `package_remove`, `package_refresh`, `package_install_cinemachine`, `package_install_splines`) — use `Unity_PackageManager_ExecuteAction`.

Use `Unity_RunCommand` recipes only when you need custom logic not available through the native tools.

## Commands

| File | Command |
|------|---------|
| [package_list.md](package_list.md) | `package_list` |
| [package_check.md](package_check.md) | `package_check` |
| [package_install.md](package_install.md) | `package_install` |
| [package_remove.md](package_remove.md) | `package_remove` |
| [package_refresh.md](package_refresh.md) | `package_refresh` |
| [package_install_cinemachine.md](package_install_cinemachine.md) | `package_install_cinemachine` |
| [package_install_splines.md](package_install_splines.md) | `package_install_splines` |
| [package_get_cinemachine_status.md](package_get_cinemachine_status.md) | `package_get_cinemachine_status` |
| [package_search.md](package_search.md) | `package_search` |
| [package_get_dependencies.md](package_get_dependencies.md) | `package_get_dependencies` |
| [package_get_versions.md](package_get_versions.md) | `package_get_versions` |

## Usage

Use these templates in `Unity_RunCommand`. Recipe path rule: `../../recipes/package/<command>.md`
