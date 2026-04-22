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

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

        if (string.IsNullOrEmpty(fieldName)) { result.SetResult(new { error = "fieldName is required" }); return; }

        // 1. Find Target
        var targetGo = GameObjectFinder.Find(name: targetName);
        if (targetGo == null) 
            { result.SetResult(new { success = false, error = $"Target '{targetName}' not found" }); return; }

        var comp = targetGo.GetComponent(componentName);
        if (comp == null) 
            { result.SetResult(new { success = false, error = $"Component '{componentName}' not found on target" }); return; }

        // 2. Find Member (field, then Unity naming conventions, then property)
        // BindingFlags with | triggers reformatter NRE; use no-arg GetFields()/GetProperties() + manual filter
        var type = comp.GetType();
        var field = type.GetFields().FirstOrDefault(f => f.Name == fieldName);
        if (field == null)
            field = type.GetFields().FirstOrDefault(f => f.Name == "m_" + char.ToUpper(fieldName[0]) + fieldName.Substring(1));
        if (field == null)
            field = type.GetFields().FirstOrDefault(f => f.Name == "_" + fieldName);

        System.Reflection.PropertyInfo propFallback = null;
        if (field == null)
        {
            propFallback = type.GetProperties().FirstOrDefault(p => p.Name == fieldName && p.CanWrite);
        }

        if (field == null && propFallback == null)
            { result.SetResult(new { success = false, error = $"Field '{fieldName}' not found on {componentName}" }); return; }

        // 3. Find Source Objects
        var sources = new List<GameObject>();
        if (!string.IsNullOrEmpty(sourceTag))
        {
            try { sources.AddRange(GameObject.FindGameObjectsWithTag(sourceTag)); }
            catch { { result.SetResult(new { success = false, error = $"Tag '{sourceTag}' does not exist" }); return; } }
        }
        if (!string.IsNullOrEmpty(sourceName))
        {
            sources.AddRange(FindHelper.FindAll<GameObject>().Where(g => g.name.Contains(sourceName)));
        }
        sources = sources.Distinct().ToList();

        if (sources.Count == 0) 
            { result.SetResult(new { success = false, error = "No source objects found matching criteria" }); return; }

        // 4. Validate field type
        var fieldType = field != null ? field.FieldType : propFallback.PropertyType;
        bool isList = fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>);
        bool isArray = fieldType.IsArray;

        if (!isList && !isArray)
            { result.SetResult(new { success = false, error = $"Field '{fieldName}' is not a List<> or Array type" }); return; }

        WorkflowManager.SnapshotObject(comp);
        Undo.RecordObject(comp, "Smart Bind");

        // Element Type
        var elementType = isArray ? fieldType.GetElementType() : fieldType.GetGenericArguments()[0];

        // Convert GameObjects to ElementType
        var convertedList = new ArrayList();

        // Append mode: start with existing items
        if (appendMode)
        {
            var existing = (field != null ? field.GetValue(comp) : propFallback.GetValue(comp)) as IEnumerable;
            if (existing != null)
            {
                foreach (var item in existing) convertedList.Add(item);
            }
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

        // Set value
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

        { result.SetResult(new { success = true, boundCount = convertedList.Count, field = fieldName, appendMode }); return; }
    }
}
```
