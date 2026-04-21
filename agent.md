# unity-skills — Agent Overview

Documentation-only skill context for the **official Unity MCP** server.
No C# plugin, no REST server, no Python client needed.

## What This Repo Is

A curated set of skill documents that guide AI agents working with the official Unity MCP.
Every recipe is a verified C# snippet executed via `Unity_RunCommand`.

## Routing Order

1. **Native MCP tool** — check `mcp-tools.md` first; dedicated tools are faster and safer
2. **Topic skill** — load `skills/<topic>/SKILL.md` for domain knowledge and anti-hallucination rules
3. **Exact recipe** — `recipes/<topic>/<command>.md` for a verified C# template
4. **Closest recipe** — adapt the nearest recipe if no exact match exists
5. **Fresh `Unity_RunCommand`** — last resort

## Prerequisites

- Official Unity MCP server running and connected to your Unity Editor
- Optional packages installed when using modules that require them (cinemachine, probuilder, xr, timeline) — each SKILL.md Common Mistakes section has the install command

## Project Source

Forked and converted from [Besty0728/Unity-Skills](https://github.com/Besty0728/Unity-Skills).
Original architecture: C# plugin + HTTP server + Python client (REST).
This fork: documentation-only, MCP-first.
