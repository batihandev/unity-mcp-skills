# MCP Recipe Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Clean up and harden the first-wave MCP skill docs and recipe files, introduce `references/<topic>.md` as a tertiary documentation fallback, remove obsolete `_REFERENCE.md` sidecars, and make recipe-derived `Unity_RunCommand` examples trustworthy starting points again.

**Architecture:** The work is documentation-first but source-grounded. `mcp-tools.md` becomes the shared routing source of truth for currently available official Unity MCP tools, topic `SKILL.md` files are refit to better match `writing-skills` structure guidance, exact recipe files remain the primary working path when no native tool exists, vetted `references/*.md` links become a tertiary Unity-doc fallback only after a relevance check, and first-wave `recipes/*.md` are repaired by comparing them against the original upstream C# implementations instead of preserving broken extractor output.

**Tech Stack:** Markdown, shell verification with `rg`, git, upstream source at `/tmp/original-unity-skills`, Unity-oriented C# snippets documented for `Unity_RunCommand`.

---

### Task 1: Baseline Upstream Source and Current Drift

**Files:**
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/AssetSkills.cs`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/AssetImportSkills.cs`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/UISkills.cs`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/UIToolkitSkills.cs`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/ProBuilderSkills.cs`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/XRSkills.cs`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/XRReflectionHelper.cs`
- Modify later: `mcp-tools.md`, `skills/SKILL.md`, `skills/asset/SKILL.md`, `skills/ui/SKILL.md`, `skills/uitoolkit/SKILL.md`, `skills/importer/SKILL.md`, `skills/probuilder/SKILL.md`, `skills/xr/SKILL.md`, `recipes/asset-recipes.md`, `recipes/ui-recipes.md`, `recipes/uitoolkit-recipes.md`, `recipes/assetimport-recipes.md`, `recipes/probuilder-recipes.md`, `recipes/xr-recipes.md`

- [ ] **Step 1: Ensure the upstream repo exists locally**

Run:
```bash
test -d "/tmp/original-unity-skills/.git" || git clone --depth 1 https://github.com/Besty0728/Unity-Skills "/tmp/original-unity-skills"
```

Expected: command exits successfully and `/tmp/original-unity-skills` exists.

- [ ] **Step 2: Capture the current first-wave drift report**

Run:
```bash
rg -n '_REFERENCE\.md|See `recipes/` directory|project-scout|architecture|patterns|scriptdesign' "/home/batih/personal/unity-skills/skills"
```

Expected: matches in `skills/xr/SKILL.md`, `skills/SKILL.md`, and the first-wave skills that still say `See recipes/ directory`.

- [ ] **Step 3: Capture broken recipe placeholder patterns before editing**

Run:
```bash
rg -n 'string ".*" = default|string null = default|float [0-9].* = default|int [0-9].* = default|bool (true|false) = default' "/home/batih/personal/unity-skills/recipes/asset-recipes.md" "/home/batih/personal/unity-skills/recipes/ui-recipes.md" "/home/batih/personal/unity-skills/recipes/uitoolkit-recipes.md" "/home/batih/personal/unity-skills/recipes/assetimport-recipes.md" "/home/batih/personal/unity-skills/recipes/probuilder-recipes.md" "/home/batih/personal/unity-skills/recipes/xr-recipes.md"
```

Expected: many hits in `ui-recipes.md`, `probuilder-recipes.md`, `xr-recipes.md`, and `assetimport-recipes.md`; fewer or none in `asset-recipes.md`.

---

### Task 2: Rewrite Shared Routing Sources

**Files:**
- Modify: `mcp-tools.md`
- Modify: `references/index.md`
- Modify: `skills/SKILL.md`
- Modify: `docs/SETUP_GUIDE.md`

- [ ] **Step 1: Replace `mcp-tools.md` with an operation-based routing matrix**

Replace the current four-line matrix with a fuller markdown file that starts like this and then expands the table with the currently available official tools in this environment:

```markdown
# MCP Tools Matrix

This file is the shared source of truth for currently available official Unity MCP tools.

## Routing Order

1. If a dedicated official Unity MCP tool covers the requested operation, use that tool.
2. If no native tool covers the operation, use the topic skill plus the exact topic recipe.
3. If no exact recipe exists, adapt the closest recipe with minimal changes.
4. If more Unity manual or API context is still needed, consult the related `references/<topic>.md` file.
5. Write a fresh `Unity_RunCommand` script only when neither a native tool nor a close recipe covers the task.

## Official Unity MCP Tools

| Tool | What it covers | Prefer instead of `Unity_RunCommand` when... |
|------|----------------|----------------------------------------------|
| `Unity_ReadConsole` | Editor console reads and clears | The task is about logs, warnings, errors, or stack traces |
| `Unity_ScriptApplyEdits` | Structured C# edits | The task is changing an existing C# script safely |
| `Unity_ValidateScript` | Script diagnostics | The task is validating an existing C# script |
| `Unity_GetSha` | Script content hash | The task needs script identity or edit preconditions |
| `Unity_GetProjectData` | Project overview and inventory | The task is asking for project-level metadata |
| `Unity_PackageManager_ExecuteAction` | Add/remove/embed/sample packages | The task is package installation or package samples |
| `Unity_PackageManager_GetData` | Package metadata | The task is checking package availability or docs |
| `Unity_Camera_Capture` | Camera render capture | The task is about inspecting a camera view |
| `Unity_SceneView_Capture2DScene` | 2D scene capture | The task is visual validation of a 2D scene region |
| `Unity_SceneView_CaptureMultiAngleSceneView` | Multi-angle 3D scene capture | The task is visual validation of a 3D scene layout |
| `Unity_Profiler_*` | Profiling metrics | The task is performance analysis from profiler data |
| `Unity_FindProjectAssets` | Asset search and semantic lookup | The task is locating assets, not mutating them |
| `Unity_GetUserGuidelines` | Project-specific Unity conventions | The task needs project conventions before editing |
| `Unity_ImportExternalModel` | Importing external FBX content | The task is importing a model from a URL or file |
| `Unity_AudioClip_Edit` | Audio clip edits | The task is trimming, looping, or scaling an audio clip |
| `Unity_AssetGeneration_*` | Official generative asset workflows | The task is generating or converting assets through the native MCP surface |
| `Unity_RunCommand` | Custom editor automation | No dedicated native tool covers the requested operation |
```

Then append any remaining currently exposed official tools from the environment that are missing from the table.

- [ ] **Step 2: Rewrite the root index to reference `mcp-tools.md` and remove missing advisory modules**

Update the intro of `skills/SKILL.md` so the routing note becomes:

```markdown
> **Routing Order**: Check `../mcp-tools.md` first for currently available official Unity MCP tools. If a native tool covers the requested operation, use it. Otherwise open the relevant topic skill in this directory and use its exact recipe file. If more Unity-doc context is still needed, follow the skill's related reference links under `../references/`. Write a fresh `Unity_RunCommand` script only when neither a native tool nor a close recipe covers the task.
```

Then remove the stale advisory table entries for missing modules:

```markdown
project-scout
architecture
adr
asmdef
blueprints
script-roles
scene-contracts
testability
patterns
async
inspector
scriptdesign
```

If you keep an advisory section at all, it must list only folders that exist under `skills/`.

- [ ] **Step 3: Align setup instructions with the new routing order**

In `docs/SETUP_GUIDE.md`, replace the project-level rules block:

```markdown
1. Use the Unity Skills Library located at ./unity-mcp-skills
2. Always start by reading SKILL.md
3. Navigate to skills/SKILL.md to find the correct domain
4. Follow guardrails in skills/<domain>/SKILL.md
5. Use recipes/* for RunCommand execution templates
6. Do not invent Unity scripts — always use recipes
```

With:

```markdown
1. Use the Unity Skills Library located at ./unity-mcp-skills
2. Always start by reading SKILL.md
3. Check mcp-tools.md first for a dedicated official Unity MCP tool
4. If no native tool covers the operation, navigate to skills/SKILL.md and open the relevant topic skill
5. Use the exact topic recipe file before writing a fresh Unity_RunCommand script
6. Use references/<topic>.md only when you still need Unity manual or API context after checking the topic skill and recipe
7. Write a new Unity_RunCommand script only when neither a native tool nor a close recipe covers the task
```

- [ ] **Step 4: Update `references/index.md` to describe tertiary fallback usage**

Rewrite the intro of `references/index.md` so it begins like this:

```markdown
# Unity Documentation Index

These files are tertiary documentation fallbacks.

Routing order:

1. Check `../mcp-tools.md` first for a dedicated official Unity MCP tool.
2. If no native tool covers the operation, use `../skills/<topic>/SKILL.md` and `../recipes/<topic>-recipes.md`.
3. Only then use the relevant file in this directory when more Unity manual or API context is needed.

These files are background material, not the primary source of executable Unity automation patterns.
```

Add this rule immediately below the routing section:

```markdown
Only link a `references/<topic>.md` file from a skill after verifying that it is relevant, curated enough for that topic, and not just a noisy dump.
```

- [ ] **Step 5: Verify the shared routing files after editing**

Run:
```bash
rg -n 'project-scout|architecture|patterns|scriptdesign|See `recipes/` directory' "/home/batih/personal/unity-skills/skills/SKILL.md" "/home/batih/personal/unity-skills/docs/SETUP_GUIDE.md" "/home/batih/personal/unity-skills/references/index.md"
```

Expected: no matches.

- [ ] **Step 6: Commit the shared routing rewrite**

Run:
```bash
git add "/home/batih/personal/unity-skills/mcp-tools.md" "/home/batih/personal/unity-skills/references/index.md" "/home/batih/personal/unity-skills/skills/SKILL.md" "/home/batih/personal/unity-skills/docs/SETUP_GUIDE.md"
git commit -m "docs: centralize MCP routing guidance"
```

Expected: one commit containing only the shared routing docs.

---

### Task 3: Rewrite Asset and Importer Skills, Remove `IMPORT_REFERENCE.md`, Repair Asset Recipes

**Files:**
- Modify: `skills/asset/SKILL.md`
- Modify: `skills/importer/SKILL.md`
- Verify: `references/assets.md`
- Verify: `references/audio.md`
- Delete: `skills/importer/IMPORT_REFERENCE.md`
- Modify: `recipes/asset-recipes.md`
- Modify: `recipes/assetimport-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/AssetSkills.cs`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/AssetImportSkills.cs`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/TextureSkills.cs`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/AudioSkills.cs`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/ModelSkills.cs`

- [ ] **Step 1: Update `skills/asset/SKILL.md` to use shared routing, `writing-skills` structure, and an exact recipe file**

Add a routing note near the top that says:

```markdown
**Routing**:
- Check `../../mcp-tools.md` first for a dedicated official Unity MCP tool.
- If no native tool covers the operation, use `../../recipes/asset-recipes.md`.
- If no exact asset recipe exists, adapt the closest asset recipe with minimal changes.
- If more Unity asset-doc context is still needed, use `../../references/assets.md`.
- Write a fresh `Unity_RunCommand` script only when neither a native tool nor a close asset recipe covers the task.
- For texture, audio, or model import settings, route to the `importer` module.
- For script editing or validation, route to the `script` module and native script tools.
```

Also refit the skill to better match `writing-skills` guidance:

```markdown
- Frontmatter `description` starts with `Use when...`
- Add or clarify `## When to Use`
- Rename the main command tables under `## Quick Reference`
- Add `## Related References` with `../../references/assets.md`
```

Then replace every `*See `recipes/` directory for C# templates.*` line with `For Unity_RunCommand examples, use ../../recipes/asset-recipes.md.` and delete the duplicate `limit` row under `asset_find`.

- [ ] **Step 2: Update `skills/importer/SKILL.md`, fold in the useful parts of `IMPORT_REFERENCE.md`, and add reference fallbacks**

Keep the current route structure, but add the same routing pattern:

```markdown
**Routing**:
- Check `../../mcp-tools.md` first for a dedicated official Unity MCP tool.
- If no native tool covers the operation, use `../../recipes/assetimport-recipes.md`.
- If no exact importer recipe exists, adapt the closest importer recipe with minimal changes.
- If more Unity-doc context is still needed, use `../../references/assets.md` and `../../references/audio.md`.
- Write a fresh `Unity_RunCommand` script only when neither a native tool nor a close importer recipe covers the task.
```

Move the meaningful reference content into the skill itself:

```markdown
### Platform override reminders

- `Standalone`, `iPhone`, `Android`, and `WebGL` are the supported platform override names documented by the upstream implementation.
- Prefer platform overrides only when a real build target needs different limits or compression.

### Practical presets

- UI sprites: `textureType="Sprite"`, usually `mipmapEnabled=false`
- Long BGM: `loadType="Streaming"`
- Short SFX: `loadType="DecompressOnLoad"`
- Humanoid characters: `animationType="Humanoid"`
```

Then replace the final minimal example line with an exact recipe pointer and delete `skills/importer/IMPORT_REFERENCE.md`.

Also refit the skill to better match `writing-skills` guidance:

```markdown
- Frontmatter `description` starts with `Use when...`
- Add or clarify `## When to Use`
- Rename the route tables under `## Quick Reference`
- Add `## Related References` with `../../references/assets.md` and `../../references/audio.md`
```

- [ ] **Step 3: Repair `recipes/asset-recipes.md` from the upstream source methods**

Rewrite the opening note so it says the file contains recipe-derived `Unity_RunCommand` examples adapted from `AssetSkills.cs`, not raw extractor output.

For each of these headings, replace the broken extractor-style parameter prelude with valid named locals and a compilable `CommandScript` skeleton based on the upstream method body:

```markdown
AssetImport
AssetDelete
AssetMove
AssetImportBatch
AssetDeleteBatch
AssetMoveBatch
AssetDuplicate
AssetFind
AssetCreateFolder
AssetRefresh
AssetGetInfo
AssetReimport
AssetReimportBatch
AssetSetLabels
AssetGetLabels
```

Use this exact style for the prelude, not the broken extracted style:

```csharp
internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Example.asset";

        // Adapt the parameters above before running.
        // Original behavior came from the upstream AssetSkills implementation.
    }
}
```

- [ ] **Step 4: Repair `recipes/assetimport-recipes.md` from the upstream source methods**

Use `AssetImportSkills.cs`, `TextureSkills.cs`, `AudioSkills.cs`, and `ModelSkills.cs` as the authoritative source. Rewrite the broken parameter prelude for each recipe entry with valid named locals and preserve the upstream operation names.

At minimum, repair the headings surfaced by `skills/importer/SKILL.md`:

```markdown
AssetReimport
AssetReimportBatch
TextureGetSettings
TextureSetSettings
TextureSetSettingsBatch
TextureGetImportSettings
TextureSetImportSettings
TextureFindAssets
TextureGetInfo
TextureFindBySize
TextureSetType
TextureSetPlatformSettings
TextureGetPlatformSettings
TextureSetSpriteSettings
SpriteSetImportSettings
AudioGetSettings
AudioSetSettings
AudioSetSettingsBatch
AudioGetImportSettings
AudioSetImportSettings
AudioFindClips
AudioGetClipInfo
AudioAddSource
AudioGetSourceInfo
AudioSetSourceProperties
AudioFindSourcesInScene
AudioCreateMixer
ModelGetSettings
ModelSetSettings
ModelSetSettingsBatch
ModelGetImportSettings
ModelSetImportSettings
ModelFindAssets
ModelGetMeshInfo
ModelGetMaterialsInfo
ModelGetAnimationsInfo
ModelGetRigInfo
ModelSetAnimationClips
ModelSetRig
```

- [ ] **Step 5: Verify asset/importer cleanup**

Run:
```bash
rg -n 'IMPORT_REFERENCE\.md|See `recipes/` directory|string ".*" = default|string null = default|float [0-9].* = default|int [0-9].* = default' "/home/batih/personal/unity-skills/skills/asset/SKILL.md" "/home/batih/personal/unity-skills/skills/importer/SKILL.md" "/home/batih/personal/unity-skills/recipes/asset-recipes.md" "/home/batih/personal/unity-skills/recipes/assetimport-recipes.md"
```

Expected: no matches.

Then run:
```bash
rg -n '^description: "Use when|^## When to Use$|^## Quick Reference$|^## Related References$|references/assets.md|references/audio.md' "/home/batih/personal/unity-skills/skills/asset/SKILL.md" "/home/batih/personal/unity-skills/skills/importer/SKILL.md"
```

Expected: each skill shows `Use when...`, `## When to Use`, `## Quick Reference`, and `## Related References`.

- [ ] **Step 6: Commit the asset/importer pass**

Run:
```bash
git add "/home/batih/personal/unity-skills/skills/asset/SKILL.md" "/home/batih/personal/unity-skills/skills/importer/SKILL.md" "/home/batih/personal/unity-skills/skills/importer/IMPORT_REFERENCE.md" "/home/batih/personal/unity-skills/recipes/asset-recipes.md" "/home/batih/personal/unity-skills/recipes/assetimport-recipes.md"
git commit -m "docs: align asset and importer recipes with MCP routing"
```

Expected: one commit covering only the asset/importer slice.

---

### Task 4: Rewrite UI and UI Toolkit Skills, Remove Sidecars, Repair UI Recipes

**Files:**
- Modify: `skills/ui/SKILL.md`
- Modify: `skills/uitoolkit/SKILL.md`
- Verify: `references/ui.md`
- Delete: `skills/ui/UI_REFERENCE.md`
- Delete: `skills/uitoolkit/USS_REFERENCE.md`
- Modify: `recipes/ui-recipes.md`
- Modify: `recipes/uitoolkit-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/UISkills.cs`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/UIToolkitSkills.cs`

- [ ] **Step 1: Rewrite `skills/ui/SKILL.md`, absorb the useful parts of `UI_REFERENCE.md`, and refit it to `writing-skills` structure**

Add the shared routing pattern with the exact recipe file:

```markdown
**Routing**:
- Check `../../mcp-tools.md` first for a dedicated official Unity MCP tool.
- If no native tool covers the operation, use `../../recipes/ui-recipes.md`.
- If no exact UI recipe exists, adapt the closest UI recipe with minimal changes.
- If more Unity UI-doc context is still needed, use `../../references/ui.md`.
- Write a fresh `Unity_RunCommand` script only when neither a native tool nor a close UI recipe covers the task.
```

Move these useful sidecar ideas into the main skill:

```markdown
### Extended element reminders

- `ui_create_dropdown` takes comma-separated `options`.
- `ui_create_scrollview` should usually be followed by `ui_layout_children` or `ui_set_rect`.
- `ui_create_rawimage` expects a texture asset path.
- `ui_create_scrollbar` uses `direction`, `value`, `size`, and `numberOfSteps` semantics from the upstream implementation.

### Image and selectable reminders

- `ui_set_image` supports `Simple`, `Sliced`, `Tiled`, and `Filled` image types.
- `ui_configure_selectable` should be used for transition colors and navigation settings instead of ad-hoc component edits.
```

Then refit the skill to better match `writing-skills` guidance:

```markdown
- Frontmatter `description` starts with `Use when...`
- Add or clarify `## When to Use`
- Rename the command tables under `## Quick Reference`
- Add `## Related References` with `../../references/ui.md`
```

Then replace the minimal example line with `For Unity_RunCommand examples, use ../../recipes/ui-recipes.md.` and delete `skills/ui/UI_REFERENCE.md`.

- [ ] **Step 2: Rewrite `skills/uitoolkit/SKILL.md`, absorb the useful parts of `USS_REFERENCE.md`, and refit it to `writing-skills` structure**

Add the same routing pattern pointing to `../../recipes/uitoolkit-recipes.md`.

Also include:

```markdown
- If more Unity UI-doc context is still needed, use `../../references/ui.md`.
```

Keep the current USS support matrix, then add a compact section derived from the sidecar file:

```markdown
### Starter USS patterns

- Start with `:root` tokens for color, spacing, radius, and font sizes.
- Use wrapped flex rows instead of CSS grid.
- Use nested `VisualElement` shadows instead of `box-shadow`.
- Use `PanelSettings.scaleMode = ScaleWithScreenSize` for responsive runtime UI.
```

Move one concrete layout starter into the recipe file, not the skill file: card grid or navbar, but not both.

Then refit the skill to better match `writing-skills` guidance:

```markdown
- Frontmatter `description` starts with `Use when...`
- Add or clarify `## When to Use`
- Rename the grouped command tables under `## Quick Reference`
- Add `## Related References` with `../../references/ui.md`
```

Then replace the minimal example line with the exact recipe pointer and delete `skills/uitoolkit/USS_REFERENCE.md`.

- [ ] **Step 3: Repair `recipes/ui-recipes.md` from `UISkills.cs`**

Rewrite the recipe intro to say the file contains cleaned `Unity_RunCommand` examples adapted from `UISkills.cs`.

Repair the broken extractor-style parameter prelude for the first-wave headings the skill actively documents:

```markdown
UICreateCanvas
UICreatePanel
UICreateButton
UICreateText
UICreateImage
UICreateInputField
UICreateSlider
UICreateToggle
UICreateDropdown
UICreateScrollView
UICreateRawImage
UICreateScrollbar
UICreateBatch
UIFindAll
UISetText
UISetRect
UISetAnchor
UILayoutChildren
UIAlignSelected
UIDistributeSelected
UISetImage
UIAddLayoutElement
UIAddCanvasGroup
UIAddMask
UIAddOutline
UIConfigureSelectable
```

For `UICreateCanvas`, the repaired prelude should look like:

```csharp
internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "Canvas";
        string renderMode = "ScreenSpaceOverlay";

        // Adapt the parameters above before running.
    }
}
```

Apply the same valid named-local pattern to the remaining headings.

- [ ] **Step 4: Repair `recipes/uitoolkit-recipes.md` from `UIToolkitSkills.cs`**

Rewrite the file intro and repair the prelude for the headings surfaced by the skill:

```markdown
UitkCreateUss
UitkCreateUxml
UitkReadFile
UitkWriteFile
UitkDeleteFile
UitkFindFiles
UitkCreateBatch
UitkCreateDocument
UitkSetDocument
UitkCreatePanelSettings
UitkGetPanelSettings
UitkSetPanelSettings
UitkListDocuments
UitkInspectDocument
UitkAddElement
UitkRemoveElement
UitkModifyElement
UitkCloneElement
UitkInspectUxml
UitkAddUssRule
UitkRemoveUssRule
UitkListUssVariables
UitkCreateFromTemplate
UitkCreateEditorWindow
UitkCreateRuntimeUI
```

Add one compact "starter pattern" section near the top of the recipe file with a card-grid or navbar example migrated from `USS_REFERENCE.md`.

- [ ] **Step 5: Verify UI cleanup**

Run:
```bash
rg -n 'UI_REFERENCE\.md|USS_REFERENCE\.md|See `recipes/` directory|string ".*" = default|string null = default|float [0-9].* = default|int [0-9].* = default' "/home/batih/personal/unity-skills/skills/ui/SKILL.md" "/home/batih/personal/unity-skills/skills/uitoolkit/SKILL.md" "/home/batih/personal/unity-skills/recipes/ui-recipes.md" "/home/batih/personal/unity-skills/recipes/uitoolkit-recipes.md"
```

Expected: no matches.

Then run:
```bash
rg -n '^description: "Use when|^## When to Use$|^## Quick Reference$|^## Related References$|references/ui.md' "/home/batih/personal/unity-skills/skills/ui/SKILL.md" "/home/batih/personal/unity-skills/skills/uitoolkit/SKILL.md"
```

Expected: each skill shows `Use when...`, `## When to Use`, `## Quick Reference`, and `## Related References`.

- [ ] **Step 6: Commit the UI pass**

Run:
```bash
git add "/home/batih/personal/unity-skills/skills/ui/SKILL.md" "/home/batih/personal/unity-skills/skills/uitoolkit/SKILL.md" "/home/batih/personal/unity-skills/skills/ui/UI_REFERENCE.md" "/home/batih/personal/unity-skills/skills/uitoolkit/USS_REFERENCE.md" "/home/batih/personal/unity-skills/recipes/ui-recipes.md" "/home/batih/personal/unity-skills/recipes/uitoolkit-recipes.md"
git commit -m "docs: rewrite UI recipes for MCP routing"
```

Expected: one commit covering only the UI slice.

---

### Task 5: Rewrite ProBuilder and XR Skills, Remove Sidecars, Repair Recipes

**Files:**
- Modify: `skills/probuilder/SKILL.md`
- Modify: `skills/xr/SKILL.md`
- Verify: `references/3d.md`
- Modify: `references/xr.md`
- Delete: `skills/probuilder/MODELING_REFERENCE.md`
- Delete: `skills/xr/API_REFERENCE.md`
- Modify: `recipes/probuilder-recipes.md`
- Modify: `recipes/xr-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/ProBuilderSkills.cs`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/XRSkills.cs`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/XRReflectionHelper.cs`

- [ ] **Step 1: Rewrite `skills/probuilder/SKILL.md`, absorb the modeling sidecar, and refit it to `writing-skills` structure**

Add the shared routing pattern with `../../recipes/probuilder-recipes.md`.

Also include:

```markdown
- If more Unity 3D manual context is still needed, use `../../references/3d.md`.
```

Move the most useful design heuristics from the sidecar into the main skill:

```markdown
## Scale and blockout reminders

- `y` is the center of the shape, not the floor contact point.
- Doors generally need `>= 2.2m` height and corridors need `>= 1.5m` width for comfortable human scale.
- Use `probuilder_get_vertices` before vertex edits and `probuilder_get_info` before face edits.
- Prefer multipart furniture blockouts instead of one giant cube when silhouette matters.
```

Then refit the skill to better match `writing-skills` guidance:

```markdown
- Frontmatter `description` starts with `Use when...`
- Add or clarify `## When to Use`
- Rename the grouped command tables under `## Quick Reference`
- Add `## Related References` with `../../references/3d.md`
```

Then replace the minimal example line with `For Unity_RunCommand examples, use ../../recipes/probuilder-recipes.md.` and delete `skills/probuilder/MODELING_REFERENCE.md`.

- [ ] **Step 2: Rewrite `skills/xr/SKILL.md`, remove stale advisory links, absorb the API sidecar, and add curated reference fallback**

Remove the routing line that points to non-existent advisory modules:

```markdown
- For architecture or lifecycle decisions in XR gameplay code -> load advisory modules such as `architecture`, `patterns`, `async`, or `scriptdesign`
```

Replace it with:

```markdown
- Check `../../mcp-tools.md` first for native MCP tools that cover the operation.
- If no native tool covers the operation, use `../../recipes/xr-recipes.md`.
- If no exact XR recipe exists, adapt the closest XR recipe with minimal changes.
- If more Unity XR-doc context is still needed, use `../../references/xr.md`.
- Write a fresh `Unity_RunCommand` script only when neither a native tool nor a close XR recipe covers the task.
```

Fold these sidecar concepts into the skill file:

```markdown
### Verified value reminders

- `movementType`: `VelocityTracking`, `Kinematic`, `Instantaneous`
- `matchOrientation`: `WorldSpaceUp`, `TargetUp`, `TargetUpAndForward`, `None`
- interaction events: `selectEntered`, `selectExited`, `hoverEntered`, `hoverExited`, `activated`, `deactivated`
```

Keep the collider matrix and version-compatibility section; they already carry much of the valuable sidecar content. Then delete `skills/xr/API_REFERENCE.md`.

Before linking the XR skill to `../../references/xr.md`, prune clearly unrelated entries from `references/xr.md` so it reads as an actual XR-oriented docs index rather than a noisy dump.

Then refit `skills/xr/SKILL.md` to better match `writing-skills` guidance:

```markdown
- Frontmatter `description` starts with `Use when...`
- Add or clarify `## When to Use`
- Rename the grouped command tables under `## Quick Reference`
- Add `## Related References` with `../../references/xr.md`
```

- [ ] **Step 3: Repair `recipes/probuilder-recipes.md` from `ProBuilderSkills.cs`**

Rewrite the intro and repair the prelude for the headings documented in the skill:

```markdown
ProBuilderCreateShape
ProBuilderCreateBatch
ProBuilderExtrudeFaces
ProBuilderDeleteFaces
ProBuilderMergeFaces
ProBuilderFlipNormals
ProBuilderDetachFaces
ProBuilderBevelEdges
ProBuilderExtrudeEdges
ProBuilderBridgeEdges
ProBuilderSubdivide
ProBuilderConformNormals
ProBuilderMoveVertices
ProBuilderSetVertices
ProBuilderGetVertices
ProBuilderWeldVertices
ProBuilderProjectUV
ProBuilderSetFaceMaterial
ProBuilderSetMaterial
ProBuilderCombineMeshes
ProBuilderGetInfo
ProBuilderCenterPivot
```

Use valid named locals and preserve package guards like `#if !PROBUILDER` where they matter to understanding the upstream behavior.

- [ ] **Step 4: Repair `recipes/xr-recipes.md` from `XRSkills.cs` and `XRReflectionHelper.cs`**

Rewrite the intro and repair the prelude for the headings surfaced by the skill:

```markdown
XRCheckSetup
XRSetupRig
XRSetupInteractionManager
XRSetupEventSystem
XRGetSceneReport
XRAddRayInteractor
XRAddDirectInteractor
XRAddSocketInteractor
XRAddGrabInteractable
XRAddSimpleInteractable
XRConfigureInteractable
XRListInteractors
XRListInteractables
XRSetupTeleportation
XRAddTeleportArea
XRAddTeleportAnchor
XRSetupContinuousMove
XRSetupTurnProvider
XRSetupUICanvas
XRConfigureHaptics
XRAddInteractionEvent
XRConfigureInteractionLayers
```

For each entry, keep the reflection-based compatibility notes where the upstream source depends on them.

- [ ] **Step 5: Verify ProBuilder/XR cleanup**

Run:
```bash
rg -n 'MODELING_REFERENCE\.md|API_REFERENCE\.md|architecture|patterns|scriptdesign|See `recipes/` directory|string ".*" = default|string null = default|float [0-9].* = default|int [0-9].* = default|bool (true|false) = default' "/home/batih/personal/unity-skills/skills/probuilder/SKILL.md" "/home/batih/personal/unity-skills/skills/xr/SKILL.md" "/home/batih/personal/unity-skills/recipes/probuilder-recipes.md" "/home/batih/personal/unity-skills/recipes/xr-recipes.md"
```

Expected: no matches.

Then run:
```bash
rg -n '^description: "Use when|^## When to Use$|^## Quick Reference$|^## Related References$|references/3d.md|references/xr.md' "/home/batih/personal/unity-skills/skills/probuilder/SKILL.md" "/home/batih/personal/unity-skills/skills/xr/SKILL.md"
```

Expected: each skill shows `Use when...`, `## When to Use`, `## Quick Reference`, and `## Related References`.

- [ ] **Step 6: Commit the ProBuilder/XR pass**

Run:
```bash
git add "/home/batih/personal/unity-skills/skills/probuilder/SKILL.md" "/home/batih/personal/unity-skills/skills/xr/SKILL.md" "/home/batih/personal/unity-skills/skills/probuilder/MODELING_REFERENCE.md" "/home/batih/personal/unity-skills/skills/xr/API_REFERENCE.md" "/home/batih/personal/unity-skills/recipes/probuilder-recipes.md" "/home/batih/personal/unity-skills/recipes/xr-recipes.md"
git commit -m "docs: fold probuilder and xr references into MCP recipes"
```

Expected: one commit covering only the ProBuilder/XR slice.

---

### Task 6: Sweep Remaining Applicable Skills For Exact Recipe Routing

**Files:**
- Modify: `skills/animator/SKILL.md`
- Modify: `skills/cinemachine/SKILL.md`
- Modify: `skills/cleaner/SKILL.md`
- Modify: `skills/component/SKILL.md`
- Modify: `skills/console/SKILL.md`
- Modify: `skills/editor/SKILL.md`
- Modify: `skills/gameobject/SKILL.md`
- Modify: `skills/light/SKILL.md`
- Modify: `skills/material/SKILL.md`
- Modify: `skills/perception/SKILL.md`
- Modify: `skills/physics/SKILL.md`
- Modify: `skills/prefab/SKILL.md`
- Modify: `skills/scene/SKILL.md`
- Modify: `skills/script/SKILL.md`
- Modify: `skills/shader/SKILL.md`
- Modify: `skills/smart/SKILL.md`
- Modify: `skills/terrain/SKILL.md`
- Modify: `skills/test/SKILL.md`
- Modify: `skills/validation/SKILL.md`
- Modify: `skills/workflow/SKILL.md`

- [ ] **Step 1: Replace remaining generic recipe-directory references with exact recipe files**

Use these direct mappings:

```markdown
skills/animator/SKILL.md -> ../../recipes/animator-recipes.md
skills/cinemachine/SKILL.md -> ../../recipes/cinemachine-recipes.md
skills/cleaner/SKILL.md -> ../../recipes/cleaner-recipes.md
skills/component/SKILL.md -> ../../recipes/component-recipes.md
skills/console/SKILL.md -> ../../recipes/console-recipes.md
skills/editor/SKILL.md -> ../../recipes/editor-recipes.md
skills/gameobject/SKILL.md -> ../../recipes/gameobject-recipes.md
skills/light/SKILL.md -> ../../recipes/light-recipes.md
skills/material/SKILL.md -> ../../recipes/material-recipes.md
skills/perception/SKILL.md -> ../../recipes/perception-recipes.md
skills/physics/SKILL.md -> ../../recipes/physics-recipes.md
skills/prefab/SKILL.md -> ../../recipes/prefab-recipes.md
skills/scene/SKILL.md -> ../../recipes/scene-recipes.md
skills/script/SKILL.md -> ../../recipes/script-recipes.md
skills/shader/SKILL.md -> ../../recipes/shader-recipes.md
skills/smart/SKILL.md -> ../../recipes/smart-recipes.md
skills/terrain/SKILL.md -> ../../recipes/terrain-recipes.md
skills/test/SKILL.md -> ../../recipes/test-recipes.md
skills/validation/SKILL.md -> ../../recipes/validation-recipes.md
skills/workflow/SKILL.md -> ../../recipes/workflow-recipes.md
```

For each listed skill, replace `*See \`recipes/\` directory for C# templates.*` with the exact matching recipe file reference.

- [ ] **Step 2: Rename placeholder example sections so they describe what they actually provide**

For skills whose `## Minimal Example` or similar section only points to a recipe file, rename that section to `## RunCommand Examples` and keep the body as a direct pointer to the exact recipe file. Apply this to at least:

```markdown
skills/cinemachine/SKILL.md
skills/gameobject/SKILL.md
skills/light/SKILL.md
skills/perception/SKILL.md
skills/physics/SKILL.md
skills/test/SKILL.md
skills/workflow/SKILL.md
```

Apply the same treatment to any similarly misleading example heading in the listed Task 6 files.

- [ ] **Step 3: Keep native-tool-first modules native-tool-first while making their fallback references precise**

For `console`, `editor`, and `script`:

1. Keep the native-tool-first guidance intact.
2. Replace any remaining generic `recipes/` wording with the exact recipe file only where a fallback recipe section still exists.
3. If a recipe pointer is no longer useful in a section, remove the section instead of leaving a vague stub.
4. Use `../mcp-tools.md` consistently for native-tool route references.

- [ ] **Step 4: Verify the second-wave skill sweep**

Run:
```bash
rg -n 'See `recipes/` directory|recipes/\*' "/home/batih/personal/unity-skills/skills/animator/SKILL.md" "/home/batih/personal/unity-skills/skills/cinemachine/SKILL.md" "/home/batih/personal/unity-skills/skills/cleaner/SKILL.md" "/home/batih/personal/unity-skills/skills/component/SKILL.md" "/home/batih/personal/unity-skills/skills/console/SKILL.md" "/home/batih/personal/unity-skills/skills/editor/SKILL.md" "/home/batih/personal/unity-skills/skills/gameobject/SKILL.md" "/home/batih/personal/unity-skills/skills/light/SKILL.md" "/home/batih/personal/unity-skills/skills/material/SKILL.md" "/home/batih/personal/unity-skills/skills/perception/SKILL.md" "/home/batih/personal/unity-skills/skills/physics/SKILL.md" "/home/batih/personal/unity-skills/skills/prefab/SKILL.md" "/home/batih/personal/unity-skills/skills/scene/SKILL.md" "/home/batih/personal/unity-skills/skills/script/SKILL.md" "/home/batih/personal/unity-skills/skills/shader/SKILL.md" "/home/batih/personal/unity-skills/skills/smart/SKILL.md" "/home/batih/personal/unity-skills/skills/terrain/SKILL.md" "/home/batih/personal/unity-skills/skills/test/SKILL.md" "/home/batih/personal/unity-skills/skills/validation/SKILL.md" "/home/batih/personal/unity-skills/skills/workflow/SKILL.md"
```

Expected: no matches.

Then run:
```bash
rg -n 'recipes/animator-recipes.md|recipes/cinemachine-recipes.md|recipes/cleaner-recipes.md|recipes/component-recipes.md|recipes/console-recipes.md|recipes/editor-recipes.md|recipes/gameobject-recipes.md|recipes/light-recipes.md|recipes/material-recipes.md|recipes/perception-recipes.md|recipes/physics-recipes.md|recipes/prefab-recipes.md|recipes/scene-recipes.md|recipes/script-recipes.md|recipes/shader-recipes.md|recipes/smart-recipes.md|recipes/terrain-recipes.md|recipes/test-recipes.md|recipes/validation-recipes.md|recipes/workflow-recipes.md' "/home/batih/personal/unity-skills/skills/animator/SKILL.md" "/home/batih/personal/unity-skills/skills/cinemachine/SKILL.md" "/home/batih/personal/unity-skills/skills/cleaner/SKILL.md" "/home/batih/personal/unity-skills/skills/component/SKILL.md" "/home/batih/personal/unity-skills/skills/console/SKILL.md" "/home/batih/personal/unity-skills/skills/editor/SKILL.md" "/home/batih/personal/unity-skills/skills/gameobject/SKILL.md" "/home/batih/personal/unity-skills/skills/light/SKILL.md" "/home/batih/personal/unity-skills/skills/material/SKILL.md" "/home/batih/personal/unity-skills/skills/perception/SKILL.md" "/home/batih/personal/unity-skills/skills/physics/SKILL.md" "/home/batih/personal/unity-skills/skills/prefab/SKILL.md" "/home/batih/personal/unity-skills/skills/scene/SKILL.md" "/home/batih/personal/unity-skills/skills/script/SKILL.md" "/home/batih/personal/unity-skills/skills/shader/SKILL.md" "/home/batih/personal/unity-skills/skills/smart/SKILL.md" "/home/batih/personal/unity-skills/skills/terrain/SKILL.md" "/home/batih/personal/unity-skills/skills/test/SKILL.md" "/home/batih/personal/unity-skills/skills/validation/SKILL.md" "/home/batih/personal/unity-skills/skills/workflow/SKILL.md"
```

Expected: each listed skill shows its own exact recipe file path at least once, unless a recipe-only stub was removed because it was not useful.

- [ ] **Step 5: Commit the second-wave skill sweep**

Run:
```bash
git add "/home/batih/personal/unity-skills/skills/animator/SKILL.md" "/home/batih/personal/unity-skills/skills/cinemachine/SKILL.md" "/home/batih/personal/unity-skills/skills/cleaner/SKILL.md" "/home/batih/personal/unity-skills/skills/component/SKILL.md" "/home/batih/personal/unity-skills/skills/console/SKILL.md" "/home/batih/personal/unity-skills/skills/editor/SKILL.md" "/home/batih/personal/unity-skills/skills/gameobject/SKILL.md" "/home/batih/personal/unity-skills/skills/light/SKILL.md" "/home/batih/personal/unity-skills/skills/material/SKILL.md" "/home/batih/personal/unity-skills/skills/perception/SKILL.md" "/home/batih/personal/unity-skills/skills/physics/SKILL.md" "/home/batih/personal/unity-skills/skills/prefab/SKILL.md" "/home/batih/personal/unity-skills/skills/scene/SKILL.md" "/home/batih/personal/unity-skills/skills/script/SKILL.md" "/home/batih/personal/unity-skills/skills/shader/SKILL.md" "/home/batih/personal/unity-skills/skills/smart/SKILL.md" "/home/batih/personal/unity-skills/skills/terrain/SKILL.md" "/home/batih/personal/unity-skills/skills/test/SKILL.md" "/home/batih/personal/unity-skills/skills/validation/SKILL.md" "/home/batih/personal/unity-skills/skills/workflow/SKILL.md"
git commit -m "docs: point remaining skills to exact recipe files"
```

Expected: one commit covering only the second-wave skill routing sweep.

---

### Task 7: Final Verification and Top-Level Consistency Check

**Files:**
- Verify: `mcp-tools.md`
- Verify: `references/index.md`
- Verify: `skills/SKILL.md`
- Verify: `skills/asset/SKILL.md`
- Verify: `skills/ui/SKILL.md`
- Verify: `skills/uitoolkit/SKILL.md`
- Verify: `skills/importer/SKILL.md`
- Verify: `skills/probuilder/SKILL.md`
- Verify: `skills/xr/SKILL.md`
- Verify: `references/assets.md`
- Verify: `references/ui.md`
- Verify: `references/audio.md`
- Verify: `references/3d.md`
- Verify: `references/xr.md`
- Verify: `recipes/asset-recipes.md`
- Verify: `recipes/ui-recipes.md`
- Verify: `recipes/uitoolkit-recipes.md`
- Verify: `recipes/assetimport-recipes.md`
- Verify: `recipes/probuilder-recipes.md`
- Verify: `recipes/xr-recipes.md`
- Modify only if needed: `README.md`

- [ ] **Step 1: Verify all obsolete sidecars are gone**

Run:
```bash
rg -n "_REFERENCE\.md" "/home/batih/personal/unity-skills/skills" "/home/batih/personal/unity-skills/docs" "/home/batih/personal/unity-skills/README.md"
```

Expected: no matches.

- [ ] **Step 2: Verify exact recipe pointers exist in every first-wave skill**

Run:
```bash
rg -n "recipes/asset-recipes.md|recipes/ui-recipes.md|recipes/uitoolkit-recipes.md|recipes/assetimport-recipes.md|recipes/probuilder-recipes.md|recipes/xr-recipes.md" "/home/batih/personal/unity-skills/skills/asset/SKILL.md" "/home/batih/personal/unity-skills/skills/ui/SKILL.md" "/home/batih/personal/unity-skills/skills/uitoolkit/SKILL.md" "/home/batih/personal/unity-skills/skills/importer/SKILL.md" "/home/batih/personal/unity-skills/skills/probuilder/SKILL.md" "/home/batih/personal/unity-skills/skills/xr/SKILL.md"
```

Expected: each skill file shows its own exact recipe file path at least once.

- [ ] **Step 3: Verify every first-wave skill references `mcp-tools.md`**

Run:
```bash
rg -n "mcp-tools\.md" "/home/batih/personal/unity-skills/skills/asset/SKILL.md" "/home/batih/personal/unity-skills/skills/ui/SKILL.md" "/home/batih/personal/unity-skills/skills/uitoolkit/SKILL.md" "/home/batih/personal/unity-skills/skills/importer/SKILL.md" "/home/batih/personal/unity-skills/skills/probuilder/SKILL.md" "/home/batih/personal/unity-skills/skills/xr/SKILL.md" "/home/batih/personal/unity-skills/skills/SKILL.md"
```

Expected: one or more hits in every listed file.

- [ ] **Step 4: Verify first-wave skills expose `writing-skills` structure and related reference links**

Run:
```bash
rg -n '^description: "Use when|^## When to Use$|^## Quick Reference$|^## Related References$|references/assets.md|references/ui.md|references/audio.md|references/3d.md|references/xr.md' "/home/batih/personal/unity-skills/skills/asset/SKILL.md" "/home/batih/personal/unity-skills/skills/ui/SKILL.md" "/home/batih/personal/unity-skills/skills/uitoolkit/SKILL.md" "/home/batih/personal/unity-skills/skills/importer/SKILL.md" "/home/batih/personal/unity-skills/skills/probuilder/SKILL.md" "/home/batih/personal/unity-skills/skills/xr/SKILL.md"
```

Expected: every first-wave skill shows `Use when...`, `## When to Use`, `## Quick Reference`, and `## Related References`.

- [ ] **Step 5: Verify broken placeholder declarations are gone from the repaired recipe files**

Run:
```bash
rg -n 'string ".*" = default|string null = default|float [0-9].* = default|int [0-9].* = default|bool (true|false) = default' "/home/batih/personal/unity-skills/recipes/asset-recipes.md" "/home/batih/personal/unity-skills/recipes/ui-recipes.md" "/home/batih/personal/unity-skills/recipes/uitoolkit-recipes.md" "/home/batih/personal/unity-skills/recipes/assetimport-recipes.md" "/home/batih/personal/unity-skills/recipes/probuilder-recipes.md" "/home/batih/personal/unity-skills/recipes/xr-recipes.md"
```

Expected: no matches.

- [ ] **Step 6: Check whether top-level docs now need wording alignment**

Run:
```bash
rg -n "always use recipes|never hallucinate Unity editor code|use recipes/\*|RunCommand recipes" "/home/batih/personal/unity-skills/README.md" "/home/batih/personal/unity-skills/docs/SETUP_GUIDE.md"
```

Expected: `docs/SETUP_GUIDE.md` should already be aligned from Task 2. If `README.md` still contradicts the new routing order, update only the contradictory lines so it also says native-tool-first and recipe-second.

- [ ] **Step 7: Review the final diff before stopping**

Run:
```bash
git diff --stat
git diff -- "/home/batih/personal/unity-skills/mcp-tools.md" "/home/batih/personal/unity-skills/skills" "/home/batih/personal/unity-skills/recipes" "/home/batih/personal/unity-skills/docs/SETUP_GUIDE.md" "/home/batih/personal/unity-skills/README.md"
```

Expected: the diff stays within the approved first-wave files plus optional top-level wording alignment.

- [ ] **Step 8: Create the final commit for any remaining top-level alignment**

If Task 7 changed `README.md`, include it here. Otherwise commit only the remaining verification-triggered doc changes:
```bash
git add "/home/batih/personal/unity-skills/README.md" "/home/batih/personal/unity-skills/docs/SETUP_GUIDE.md"
git commit -m "docs: align top-level MCP routing guidance"
```

Expected: skip if there are no remaining staged changes.
