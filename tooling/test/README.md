# tooling/test

Install-once Editor scripts for Unity Test Runner workflows.

Tool path rule: `../../tooling/test/<tool>.md`

| Tool | File | Description |
|------|------|-------------|
| `test_runner_tool` | [test_runner_tool.md](test_runner_tool.md) | Run tests and write exactly one NUnit report, to a caller-supplied filename |

Install `test_runner_tool` before running tests in an MCP session and drive every run through it.
The recipes under `recipes/test/` read the reports it writes; they do not start runs themselves.

Registering a result callback from inside a `Unity_RunCommand` body instead is the failure this tool
exists to prevent: those callbacks can never be cleaned up and silently rewrite earlier runs'
reports, so a failing test can read back as a passing red-green cycle. See
[Why not inline](test_runner_tool.md#why-not-inline).
