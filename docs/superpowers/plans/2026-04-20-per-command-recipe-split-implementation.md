# Per-Command Recipe Split Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Restructure the recipe system from large topic markdown files into deterministic per-command recipe files at `recipes/<topic>/<command>.md`, update every skill to use the new path rule, fix any existing skill problems while touching that skill, and record a per-skill migration rating in `MIGRATION.md`.

**Architecture:** The root routing rule lives in the index skill and `recipes/README.md`, while each topic skill gets one short local invariant line: `Recipe path rule: ../../recipes/<topic>/<command>.md`. Each skill is migrated in its own task. Each per-command recipe file must be matched against the original upstream `.cs` implementation in `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/` before it is kept. No skill or recipe should contain repo-history commentary.

**Tech Stack:** Markdown, C#, Unity `Unity_RunCommand` recipe files, upstream source at `/tmp/original-unity-skills`, git, `rg`.

---

### Task 1: Shared Structure And Migration Log

**Files:**
- Modify: `skills/SKILL.md`
- Modify: `recipes/README.md`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Update the root index with the canonical path rule**

Add a short rule to `skills/SKILL.md` that says the deterministic recipe path is:

```markdown
Recipe path rule: `../../recipes/<topic>/<command>.md`
```

Also explain once that command filenames must match skill command IDs exactly.

- [ ] **Step 2: Update `recipes/README.md` for the new structure**

Rewrite `recipes/README.md` so it explains:

```markdown
recipes/<topic>/README.md
recipes/<topic>/<command>.md
```

And states:

```markdown
If the skill command is `gameobject_create`, the recipe path is `recipes/gameobject/gameobject_create.md`.
```

- [ ] **Step 3: Prepare `MIGRATION.md` for per-skill entries**

Append this section to `MIGRATION.md`:

```markdown
## Per-Skill Recipe Split

Ratings:
- `keep` — still valuable as a first-class skill
- `shrink` — keep, but likely trim later because native MCP overlap is high
- `audit` — revisit after the split because current value is unclear
```

- [ ] **Step 4: Verify shared structure**

Run:
```bash
rg -n 'Recipe path rule: `\.\./\.\./recipes/<topic>/<command>\.md`|command filenames must match skill command IDs exactly' "/home/batih/personal/unity-skills/skills/SKILL.md" "/home/batih/personal/unity-skills/recipes/README.md"
```

Expected: both files contain the canonical rule.

- [ ] **Step 5: Commit**

```bash
git add "/home/batih/personal/unity-skills/skills/SKILL.md" "/home/batih/personal/unity-skills/recipes/README.md" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: define per-command recipe structure"
```

---

### Task 2: Shared Recipe Utilities

**Files:**
- Create: `recipes/_shared/README.md`
- Create: `recipes/_shared/validate.md`
- Create: `recipes/_shared/skills_common.md`
- Create: `recipes/_shared/gameobject_finder.md`
- Source: `recipes/validation-recipes.md`
- Source: `recipes/skills-common-recipes.md`
- Source: `recipes/gameobject-finder-recipes.md`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split shared helper recipe files**

Create:

```markdown
recipes/_shared/validate.md
recipes/_shared/skills_common.md
recipes/_shared/gameobject_finder.md
```

Keep them purpose-built. Do not include repo-history notes.

- [ ] **Step 2: Create `recipes/_shared/README.md`**

List the three shared helper files and what they provide.

- [ ] **Step 3: Append the migration log entry**

Append this exact line to `MIGRATION.md`:

```markdown
- _shared: rating=keep; shared helper recipes moved to recipes/_shared/*.md.
```

- [ ] **Step 4: Verify and commit**

```bash
test -f "/home/batih/personal/unity-skills/recipes/_shared/README.md" && test -f "/home/batih/personal/unity-skills/recipes/_shared/validate.md" && test -f "/home/batih/personal/unity-skills/recipes/_shared/skills_common.md" && test -f "/home/batih/personal/unity-skills/recipes/_shared/gameobject_finder.md"
git add "/home/batih/personal/unity-skills/recipes/_shared" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split shared recipe helpers"
```

---

### Task 3: Animator Skill And Recipes

**Files:**
- Modify: `skills/animator/SKILL.md`
- Create: `recipes/animator/README.md`
- Create: `recipes/animator/*.md`
- Source: `recipes/animator-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/AnimatorSkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the animator recipes by command**

Create `recipes/animator/<command>.md` for every command currently documented in `skills/animator/SKILL.md`. Match each new file against `AnimatorSkills.cs` before keeping it.

- [ ] **Step 2: Update the skill and fix local issues**

Add this exact local rule to `skills/animator/SKILL.md`:

```markdown
Recipe path rule: `../../recipes/animator/<command>.md`
```

Replace old monolith references, apply `writing-skills` structure fixes if needed, and fix any other local skill problems found while touching the file.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- animator: rating=keep; recipes split to recipes/animator/<command>.md; skills/animator/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/animator/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/animator/<command>\.md`|recipes/animator/' "/home/batih/personal/unity-skills/skills/animator/SKILL.md" "/home/batih/personal/unity-skills/recipes/animator/README.md" && rg -n 'animator-recipes\.md' "/home/batih/personal/unity-skills/skills/animator/SKILL.md"
```

Expected: first checks pass, last command returns no matches.

```bash
git add "/home/batih/personal/unity-skills/skills/animator/SKILL.md" "/home/batih/personal/unity-skills/recipes/animator" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split animator recipes by command"
```

---

### Task 4: Asset Skill And Recipes

**Files:**
- Modify: `skills/asset/SKILL.md`
- Create: `recipes/asset/README.md`
- Create: `recipes/asset/*.md`
- Source: `recipes/asset-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/AssetSkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the asset recipes by command**

Create `recipes/asset/<command>.md` for every command documented in `skills/asset/SKILL.md`. Match each split file against `AssetSkills.cs`.

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/asset/<command>.md`
```

Replace old monolith references, keep the skill compact, and fix any local table, routing, or wording problems.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- asset: rating=keep; recipes split to recipes/asset/<command>.md; skills/asset/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/asset/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/asset/<command>\.md`|recipes/asset/' "/home/batih/personal/unity-skills/skills/asset/SKILL.md" "/home/batih/personal/unity-skills/recipes/asset/README.md" && rg -n 'asset-recipes\.md' "/home/batih/personal/unity-skills/skills/asset/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/asset/SKILL.md" "/home/batih/personal/unity-skills/recipes/asset" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split asset recipes by command"
```

---

### Task 5: Camera Skill And Recipes

**Files:**
- Modify: `skills/camera/SKILL.md`
- Create: `recipes/camera/README.md`
- Create: `recipes/camera/*.md`
- Source: `recipes/camera-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/CameraSkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the camera recipes by command**

Create `recipes/camera/<command>.md` for every command documented in `skills/camera/SKILL.md`. Match each split file against `CameraSkills.cs`.

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/camera/<command>.md`
```

Keep the native-tool overlap obvious if it exists, and fix any local issues found while touching the skill.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- camera: rating=shrink; recipes split to recipes/camera/<command>.md; skills/camera/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/camera/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/camera/<command>\.md`|recipes/camera/' "/home/batih/personal/unity-skills/skills/camera/SKILL.md" "/home/batih/personal/unity-skills/recipes/camera/README.md" && rg -n 'camera-recipes\.md' "/home/batih/personal/unity-skills/skills/camera/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/camera/SKILL.md" "/home/batih/personal/unity-skills/recipes/camera" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split camera recipes by command"
```

---

### Task 6: Cinemachine Skill And Recipes

**Files:**
- Modify: `skills/cinemachine/SKILL.md`
- Create: `recipes/cinemachine/README.md`
- Create: `recipes/cinemachine/*.md`
- Source: `recipes/cinemachine-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/CinemachineSkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the cinemachine recipes by command**

Create `recipes/cinemachine/<command>.md` for every command documented in `skills/cinemachine/SKILL.md`. Match each split file against `CinemachineSkills.cs`.

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/cinemachine/<command>.md`
```

Replace old monolith references, keep example sections accurate, and fix any local skill problems.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- cinemachine: rating=keep; recipes split to recipes/cinemachine/<command>.md; skills/cinemachine/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/cinemachine/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/cinemachine/<command>\.md`|recipes/cinemachine/' "/home/batih/personal/unity-skills/skills/cinemachine/SKILL.md" "/home/batih/personal/unity-skills/recipes/cinemachine/README.md" && rg -n 'cinemachine-recipes\.md' "/home/batih/personal/unity-skills/skills/cinemachine/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/cinemachine/SKILL.md" "/home/batih/personal/unity-skills/recipes/cinemachine" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split cinemachine recipes by command"
```

---

### Task 7: Cleaner Skill And Recipes

**Files:**
- Modify: `skills/cleaner/SKILL.md`
- Create: `recipes/cleaner/README.md`
- Create: `recipes/cleaner/*.md`
- Source: `recipes/cleaner-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/CleanerSkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the cleaner recipes by command**

Create `recipes/cleaner/<command>.md` for every command documented in `skills/cleaner/SKILL.md`. Match each split file against `CleanerSkills.cs`.

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/cleaner/<command>.md`
```

Replace monolith references and fix any local skill issues.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- cleaner: rating=keep; recipes split to recipes/cleaner/<command>.md; skills/cleaner/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/cleaner/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/cleaner/<command>\.md`|recipes/cleaner/' "/home/batih/personal/unity-skills/skills/cleaner/SKILL.md" "/home/batih/personal/unity-skills/recipes/cleaner/README.md" && rg -n 'cleaner-recipes\.md' "/home/batih/personal/unity-skills/skills/cleaner/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/cleaner/SKILL.md" "/home/batih/personal/unity-skills/recipes/cleaner" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split cleaner recipes by command"
```

---

### Task 8: Component Skill And Recipes

**Files:**
- Modify: `skills/component/SKILL.md`
- Create: `recipes/component/README.md`
- Create: `recipes/component/*.md`
- Source: `recipes/component-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/ComponentSkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the component recipes by command**

Create `recipes/component/<command>.md` for every command documented in `skills/component/SKILL.md`. Match each split file against `ComponentSkills.cs`.

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/component/<command>.md`
```

Replace monolith references and fix any local skill issues.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- component: rating=keep; recipes split to recipes/component/<command>.md; skills/component/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/component/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/component/<command>\.md`|recipes/component/' "/home/batih/personal/unity-skills/skills/component/SKILL.md" "/home/batih/personal/unity-skills/recipes/component/README.md" && rg -n 'component-recipes\.md' "/home/batih/personal/unity-skills/skills/component/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/component/SKILL.md" "/home/batih/personal/unity-skills/recipes/component" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split component recipes by command"
```

---

### Task 9: Console Skill And Recipes

**Files:**
- Modify: `skills/console/SKILL.md`
- Create: `recipes/console/README.md`
- Create: `recipes/console/*.md`
- Source: `recipes/console-recipes.md`
- Source: `recipes/debug-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/ConsoleSkills.cs`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/DebugSkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the console recipes by command**

Create `recipes/console/<command>.md` for every command documented in `skills/console/SKILL.md`. Match each split file against `ConsoleSkills.cs`.

Also keep these three retained `debug` commands under `recipes/console/` and match them against `DebugSkills.cs`:

```markdown
debug_force_recompile.md
debug_get_defines.md
debug_set_defines.md
```

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/console/<command>.md`
```

Keep the native-tool-first guidance, replace monolith references, and fix any local skill issues.

Add a compact section documenting that the standalone `debug` module is gone and only these define-symbol / recompile commands remain available here:

```markdown
debug_force_recompile
debug_get_defines
debug_set_defines
```

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- console: rating=shrink; recipes split to recipes/console/<command>.md; retained debug_force_recompile/debug_get_defines/debug_set_defines merged here.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/console/README.md" && test -f "/home/batih/personal/unity-skills/recipes/console/debug_force_recompile.md" && test -f "/home/batih/personal/unity-skills/recipes/console/debug_get_defines.md" && test -f "/home/batih/personal/unity-skills/recipes/console/debug_set_defines.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/console/<command>\.md`|recipes/console/' "/home/batih/personal/unity-skills/skills/console/SKILL.md" "/home/batih/personal/unity-skills/recipes/console/README.md" && ! rg -n 'console-recipes\.md|debug-recipes\.md' "/home/batih/personal/unity-skills/skills/console/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/console/SKILL.md" "/home/batih/personal/unity-skills/recipes/console" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split console recipes by command"
```

---

### Task 10: Debug Skill And Recipes

**Files:**
- Delete: `skills/debug/SKILL.md`
- Delete: `recipes/debug-recipes.md`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Remove the standalone debug skill**

Delete `skills/debug/SKILL.md`. Do not keep a standalone debug domain.

- [ ] **Step 2: Remove the old debug recipe monolith**

Delete `recipes/debug-recipes.md` after the retained commands have been moved into `recipes/console/` in Task 9.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- debug: rating=remove; standalone debug skill removed; retained define-symbol and recompile commands merged into console.
```

Run:
```bash
test ! -e "/home/batih/personal/unity-skills/skills/debug/SKILL.md" && test ! -e "/home/batih/personal/unity-skills/recipes/debug-recipes.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/debug/SKILL.md" "/home/batih/personal/unity-skills/recipes/debug-recipes.md" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: remove standalone debug skill"
```

---

### Task 11: Editor Skill And Recipes

**Files:**
- Modify: `skills/editor/SKILL.md`
- Create: `recipes/editor/README.md`
- Create: `recipes/editor/*.md`
- Source: `recipes/editor-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/EditorSkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the editor recipes by command**

Create `recipes/editor/<command>.md` for every command documented in `skills/editor/SKILL.md`. Match each split file against `EditorSkills.cs`.

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/editor/<command>.md`
```

Keep native-tool overlap clear, replace monolith references, and fix any local skill issues.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- editor: rating=shrink; recipes split to recipes/editor/<command>.md; skills/editor/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/editor/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/editor/<command>\.md`|recipes/editor/' "/home/batih/personal/unity-skills/skills/editor/SKILL.md" "/home/batih/personal/unity-skills/recipes/editor/README.md" && rg -n 'editor-recipes\.md' "/home/batih/personal/unity-skills/skills/editor/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/editor/SKILL.md" "/home/batih/personal/unity-skills/recipes/editor" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split editor recipes by command"
```

---

### Task 12: Event Skill And Recipes

**Files:**
- Modify: `skills/event/SKILL.md`
- Create: `recipes/event/README.md`
- Create: `recipes/event/*.md`
- Source: `recipes/event-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/EventSkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the event recipes by command**

Create `recipes/event/<command>.md` for every command documented in `skills/event/SKILL.md`. Match each split file against `EventSkills.cs`.

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/event/<command>.md`
```

Replace monolith references and fix any local skill issues.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- event: rating=keep; recipes split to recipes/event/<command>.md; skills/event/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/event/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/event/<command>\.md`|recipes/event/' "/home/batih/personal/unity-skills/skills/event/SKILL.md" "/home/batih/personal/unity-skills/recipes/event/README.md" && rg -n 'event-recipes\.md' "/home/batih/personal/unity-skills/skills/event/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/event/SKILL.md" "/home/batih/personal/unity-skills/recipes/event" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split event recipes by command"
```

---

### Task 13: GameObject Skill And Recipes

**Files:**
- Modify: `skills/gameobject/SKILL.md`
- Create: `recipes/gameobject/README.md`
- Create: `recipes/gameobject/*.md`
- Source: `recipes/gameobject-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/GameObjectSkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the gameobject recipes by command**

Create `recipes/gameobject/<command>.md` for every command documented in `skills/gameobject/SKILL.md`. Match each split file against `GameObjectSkills.cs`.

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/gameobject/<command>.md`
```

Replace monolith references, rename misleading example stubs, and fix any local skill issues.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- gameobject: rating=keep; recipes split to recipes/gameobject/<command>.md; skills/gameobject/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/gameobject/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/gameobject/<command>\.md`|recipes/gameobject/' "/home/batih/personal/unity-skills/skills/gameobject/SKILL.md" "/home/batih/personal/unity-skills/recipes/gameobject/README.md" && rg -n 'gameobject-recipes\.md' "/home/batih/personal/unity-skills/skills/gameobject/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/gameobject/SKILL.md" "/home/batih/personal/unity-skills/recipes/gameobject" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split gameobject recipes by command"
```

---

### Task 14: Importer Skill And Recipes

**Files:**
- Modify: `skills/importer/SKILL.md`
- Create: `recipes/importer/README.md`
- Create: `recipes/importer/*.md`
- Source: `recipes/assetimport-recipes.md`
- Source: `recipes/texture-recipes.md`
- Source: `recipes/audio-recipes.md`
- Source: `recipes/model-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/AssetImportSkills.cs`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/TextureSkills.cs`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/AudioSkills.cs`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/ModelSkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the importer recipes by command**

Create `recipes/importer/<command>.md` for every command documented in `skills/importer/SKILL.md`. Pull the content from `assetimport-recipes.md`, `texture-recipes.md`, `audio-recipes.md`, and `model-recipes.md`, then match each split file against the corresponding upstream `.cs` source.

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/importer/<command>.md`
```

Replace monolith references and fix any local skill issues.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- importer: rating=keep; recipes split to recipes/importer/<command>.md; skills/importer/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/importer/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/importer/<command>\.md`|recipes/importer/' "/home/batih/personal/unity-skills/skills/importer/SKILL.md" "/home/batih/personal/unity-skills/recipes/importer/README.md" && rg -n 'assetimport-recipes\.md|texture-recipes\.md|audio-recipes\.md|model-recipes\.md' "/home/batih/personal/unity-skills/skills/importer/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/importer/SKILL.md" "/home/batih/personal/unity-skills/recipes/importer" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split importer recipes by command"
```

---

### Task 15: Light Skill And Recipes

**Files:**
- Modify: `skills/light/SKILL.md`
- Create: `recipes/light/README.md`
- Create: `recipes/light/*.md`
- Source: `recipes/light-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/LightSkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the light recipes by command**

Create `recipes/light/<command>.md` for every command documented in `skills/light/SKILL.md`. Match each split file against `LightSkills.cs`.

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/light/<command>.md`
```

Replace monolith references and fix any local skill issues.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- light: rating=keep; recipes split to recipes/light/<command>.md; skills/light/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/light/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/light/<command>\.md`|recipes/light/' "/home/batih/personal/unity-skills/skills/light/SKILL.md" "/home/batih/personal/unity-skills/recipes/light/README.md" && rg -n 'light-recipes\.md' "/home/batih/personal/unity-skills/skills/light/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/light/SKILL.md" "/home/batih/personal/unity-skills/recipes/light" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split light recipes by command"
```

---

### Task 16: Material Skill And Recipes

**Files:**
- Modify: `skills/material/SKILL.md`
- Create: `recipes/material/README.md`
- Create: `recipes/material/*.md`
- Source: `recipes/material-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/MaterialSkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the material recipes by command**

Create `recipes/material/<command>.md` for every command documented in `skills/material/SKILL.md`. Match each split file against `MaterialSkills.cs`.

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/material/<command>.md`
```

Replace monolith references and fix any local skill issues.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- material: rating=keep; recipes split to recipes/material/<command>.md; skills/material/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/material/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/material/<command>\.md`|recipes/material/' "/home/batih/personal/unity-skills/skills/material/SKILL.md" "/home/batih/personal/unity-skills/recipes/material/README.md" && rg -n 'material-recipes\.md' "/home/batih/personal/unity-skills/skills/material/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/material/SKILL.md" "/home/batih/personal/unity-skills/recipes/material" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split material recipes by command"
```

---

### Task 17: NavMesh Skill And Recipes

**Files:**
- Modify: `skills/navmesh/SKILL.md`
- Create: `recipes/navmesh/README.md`
- Create: `recipes/navmesh/*.md`
- Source: `recipes/navmesh-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/NavMeshSkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the navmesh recipes by command**

Create `recipes/navmesh/<command>.md` for every command documented in `skills/navmesh/SKILL.md`. Match each split file against `NavMeshSkills.cs`.

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/navmesh/<command>.md`
```

Replace monolith references and fix any local skill issues.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- navmesh: rating=keep; recipes split to recipes/navmesh/<command>.md; skills/navmesh/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/navmesh/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/navmesh/<command>\.md`|recipes/navmesh/' "/home/batih/personal/unity-skills/skills/navmesh/SKILL.md" "/home/batih/personal/unity-skills/recipes/navmesh/README.md" && rg -n 'navmesh-recipes\.md' "/home/batih/personal/unity-skills/skills/navmesh/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/navmesh/SKILL.md" "/home/batih/personal/unity-skills/recipes/navmesh" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split navmesh recipes by command"
```

---

### Task 18: Optimization Skill And Recipes

**Files:**
- Modify: `skills/optimization/SKILL.md`
- Create: `recipes/optimization/README.md`
- Create: `recipes/optimization/*.md`
- Source: `recipes/optimization-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/OptimizationSkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the optimization recipes by command**

Create `recipes/optimization/<command>.md` for every command documented in `skills/optimization/SKILL.md`. Match each split file against `OptimizationSkills.cs`.

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/optimization/<command>.md`
```

Replace monolith references and fix any local skill issues.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- optimization: rating=keep; recipes split to recipes/optimization/<command>.md; skills/optimization/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/optimization/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/optimization/<command>\.md`|recipes/optimization/' "/home/batih/personal/unity-skills/skills/optimization/SKILL.md" "/home/batih/personal/unity-skills/recipes/optimization/README.md" && rg -n 'optimization-recipes\.md' "/home/batih/personal/unity-skills/skills/optimization/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/optimization/SKILL.md" "/home/batih/personal/unity-skills/recipes/optimization" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split optimization recipes by command"
```

---

### Task 19: Package Skill And Recipes

**Files:**
- Modify: `skills/package/SKILL.md`
- Create: `recipes/package/README.md`
- Create: `recipes/package/*.md`
- Source: `recipes/package-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/PackageSkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the package recipes by command**

Create `recipes/package/<command>.md` for every command documented in `skills/package/SKILL.md`. Match each split file against `PackageSkills.cs`.

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/package/<command>.md`
```

Keep the native-tool-first guidance, replace monolith references, and fix any local skill issues.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- package: rating=shrink; recipes split to recipes/package/<command>.md; skills/package/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/package/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/package/<command>\.md`|recipes/package/' "/home/batih/personal/unity-skills/skills/package/SKILL.md" "/home/batih/personal/unity-skills/recipes/package/README.md" && rg -n 'package-recipes\.md' "/home/batih/personal/unity-skills/skills/package/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/package/SKILL.md" "/home/batih/personal/unity-skills/recipes/package" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split package recipes by command"
```

---

### Task 20: Perception Skill And Recipes

**Files:**
- Modify: `skills/perception/SKILL.md`
- Create: `recipes/perception/README.md`
- Create: `recipes/perception/*.md`
- Source: `recipes/perception-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/PerceptionSkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the perception recipes by command**

Create `recipes/perception/<command>.md` for every command documented in `skills/perception/SKILL.md`. Match each split file against `PerceptionSkills.cs`.

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/perception/<command>.md`
```

Replace monolith references and fix any local skill issues.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- perception: rating=keep; recipes split to recipes/perception/<command>.md; skills/perception/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/perception/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/perception/<command>\.md`|recipes/perception/' "/home/batih/personal/unity-skills/skills/perception/SKILL.md" "/home/batih/personal/unity-skills/recipes/perception/README.md" && rg -n 'perception-recipes\.md' "/home/batih/personal/unity-skills/skills/perception/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/perception/SKILL.md" "/home/batih/personal/unity-skills/recipes/perception" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split perception recipes by command"
```

---

### Task 21: Physics Skill And Recipes

**Files:**
- Modify: `skills/physics/SKILL.md`
- Create: `recipes/physics/README.md`
- Create: `recipes/physics/*.md`
- Source: `recipes/physics-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/PhysicsSkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the physics recipes by command**

Create `recipes/physics/<command>.md` for every command documented in `skills/physics/SKILL.md`. Match each split file against `PhysicsSkills.cs`.

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/physics/<command>.md`
```

Replace monolith references and fix any local skill issues.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- physics: rating=keep; recipes split to recipes/physics/<command>.md; skills/physics/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/physics/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/physics/<command>\.md`|recipes/physics/' "/home/batih/personal/unity-skills/skills/physics/SKILL.md" "/home/batih/personal/unity-skills/recipes/physics/README.md" && rg -n 'physics-recipes\.md' "/home/batih/personal/unity-skills/skills/physics/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/physics/SKILL.md" "/home/batih/personal/unity-skills/recipes/physics" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split physics recipes by command"
```

---

### Task 22: Prefab Skill And Recipes

**Files:**
- Modify: `skills/prefab/SKILL.md`
- Create: `recipes/prefab/README.md`
- Create: `recipes/prefab/*.md`
- Source: `recipes/prefab-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/PrefabSkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the prefab recipes by command**

Create `recipes/prefab/<command>.md` for every command documented in `skills/prefab/SKILL.md`. Match each split file against `PrefabSkills.cs`.

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/prefab/<command>.md`
```

Replace monolith references and fix any local skill issues.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- prefab: rating=keep; recipes split to recipes/prefab/<command>.md; skills/prefab/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/prefab/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/prefab/<command>\.md`|recipes/prefab/' "/home/batih/personal/unity-skills/skills/prefab/SKILL.md" "/home/batih/personal/unity-skills/recipes/prefab/README.md" && rg -n 'prefab-recipes\.md' "/home/batih/personal/unity-skills/skills/prefab/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/prefab/SKILL.md" "/home/batih/personal/unity-skills/recipes/prefab" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split prefab recipes by command"
```

---

### Task 23: ProBuilder Skill And Recipes

**Files:**
- Modify: `skills/probuilder/SKILL.md`
- Create: `recipes/probuilder/README.md`
- Create: `recipes/probuilder/*.md`
- Source: `recipes/probuilder-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/ProBuilderSkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the probuilder recipes by command**

Create `recipes/probuilder/<command>.md` for every command documented in `skills/probuilder/SKILL.md`. Match each split file against `ProBuilderSkills.cs`.

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/probuilder/<command>.md`
```

Replace monolith references and fix any local skill issues.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- probuilder: rating=keep; recipes split to recipes/probuilder/<command>.md; skills/probuilder/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/probuilder/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/probuilder/<command>\.md`|recipes/probuilder/' "/home/batih/personal/unity-skills/skills/probuilder/SKILL.md" "/home/batih/personal/unity-skills/recipes/probuilder/README.md" && rg -n 'probuilder-recipes\.md' "/home/batih/personal/unity-skills/skills/probuilder/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/probuilder/SKILL.md" "/home/batih/personal/unity-skills/recipes/probuilder" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split probuilder recipes by command"
```

---

### Task 24: Profiler Skill And Recipes

**Files:**
- Delete: `skills/profiler/SKILL.md`
- Delete: `recipes/profiler-recipes.md`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Remove the standalone profiler skill**

Delete `skills/profiler/SKILL.md`.

- [ ] **Step 2: Remove the old profiler recipe monolith**

Delete `recipes/profiler-recipes.md`.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- profiler: rating=remove; standalone profiler skill removed; use native Unity_Profiler_* tools through mcp-tools.md.
```

Run:
```bash
test ! -e "/home/batih/personal/unity-skills/skills/profiler/SKILL.md" && test ! -e "/home/batih/personal/unity-skills/recipes/profiler-recipes.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/profiler/SKILL.md" "/home/batih/personal/unity-skills/recipes/profiler-recipes.md" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: remove standalone profiler skill"
```

---

### Task 25: Project Skill And Recipes

**Files:**
- Modify: `skills/project/SKILL.md`
- Create: `recipes/project/README.md`
- Create: `recipes/project/*.md`
- Source: `recipes/project-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/ProjectSkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the project recipes by command**

Create `recipes/project/<command>.md` for every command documented in `skills/project/SKILL.md`. Match each split file against `ProjectSkills.cs`.

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/project/<command>.md`
```

Keep native-tool overlap clear, replace monolith references, and fix any local skill issues.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- project: rating=shrink; recipes split to recipes/project/<command>.md; skills/project/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/project/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/project/<command>\.md`|recipes/project/' "/home/batih/personal/unity-skills/skills/project/SKILL.md" "/home/batih/personal/unity-skills/recipes/project/README.md" && rg -n 'project-recipes\.md' "/home/batih/personal/unity-skills/skills/project/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/project/SKILL.md" "/home/batih/personal/unity-skills/recipes/project" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split project recipes by command"
```

---

### Task 26: Sample Skill And Recipes

**Files:**
- Modify: `skills/sample/SKILL.md`
- Create: `recipes/sample/README.md`
- Create: `recipes/sample/*.md`
- Source: `recipes/sample-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/SampleSkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the sample recipes by command**

Create `recipes/sample/<command>.md` for every command documented in `skills/sample/SKILL.md`. Match each split file against `SampleSkills.cs`.

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/sample/<command>.md`
```

Replace monolith references and fix any local skill issues.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- sample: rating=keep; recipes split to recipes/sample/<command>.md; skills/sample/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/sample/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/sample/<command>\.md`|recipes/sample/' "/home/batih/personal/unity-skills/skills/sample/SKILL.md" "/home/batih/personal/unity-skills/recipes/sample/README.md" && rg -n 'sample-recipes\.md' "/home/batih/personal/unity-skills/skills/sample/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/sample/SKILL.md" "/home/batih/personal/unity-skills/recipes/sample" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split sample recipes by command"
```

---

### Task 27: Scene Skill And Recipes

**Files:**
- Modify: `skills/scene/SKILL.md`
- Create: `recipes/scene/README.md`
- Create: `recipes/scene/*.md`
- Source: `recipes/scene-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/SceneSkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the scene recipes by command**

Create `recipes/scene/<command>.md` for every command documented in `skills/scene/SKILL.md`. Match each split file against `SceneSkills.cs`.

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/scene/<command>.md`
```

Replace monolith references and fix any local skill issues.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- scene: rating=keep; recipes split to recipes/scene/<command>.md; skills/scene/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/scene/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/scene/<command>\.md`|recipes/scene/' "/home/batih/personal/unity-skills/skills/scene/SKILL.md" "/home/batih/personal/unity-skills/recipes/scene/README.md" && rg -n 'scene-recipes\.md' "/home/batih/personal/unity-skills/skills/scene/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/scene/SKILL.md" "/home/batih/personal/unity-skills/recipes/scene" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split scene recipes by command"
```

---

### Task 28: Script Skill And Recipes

**Files:**
- Modify: `skills/script/SKILL.md`
- Delete: `recipes/script-recipes.md`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Reduce the script skill to guardrails only**

Keep only:

```markdown
- native-tool-first instruction
- filename/class-name matching rule
- domain reload and compile warning guidance
- best-practice guardrails worth keeping for future agents
```

Delete API tables, pseudo-REST command inventory, and recipe fallback sections.

- [ ] **Step 2: Remove the old script recipe monolith**

Delete `recipes/script-recipes.md`.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- script: rating=shrink; kept as guardrails only; API tables removed; no standalone recipe split retained.
```

Run:
```bash
test ! -e "/home/batih/personal/unity-skills/recipes/script-recipes.md" && rg -n 'filename|class|domain reload|compile' "/home/batih/personal/unity-skills/skills/script/SKILL.md" && ! rg -n 'script-recipes\.md|Recipe path rule:' "/home/batih/personal/unity-skills/skills/script/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/script/SKILL.md" "/home/batih/personal/unity-skills/recipes/script-recipes.md" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: reduce script skill to guardrails"
```

---

### Task 29: ScriptableObject Skill And Recipes

**Files:**
- Modify: `skills/scriptableobject/SKILL.md`
- Create: `recipes/scriptableobject/README.md`
- Create: `recipes/scriptableobject/*.md`
- Source: `recipes/scriptableobject-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/ScriptableObjectSkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the scriptableobject recipes by command**

Create `recipes/scriptableobject/<command>.md` for every command documented in `skills/scriptableobject/SKILL.md`. Match each split file against `ScriptableObjectSkills.cs`.

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/scriptableobject/<command>.md`
```

Replace monolith references and fix any local skill issues.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- scriptableobject: rating=keep; recipes split to recipes/scriptableobject/<command>.md; skills/scriptableobject/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/scriptableobject/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/scriptableobject/<command>\.md`|recipes/scriptableobject/' "/home/batih/personal/unity-skills/skills/scriptableobject/SKILL.md" "/home/batih/personal/unity-skills/recipes/scriptableobject/README.md" && rg -n 'scriptableobject-recipes\.md' "/home/batih/personal/unity-skills/skills/scriptableobject/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/scriptableobject/SKILL.md" "/home/batih/personal/unity-skills/recipes/scriptableobject" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split scriptableobject recipes by command"
```

---

### Task 30: Shader Skill And Recipes

**Files:**
- Modify: `skills/shader/SKILL.md`
- Create: `recipes/shader/README.md`
- Create: `recipes/shader/*.md`
- Source: `recipes/shader-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/ShaderSkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the shader recipes by command**

Create `recipes/shader/<command>.md` for every command documented in `skills/shader/SKILL.md`. Match each split file against `ShaderSkills.cs`.

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/shader/<command>.md`
```

Replace monolith references and fix any local skill issues.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- shader: rating=keep; recipes split to recipes/shader/<command>.md; skills/shader/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/shader/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/shader/<command>\.md`|recipes/shader/' "/home/batih/personal/unity-skills/skills/shader/SKILL.md" "/home/batih/personal/unity-skills/recipes/shader/README.md" && rg -n 'shader-recipes\.md' "/home/batih/personal/unity-skills/skills/shader/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/shader/SKILL.md" "/home/batih/personal/unity-skills/recipes/shader" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split shader recipes by command"
```

---

### Task 31: Smart Skill And Recipes

**Files:**
- Modify: `skills/smart/SKILL.md`
- Create: `recipes/smart/README.md`
- Create: `recipes/smart/*.md`
- Source: `recipes/smart-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/SmartSkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the smart recipes by command**

Create `recipes/smart/<command>.md` for every command documented in `skills/smart/SKILL.md`. Match each split file against `SmartSkills.cs`.

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/smart/<command>.md`
```

Replace monolith references and fix any local skill issues.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- smart: rating=keep; recipes split to recipes/smart/<command>.md; skills/smart/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/smart/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/smart/<command>\.md`|recipes/smart/' "/home/batih/personal/unity-skills/skills/smart/SKILL.md" "/home/batih/personal/unity-skills/recipes/smart/README.md" && rg -n 'smart-recipes\.md' "/home/batih/personal/unity-skills/skills/smart/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/smart/SKILL.md" "/home/batih/personal/unity-skills/recipes/smart" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split smart recipes by command"
```

---

### Task 32: Terrain Skill And Recipes

**Files:**
- Modify: `skills/terrain/SKILL.md`
- Create: `recipes/terrain/README.md`
- Create: `recipes/terrain/*.md`
- Source: `recipes/terrain-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/TerrainSkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the terrain recipes by command**

Create `recipes/terrain/<command>.md` for every command documented in `skills/terrain/SKILL.md`. Match each split file against `TerrainSkills.cs`.

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/terrain/<command>.md`
```

Replace monolith references and fix any local skill issues.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- terrain: rating=keep; recipes split to recipes/terrain/<command>.md; skills/terrain/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/terrain/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/terrain/<command>\.md`|recipes/terrain/' "/home/batih/personal/unity-skills/skills/terrain/SKILL.md" "/home/batih/personal/unity-skills/recipes/terrain/README.md" && rg -n 'terrain-recipes\.md' "/home/batih/personal/unity-skills/skills/terrain/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/terrain/SKILL.md" "/home/batih/personal/unity-skills/recipes/terrain" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split terrain recipes by command"
```

---

### Task 33: Test Skill And Recipes

**Files:**
- Modify: `skills/test/SKILL.md`
- Create: `recipes/test/README.md`
- Create: `recipes/test/*.md`
- Source: `recipes/test-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/TestSkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the test recipes by command**

Create `recipes/test/<command>.md` for every command documented in `skills/test/SKILL.md`. Match each split file against `TestSkills.cs`.

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/test/<command>.md`
```

Replace monolith references and fix any local skill issues.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- test: rating=keep; recipes split to recipes/test/<command>.md; skills/test/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/test/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/test/<command>\.md`|recipes/test/' "/home/batih/personal/unity-skills/skills/test/SKILL.md" "/home/batih/personal/unity-skills/recipes/test/README.md" && rg -n 'test-recipes\.md' "/home/batih/personal/unity-skills/skills/test/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/test/SKILL.md" "/home/batih/personal/unity-skills/recipes/test" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split test recipes by command"
```

---

### Task 34: Timeline Skill And Recipes

**Files:**
- Modify: `skills/timeline/SKILL.md`
- Create: `recipes/timeline/README.md`
- Create: `recipes/timeline/*.md`
- Source: `recipes/timeline-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/TimelineSkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the timeline recipes by command**

Create `recipes/timeline/<command>.md` for every command documented in `skills/timeline/SKILL.md`. Match each split file against `TimelineSkills.cs`.

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/timeline/<command>.md`
```

Replace monolith references and fix any local skill issues.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- timeline: rating=keep; recipes split to recipes/timeline/<command>.md; skills/timeline/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/timeline/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/timeline/<command>\.md`|recipes/timeline/' "/home/batih/personal/unity-skills/skills/timeline/SKILL.md" "/home/batih/personal/unity-skills/recipes/timeline/README.md" && rg -n 'timeline-recipes\.md' "/home/batih/personal/unity-skills/skills/timeline/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/timeline/SKILL.md" "/home/batih/personal/unity-skills/recipes/timeline" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split timeline recipes by command"
```

---

### Task 35: UI Skill And Recipes

**Files:**
- Modify: `skills/ui/SKILL.md`
- Create: `recipes/ui/README.md`
- Create: `recipes/ui/*.md`
- Source: `recipes/ui-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/UISkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the UI recipes by command**

Create `recipes/ui/<command>.md` for every command documented in `skills/ui/SKILL.md`. Match each split file against `UISkills.cs`.

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/ui/<command>.md`
```

Replace monolith references and fix any local skill issues.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- ui: rating=keep; recipes split to recipes/ui/<command>.md; skills/ui/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/ui/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/ui/<command>\.md`|recipes/ui/' "/home/batih/personal/unity-skills/skills/ui/SKILL.md" "/home/batih/personal/unity-skills/recipes/ui/README.md" && rg -n 'ui-recipes\.md' "/home/batih/personal/unity-skills/skills/ui/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/ui/SKILL.md" "/home/batih/personal/unity-skills/recipes/ui" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split ui recipes by command"
```

---

### Task 36: UIToolkit Skill And Recipes

**Files:**
- Modify: `skills/uitoolkit/SKILL.md`
- Create: `recipes/uitoolkit/README.md`
- Create: `recipes/uitoolkit/*.md`
- Source: `recipes/uitoolkit-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/UIToolkitSkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the UIToolkit recipes by command**

Create `recipes/uitoolkit/<command>.md` for every command documented in `skills/uitoolkit/SKILL.md`. Match each split file against `UIToolkitSkills.cs`.

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/uitoolkit/<command>.md`
```

Replace monolith references and fix any local skill issues.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- uitoolkit: rating=keep; recipes split to recipes/uitoolkit/<command>.md; skills/uitoolkit/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/uitoolkit/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/uitoolkit/<command>\.md`|recipes/uitoolkit/' "/home/batih/personal/unity-skills/skills/uitoolkit/SKILL.md" "/home/batih/personal/unity-skills/recipes/uitoolkit/README.md" && rg -n 'uitoolkit-recipes\.md' "/home/batih/personal/unity-skills/skills/uitoolkit/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/uitoolkit/SKILL.md" "/home/batih/personal/unity-skills/recipes/uitoolkit" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split uitoolkit recipes by command"
```

---

### Task 37: Validation Skill And Recipes

**Files:**
- Modify: `skills/validation/SKILL.md`
- Create: `recipes/validation/README.md`
- Create: `recipes/validation/*.md`
- Source: `recipes/validation-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/ValidationSkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the validation recipes by command**

Create `recipes/validation/<command>.md` for every command documented in `skills/validation/SKILL.md`. Match each split file against `ValidationSkills.cs`.

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/validation/<command>.md`
```

Replace monolith references and fix any local skill issues.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- validation: rating=keep; recipes split to recipes/validation/<command>.md; skills/validation/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/validation/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/validation/<command>\.md`|recipes/validation/' "/home/batih/personal/unity-skills/skills/validation/SKILL.md" "/home/batih/personal/unity-skills/recipes/validation/README.md" && rg -n 'validation-recipes\.md' "/home/batih/personal/unity-skills/skills/validation/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/validation/SKILL.md" "/home/batih/personal/unity-skills/recipes/validation" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split validation recipes by command"
```

---

### Task 38: Workflow Skill And Recipes

**Files:**
- Modify: `skills/workflow/SKILL.md`
- Create: `recipes/workflow/README.md`
- Create: `recipes/workflow/*.md`
- Source: `recipes/workflow-recipes.md`
- Source: `recipes/batch-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/WorkflowSkills.cs`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/BatchSkills.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the workflow recipes by command**

Create `recipes/workflow/<command>.md` for every command documented in `skills/workflow/SKILL.md`. Pull commands from `workflow-recipes.md` and `batch-recipes.md`, then match each split file against the corresponding upstream `.cs` source.

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/workflow/<command>.md`
```

Replace monolith references and fix any local skill issues.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- workflow: rating=keep; recipes split to recipes/workflow/<command>.md; skills/workflow/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/workflow/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/workflow/<command>\.md`|recipes/workflow/' "/home/batih/personal/unity-skills/skills/workflow/SKILL.md" "/home/batih/personal/unity-skills/recipes/workflow/README.md" && rg -n 'workflow-recipes\.md|batch-recipes\.md' "/home/batih/personal/unity-skills/skills/workflow/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/workflow/SKILL.md" "/home/batih/personal/unity-skills/recipes/workflow" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split workflow recipes by command"
```

---

### Task 39: XR Skill And Recipes

**Files:**
- Modify: `skills/xr/SKILL.md`
- Create: `recipes/xr/README.md`
- Create: `recipes/xr/*.md`
- Source: `recipes/xr-recipes.md`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/XRSkills.cs`
- Reference: `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/XRReflectionHelper.cs`
- Modify: `MIGRATION.md`

- [ ] **Step 1: Split the XR recipes by command**

Create `recipes/xr/<command>.md` for every command documented in `skills/xr/SKILL.md`. Match each split file against `XRSkills.cs` and `XRReflectionHelper.cs`.

- [ ] **Step 2: Update the skill and fix local issues**

Add:

```markdown
Recipe path rule: `../../recipes/xr/<command>.md`
```

Replace monolith references and fix any local skill issues.

- [ ] **Step 3: Log, verify, and commit**

Append:

```markdown
- xr: rating=keep; recipes split to recipes/xr/<command>.md; skills/xr/SKILL.md refreshed.
```

Run:
```bash
test -f "/home/batih/personal/unity-skills/recipes/xr/README.md" && rg -n 'Recipe path rule: `\.\./\.\./recipes/xr/<command>\.md`|recipes/xr/' "/home/batih/personal/unity-skills/skills/xr/SKILL.md" "/home/batih/personal/unity-skills/recipes/xr/README.md" && rg -n 'xr-recipes\.md' "/home/batih/personal/unity-skills/skills/xr/SKILL.md"
```

```bash
git add "/home/batih/personal/unity-skills/skills/xr/SKILL.md" "/home/batih/personal/unity-skills/recipes/xr" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: split xr recipes by command"
```

---

### Task 40: Remaining Skill Tasks

**Files:**
- Modify: remaining `skills/*/SKILL.md` not already handled by their own dedicated tasks above

- [ ] **Step 1: Expand the same per-skill template to every remaining skill not already covered above**

For every skill directory under `skills/` that is not the root index and does not already have a completed per-skill task above, create the matching `recipes/<topic>/README.md`, split `recipes/<topic>/<command>.md` files from the current monolith, add the local `Recipe path rule: ../../recipes/<topic>/<command>.md` when the skill remains recipe-backed, fix any local skill issues, append a migration entry, verify, and commit.

Do not recreate exceptions already decided in this plan:

```markdown
- debug stays removed as a standalone skill
- profiler stays removed completely
- script stays guardrails-only with no recipe split
```

- [ ] **Step 2: Ensure skill count coverage**

Run:
```bash
ls "/home/batih/personal/unity-skills/skills" | wc -l
ls "/home/batih/personal/unity-skills/recipes" | wc -l
```

Expected: every topic skill in `skills/` has been addressed by its own task or by the explicit catch-up step in this task.

- [ ] **Step 3: Commit any remaining per-skill migrations**

```bash
git add "/home/batih/personal/unity-skills/skills" "/home/batih/personal/unity-skills/recipes" "/home/batih/personal/unity-skills/MIGRATION.md"
git commit -m "docs: finish per-skill recipe split coverage"
```

---

### Task 41: Delete Legacy Monolith Recipe Files And Verify Repo

**Files:**
- Delete: `recipes/asset-recipes.md`
- Delete: `recipes/assetimport-recipes.md`
- Delete: `recipes/animator-recipes.md`
- Delete: `recipes/audio-recipes.md`
- Delete: `recipes/batch-recipes.md`
- Delete: `recipes/camera-recipes.md`
- Delete: `recipes/cinemachine-recipes.md`
- Delete: `recipes/cleaner-recipes.md`
- Delete: `recipes/component-recipes.md`
- Delete: `recipes/console-recipes.md`
- Delete: `recipes/debug-recipes.md`
- Delete: `recipes/editor-recipes.md`
- Delete: `recipes/event-recipes.md`
- Delete: `recipes/gameobject-recipes.md`
- Delete: `recipes/gameobject-finder-recipes.md`
- Delete: `recipes/light-recipes.md`
- Delete: `recipes/material-recipes.md`
- Delete: `recipes/model-recipes.md`
- Delete: `recipes/navmesh-recipes.md`
- Delete: `recipes/optimization-recipes.md`
- Delete: `recipes/package-recipes.md`
- Delete: `recipes/perception-recipes.md`
- Delete: `recipes/physics-recipes.md`
- Delete: `recipes/prefab-recipes.md`
- Delete: `recipes/probuilder-recipes.md`
- Delete: `recipes/profiler-recipes.md`
- Delete: `recipes/project-recipes.md`
- Delete: `recipes/sample-recipes.md`
- Delete: `recipes/scene-recipes.md`
- Delete: `recipes/script-recipes.md`
- Delete: `recipes/scriptableobject-recipes.md`
- Delete: `recipes/shader-recipes.md`
- Delete: `recipes/skills-common-recipes.md`
- Delete: `recipes/smart-recipes.md`
- Delete: `recipes/terrain-recipes.md`
- Delete: `recipes/test-recipes.md`
- Delete: `recipes/texture-recipes.md`
- Delete: `recipes/timeline-recipes.md`
- Delete: `recipes/ui-recipes.md`
- Delete: `recipes/uitoolkit-recipes.md`
- Delete: `recipes/validation-recipes.md`
- Delete: `recipes/workflow-recipes.md`
- Delete: `recipes/xr-recipes.md`
- Verify: `skills/*/SKILL.md`
- Verify: `recipes/*/README.md`
- Modify if needed: `README.md`

- [ ] **Step 1: Delete the old monolith recipe files**

Run:
```bash
git rm "/home/batih/personal/unity-skills/recipes/asset-recipes.md" "/home/batih/personal/unity-skills/recipes/assetimport-recipes.md" "/home/batih/personal/unity-skills/recipes/animator-recipes.md" "/home/batih/personal/unity-skills/recipes/audio-recipes.md" "/home/batih/personal/unity-skills/recipes/batch-recipes.md" "/home/batih/personal/unity-skills/recipes/camera-recipes.md" "/home/batih/personal/unity-skills/recipes/cinemachine-recipes.md" "/home/batih/personal/unity-skills/recipes/cleaner-recipes.md" "/home/batih/personal/unity-skills/recipes/component-recipes.md" "/home/batih/personal/unity-skills/recipes/console-recipes.md" "/home/batih/personal/unity-skills/recipes/debug-recipes.md" "/home/batih/personal/unity-skills/recipes/editor-recipes.md" "/home/batih/personal/unity-skills/recipes/event-recipes.md" "/home/batih/personal/unity-skills/recipes/gameobject-recipes.md" "/home/batih/personal/unity-skills/recipes/gameobject-finder-recipes.md" "/home/batih/personal/unity-skills/recipes/light-recipes.md" "/home/batih/personal/unity-skills/recipes/material-recipes.md" "/home/batih/personal/unity-skills/recipes/model-recipes.md" "/home/batih/personal/unity-skills/recipes/navmesh-recipes.md" "/home/batih/personal/unity-skills/recipes/optimization-recipes.md" "/home/batih/personal/unity-skills/recipes/package-recipes.md" "/home/batih/personal/unity-skills/recipes/perception-recipes.md" "/home/batih/personal/unity-skills/recipes/physics-recipes.md" "/home/batih/personal/unity-skills/recipes/prefab-recipes.md" "/home/batih/personal/unity-skills/recipes/probuilder-recipes.md" "/home/batih/personal/unity-skills/recipes/profiler-recipes.md" "/home/batih/personal/unity-skills/recipes/project-recipes.md" "/home/batih/personal/unity-skills/recipes/sample-recipes.md" "/home/batih/personal/unity-skills/recipes/scene-recipes.md" "/home/batih/personal/unity-skills/recipes/script-recipes.md" "/home/batih/personal/unity-skills/recipes/scriptableobject-recipes.md" "/home/batih/personal/unity-skills/recipes/shader-recipes.md" "/home/batih/personal/unity-skills/recipes/skills-common-recipes.md" "/home/batih/personal/unity-skills/recipes/smart-recipes.md" "/home/batih/personal/unity-skills/recipes/terrain-recipes.md" "/home/batih/personal/unity-skills/recipes/test-recipes.md" "/home/batih/personal/unity-skills/recipes/texture-recipes.md" "/home/batih/personal/unity-skills/recipes/timeline-recipes.md" "/home/batih/personal/unity-skills/recipes/ui-recipes.md" "/home/batih/personal/unity-skills/recipes/uitoolkit-recipes.md" "/home/batih/personal/unity-skills/recipes/validation-recipes.md" "/home/batih/personal/unity-skills/recipes/workflow-recipes.md" "/home/batih/personal/unity-skills/recipes/xr-recipes.md"
```

- [ ] **Step 2: Verify all skills use the new local rule**

Run:
```bash
rg -n 'Recipe path rule: `\.\./\.\./recipes/.+/<command>\.md`' "/home/batih/personal/unity-skills/skills" --glob '*/SKILL.md'
```

Expected: one hit in every recipe-backed topic skill. `script` is guardrails-only and should not have the local recipe path rule. Removed standalone skills such as `debug` and `profiler` should not exist.

- [ ] **Step 3: Verify no skill still points to a top-level `*-recipes.md` file**

Run:
```bash
rg -n 'recipes/.+-recipes\.md' "/home/batih/personal/unity-skills/skills" --glob '*/SKILL.md'
```

Expected: no matches.

- [ ] **Step 4: Verify each topic folder has an index file**

Run:
```bash
find "/home/batih/personal/unity-skills/recipes" -mindepth 1 -maxdepth 1 -type d | sort
```

Expected: each topic directory exists and contains `README.md`.

- [ ] **Step 5: Check top-level docs for contradictions**

Run:
```bash
rg -n 'recipes/<topic>-recipes|recipes/.+-recipes\.md|See `recipes/` directory' "/home/batih/personal/unity-skills/README.md" "/home/batih/personal/unity-skills/skills/SKILL.md" "/home/batih/personal/unity-skills/recipes/README.md"
```

Expected: no stale monolith references.

- [ ] **Step 6: Commit the final structure change**

```bash
git add "/home/batih/personal/unity-skills/recipes" "/home/batih/personal/unity-skills/skills" "/home/batih/personal/unity-skills/MIGRATION.md" "/home/batih/personal/unity-skills/README.md"
git commit -m "docs: switch recipes to per-command structure"
```
