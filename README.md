# Unity MCP Skills Library

A context plugin for AI agents working inside Unity via the official Unity MCP (Model Context Protocol).

Instead of asking your AI to guess how Unity works, you give it this library as context. It contains skills (per-system guardrails and guidance) and recipes (ready-to-run C# `IRunCommand` templates) so the agent can work more predictably without wrecking your scene.

> **Work in progress.** Things may be incomplete or rough around the edges — use with that expectation.

---

### Installation & Usage

See **[SETUP_GUIDE.md](docs/SETUP_GUIDE.md)** for how to connect this to your agent (Claude Code, Cursor, Windsurf, etc.).

---

### What's in here

- **`/skills`** — per-system guidance (UI, Physics, Animation, etc.) that tells the agent when and how to approach a task
- **`/recipes`** — C# `IRunCommand` templates the agent fills in and runs; designed to support Editor Undo/Redo
- **`/references`** — offline reference dumps for AI context

---

### Credits

Skill library structure and data originally curated by [Besty0728](https://github.com/Besty0728/Unity-Skills).
