# MCP Tools — Operation Routing Matrix

## Routing Order

When handling a Unity Editor task, resolve the tool to use in this order:

1. **Native MCP tool** — if a dedicated tool exists for the operation, use it directly (no `RunCommand`).
2. **Topic skill + exact topic recipe** — load `skills/<topic>/SKILL.md` and find a matching recipe in `recipes/<topic>/`.
3. **Closest recipe** — if no exact match, pick the nearest recipe as a template and adapt it.
4. **`references/<topic>.md`** — tertiary docs fallback; consult only when the skill and recipes lack the domain detail needed.
5. **Fresh `Unity_RunCommand`** — last resort; write a new command only when no recipe covers the case.

---

## Dedicated MCP Tools

Use these tools directly. Do **not** reach for `Unity_RunCommand` when a dedicated tool covers the domain.

| Domain | Tool | Notes |
|--------|------|-------|
| Console / logs | `Unity_ReadConsole` | Read or clear console entries with filtering, formatting, and stack trace support |
| Console / logs | `Unity_GetConsoleLogs` | Fetch Unity console log entries and stack traces in a simpler log-focused shape |
| Script creation | `Unity_CreateScript` | Create a new C# script under `Assets/` |
| Script deletion | `Unity_DeleteScript` | Delete a C# script by URI or `Assets/...` path |
| Script search | `Unity_FindInFile` | Regex search within a script file with line-level matches |
| Script discovery | `Unity_ListResources` | List project resources, commonly `*.cs` under `Assets/` |
| Script edits | `Unity_ScriptApplyEdits` | Structured method-level C# edits |
| Script validation | `Unity_ValidateScript` | Syntax and diagnostic checks |
| Script SHA / metadata | `Unity_GetSha` | Hash check before editing |
| Scene view (3D multi-angle) | `Unity_SceneView_CaptureMultiAngleSceneView` | 2×2 isometric/front/top/right grid |
| Scene view (2D region) | `Unity_SceneView_Capture2DScene` | Orthographic crop by world coords |
| Camera capture | `Unity_Camera_Capture` | Render from a specific Camera component |
| Arbitrary editor code | `Unity_RunCommand` | Only when no dedicated tool applies |
| Package management | `Unity_PackageManager_ExecuteAction` | Add / Remove / Embed / Sample |
| Package info | `Unity_PackageManager_GetData` | Inspect installed packages |
| Project overview | `Unity_GetProjectData` | Structure, naming conventions, taxonomy |
| Project guidelines | `Unity_GetUserGuidelines` | Coding style, folder conventions, and Unity-specific standards; load before editing any file |
| Asset search | `Unity_FindProjectAssets` | Name + semantic visual search |
| Import external model | `Unity_ImportExternalModel` | FBX URL → prefab in scene |
| Profiler — frame top time | `Unity_Profiler_GetFrameTopTimeSam_ccc85b2d` | Hottest samples by total time |
| Profiler — frame self time | `Unity_Profiler_GetFrameSelfTimeSa_e44ee448` | Hottest samples by self time |
| Profiler — frame GC allocs | `Unity_Profiler_GetFrameGcAllocati_a7eb5b61` | Top allocations per frame |
| Profiler — range time | `Unity_Profiler_GetFrameRangeTopTimeSummary` | Multi-frame time summary |
| Profiler — range GC allocs | `Unity_Profiler_GetFrameRangeGcAll_90f409da` | Multi-frame allocation summary |
| Profiler — overall GC | `Unity_Profiler_GetOverallGcAlloca_ac50c101` | Full-session allocation overview |
| Profiler — sample time | `Unity_Profiler_GetSampleTimeSummary` | Single sample by ID |
| Profiler — sample GC | `Unity_Profiler_GetSampleGcAllocat_4a279ae5` | GC detail for one sample |
| Profiler — sample GC (by marker path) | `Unity_Profiler_GetSampleGcAllocat_89f626bb` | GC detail for one sample located by marker ID path |
| Profiler — sample time (by marker path) | `Unity_Profiler_GetSampleTimeSumma_a680062a` | Time summary for one sample located by marker ID path |
| Profiler — bottom-up sample time | `Unity_Profiler_GetBottomUpSampleT_55cc1e4e` | Bottom-up timing summary for a selected sample |
| Profiler — related threads | `Unity_Profiler_GetRelatedSamplesT_a6086ba0` | Cross-thread sample correlation |
| Asset generation | `Unity_AssetGeneration_GenerateAsset` | Generate sprites, images, spritesheets, meshes, sounds, materials, terrain layers, and cubemaps |
| Animation clip convert | `Unity_AssetGeneration_ConvertSpri_dca62520` | Sprite sheet → AnimationClip |
| Material convert | `Unity_AssetGeneration_ConvertToMaterial` | Texture → Material |
| Terrain layer convert | `Unity_AssetGeneration_ConvertToTe_debf7698` | Texture → TerrainLayer |
| Animator controller | `Unity_AssetGeneration_CreateAnima_40e1a9ab` | AnimationClip → AnimatorController |
| Animation edit | `Unity_AssetGeneration_EditAnimati_47017090` | Make stationary / trim to loop |
| Audio clip edit | `Unity_AudioClip_Edit` | Trim / volume / loop |
| Composition patterns | `Unity_AssetGeneration_GetComposit_832d2c69` | Available material/terrain patterns |
| Generation models | `Unity_AssetGeneration_GetModels` | List available AI model IDs |
| Interrupted generations | `Unity_AssetGeneration_ManageInterrupted` | List / Resume / Discard |
