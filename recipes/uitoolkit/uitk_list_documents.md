# uitk_list_documents

List all UIDocument components present in the active scene.

**Signature:** `UitkListDocuments()`

**Returns:** `{ count, documents[] { name, instanceId, visualTreeAsset, panelSettings, sortingOrder, active } }`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var docs = FindHelper.FindAll<UIDocument>();
        var documents = docs.Select(doc => new
        {
            name             = doc.gameObject.name,
            instanceId       = doc.gameObject.GetInstanceID(),
            visualTreeAsset  = doc.visualTreeAsset != null ? AssetDatabase.GetAssetPath(doc.visualTreeAsset) : null,
            panelSettings    = doc.panelSettings   != null ? AssetDatabase.GetAssetPath(doc.panelSettings)   : null,
            sortingOrder     = doc.sortingOrder,
            active           = doc.gameObject.activeInHierarchy
        }).ToArray();

        result.SetResult(new { count = documents.Length, documents });
    }
}
```
