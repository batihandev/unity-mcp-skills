# package_install

Install a package.

> **Retired 2026-04-21 — use the native Unity MCP tool instead.**
>
> This recipe duplicated functionality provided by a first-class Unity MCP tool.
> The file is preserved as a redirect so existing links and agents still land
> on a correct pointer.

## Use this instead

**MCP tool:** `Unity_PackageManager_ExecuteAction`

Example payload:

```json
{ "action": "Add", "packageID": "com.unity.cinemachine", "version": "3.1.4" }
```

See `mcp-tools.md` in the repo root for the full MCP tool surface.
