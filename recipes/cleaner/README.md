# Cleaner Recipes

Recipes for the `unity-cleaner` skill module. Each file corresponds to one command.

## Commands

| Command | Description |
|---------|-------------|
| [cleaner_find_unused_assets](cleaner_find_unused_assets.md) | Find potentially unused assets of a specific type |
| [cleaner_find_duplicates](cleaner_find_duplicates.md) | Find duplicate files by content hash |
| [cleaner_find_missing_references](cleaner_find_missing_references.md) | Find components with missing scripts or null references |
| [cleaner_delete_assets](cleaner_delete_assets.md) | Delete assets using two-step confirmation (preview + confirm) |
| [cleaner_get_asset_usage](cleaner_get_asset_usage.md) | Find what assets reference a specific asset |
| [cleaner_find_empty_folders](cleaner_find_empty_folders.md) | Find empty folders in the project |
| [cleaner_find_large_assets](cleaner_find_large_assets.md) | Find largest assets by file size |
| [cleaner_delete_empty_folders](cleaner_delete_empty_folders.md) | Delete all empty folders |
| [cleaner_fix_missing_scripts](cleaner_fix_missing_scripts.md) | Remove missing script components from GameObjects |
| [cleaner_get_dependency_tree](cleaner_get_dependency_tree.md) | Get dependency tree for an asset |

## Safety Notes

- All analysis commands (`cleaner_find_*`, `cleaner_get_*`) are read-only.
- `cleaner_delete_assets` requires a two-step confirm flow — tokens expire after 5 minutes.
- `cleaner_delete_empty_folders` and `cleaner_fix_missing_scripts` track workflow state.
- To delete found assets, use `cleaner_delete_assets`. Never use `cleaner_delete` (does not exist).

## Skill reference

See `skills/cleaner/SKILL.md` for parameter tables and routing rules.
