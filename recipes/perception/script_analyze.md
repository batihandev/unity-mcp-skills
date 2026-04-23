# script_analyze

## Signature

```
ScriptAnalyze(string scriptName, bool includePrivate = false)
```

## Return Shape

Returns `success`, `script`, `fullName`, `kind` (`MonoBehaviour` / `ScriptableObject` / `Class`), `baseClass`, `fields` (name, type, isSerializable), `properties` (name, type, canWrite), `methods` (name, returnType, parameters), `unityCallbacks` (list of implemented Unity lifecycle methods, MonoBehaviour only).

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`skills_common`](../_shared/skills_common.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

internal class CommandScript : IRunCommand
{
    static readonly string[] _UnityCallbacks =
    {
        "Awake","Start","Update","FixedUpdate","LateUpdate","OnEnable","OnDisable","OnDestroy",
        "OnApplicationQuit","OnTriggerEnter","OnTriggerExit","OnTriggerStay",
        "OnCollisionEnter","OnCollisionExit","OnCollisionStay",
        "OnMouseDown","OnMouseUp","OnMouseDrag","OnMouseEnter","OnMouseExit",
        "OnGUI","OnDrawGizmos","OnDrawGizmosSelected","OnValidate","Reset"
    };

    public void Execute(ExecutionResult result)
    {
        string scriptName = "MyScript"; // replace with target class name
        bool includePrivate = false;

        var type = SkillsCommon.GetAllLoadedTypes()
            .FirstOrDefault(t => t.Name.Equals(scriptName, System.StringComparison.OrdinalIgnoreCase) &&
                                 (typeof(MonoBehaviour).IsAssignableFrom(t) ||
                                  typeof(ScriptableObject).IsAssignableFrom(t) ||
                                  (t.IsClass && !t.IsAbstract && t.Namespace != null &&
                                   !t.Namespace.StartsWith("Unity") && !t.Namespace.StartsWith("System"))));

        if (type == null)
        {
            result.SetResult(new { success = false, error = $"Script '{scriptName}' not found" });
            return;
        }

        var fields = new List<object>();
        foreach (var f in type.GetFields())
        {
            if (f.DeclaringType != type) continue;
            if (!f.IsPublic && !includePrivate) continue;
            if (f.Name.StartsWith("<")) continue;
            fields.Add(new
            {
                name = f.Name,
                type = GetFriendlyTypeName(f.FieldType),
                isSerializable = f.IsPublic || f.GetCustomAttributes(typeof(SerializeField), false).Length > 0
            });
        }

        var properties = new List<object>();
        foreach (var p in type.GetProperties())
        {
            if (p.DeclaringType != type || !p.CanRead || p.GetIndexParameters().Length != 0) continue;
            var getter = p.GetGetMethod(true);
            if (getter == null || (!getter.IsPublic && !includePrivate)) continue;
            properties.Add(new { name = p.Name, type = GetFriendlyTypeName(p.PropertyType), canWrite = p.CanWrite });
        }

        var methods = new List<object>();
        foreach (var m in type.GetMethods())
        {
            if (m.DeclaringType != type || m.IsSpecialName) continue;
            if (!m.IsPublic && !includePrivate) continue;
            methods.Add(new
            {
                name = m.Name,
                returnType = GetFriendlyTypeName(m.ReturnType),
                parameters = string.Join(", ", m.GetParameters().Select(p => $"{GetFriendlyTypeName(p.ParameterType)} {p.Name}"))
            });
        }

        List<string> unityEvents = null;
        if (typeof(MonoBehaviour).IsAssignableFrom(type))
        {
            unityEvents = new List<string>();
            foreach (var m in type.GetMethods())
                if (m.DeclaringType == type && System.Array.IndexOf(_UnityCallbacks, m.Name) >= 0) unityEvents.Add(m.Name);
        }

        string scriptKind = typeof(MonoBehaviour).IsAssignableFrom(type) ? "MonoBehaviour"
            : typeof(ScriptableObject).IsAssignableFrom(type) ? "ScriptableObject"
            : "Class";

        result.SetResult(new
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

    string GetFriendlyTypeName(System.Type t)
    {
        if (t == null) return "null";
        if (t == typeof(int)) return "int";
        if (t == typeof(float)) return "float";
        if (t == typeof(bool)) return "bool";
        if (t == typeof(string)) return "string";
        if (t == typeof(void)) return "void";
        if (t.IsArray) return GetFriendlyTypeName(t.GetElementType()) + "[]";
        if (t.IsGenericType)
        {
            var args = string.Join(", ", System.Array.ConvertAll(t.GetGenericArguments(), x => GetFriendlyTypeName(x)));
            return t.Name.Split('`')[0] + "<" + args + ">";
        }
        return t.Name;
    }
}
```

## Notes

- Searches by class name, case-insensitive. Qualify with namespace if there are name collisions.
- Only inspects declared members (`DeclaredOnly`); inherited members from base classes are not listed.
- Use `script_dependency_graph` to understand which other scripts this one depends on.
