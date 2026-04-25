# Unity MCP Skills Library

A documentation-only skill pack for AI agents working inside Unity via the official Unity MCP. No C# plugin, no REST server, no Python client — just markdown that teaches your agent how Unity actually works.

It contains **skills** (per-system guardrails and routing) and **recipes** (ready-to-run C# `IRunCommand` templates) so the agent can act predictably without wrecking your scene.

> **Work in progress.** Things may be incomplete or rough around the edges — use with that expectation.

**Requires:**

- **Unity's `com.unity.ai.assistant` package** with its **Unity MCP Server** enabled in *Project Settings → AI → Unity MCP Server* — the hard dependency that provides `Unity_RunCommand` and the `IRunCommand` contract every recipe targets. Nothing in this pack runs without it.
- **Unity 6000+ (Unity 6).** Recipes use `FindFirstObjectByType` / `FindObjectsByType`; older versions are not supported.
- **Per-domain package baselines** listed in each `skills/<domain>/SKILL.md` `## Requirements` block. Domain skills tell the agent how to install missing ones via `Unity_PackageManager_ExecuteAction`.

---

## Install

Clone this repo as a single folder inside your agent's skills directory. The whole repo is the install unit — domain skills depend on shared helpers (`recipes/_shared/`), tool routing (`mcp-tools.md`), and fallback docs (`references/`) that ship together.

### Claude Code

```bash
git clone https://github.com/batihandev/unity-mcp-skills.git ~/.claude/skills/unity-mcp-skills
```

### Codex

```bash
git clone https://github.com/batihandev/unity-mcp-skills.git ~/.codex/skills/unity-mcp-skills
```

### Antigravity / Gemini CLI

```bash
git clone https://github.com/batihandev/unity-mcp-skills.git ~/.gemini/antigravity/skills/unity-mcp-skills
```

### Symlink alternative (for hacking on the library)

```bash
git clone https://github.com/batihandev/unity-mcp-skills.git ~/src/unity-mcp-skills
ln -s ~/src/unity-mcp-skills ~/.claude/skills/unity-mcp-skills
```

## Update

```bash
cd ~/.claude/skills/unity-mcp-skills && git pull
```

## Uninstall

```bash
rm -rf ~/.claude/skills/unity-mcp-skills
```

---

## How discovery works

Only the top-level `SKILL.md` is registered with your agent — under the name `unity-mcp-skills`. That skill routes to domain-specific skills under `skills/<domain>/SKILL.md` internally. You will **not** see `unity-scene`, `unity-physics`, etc. as separate skills in your agent — they are loaded on demand by the library.

## What's in here

- **`SKILL.md`** — the discoverable entry point; routes to the right domain.
- **`skills/<domain>/`** — per-system guidance (UI, Physics, Animation, …) telling the agent when and how to approach a task.
- **`recipes/<domain>/`** — C# `IRunCommand` templates the agent fills in and runs; designed to support Editor Undo/Redo.
- **`recipes/_shared/`** — cross-domain C# helpers embedded by recipes.
- **`references/`** — offline reference dumps used as a tertiary fallback when skills and recipes lack detail.
- **`mcp-tools.md`** — routing matrix for native MCP tools vs `Unity_RunCommand`.

## Works with

Skills follow the [superpowers](https://github.com/obra/superpowers) format.

## Credits

Skill library structure and data originally curated by [Besty0728](https://github.com/Besty0728/Unity-Skills).
