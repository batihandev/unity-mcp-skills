# package_check

Check whether a specific package is installed and retrieve its version.

> **Retired 2026-04-21 — use the native Unity MCP tool instead.**
>
> This recipe duplicated functionality provided by a first-class Unity MCP tool.
> The file is preserved as a redirect so existing links and agents still land
> on a correct pointer.

## Use this instead

**MCP tool:** `Unity_PackageManager_GetData`

Example payload:

```json
{ "packageID": "com.unity.cinemachine", "installedOnly": true }
```

See `mcp-tools.md` in the repo root for the full MCP tool surface.
