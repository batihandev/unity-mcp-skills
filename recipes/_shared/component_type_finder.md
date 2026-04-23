# ComponentSkills.FindComponentType Helper

Paste-in type lookup. Resolves a Unity `Component` type by simple name, full
name, or common namespace (`UnityEngine.*`, `TMPro.*`, `Cinemachine.*`,
`UnityEngine.InputSystem.*`, render pipeline namespaces, etc.). Returns `null`
if nothing matches.

## Requires

`recipes/_shared/skills_common.md` — uses `SkillsCommon.GetAllLoadedTypes`.

## Call pattern

```csharp
var compType = ComponentSkills.FindComponentType(componentTypeName);
if (compType == null)
{
    result.SetResult(new { error = $"Component type not found: {componentTypeName}" });
    return;
}
```

## Do not

- Do not paste both this file and `value_converter.md` in the same
  `Unity_RunCommand` — both define `internal static class ComponentSkills`.

## Paste-in

```csharp
    internal static class ComponentSkills
    {
        private static readonly System.Collections.Generic.Dictionary<string, System.Type> _typeCache =
            new System.Collections.Generic.Dictionary<string, System.Type>();

        private static readonly string[] ExtendedNamespaces = new[]
        {
            "",
            "UnityEngine.",
            "UnityEngine.UI.",
            "UnityEngine.EventSystems.",
            "UnityEngine.Rendering.",
            "UnityEngine.Rendering.Universal.",
            "UnityEngine.Rendering.HighDefinition.",
            "UnityEngine.AI.",
            "UnityEngine.Animations.",
            "UnityEngine.Audio.",
            "UnityEngine.Video.",
            "UnityEngine.Playables.",
            "UnityEngine.Timeline.",
            "UnityEngine.Tilemaps.",
            "UnityEngine.InputSystem.",
            "TMPro.",
            "Cinemachine.",
            "Unity.Cinemachine.",
            "UnityEngine.XR.",
            "UnityEngine.XR.Interaction.Toolkit."
        };

        public static System.Type FindComponentType(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            if (_typeCache.TryGetValue(name, out var cached))
                return cached;

            System.Type result = System.Type.GetType(name);
            if (result != null && typeof(UnityEngine.Component).IsAssignableFrom(result))
            {
                if (_typeCache.Count > 500) _typeCache.Clear();
                _typeCache[name] = result;
                return result;
            }

            var simpleName = name.Contains(".") ? name.Substring(name.LastIndexOf('.') + 1) : name;

            foreach (var ns in ExtendedNamespaces)
            {
                result = TryGetTypeFromAssemblies(ns + simpleName);
                if (result != null && typeof(UnityEngine.Component).IsAssignableFrom(result))
                {
                    if (_typeCache.Count > 500) _typeCache.Clear();
                    _typeCache[name] = result;
                    return result;
                }
            }

            result = System.Linq.Enumerable.FirstOrDefault(
                SkillsCommon.GetAllLoadedTypes(),
                t =>
                    (t.Name.Equals(simpleName, System.StringComparison.OrdinalIgnoreCase) ||
                     t.FullName == name) &&
                    typeof(UnityEngine.Component).IsAssignableFrom(t));

            if (result != null)
            {
                if (_typeCache.Count > 500) _typeCache.Clear();
                _typeCache[name] = result;
            }

            return result;
        }

        private static System.Type TryGetTypeFromAssemblies(string fullName)
        {
            var assemblyNames = new[]
            {
                "UnityEngine",
                "UnityEngine.UI",
                "UnityEngine.CoreModule",
                "Unity.TextMeshPro",
                "Unity.Cinemachine",
                "Cinemachine",
                "Unity.InputSystem",
                "Unity.RenderPipelines.Universal.Runtime",
                "Unity.RenderPipelines.HighDefinition.Runtime"
            };

            foreach (var asmName in assemblyNames)
            {
                try
                {
                    var type = System.Type.GetType($"{fullName}, {asmName}");
                    if (type != null) return type;
                }
                catch { }
            }
            return null;
        }
    }
```
