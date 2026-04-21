---
name: unity-skills-index
description: "Index of all Unity domain modules. Browse available capabilities and find the right module to reference for RunCommand recipes or dedicated MCP tools."
---

# Unity Skills — Module Index

This index lists all available domain modules for interacting with the Unity Editor. Before picking a tool, follow the routing order defined in [`../mcp-tools.md`](../mcp-tools.md):

1. **Native MCP tool** — use the dedicated tool if one exists for the operation.
2. **Topic skill + exact topic recipe** — load `skills/<topic>/SKILL.md` and find a matching recipe.
3. **Closest recipe** — pick the nearest recipe as a template if no exact match exists.
4. **`references/<topic>.md`** — tertiary docs fallback; consult only when skills and recipes lack the domain detail.
5. **Fresh `Unity_RunCommand`** — last resort when no recipe covers the case.

> **MCP Tool Routing**: Pay attention to the **Tool Route** column. If a module lists a dedicated MCP tool (e.g. `Unity_ReadConsole`), use that tool directly. If it says `Unity_RunCommand`, use that tool and pull C# examples from the `recipes/` directory.

## Functional Modules

| Module | Description | Tool Route |
|--------|-------------|------------|
| [gameobject](./gameobject/SKILL.md) | Object create/move/parent | `Unity_RunCommand` |
| [component](./component/SKILL.md) | Component add/remove/configure | `Unity_RunCommand` |
| [material](./material/SKILL.md) | Material property edits | `Unity_RunCommand` |
| [light](./light/SKILL.md) | Light create/configure | `Unity_RunCommand` |
| [prefab](./prefab/SKILL.md) | Prefab create/apply/spawn | `Unity_RunCommand` |
| [asset](./asset/SKILL.md) | Asset refresh/find/info | `Unity_RunCommand` |
| [ui](./ui/SKILL.md) | UGUI Canvas/UI creation | `Unity_RunCommand` |
| [uitoolkit](./uitoolkit/SKILL.md) | UXML/USS/UIDocument | `Unity_RunCommand` |
| [script](./script/SKILL.md) | Script edit/validate/SHA | `Unity_ScriptApplyEdits` |
| [scene](./scene/SKILL.md) | Scene load/save/query | `Unity_RunCommand` |
| [editor](./editor/SKILL.md) | Play/select/undo/redo | `Unity_RunCommand` |
| [animator](./animator/SKILL.md) | Animator controllers | `Unity_RunCommand` |
| [shader](./shader/SKILL.md) | Shader create/list | `Unity_RunCommand` |
| [console](./console/SKILL.md) | Log capture/debug | `Unity_ReadConsole` |
| [validation](./validation/SKILL.md) | Broken reference checks | `Unity_RunCommand` |
| [importer](./importer/SKILL.md) | Texture/audio/model import | `Unity_RunCommand` |
| [cinemachine](./cinemachine/SKILL.md) | VCam operations | `Unity_RunCommand` |
| [probuilder](./probuilder/SKILL.md) | ProBuilder mesh edits | `Unity_RunCommand` |
| [xr](./xr/SKILL.md) | XRI setup | `Unity_RunCommand` |
| [terrain](./terrain/SKILL.md) | Terrain create/paint | `Unity_RunCommand` |
| [physics](./physics/SKILL.md) | Raycast/overlap/gravity | `Unity_RunCommand` |
| [navmesh](./navmesh/SKILL.md) | NavMesh bake/query | `Unity_RunCommand` |
| [timeline](./timeline/SKILL.md) | Timeline tracks/clips | `Unity_RunCommand` |
| [cleaner](./cleaner/SKILL.md) | Unused/duplicate assets | `Unity_RunCommand` |
| [smart](./smart/SKILL.md) | Query/layout/auto-bind | `Unity_RunCommand` |
| [perception](./perception/SKILL.md) | Scene/project analysis | `Unity_RunCommand` |
| [camera](./camera/SKILL.md) | Scene View camera | `Unity_Camera_Capture` |
| [event](./event/SKILL.md) | UnityEvent wiring | `Unity_RunCommand` |
| [package](./package/SKILL.md) | UPM install/query | `Unity_PackageManager_ExecuteAction` / `Unity_PackageManager_GetData` |
| [project](./project/SKILL.md) | Project info/settings | `Unity_GetProjectData` |
| [optimization](./optimization/SKILL.md) | Asset optimization | `Unity_RunCommand` |
| [sample](./sample/SKILL.md) | Demo/test skills | `Unity_RunCommand` |
| [test](./test/SKILL.md) | Unity Test Runner | `Unity_RunCommand` |
| [scriptableobject](./scriptableobject/SKILL.md) | ScriptableObject assets | `Unity_RunCommand` |

## Recipe Path Rule

- Recipe path rule: `../../recipes/<topic>/<command>.md`
- command filenames must match skill command IDs exactly
