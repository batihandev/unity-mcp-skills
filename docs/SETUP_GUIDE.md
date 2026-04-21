# Unity Skills Library: Installation & Setup

## Prerequisites

1. **Unity MCP Server**
   - A Unity MCP-compatible plugin/server must be running in your Unity Editor
   - Unity Editor must be open during usage

2. **AI Agent**
   Your agent must support:
   - reading local files (skills / rules / context)
   - executing MCP tools (e.g. `Unity_RunCommand`)

---

## 🔧 Installation

### Option A — Skills Directory (Recommended)

Clone this repository into your agent's **skills directory**:

```bash
git clone https://github.com/batihandev/unity-skills.git <your-skills-folder>/unity-mcp-skills
```

> `<your-skills-folder>` depends on your agent (e.g. `.claude/skills`, `skills/`, etc.)

Then:

- restart or reload your agent
- it will automatically discover `SKILL.md`

---

### Option B — Project-Level Setup

Clone the repository inside your project:

```bash
git clone https://github.com/batihandev/unity-skills.git ./unity-mcp-skills
```

Then add this instruction to your agent (system prompt or rules):

When working with Unity:

1. Use the Unity Skills Library located at ./unity-mcp-skills
2. Always start by reading SKILL.md
3. Navigate to skills/SKILL.md to find the correct domain
4. Follow guardrails in skills/<domain>/SKILL.md
5. Use recipes/\* for RunCommand execution templates
6. Do not invent Unity scripts — always use recipes

---

### Option C — Rules-Based Setup (IDE Agents)

If your agent supports rules files (e.g. `.rules`, `.cursorrules`, `.windsurfrules`), add:

When manipulating the Unity Editor:

- Use the Unity Skills Library at [PATH_TO_REPO]
- Start from SKILL.md
- Follow domain routing via skills/SKILL.md
- Read guardrails before executing anything
- Always use recipes/\* templates for Unity_RunCommand
- Never hallucinate Unity editor code
