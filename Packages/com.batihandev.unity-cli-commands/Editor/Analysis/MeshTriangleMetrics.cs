using UnityEngine;
namespace BatihanDev.UnityCliCommands.Analysis
{
    public static class MeshTriangleMetrics
    {
        public static long Count(Mesh mesh)
        {
            if (mesh == null) return 0;
            long indices = 0;
            for (var index = 0; index < mesh.subMeshCount; index++) indices = checked(indices + mesh.GetIndexCount(index));
            return indices / 3;
        }
    }
}
