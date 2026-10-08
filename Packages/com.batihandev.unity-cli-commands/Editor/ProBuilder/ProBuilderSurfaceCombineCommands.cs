using System;
using System.Collections.Generic;
using System.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

namespace BatihanDev.UnityCliCommands.ProBuilderAuthoring
{
    public static partial class ProBuilderCommands
    {
        [CliCommand("probuilder.face-material", "Assign one existing material slot to exact faces.", Tags = new[] { "unity-cli-commands", "probuilder" })]
        public static CommandResult<ProBuilderMaterialResult> SetFaceMaterial(string target, string faceIndexes = null,
            string materialPath = null, int submeshIndex = -1, bool confirm = false, bool dryRun = false)
        {
            Face[] faces = null;
            Material material = null;
            int slot = -1;
            return ProBuilderCore.Run("unity.probuilder.face-material@1", target, confirm, dryRun,
                mesh =>
                {
                    faces = ProBuilderCore.Faces(mesh, faceIndexes);
                    if (faces.Length == 0) throw new ArgumentException("No faces to assign.");
                    var renderer = mesh.GetComponent<MeshRenderer>();
                    if (renderer == null) throw new ArgumentException("The selected mesh needs a MeshRenderer.");
                    var materials = renderer.sharedMaterials;
                    if (!string.IsNullOrWhiteSpace(materialPath))
                    {
                        if (submeshIndex != -1) throw new ArgumentException("Choose materialPath or submeshIndex, not both.");
                        material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                        if (material == null) throw new ArgumentException("Material path must identify an existing Material asset.");
                        slot = Array.IndexOf(materials, material);
                        if (slot < 0) slot = materials.Length;
                    }
                    else
                    {
                        if (submeshIndex < 0 || submeshIndex >= materials.Length || materials[submeshIndex] == null)
                            throw new ArgumentException("Select an existing nonempty renderer material slot.");
                        slot = submeshIndex;
                    }
                    return new ProBuilderMaterialResult { SelectedFaces = faces.Length, Slot = slot, MaterialSlots = materials.Length };
                },
                (mesh, result) =>
                {
                    var renderer = mesh.GetComponent<MeshRenderer>();
                    if (material != null && slot == renderer.sharedMaterials.Length)
                    {
                        var materials = renderer.sharedMaterials.ToList();
                        materials.Add(material);
                        renderer.sharedMaterials = materials.ToArray();
                    }
                    foreach (var face in faces) face.submeshIndex = slot;
                    ProBuilderCore.Rebuild(mesh);
                    if (slot >= renderer.sharedMaterials.Length || renderer.sharedMaterials[slot] == null)
                        throw new InvalidOperationException("Material slot did not survive mesh compilation.");
                    result.MaterialSlots = renderer.sharedMaterials.Length;
                }, "Set ProBuilder Face Material");
        }

        private static Vector3 BoxAxis(Vector3 normal)
        {
            var x = System.Math.Abs(normal.x);
            var y = System.Math.Abs(normal.y);
            var z = System.Math.Abs(normal.z);
            const float epsilon = 0.0001f;
            if (x > y && x - y >= epsilon && x > z && x - z >= epsilon) return normal.x > 0 ? Vector3.right : Vector3.left;
            if (y > z && y - z >= epsilon) return normal.y > 0 ? Vector3.up : Vector3.down;
            return normal.z > 0 ? Vector3.forward : Vector3.back;
        }

        private static List<int[]> ReadSharedTextureGroups(SerializedProperty groups)
        {
            var output = new List<int[]>();
            for (var i = 0; i < groups.arraySize; i++)
            {
                var vertices = groups.GetArrayElementAtIndex(i).FindPropertyRelative("m_Vertices");
                if (vertices == null || !vertices.isArray) throw new InvalidOperationException("ProBuilder UV association layout is unsupported.");
                var group = new int[vertices.arraySize];
                for (var j = 0; j < group.Length; j++) group[j] = vertices.GetArrayElementAtIndex(j).intValue;
                output.Add(group);
            }
            return output;
        }

        [CliCommand("probuilder.uv-box", "Box-project selected faces into one UV channel.", Tags = new[] { "unity-cli-commands", "probuilder" })]
        public static CommandResult<ProBuilderUvResult> ProjectBoxUv(string target, string faceIndexes = null, int channel = 0,
            bool confirm = false, bool dryRun = false)
        {
            int[] faceSelection = null;
            HashSet<int> selectedVertices = null;
            List<int[]> replacementGroups = null;
            Dictionary<int, Vector2> projected = null;
            List<Vector4>[] originalUv = null;
            return ProBuilderCore.Run("unity.probuilder.uv-box@1", target, confirm, dryRun,
                mesh =>
                {
                    if (channel < 0 || channel > 3) throw new ArgumentException("UV channel must be 0, 1, 2 or 3.");
                    originalUv = Enumerable.Range(0, 4).Select(_ => new List<Vector4>()).ToArray();
                    for (var index = 0; index < originalUv.Length; index++)
                    {
                        mesh.GetUVs(index, originalUv[index]);
                        if (originalUv[index].Count != 0 && originalUv[index].Count != mesh.vertexCount)
                            throw new ArgumentException("An existing UV channel length does not match the vertex count.");
                    }
                    faceSelection = ProBuilderCore.Indexes(faceIndexes, mesh.faceCount, false);
                    if (faceSelection.Length == 0) throw new ArgumentException("No faces to project.");
                    selectedVertices = new HashSet<int>(faceSelection.SelectMany(index => mesh.faces[index].distinctIndexes));
                    var others = new HashSet<int>(Enumerable.Range(0, mesh.faceCount).Except(faceSelection)
                        .SelectMany(index => mesh.faces[index].distinctIndexes));
                    if (selectedVertices.Overlaps(others))
                        throw new ArgumentException("Selected and unselected faces share vertex indexes; split topology before projecting only these faces.");
                    var serialized = new SerializedObject(mesh);
                    var faceArray = serialized.FindProperty("m_Faces");
                    var groups = serialized.FindProperty("m_SharedTextures");
                    if (faceArray == null || !faceArray.isArray || faceArray.arraySize != mesh.faceCount || groups == null || !groups.isArray)
                        throw new InvalidOperationException("ProBuilder face or UV association layout is unsupported.");
                    foreach (var index in faceSelection)
                    {
                        var face = faceArray.GetArrayElementAtIndex(index);
                        if (face.FindPropertyRelative("elementGroup")?.propertyType != SerializedPropertyType.Integer ||
                            face.FindPropertyRelative("m_ManualUV")?.propertyType != SerializedPropertyType.Boolean)
                            throw new InvalidOperationException("ProBuilder face UV metadata layout is unsupported.");
                    }
                    replacementGroups = ReadSharedTextureGroups(groups)
                        .Select(group => group.Where(index => !selectedVertices.Contains(index)).ToArray())
                        .Where(group => group.Length > 0).ToList();
                    replacementGroups.AddRange(selectedVertices.OrderBy(index => index).Select(index => new[] { index }));
                    projected = new Dictionary<int, Vector2>();
                    var grouped = faceSelection.GroupBy(index => BoxAxis(UnityEngine.ProBuilder.Math.Normal(mesh, mesh.faces[index])));
                    foreach (var group in grouped)
                    {
                        var indexes = group.SelectMany(index => mesh.faces[index].distinctIndexes).ToArray();
                        var uv = Projection.PlanarProject(mesh.positions, indexes, group.Key);
                        for (var i = 0; i < indexes.Length; i++) projected[indexes[i]] = uv[i];
                    }
                    return new ProBuilderUvResult { Channel = channel, SelectedFaces = faceSelection.Length,
                        AssignedVertices = projected.Count, ChannelOneClearedByFutureRebuild = channel == 1 };
                },
                (mesh, result) =>
                {
                    var serialized = new SerializedObject(mesh);
                    var faceArray = serialized.FindProperty("m_Faces");
                    var groups = serialized.FindProperty("m_SharedTextures");
                    foreach (var index in faceSelection)
                    {
                        var face = faceArray.GetArrayElementAtIndex(index);
                        face.FindPropertyRelative("elementGroup").intValue = -1;
                        face.FindPropertyRelative("m_ManualUV").boolValue = true;
                    }
                    groups.arraySize = replacementGroups.Count;
                    for (var i = 0; i < replacementGroups.Count; i++)
                    {
                        var vertices = groups.GetArrayElementAtIndex(i).FindPropertyRelative("m_Vertices");
                        vertices.arraySize = replacementGroups[i].Length;
                        for (var j = 0; j < replacementGroups[i].Length; j++)
                            vertices.GetArrayElementAtIndex(j).intValue = replacementGroups[i][j];
                    }
                    serialized.ApplyModifiedProperties();
                    mesh.OnAfterDeserialize();
                    // ToMesh clears channel 1; restore every channel after the metadata rebuild.
                    mesh.ToMesh();
                    mesh.Refresh();
                    var uv = originalUv[channel].Count == mesh.vertexCount ? new List<Vector4>(originalUv[channel]) :
                        Enumerable.Repeat(Vector4.zero, mesh.vertexCount).ToList();
                    foreach (var item in projected) uv[item.Key] = new Vector4(item.Value.x, item.Value.y, uv[item.Key].z, uv[item.Key].w);
                    var compiled = ProBuilderCore.CompiledMesh(mesh);
                    for (var index = 0; index < originalUv.Length; index++)
                    {
                        var values = index == channel ? uv : originalUv[index];
                        mesh.SetUVs(index, values);
                        compiled.SetUVs(index, values);
                    }
                    for (var index = 0; index < originalUv.Length; index++)
                    {
                        var expected = index == channel ? uv : originalUv[index];
                        var editable = new List<Vector4>();
                        var output = new List<Vector4>();
                        mesh.GetUVs(index, editable);
                        compiled.GetUVs(index, output);
                        if (editable.Count != expected.Count || output.Count != expected.Count ||
                            Enumerable.Range(0, expected.Count).Any(vertex =>
                                editable[vertex] != expected[vertex] || output[vertex] != expected[vertex]))
                            throw new InvalidOperationException("UV coordinates did not survive mesh compilation.");
                    }
                    EditorUtility.SetDirty(mesh);
                    EditorUtility.SetDirty(compiled);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(mesh);
                    EditorSceneManager.MarkSceneDirty(mesh.gameObject.scene);
                }, "Box Project ProBuilder UVs");
        }

        [CliCommand("probuilder.combine", "Combine exact ordered ProBuilder meshes, preserving every returned output.", Tags = new[] { "unity-cli-commands", "probuilder" })]
        public static CommandResult<ProBuilderCombineResult> Combine(string targets, bool confirm = false, bool dryRun = false)
        {
            const string schema = "unity.probuilder.combine@1";
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<ProBuilderCombineResult>.Failure(schema, compatibility.Error);
            try
            {
                ProBuilderCore.EditMode();
                if (string.IsNullOrWhiteSpace(targets)) throw new ArgumentException("Supply two or more exact ProBuilderMesh handles, separated by commas.");
                var meshes = targets.Split(',').Select(ProBuilderCore.Mesh).ToArray();
                if (meshes.Length < 2 || meshes.Distinct().Count() != meshes.Length)
                    throw new ArgumentException("Supply at least two unique exact ProBuilder meshes.");
                var scene = meshes[0].gameObject.scene;
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene() != scene || meshes.Any(mesh => mesh.gameObject.scene != scene))
                    throw new ArgumentException("All meshes must be in the active loaded regular scene.");
                foreach (var mesh in meshes) ProBuilderCore.OwnedMesh(mesh);
                for (var i = 0; i < meshes.Length; i++)
                    for (var j = i + 1; j < meshes.Length; j++)
                        if (meshes[i].transform.IsChildOf(meshes[j].transform) || meshes[j].transform.IsChildOf(meshes[i].transform))
                            throw new ArgumentException("Combine inputs cannot contain each other in the hierarchy.");
                var result = new ProBuilderCombineResult
                {
                    Target = ProBuilderCore.Id(meshes[0]), DryRun = dryRun, SceneDirty = scene.isDirty,
                    Outputs = meshes.Select(ProBuilderCore.Id).ToArray(),
                    OutputFaces = meshes.Select(mesh => mesh.faceCount).ToArray(),
                    OutputVertices = meshes.Select(mesh => mesh.vertexCount).ToArray(),
                    ConsumedInputs = Array.Empty<string>()
                };
                if (dryRun) return CommandResult<ProBuilderCombineResult>.Success(schema, result);
                if (!confirm) return CommandResult<ProBuilderCombineResult>.Failure(schema, "CONFIRMATION_REQUIRED", "Set confirm=true or dryRun=true.");
                Undo.IncrementCurrentGroup();
                var group = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Combine ProBuilder Meshes");
                var before = new HashSet<ProBuilderMesh>(Resources.FindObjectsOfTypeAll<ProBuilderMesh>());
                try
                {
                    foreach (var mesh in meshes) ProBuilderCore.Record(mesh, "Combine ProBuilder Meshes");
                    var returned = CombineMeshes.Combine(meshes, meshes[0]);
                    if (returned == null || returned.Count == 0 || returned.Any(mesh => mesh == null) ||
                        returned.Distinct().Count() != returned.Count || !returned.Contains(meshes[0]))
                        throw new InvalidOperationException("Combine returned an invalid output set.");
                    foreach (var mesh in returned)
                    {
                        if (mesh.gameObject.scene != scene) throw new InvalidOperationException("Combine produced an output in another scene.");
                        if (!before.Contains(mesh)) Undo.RegisterCreatedObjectUndo(mesh.gameObject, "Combine ProBuilder Meshes");
                        if (mesh == meshes[0] || !before.Contains(mesh)) ProBuilderCore.Rebuild(mesh);
                    }
                    var consumed = meshes.Where(mesh => !returned.Contains(mesh)).ToArray();
                    result.ConsumedInputs = consumed.Select(ProBuilderCore.Id).ToArray();
                    foreach (var mesh in consumed) Undo.DestroyObjectImmediate(mesh.gameObject);
                    result.Outputs = returned.Select(ProBuilderCore.Id).ToArray();
                    result.OutputFaces = returned.Select(mesh => mesh.faceCount).ToArray();
                    result.OutputVertices = returned.Select(mesh => mesh.vertexCount).ToArray();
                    result.Applied = true;
                    EditorSceneManager.MarkSceneDirty(scene);
                    result.SceneDirty = scene.isDirty;
                    Undo.CollapseUndoOperations(group);
                    return CommandResult<ProBuilderCombineResult>.Success(schema, result);
                }
                catch
                {
                    foreach (var created in Resources.FindObjectsOfTypeAll<ProBuilderMesh>().Where(mesh => !before.Contains(mesh) && mesh != null && mesh.gameObject.scene == scene).ToArray())
                        UnityEngine.Object.DestroyImmediate(created.gameObject);
                    Undo.RevertAllDownToGroup(group);
                    throw;
                }
            }
            catch (Exception error) { return CommandResult<ProBuilderCombineResult>.Failure(schema, "PROBUILDER_REFUSED", error.Message); }
        }
    }
}
