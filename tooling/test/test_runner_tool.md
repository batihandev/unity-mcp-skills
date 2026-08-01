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
for it.

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

Fire-and-forget: the file does not exist yet when the call returns. Poll for it from the shell and
parse it only once it is whole and fresh.

```bash
F=TestResults/EditMode-mytask-red.xml
MAX_AGE=120   # a report older than this belongs to an earlier run, not this one

# Wait for a report that is whole (closing tag) and fresh (not a leftover under
# the same name). stat -c is GNU, stat -f is macOS.
for i in $(seq 1 150); do
  if [ -f "$F" ] && grep -q '</test-run>' "$F" 2>/dev/null; then
    MTIME=$(stat -c %Y "$F" 2>/dev/null || stat -f %m "$F")
    [ $(( $(date +%s) - MTIME )) -le "$MAX_AGE" ] && break
  fi
  sleep 2
done

# On fall-through whatever is on disk is stale or half-written; parsing it would
# report an older run's numbers as this one's.
{ [ -f "$F" ] && grep -q '</test-run>' "$F" 2>/dev/null && \
  [ $(( $(date +%s) - $(stat -c %Y "$F" 2>/dev/null || stat -f %m "$F") )) -le "$MAX_AGE" ]; } \
  || { echo "no fresh report at $F" >&2; exit 1; }

python3 -c "import xml.etree.ElementTree as ET;print(ET.parse('$F').getroot().attrib)"
```

See [`recipes/test/test_run`](../../recipes/test/test_run.md) for the version that also turns the
counts into an exit code, including the zero-test run that NUnit reports as a pass.

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
next invocation — a static written by one invocation reads back null in the next. So a callback
registered that way can never be found again, and never cleaned up. Every writer registered across a
domain's lifetime keeps firing on every later run, each re-writing *its own* path with the new run's
results.

The symptom is a set of differently-named reports that are byte-identical: a report captured as
"red" gets retroactively overwritten by a later green run, so a failing test reads as a passing
red-green cycle that never happened.

Repairs that do not fix it — do not re-attempt these:

| Attempt | Result |
|---|---|
| Unregister the previous callback held in a `static` | Dead on arrival — the static does not survive to the next invocation, so there is nothing to unregister |
| Self-unregister via `EditorApplication.delayCall` | Earlier report still overwritten |
| Self-unregister synchronously inside `RunFinished` | Earlier report still overwritten; `UnregisterCallbacks` throws nothing and detaches nothing |

The tool below sidesteps all of it. Its statics are real because the assembly is stable, so it
registers exactly one callback for the lifetime of the domain and there is never a second writer to
leak.

```csharp
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
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
        /// <param name="assemblyName">Optional filter naming one test assembly, for running a whole
        /// suite without naming every class in it.</param>
        /// <returns>The absolute report path, or an "ERROR: " string when the run cannot start.</returns>
        public static string Run(string resultFileName, bool editMode = true,
            string fullyQualifiedTestName = null, string assemblyName = null)
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

            // The Test Runner happily runs the assemblies from the last successful compile, which
            // reports old code's numbers as this code's.
            if (EditorUtility.scriptCompilationFailed)
                return "ERROR: the last compile failed; read the errors from Editor.log and fix them first.";

            // A second Run() while one is active would re-point _pendingResultPath and land the
            // active run's report on the new filename. A domain reload clears the flag, so a run
            // that dies with the domain cannot wedge this permanently.
            if (_pendingResultPath != null)
                return "ERROR: a run is already pending; wait for its report to settle first.";

            string dir = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName, "TestResults");
            Directory.CreateDirectory(dir);
            string resultsPath = Path.Combine(dir, resultFileName);

            SaveDirtyScenes();

            EnsureRegistered();
            _pendingResultPath = resultsPath;
            LastRequestedPath = resultsPath;

            Filter filter = BuildFilter(editMode, fullyQualifiedTestName, assemblyName);

            // A throw leaves no run behind to clear the flag, which would refuse every later Run()
            // until the next domain reload.
            try
            {
                _api.Execute(new ExecutionSettings(filter));
            }
            catch
            {
                _pendingResultPath = null;
                throw;
            }

            return resultsPath.Replace('\\', '/');
        }

        /// <summary>
        /// Builds the run's filter. An absent narrowing stays null rather than becoming an empty
        /// array: an empty array is a filter that matches nothing, and a run of zero tests reports
        /// as a pass.
        /// </summary>
        public static Filter BuildFilter(bool editMode, string fullyQualifiedTestName,
            string assemblyName)
        {
            var filter = new Filter
            {
                testMode = editMode ? TestMode.EditMode : TestMode.PlayMode
            };

            if (!string.IsNullOrWhiteSpace(fullyQualifiedTestName))
                filter.testNames = new[] { fullyQualifiedTestName };
            if (!string.IsNullOrWhiteSpace(assemblyName))
                filter.assemblyNames = new[] { assemblyName };

            return filter;
        }

        /// <summary>
        /// Whether an open scene must be written to disk before a run starts.
        /// </summary>
        /// <remarks>
        /// The Test Runner reloads scenes for the run, and an unsaved one makes Unity raise a
        /// blocking "Save scene?" dialog. Nobody is there to dismiss it in an MCP or headless run, so
        /// the run hangs until a human notices. An untitled scene is skipped because saving it has no
        /// destination and opens a file dialog, which is the same stall by another route.
        /// </remarks>
        public static bool ShouldSaveBeforeRun(bool isDirty, string scenePath)
        {
            return isDirty && !string.IsNullOrEmpty(scenePath);
        }

        private static void SaveDirtyScenes()
        {
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetSceneAt(i);
                if (ShouldSaveBeforeRun(scene.isDirty, scene.path))
                    EditorSceneManager.SaveScene(scene);
            }
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

## Keep it the only registrar

Installing this tool fixes nothing on its own if other project code also calls `RegisterCallbacks`
on `TestRunnerApi`. Each extra registrar keeps writing its captured path for the rest of the
domain's life, silently rewriting this tool's reports. After installing, search the project for
other `RegisterCallbacks(` calls on `TestRunnerApi` and route those call sites through `Run` so
they no longer register their own callback.

## Verifying a run from an MCP session

Two behaviours will otherwise hand you a false green:

- **`Unity_ReadConsole` under-reports compile errors.** It can return zero entries, filtered and
  unfiltered, while `EditorUtility.scriptCompilationFailed` is true and the editor log holds
  `error CS` lines. `Run` refuses to start on that flag; read the errors themselves from
  `Editor.log`. An empty console is not a clean compile.
- **`AssetDatabase.DeleteAsset()` trips the MCP "user interactions are not supported" guard**, as do
  `AssetDatabase.Refresh()`, `File.Delete()` and an inline `TestRunnerApi.Execute()`. To make Unity
  notice a `.cs` written or deleted on disk, use
  `AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate)` followed by fully-qualified
  `UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation()`. The bare name resolves
  against the dynamic wrapper namespace and fails to compile.

## PlayMode caveat

Entering Play Mode can trigger a domain reload, which clears both this class's statics and Unity's
callback registry before `RunFinished` fires, so no report is written. Disable domain reload for the
run (Project Settings → Editor → Enter Play Mode Options) or keep PlayMode runs on the Test Runner
window. EditMode runs do not reload the domain and are unaffected.
