using System;
using System.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using UnityEngine.ProBuilder.Shapes;

namespace BatihanDev.UnityCliCommands.ProBuilderAuthoring
{
    public static partial class ProBuilderCommands
    {
        private const string CreateSchema = "unity.probuilder.create-shape@1";
        private const string InspectSchema = "unity.probuilder.inspect@1";
        private const string VerticesSchema = "unity.probuilder.vertices@1";

        private static Type ShapeType(string shape)
        {
            switch (shape)
            {
                case "Cube": return typeof(Cube);
                case "Sphere": return typeof(Sphere);
                case "Cylinder": return typeof(Cylinder);
                case "Cone": return typeof(Cone);
                case "Torus": return typeof(Torus);
                case "Prism": return typeof(Prism);
                case "Arch": return typeof(Arch);
                case "Pipe": return typeof(Pipe);
                case "Stairs": return typeof(Stairs);
                case "Door": return typeof(Door);
                case "Plane": return typeof(UnityEngine.ProBuilder.Shapes.Plane);
                default: return null;
            }
        }

        [CliCommand("probuilder.create-shape", "Create one editable ProBuilder shape in a loaded scene.", Tags = new[] { "unity-cli-commands", "probuilder" })]
        public static CommandResult<ProBuilderCreateResult> CreateShape(
            string shape = "Cube", string name = null,
            float x = 0, float y = 0, float z = 0,
            float sizeX = 1, float sizeY = 1, float sizeZ = 1,
            float rotX = 0, float rotY = 0, float rotZ = 0,
            string parent = null, string materialPath = null,
            bool confirm = false, bool dryRun = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<ProBuilderCreateResult>.Failure(CreateSchema, compatibility.Error);
            try
            {
                ProBuilderCore.EditMode();
                var type = ShapeType(shape);
                if (type == null) throw new ArgumentException("Use one of the eleven documented ProBuilder shape names.");
                if (!(new[] { x, y, z, sizeX, sizeY, sizeZ, rotX, rotY, rotZ }).All(ProBuilderCore.Finite) ||
                    sizeX <= 0 || sizeY <= 0 || sizeZ <= 0)
                    throw new ArgumentException("Position, rotation and positive size must be finite.");
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if (!ProBuilderCore.RegularActiveScene(scene))
                    throw new ArgumentException("Select a saved, loaded regular scene before creating a shape.");
                var parentObject = string.IsNullOrWhiteSpace(parent) ? null : ProBuilderCore.Resolve<GameObject>(parent);
                if (!string.IsNullOrWhiteSpace(parent) && (!ProBuilderCore.RegularScene(parentObject?.transform) || parentObject.scene != scene))
                    throw new ArgumentException("Parent must identify an exact object in the active loaded scene.");
                var material = string.IsNullOrWhiteSpace(materialPath) ? null : AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (!string.IsNullOrWhiteSpace(materialPath) && material == null)
                    throw new ArgumentException("Material path must identify an existing Material asset.");
                var result = new ProBuilderCreateResult { Name = string.IsNullOrWhiteSpace(name) ? shape : name, Shape = shape,
                    DryRun = dryRun, SceneDirty = scene.isDirty };
                if (dryRun) return CommandResult<ProBuilderCreateResult>.Success(CreateSchema, result);
                if (!confirm) return CommandResult<ProBuilderCreateResult>.Failure(CreateSchema, "CONFIRMATION_REQUIRED", "Set confirm=true or dryRun=true.");
                Undo.IncrementCurrentGroup();
                var group = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Create ProBuilder Shape");
                ProBuilderMesh mesh = null;
                try
                {
                    mesh = ShapeFactory.Instantiate(type);
                    if (mesh == null) throw new InvalidOperationException("ProBuilder shape creation returned no mesh.");
                    Undo.RegisterCreatedObjectUndo(mesh.gameObject, "Create ProBuilder Shape");
                    mesh.gameObject.name = result.Name;
                    mesh.transform.localScale = new Vector3(sizeX, sizeY, sizeZ);
                    mesh.FreezeScaleTransform();
                    mesh.transform.position = new Vector3(x, y, z);
                    mesh.transform.rotation = Quaternion.Euler(rotX, rotY, rotZ);
                    if (parentObject != null) Undo.SetTransformParent(mesh.transform, parentObject.transform, "Parent ProBuilder Shape");
                    if (material != null) mesh.GetComponent<MeshRenderer>().sharedMaterial = material;
                    ProBuilderCore.Rebuild(mesh);
                    result.Target = ProBuilderCore.Id(mesh);
                    result.Name = mesh.gameObject.name;
                    result.TotalFaces = mesh.faceCount;
                    result.TotalVertices = mesh.vertexCount;
                    result.Applied = true;
                    result.SceneDirty = mesh.gameObject.scene.isDirty;
                    Undo.CollapseUndoOperations(group);
                    return CommandResult<ProBuilderCreateResult>.Success(CreateSchema, result);
                }
                catch
                {
                    Undo.RevertAllDownToGroup(group);
                    throw;
                }
            }
            catch (Exception error) { return CommandResult<ProBuilderCreateResult>.Failure(CreateSchema, "PROBUILDER_REFUSED", error.Message); }
        }

        [CliCommand("probuilder.inspect", "Inspect one exact ProBuilder mesh and its renderer slots.", Tags = new[] { "unity-cli-commands", "probuilder" })]
        public static CommandResult<ProBuilderInspectResult> Inspect([CliArg("target", "Exact ProBuilderMesh or GameObject.", Required = true)] string target)
        {
            try
            {
                var mesh = ProBuilderCore.Mesh(target);
                var renderer = mesh.GetComponent<MeshRenderer>();
                var materials = renderer == null ? Array.Empty<Material>() : renderer.sharedMaterials;
                var compiled = ProBuilderCore.CompiledMesh(mesh);
                var bounds = compiled == null ? new Bounds() : compiled.bounds;
                var shape = "unknown";
                foreach (var component in mesh.GetComponents<UnityEngine.Component>())
                {
                    if (component == null) continue;
                    var property = new SerializedObject(component).FindProperty("m_Shape");
                    if (property == null || property.propertyType != SerializedPropertyType.ManagedReference) continue;
                    var typeName = property.managedReferenceFullTypename;
                    if (!string.IsNullOrEmpty(typeName))
                    {
                        var fullName = typeName.Split(' ').Last();
                        shape = fullName.Split('.').Last();
                        break;
                    }
                }
                var result = new ProBuilderInspectResult
                {
                    Target = ProBuilderCore.Id(mesh), Name = mesh.gameObject.name, Shape = shape,
                    FaceCount = mesh.faceCount, VertexCount = mesh.vertexCount, EdgeIncidenceCount = mesh.edgeCount,
                    Position = ProBuilderCore.Values(mesh.transform.position), BoundsMin = ProBuilderCore.Values(bounds.min),
                    BoundsMax = ProBuilderCore.Values(bounds.max),
                    Materials = materials.Select(ProBuilderCore.Id).ToArray(),
                    FacesPerMaterialSlot = Enumerable.Range(0, materials.Length).Select(slot => mesh.faces.Count(face => face.submeshIndex == slot)).ToArray(),
                    SceneDirty = mesh.gameObject.scene.isDirty
                };
                return CommandResult<ProBuilderInspectResult>.Success(InspectSchema, result);
            }
            catch (Exception error) { return CommandResult<ProBuilderInspectResult>.Failure(InspectSchema, "PROBUILDER_REFUSED", error.Message); }
        }

        [CliCommand("probuilder.vertices", "Read local vertex positions or a large-mesh bounds summary.", Tags = new[] { "unity-cli-commands", "probuilder" })]
        public static CommandResult<ProBuilderVertexResult> Vertices(
            [CliArg("target", "Exact ProBuilderMesh or GameObject.", Required = true)] string target,
            string vertexIndexes = null, bool verbose = true)
        {
            try
            {
                var mesh = ProBuilderCore.Mesh(target);
                var explicitIndexes = !string.IsNullOrWhiteSpace(vertexIndexes);
                var indexes = ProBuilderCore.Indexes(vertexIndexes, mesh.vertexCount, false);
                var positions = mesh.positions;
                var compiled = ProBuilderCore.CompiledMesh(mesh);
                var bounds = compiled == null ? new Bounds() : compiled.bounds;
                return CommandResult<ProBuilderVertexResult>.Success(VerticesSchema, new ProBuilderVertexResult
                {
                    Target = ProBuilderCore.Id(mesh), TotalVertices = mesh.vertexCount,
                    SelectedVertices = indexes.Length,
                    Positions = !explicitIndexes && !verbose && indexes.Length > 100 ? null :
                        indexes.Select(index => ProBuilderCore.Values(positions[index])).ToArray(),
                    BoundsMin = ProBuilderCore.Values(bounds.min), BoundsMax = ProBuilderCore.Values(bounds.max),
                    SceneDirty = mesh.gameObject.scene.isDirty
                });
            }
            catch (Exception error) { return CommandResult<ProBuilderVertexResult>.Failure(VerticesSchema, "PROBUILDER_REFUSED", error.Message); }
        }
    }
}
