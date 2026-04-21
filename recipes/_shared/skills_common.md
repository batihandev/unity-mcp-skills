# Skills Common Utilities

General helper classes for common cross-domain tasks in `IRunCommand` scripts.

```csharp
    /// <summary>
    /// Shared utilities used across skill modules.
    /// </summary>
    public static class SkillsCommon
    {
        /// <summary>UTF-8 encoding without BOM.</summary>
        public static readonly System.Text.Encoding Utf8NoBom = new System.Text.UTF8Encoding(false);

        /// <summary>
        /// Get all loaded types across all non-dynamic assemblies.
        /// </summary>
        public static System.Collections.Generic.IEnumerable<System.Type> GetAllLoadedTypes()
        {
            return System.Linq.Enumerable.SelectMany(
                System.Linq.Enumerable.Where(System.AppDomain.CurrentDomain.GetAssemblies(), a => !a.IsDynamic),
                a => { try { return a.GetTypes(); } catch { return System.Type.EmptyTypes; } });
        }

        /// <summary>
        /// Get triangle count for a mesh without allocating the full triangles array.
        /// </summary>
        public static int GetTriangleCount(UnityEngine.Mesh mesh)
        {
            int count = 0;
            for (int i = 0; i < mesh.subMeshCount; i++)
                count += (int)mesh.GetIndexCount(i);
            return count / 3;
        }
    }
```
