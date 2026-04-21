# Standalone MCP Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Transform the unity-skills repository from a REST-based framework into a standard, pass-through MCP skill and recipe library. Ensure zero feature loss by extracting all C# logic into `recipes/` with explicit paste-able code. Run a `writing-skills` conformance pass on existing documentation. 

**Architecture:** A static markdown documentation directory structure serving as "knowledge" for MCP-compliant AI agents. The AI reads `SKILL.md`, navigates to module documentation in `skills/`, then uses standalone C# code blocks found in `recipes/` to drive unity via `Unity_RunCommand`. 

**Tech Stack:** Markdown, C# (as standalone RunCommand scripts), Unity MCP Editor plugin.

---

## User Review Required

- **Extensive Scope**: This plan covers exactly what files to touch. Because there are 41 C# files to extract, I've batched the extraction tasks logically.
- **GameObjectFinder Expansion**: As requested, utility classes like `GameObjectFinder` will NOT be crammed into one file. They will be explicitly saved as `gameobject-finder-recipes.md`, `validate-recipes.md`, etc., containing raw copy-pasteable C# definitions.
- **Writing-Skills Pass**: A dedicated task outlines the `writing-skills` structural pass (Overview, When to Use, Quick References) to be performed on existing `.md` files as they are migrated to the new `skills/` directory.

---

### Task 1: Repo Setup & Skeleton

**Files:**
- Create: `SKILL.md` (root entry)
- Create: `mcp-tools.md`
- Create: `recipes/README.md`
- Create: `MIGRATION.md`

- [ ] **Step 1: Create `MIGRATION.md`**
  Create `MIGRATION.md` at root to track changes.
  ```markdown
  # Migration Log
  - Phase 1: Repo skeleton created.
  ```

- [ ] **Step 2: Create root `SKILL.md`**
  ```markdown
  ---
  name: unity-skills-library
  description: Use when you are an AI agent analyzing or automating a Unity project. This is the root skill for the Unity MCP skills library.
  ---
  
  # Unity MCP Skills Library
  
  This repository provides modular domain-specific guidance and recipes for controlling Unity via MCP.
  
  ## Routing
  - See `skills/SKILL.md` for the index of all Unity domain skills.
  - See `mcp-tools.md` for exact tracking of when to use generic tools vs `Unity_RunCommand`.
  - See `recipes/README.md` on how to use snippet recipes.
  ```

- [ ] **Step 3: Create `mcp-tools.md`**
  Create the cross-reference for native MCP tools vs generic ones.
  ```markdown
  # MCP Tools Matrix
  
  | Domain | Dedicated MCP Tool | RunCommand? |
  |--------|-------------------|-------------|
  | Logs   | `Unity_ReadConsole` | No |
  | Code   | `Unity_ScriptApplyEdits` | No |
  | UI/3D  | `Unity_SceneView_Capture*` | No |
  | Editor | `Unity_RunCommand` (w/ Recipes) | Yes |
  ```

- [ ] **Step 4: Create `recipes/README.md`**
  ```markdown
  # Unity RunCommand Recipes
  
  These markdown files contain raw C# templates designed to be executed via `Unity_RunCommand`.
  
  Golden Template:
  ```csharp
  using UnityEngine;
  using UnityEditor;
  internal class CommandScript : IRunCommand {
      public void Execute(ExecutionResult result) {
          // logic
      }
  }
  ```
  ```

- [ ] **Step 5: Commit**
  ```bash
  git add SKILL.md mcp-tools.md recipes/README.md MIGRATION.md
  git commit -m "chore: setup new mcp skeleton files"
  ```

---

### Task 2: Migrate Documentation and Clean Chinese Content

**Files:**
- Modify: `README.md`
- Delete: `README_CN.md`, `docs/SETUP_GUIDE_CN.md`
- Move: `SkillsForUnity/unity-skills~/references` to `references`
- Modify: `docs/SETUP_GUIDE.md`

- [ ] **Step 1: Delete localized files**
  ```bash
  git rm README_CN.md docs/SETUP_GUIDE_CN.md
  ```

- [ ] **Step 2: Rewrite `README.md`**
  Replace content with the standalone description.
  ```markdown
  # Unity Skills Library
  Standalone skills repository to supercharge AI Agents connected to Unity via MCP.
  To setup, see [SETUP_GUIDE.md](docs/SETUP_GUIDE.md).
  ```

- [ ] **Step 3: Rewrite `docs/SETUP_GUIDE.md`**
  Remove REST API references and replace with instructions to load the SKILL.md for their agent.

- [ ] **Step 4: Extract reference modules**
  ```bash
  mv SkillsForUnity/unity-skills~/references ./references
  git add references
  ```

- [ ] **Step 5: Commit**
  ```bash
  git add README.md docs/SETUP_GUIDE.md
  git commit -m "docs: strip chinese and update readme/setup"
  ```

---

### Task 3: Migrate and Format Advisory Skills (`writing-skills` Pass)

**Files:**
- Move: `SkillsForUnity/unity-skills~/skills/<advisory_modules>` -> `skills/`
- Modify: 13 advisory SKILL.md files (e.g. architecture, performance, patterns)

- [ ] **Step 1: Move files**
  ```bash
  mkdir -p skills
  cp -r SkillsForUnity/unity-skills~/skills/architecture skills/
  cp -r SkillsForUnity/unity-skills~/skills/patterns skills/
  cp -r SkillsForUnity/unity-skills~/skills/performance skills/
  cp -r SkillsForUnity/unity-skills~/skills/asmdef skills/
  cp -r SkillsForUnity/unity-skills~/skills/async skills/
  cp -r SkillsForUnity/unity-skills~/skills/inspector skills/
  cp -r SkillsForUnity/unity-skills~/skills/blueprints skills/
  cp -r SkillsForUnity/unity-skills~/skills/script-roles skills/
  cp -r SkillsForUnity/unity-skills~/skills/scene-contracts skills/
  cp -r SkillsForUnity/unity-skills~/skills/testability skills/
  cp -r SkillsForUnity/unity-skills~/skills/adr skills/
  cp -r SkillsForUnity/unity-skills~/skills/project-scout skills/
  cp -r SkillsForUnity/unity-skills~/skills/scriptdesign skills/
  ```

- [ ] **Step 2: Apply `writing-skills` structural pass**
  For EACH advisory skill:
  - Remove Chinese trigger words from YAML description. Ensure `description` starts with "Use when...".
  - Ensure `# [Header]` is followed by `## Overview` and `## When to Use` sections.
  - Remove any legacy `.md` references to REST servers.

- [ ] **Step 3: Commit**
  ```bash
  git add skills/
  git commit -m "docs: port advisory skills and conform to writing-skills structure"
  ```

---

### Task 4: Migrate Functional Skills (`writing-skills` Pass)

**Files:**
- Move: `SkillsForUnity/unity-skills~/skills/<functional_modules>` -> `skills/`
- Modify: ~40 functional SKILL.md files (e.g. gameobject, scene, cinemachine)

- [ ] **Step 1: Functional Move**
  Copy the remaining module folders into `skills/`. Merge `debug` into `console`, and `batch`, `history`, `bookmark` into `workflow`.

- [ ] **Step 2: Structural Pass & REST Strip**
  For EACH functional skill:
  - Remove Chinese triggers.
  - Remove Python signature code blocks (`call_skill(...)`).
  - Add standard headings `## Overview`, `## Common Mistakes` (derived from Guardrails DO NOT section).
  - Update table headers to refer to `recipes/` instead of API parameters.
  - For `console`, `script`, `package`, `profiler` explicitly document the usage of Dedicated MCP tools over `RunCommand`.

- [ ] **Step 3: Update `skills/SKILL.md` Index**
  Update the root index of skills. Remove the "Mode" columns, update the matrix to list what uses Reusable MCP tools.

- [ ] **Step 4: Commit**
  ```bash
  git add skills/
  git commit -m "docs: port functional skills and remove REST references"
  ```

---

### Task 5: Extract C# Utilities (GameObjectFinder & Validations)

**Files:**
- Create: `recipes/gameobject-finder-recipes.md`
- Create: `recipes/validation-recipes.md`
- Source: `SkillsForUnity/Editor/Skills/GameObjectFinder.cs`

- [ ] **Step 1: Extract Validate static class**
  Create `recipes/validation-recipes.md`. Copy and paste the `Validate` class and its methods exactly. Include wrapping documentation instructing how to paste it into `IRunCommand`.

- [ ] **Step 2: Extract GameObjectFinder static class**
  Create `recipes/gameobject-finder-recipes.md`. Copy and paste `FindHelper`, `SceneObjectCache`, and `GameObjectFinder` classes inside it exactly. Include instruction to paste into `IRunCommand`.

- [ ] **Step 3: Extract SkillsCommon static class**
  Create `recipes/skills-common-recipes.md`. Copy and paste `SkillsCommon` class identically.

- [ ] **Step 4: Commit**
  ```bash
  git add recipes/
  git commit -m "feat: extract C# utilities to explicit recipes"
  ```

---

### Task 6: Extract Recipes (Core Modules)

**Files:**
- Create: `recipes/gameobject-recipes.md`, `recipes/component-recipes.md`, `recipes/material-recipes.md`, `recipes/scene-recipes.md`

- [ ] **Step 1: GameObject Recipes**
  Read `GameObjectSkills.cs`. Extract `GameObjectCreateBatch`, parent/child, tag modification into `gameobject-recipes.md`.
  Ensure `using UnityEditor; using UnityEngine; internal class CommandScript : IRunCommand { public void Execute(ExecutionResult result) { ... } }` wrapper encapsulates the logic exactly.

- [ ] **Step 2: Component Recipes**
  Read `ComponentSkills.cs`. Extract `ConvertValue` reflection utility and component addition logic into `component-recipes.md`.

- [ ] **Step 3: Scene & Material Recipes**
  Extract `SceneSkills.cs` and `MaterialSkills.cs` logic into their respective markdown files.

- [ ] **Step 4: Commit**
  ```bash
  git add recipes/
  git commit -m "feat: extract core component recipes"
  ```

---

### Task 7: Extract Recipes (Remaining Domains via Sub-Task Execution)

(For an execution agent, process these files step-by-step to avoid context limits).

- [ ] **Step 1: Process UI and Toolkit**
  Extract `UISkills.cs` -> `recipes/ui-recipes.md`
  Extract `UIToolkitSkills.cs` -> `recipes/uitoolkit-recipes.md`

- [ ] **Step 2: Process Advanced Systems**
  Extract `CinemachineSkills.cs` & `CinemachineAdapter.cs` -> `recipes/cinemachine-recipes.md`
  Extract `ProBuilderSkills.cs` -> `recipes/probuilder-recipes.md`
  Extract `XRSkills.cs` & `XRReflectionHelper.cs` -> `recipes/xr-recipes.md`

- [ ] **Step 3: Process Scene Auth & Architecture**
  Extract `TerrainSkills.cs` -> `recipes/terrain-recipes.md`
  Extract `PhysicsSkills.cs` -> `recipes/physics-recipes.md`
  Extract `NavMeshSkills.cs` -> `recipes/navmesh-recipes.md`
  Extract `LightSkills.cs` -> `recipes/light-recipes.md`
  Extract all remaining `*Skills.cs` files to their `recipes/*-recipes.md` equivalents.

- [ ] **Step 4: Commit all recipe additions**
  ```bash
  git add recipes/
  git commit -m "feat: extract all remaining C# skills into recipes"
  ```

---

### Task 8: Teardown REST Infrastructure

> **Do not begin this task until ALL `.cs` logic is safely verified inside `recipes/`.**

**Files:**
- Modify: `.claude/commands/`
- Delete: `SkillsForUnity/` directory entirely
- Delete: `scripts/unity_skills.py` & `agent_config.json`

- [ ] **Step 1: Destroy legacy commands**
  ```bash
  rm -f .claude/commands/release.md .claude/commands/updateversion.md .claude/commands/skillcheck.md .claude/commands/skillcount.md
  ```

- [ ] **Step 2: Delete legacy changelog and strategy docs**
  ```bash
  rm -f CHANGELOG.md Unity_MCP_Tool_Strategy.md
  ```

- [ ] **Step 3: Delete Python clients**
  ```bash
  rm -f scripts/unity_skills.py scripts/agent_config.json
  ```

- [ ] **Step 4: Target elimination of plugin**
  ```bash
  git rm -r SkillsForUnity
  ```

- [ ] **Step 5: Commit**
  ```bash
  git add .claude/
  git commit -m "refactor: teardown REST server and UPM package"
  ```

---

### Task 9: Final Integrity Verification

- [ ] **Step 1: Check recipe syntax**
  Grep through `recipes/` to ensure no `[UnitySkill]` attributes leaked, and all `IRunCommand` blocks are well formed.

- [ ] **Step 2: Verify SKILL.md indexes**
  Verify that the root `SKILL.md` correctly links to all sub-directories and there are no dead links.

- [ ] **Step 3: Finalize MIGRATION.md**
  Add completion notes to `MIGRATION.md`.

- [ ] **Step 4: Commit**
  ```bash
  git commit -am "chore: final mcp migration verification"
  ```
