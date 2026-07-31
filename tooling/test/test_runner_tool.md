# test_runner_tool

Runs the Test Runner and writes exactly one NUnit report, to the path the caller asked for.

Install this once per project and drive every test run through it. It is the working way to get a
test report out of an MCP session; registering an `ICallbacks` from inside a `Unity_RunCommand` body
is not, for reasons under [Why not inline](#why-not-inline).

## Install

**Path:** `Assets/<YourProject>/Scripts/Editor/Testing/ProjectTestRunner.cs`

Install via `Unity_CreateScript` with the C# content below.

The file must land in an Editor assembly that references `UnityEditor.TestRunner`. If your project
uses asmdefs, add that reference to the existing Editor asmdef rather than creating a new assembly
for it — see `unity-asmdef-setup`, which caps a project at its default assembly set.

## Post-install

Wait for Unity to recompile before calling it. Then from a `Unity_RunCommand` body:

```csharp
using UnityEngine;
using YourProject.Editor.Testing;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string path = ProjectTestRunner.Run(
            "EditMode-mytask-red.xml",
            editMode: true,
            fullyQualifiedTestName: "YourProject.Tests.EditMode.Foo.BarTests");
        result.Log("started -> " + path);
    }
}
```

Fire-and-forget: the file does not exist yet when the call returns. Poll for it from the shell, wait
for its mtime to stop changing, then parse it.

```bash
F=TestResults/EditMode-mytask-red.xml
for i in $(seq 1 12); do [ -f "$F" ] && break; sleep 8; done
prev=""; for i in $(seq 1 6); do cur=$(stat -c %Y "$F"); [ "$cur" = "$prev" ] && break; prev=$cur; sleep 5; done
python3 -c "import xml.etree.ElementTree as ET;r=ET.parse('$F').getroot();print(r.attrib)"
```

Two rules that decide whether the result means anything:

- **Fully-qualify the filter.** `fullyQualifiedTestName` must be `Namespace.Class` or
  `Namespace.Class.Method`. A bare class name matches zero tests, and a run of zero tests reports as
  a pass.
- **Assert `total` is non-zero** before believing any green.

## Adapt

| What to change | Where |
|----------------|-------|
| Namespace | `namespace YourProject.Editor.Testing` at top of file |
| Class name | `ProjectTestRunner` |
| Output directory | `TestResults` literal in `Run` |

## Why not inline

The obvious approach — declaring an `ICallbacks` in the `Unity_RunCommand` body and registering it
there — silently corrupts test evidence, and cannot be repaired in place.

A RunCommand body compiles into a throwaway dynamic assembly whose statics do not survive to the
next invocation. Measured: a static written by one invocation reads back null in the next. So a
callback registered that way can never be found again, and never cleaned up. Every writer registered
across a domain's lifetime keeps firing on every later run, each re-writing *its own* path with the
new run's results.

The symptom is a set of differently-named reports that are byte-identical. A report captured as
"red" gets retroactively overwritten by a later green run, so a failing test reads as a passing
red-green cycle that never happened. In one project's history this produced six identical reports
from a single run, and a committed `-red`/`-green` pair that were the same bytes.

Three repairs were tried against this and all three were measured to fail:

| Attempt | Result |
|---|---|
| Unregister the previous callback held in a `static` | Dead on arrival — the static does not survive to the next invocation, so there is nothing to unregister |
| Self-unregister via `EditorApplication.delayCall` | Earlier report still overwritten |
| Self-unregister synchronously inside `RunFinished` | Earlier report still overwritten; `UnregisterCallbacks` threw nothing and detached nothing |

The tool below sidesteps all of it. Its statics are real because the assembly is stable, so it
registers exactly one callback for the lifetime of the domain and there is never a second writer to
leak. This is the same conclusion `unity-asmdef-setup` reaches under "Cross-reload state in
throwaway assemblies".

Verified by the experiment that falsified the three attempts above: run A over 3 tests, let it
settle, run B over 6 tests, confirm A still reads 3 with an unchanged hash.

```csharp
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace YourProject.Editor.Testing
{
    /// <summary>
    /// Runs the Test Runner and writes exactly one NUnit report, to the path the caller asked for.
    ///
    /// Registering an ICallbacks from a Unity_RunCommand body registers it from a throwaway dynamic
    /// assembly whose statics do not survive to the next invocation, so it can never be cleaned up.
    /// Every writer registered that way keeps firing on every later run, rewriting its own old path
    /// with the new run's results, which turns a "red" report into a copy of a later green one.
    /// Unregistering does not rescue it, deferred or synchronous.
    ///
    /// This type lives in a stable compiled assembly, so its statics persist and it registers
    /// exactly ONE callback for the lifetime of the domain. There is never a second writer to leak.
    /// The caller sets the destination per run.
    /// </summary>
    public static class ProjectTestRunner
    {
        private static TestRunnerApi _api;
        private static ResultWriter _writer;

        /// <summary>Where the next finished run writes. Null means discard, which is what a run
        /// started from the Test Runner window rather than by <see cref="Run"/> should do.</summary>
        private static string _pendingResultPath;

        /// <summary>Absolute path the most recent <see cref="Run"/> asked for.</summary>
        public static string LastRequestedPath { get; private set; }

        /// <summary>
        /// Starts a run and returns the absolute path its report will be written to. Fire-and-forget:
        /// the file does not exist yet on return.
        /// </summary>
        /// <param name="resultFileName">File name only, no directory. Written under
        /// &lt;project&gt;/TestResults/. Use a fresh name per run so a stale file cannot be misread
        /// as this run's result.</param>
        /// <param name="editMode">EditMode when true, PlayMode when false.</param>
        /// <param name="fullyQualifiedTestName">Optional filter. Must be Namespace.Class or
        /// Namespace.Class.Method — a bare class name matches nothing and a run of zero tests reads
        /// as a pass.</param>
        /// <returns>The absolute report path, or an "ERROR: " string when the run cannot start.</returns>
        public static string Run(string resultFileName, bool editMode = true,
            string fullyQualifiedTestName = null)
        {
            if (string.IsNullOrWhiteSpace(resultFileName))
                return "ERROR: resultFileName is required.";
            if (resultFileName.IndexOfAny(new[] { '/', '\\' }) >= 0)
                return "ERROR: resultFileName must be a file name, not a path.";

            // An EditMode run started while the editor is in PlayMode silently never starts and never
            // writes a report, which is indistinguishable from a broken callback.
            if (editMode && EditorApplication.isPlayingOrWillChangePlaymode)
                return "ERROR: editor is in PlayMode; exit it before running EditMode tests.";
            if (EditorApplication.isCompiling)
                return "ERROR: editor is compiling; retry once it settles.";

            string dir = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName, "TestResults");
            Directory.CreateDirectory(dir);
            string resultsPath = Path.Combine(dir, resultFileName);

            EnsureRegistered();
            _pendingResultPath = resultsPath;
            LastRequestedPath = resultsPath;

            var filter = new Filter
            {
                testMode = editMode ? TestMode.EditMode : TestMode.PlayMode
            };
            if (!string.IsNullOrWhiteSpace(fullyQualifiedTestName))
                filter.testNames = new[] { fullyQualifiedTestName };

            _api.Execute(new ExecutionSettings(filter));
            return resultsPath.Replace('\\', '/');
        }

        private static void EnsureRegistered()
        {
            if (_api != null) return;

            // Survives until the next domain reload, which also clears Unity's callback registry, so
            // the two stay in step and a second writer is never created.
            _api = ScriptableObject.CreateInstance<TestRunnerApi>();
            _writer = new ResultWriter();
            _api.RegisterCallbacks(_writer);
        }

        private sealed class ResultWriter : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                if (string.IsNullOrEmpty(_pendingResultPath)) return;

                // Consume the destination so a run this class did not start cannot land on the
                // previous caller's file.
                string path = _pendingResultPath;
                _pendingResultPath = null;
                TestRunnerApi.SaveResultToFile(result, path);
            }

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result) { }
        }
    }
}
```

## PlayMode caveat

Entering Play Mode can trigger a domain reload, which clears both this class's statics and Unity's
callback registry before `RunFinished` fires, so no report is written. Disable domain reload for the
run (Project Settings → Editor → Enter Play Mode Options) or keep PlayMode runs on the Test Runner
window. EditMode runs do not reload the domain and are unaffected.
