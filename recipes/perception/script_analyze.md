# script_analyze

**Skill:** `script_analyze`
**C# method:** `PerceptionSkills.ScriptAnalyze`

## Signature

```
ScriptAnalyze(string scriptName, bool includePrivate = false)
```

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `scriptName` | `string` | required | Class name to analyze (case-insensitive). Must be a MonoBehaviour, ScriptableObject, or non-abstract user class |
| `includePrivate` | `bool` | `false` | Whether to include private fields and methods |

## Return Shape

Returns `success`, `script`, `fullName`, `kind` (`MonoBehaviour` / `ScriptableObject` / `Class`), `baseClass`, `fields` (name, type, isSerializable), `properties` (name, type, canWrite), `methods` (name, returnType, parameters), `unityCallbacks` (list of implemented Unity lifecycle methods, MonoBehaviour only).

**Prerequisites:** [`skills_common`](../_shared/skills_common.md)

## RunCommand Recipe

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string scriptName = "MyScript"; // replace with target class name
        bool includePrivate = false;

        var type = SkillsCommon.GetAllLoadedTypes()
            .FirstOrDefault(t => t.Name.Equals(scriptName, StringComparison.OrdinalIgnoreCase) &&
                                 (typeof(MonoBehaviour).IsAssignableFrom(t) ||
                                  typeof(ScriptableObject).IsAssignableFrom(t) ||
                                  (t.IsClass && !t.IsAbstract && t.Namespace != null &&
                                   !t.Namespace.StartsWith("Unity") && !t.Namespace.StartsWith("System"))));

        if (type == null)
        {
            result.SetValue(new { success = false, error = $"Script '{scriptName}' not found (searched MonoBehaviour, ScriptableObject, and user classes)" });
            return;
        }

        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly;
        if (includePrivate) flags |= System.Reflection.BindingFlags.NonPublic;

        var fields = type.GetFields(flags)
            .Where(f => !f.Name.StartsWith("<"))
            .Select(f => new
            {
                name = f.Name,
                type = GetFriendlyTypeName(f.FieldType),
                isSerializable = f.IsPublic || f.GetCustomAttribute<SerializeField>() != null
            })
            .ToList();

        var properties = type.GetProperties(flags)
            .Where(p => p.CanRead)
            .Select(p => new
            {
                name = p.Name,
                type = GetFriendlyTypeName(p.PropertyType),
                canWrite = p.CanWrite
            })
            .ToList();

        var methods = type.GetMethods(flags)
            .Where(m => !m.IsSpecialName)
            .Select(m => new
            {
                name = m.Name,
                returnType = GetFriendlyTypeName(m.ReturnType),
                parameters = string.Join(", ", m.GetParameters().Select(p => $"{GetFriendlyTypeName(p.ParameterType)} {p.Name}"))
            })
            .ToList();

        List<string> unityEvents = null;
        if (typeof(MonoBehaviour).IsAssignableFrom(type))
        {
            unityEvents = type.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly)
                .Where(m => UnityCallbacks.Contains(m.Name))
                .Select(m => m.Name)
                .ToList();
        }

        string scriptKind = typeof(MonoBehaviour).IsAssignableFrom(type) ? "MonoBehaviour"
            : typeof(ScriptableObject).IsAssignableFrom(type) ? "ScriptableObject"
            : "Class";

        result.SetValue(new
        {
            success = true,
            script = scriptName,
            fullName = type.FullName,
            kind = scriptKind,
            baseClass = type.BaseType?.Name,
            fields,
            properties,
            methods,
            unityCallbacks = unityEvents
        });
    }
}
```

## Notes

- Searches by class name, case-insensitive. Qualify with namespace if there are name collisions.
- Only inspects declared members (`DeclaredOnly`); inherited members from base classes are not listed.
- Use `script_dependency_graph` to understand which other scripts this one depends on.
