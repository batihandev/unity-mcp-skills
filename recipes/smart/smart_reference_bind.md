# smart_reference_bind

Auto-fill a List or Array serialized field on a component with matching scene objects.

**Signature:** `SmartReferenceBind(string targetName, string componentName, string fieldName, string sourceTag = null, string sourceName = null, bool appendMode = false)`

**Returns:** `{ success, boundCount, field, appendMode }`

**Notes:**
- `targetName`, `componentName`, and `fieldName` are required.
- The target field must be a `List<>` or array type; plain fields are rejected.
- Field lookup tries: exact name, `m_<Name>` prefix, `_<name>` prefix, then property fallback.
- Provide at least one of `sourceTag` or `sourceName` to locate source objects.
- `appendMode = true` keeps existing items and appends new ones; `false` replaces.
- Supports undo and workflow snapshots.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string targetName = "EnemyManager";   // GameObject holding the component
        string componentName = "EnemyManager"; // Component type name
        string fieldName = "enemies";          // List<> or array field to fill
        string sourceTag = "Enemy";            // find sources by tag (or null)
        string sourceName = null;              // find sources by name contains (or null)
        bool appendMode = false;               // false = replace, true = append

        /* Original Logic:

            if (string.IsNullOrEmpty(fieldName)) return new { error = "fieldName is required" };

            var targetGo = GameObjectFinder.Find(name: targetName);
            if (targetGo == null)
                return new { success = false, error = $"Target '{targetName}' not found" };

            var comp = targetGo.GetComponent(componentName);
            if (comp == null)
                return new { success = false, error = $"Component '{componentName}' not found on target" };

            var type = comp.GetType();
            var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
                field = type.GetField("m_" + char.ToUpper(fieldName[0]) + fieldName.Substring(1), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
                field = type.GetField("_" + fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            PropertyInfo propFallback = null;
            if (field == null)
            {
                propFallback = type.GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (propFallback != null && !propFallback.CanWrite) propFallback = null;
            }

            if (field == null && propFallback == null)
                return new { success = false, error = $"Field '{fieldName}' not found on {componentName}" };

            var sources = new List<GameObject>();
            if (!string.IsNullOrEmpty(sourceTag))
            {
                try { sources.AddRange(GameObject.FindGameObjectsWithTag(sourceTag)); }
                catch { return new { success = false, error = $"Tag '{sourceTag}' does not exist" }; }
            }
            if (!string.IsNullOrEmpty(sourceName))
            {
                sources.AddRange(FindHelper.FindAll<GameObject>().Where(g => g.name.Contains(sourceName)));
            }
            sources = sources.Distinct().ToList();

            if (sources.Count == 0)
                return new { success = false, error = "No source objects found matching criteria" };

            var fieldType = field != null ? field.FieldType : propFallback.PropertyType;
            bool isList = fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>);
            bool isArray = fieldType.IsArray;

            if (!isList && !isArray)
                return new { success = false, error = $"Field '{fieldName}' is not a List<> or Array type" };

            WorkflowManager.SnapshotObject(comp);
            Undo.RecordObject(comp, "Smart Bind");

            var elementType = isArray ? fieldType.GetElementType() : fieldType.GetGenericArguments()[0];
            var convertedList = new ArrayList();

            if (appendMode)
            {
                var existing = (field != null ? field.GetValue(comp) : propFallback.GetValue(comp)) as IEnumerable;
                if (existing != null)
                    foreach (var item in existing) convertedList.Add(item);
            }

            foreach (var src in sources)
            {
                if (elementType == typeof(GameObject))
                {
                    if (!convertedList.Contains(src)) convertedList.Add(src);
                }
                else if (typeof(Component).IsAssignableFrom(elementType))
                {
                    var c = src.GetComponent(elementType);
                    if (c != null && !convertedList.Contains(c)) convertedList.Add(c);
                }
            }

            if (isArray)
            {
                var array = System.Array.CreateInstance(elementType, convertedList.Count);
                convertedList.CopyTo(array);
                if (field != null) field.SetValue(comp, array);
                else propFallback.SetValue(comp, array);
            }
            else
            {
                var list = System.Activator.CreateInstance(fieldType) as IList;
                foreach (var item in convertedList) list.Add(item);
                if (field != null) field.SetValue(comp, list);
                else propFallback.SetValue(comp, list);
            }

            EditorUtility.SetDirty(comp);

            return new { success = true, boundCount = convertedList.Count, field = fieldName, appendMode };
        */
    }
}
```
