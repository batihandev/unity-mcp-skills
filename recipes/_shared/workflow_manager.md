# WorkflowManager Undo Shim

Paste-in undo registration. Maps each `Snapshot*` call to the matching
`UnityEditor.Undo.*` API.

## Call surface

- `WorkflowManager.IsRecording` — always `true`; gates with `if (WorkflowManager.IsRecording)` still run.
- `WorkflowManager.SnapshotObject(Object obj, SnapshotType type = SnapshotType.Modified)` — `Created` → `RegisterCreatedObjectUndo`; else `RegisterCompleteObjectUndo`.
- `WorkflowManager.SnapshotCreatedAsset(Object asset)` — for assets created on disk.
- `WorkflowManager.SnapshotCreatedComponent(Component comp)` — for components just added to a GameObject.
- `WorkflowManager.SnapshotCreatedGameObject(GameObject go, string primitiveType = null)` — for newly-created GameObjects. `primitiveType` is a string, not the `UnityEngine.PrimitiveType` enum.
- `SnapshotType.Modified` / `SnapshotType.Created`.

## Call pattern

```csharp
WorkflowManager.SnapshotObject(go);                              // before mutating
WorkflowManager.SnapshotObject(asset, SnapshotType.Created);     // after creating an asset
WorkflowManager.SnapshotCreatedComponent(comp);                  // after AddComponent
WorkflowManager.SnapshotCreatedGameObject(go, "Cube");           // after primitive creation
```

## Paste-in

```csharp
    internal enum SnapshotType
    {
        Modified = 0,
        Created = 1
    }

    internal static class WorkflowManager
    {
        public static bool IsRecording => true;

        public static void SnapshotObject(UnityEngine.Object obj, SnapshotType type = SnapshotType.Modified)
        {
            if (obj == null) return;
            if (type == SnapshotType.Created)
                UnityEditor.Undo.RegisterCreatedObjectUndo(obj, "Create " + obj.GetType().Name);
            else
                UnityEditor.Undo.RegisterCompleteObjectUndo(obj, "Workflow Snapshot");
        }

        public static void SnapshotCreatedAsset(UnityEngine.Object asset)
        {
            if (asset == null) return;
            UnityEditor.Undo.RegisterCreatedObjectUndo(asset, "Create Asset");
        }

        public static void SnapshotCreatedComponent(UnityEngine.Component comp)
        {
            if (comp == null) return;
            UnityEditor.Undo.RegisterCreatedObjectUndo(comp, "Add Component");
        }

        public static void SnapshotCreatedGameObject(UnityEngine.GameObject go, string primitiveType = null)
        {
            if (go == null) return;
            var label = string.IsNullOrEmpty(primitiveType) ? "Create GameObject" : $"Create {primitiveType}";
            UnityEditor.Undo.RegisterCreatedObjectUndo(go, label);
        }
    }
```

## Notes

- Session-level undo (`BeginTask` / `EndTask` / `UndoSession`) is not provided.
- Paste this class in the same `Unity_RunCommand` code block as `CommandScript`.
