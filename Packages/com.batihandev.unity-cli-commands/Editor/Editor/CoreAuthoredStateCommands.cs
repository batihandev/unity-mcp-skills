using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using BatihanDev.UnityCliCommands.Foundation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityObject = UnityEngine.Object;

namespace BatihanDev.UnityCliCommands.Editor
{
    internal static class ExactObjectReference
    {
        internal static T Resolve<T>(string reference) where T : UnityObject
        {
            if (string.IsNullOrWhiteSpace(reference))
                return null;
            if (reference.StartsWith("GlobalObjectId_V1-", StringComparison.Ordinal) &&
                GlobalObjectId.TryParse(reference, out var globalId))
                return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId) as T;
            if (ulong.TryParse(reference, NumberStyles.None, CultureInfo.InvariantCulture, out var exact))
                return EditorUtility.EntityIdToObject(EntityId.FromULong(exact)) as T;
            if (reference.StartsWith("Assets/", StringComparison.Ordinal) ||
                reference.StartsWith("Packages/", StringComparison.Ordinal))
                return AssetDatabase.LoadAssetAtPath<T>(reference);
            return null;
        }

        internal static string ExactId(UnityObject value) => value == null
            ? null
            : EntityId.ToULong(value.GetEntityId()).ToString(CultureInfo.InvariantCulture);
    }

    internal static class PublicValueConversion
    {
        internal static bool TryConvert(string value, Type targetType, string reference, out object converted)
        {
            converted = null;
            var nullable = Nullable.GetUnderlyingType(targetType);
            if (nullable != null)
                targetType = nullable;
            if (typeof(UnityObject).IsAssignableFrom(targetType))
            {
                if (string.Equals(reference, "null", StringComparison.OrdinalIgnoreCase))
                    return true;
                converted = ResolveUnityObject(reference, targetType);
                return converted != null;
            }
            if (targetType == typeof(string)) { converted = value; return true; }
            if (targetType == typeof(bool))
            {
                if (bool.TryParse(value, out var parsed)) { converted = parsed; return true; }
                if (value == "1" || value == "0") { converted = value == "1"; return true; }
                return false;
            }
            if (targetType.IsEnum)
            {
                var enumName = value;
                var qualifierEnd = value?.LastIndexOf('.') ?? -1;
                if (qualifierEnd >= 0)
                {
                    var qualifier = value.Substring(0, qualifierEnd);
                    if (qualifier != targetType.Name && qualifier != targetType.FullName) return false;
                    enumName = value.Substring(qualifierEnd + 1);
                }
                var matches = Enum.GetNames(targetType)
                    .Where(name => string.Equals(name, enumName, StringComparison.OrdinalIgnoreCase)).ToArray();
                if (matches.Length != 1) return false;
                converted = Enum.Parse(targetType, matches[0], false);
                return true;
            }
            if (targetType == typeof(int) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)) { converted = i; return true; }
            if (targetType == typeof(long) && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l)) { converted = l; return true; }
            if (targetType == typeof(float) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var f)) { converted = f; return true; }
            if (targetType == typeof(double) && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) { converted = d; return true; }
            if (targetType == typeof(LayerMask))
            {
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var mask)) { converted = (LayerMask)mask; return true; }
                var layer = LayerMask.NameToLayer(value);
                if (layer < 0) return false;
                converted = (LayerMask)(1 << layer); return true;
            }
            if (targetType == typeof(Color)) return TryColor(value, out converted);
            if (targetType == typeof(AnimationCurve)) return TryCurve(value, out converted);
            var parts = (value ?? string.Empty).Split(',')
                .Select(part => part.Trim()).ToArray();
            if (!parts.All(part => float.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out _)))
                return false;
            var numbers = parts.Select(part => float.Parse(part, CultureInfo.InvariantCulture)).ToArray();
            if (targetType == typeof(Vector2) && numbers.Length == 2) { converted = new Vector2(numbers[0], numbers[1]); return true; }
            if (targetType == typeof(Vector3) && numbers.Length == 3) { converted = new Vector3(numbers[0], numbers[1], numbers[2]); return true; }
            if (targetType == typeof(Vector4) && numbers.Length == 4) { converted = new Vector4(numbers[0], numbers[1], numbers[2], numbers[3]); return true; }
            if (targetType == typeof(Quaternion) && numbers.Length == 3) { converted = Quaternion.Euler(numbers[0], numbers[1], numbers[2]); return true; }
            if (targetType == typeof(Quaternion) && numbers.Length == 4) { converted = new Quaternion(numbers[0], numbers[1], numbers[2], numbers[3]); return true; }
            return false;
        }

        private static UnityObject ResolveUnityObject(string reference, Type targetType)
        {
            var value = ExactObjectReference.Resolve<UnityObject>(reference);
            return value != null && targetType.IsInstanceOfType(value) ? value : null;
        }

        private static bool TryColor(string value, out object converted)
        {
            converted = null;
            if (ColorUtility.TryParseHtmlString(value, out var html)) { converted = html; return true; }
            var named = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
            {
                ["red"] = Color.red, ["green"] = Color.green, ["blue"] = Color.blue,
                ["black"] = Color.black, ["white"] = Color.white, ["yellow"] = Color.yellow,
                ["cyan"] = Color.cyan, ["magenta"] = Color.magenta, ["gray"] = Color.gray,
                ["grey"] = Color.grey, ["clear"] = Color.clear
            };
            if (named.TryGetValue(value ?? string.Empty, out var color)) { converted = color; return true; }
            var parts = (value ?? string.Empty).Split(',');
            if ((parts.Length == 3 || parts.Length == 4) && parts.All(part =>
                    float.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out _)))
            {
                var values = parts.Select(part => float.Parse(part, CultureInfo.InvariantCulture)).ToArray();
                converted = new Color(values[0], values[1], values[2], values.Length == 4 ? values[3] : 1f);
                return true;
            }
            return false;
        }

        private static bool TryCurve(string value, out object converted)
        {
            converted = null;
            switch ((value ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "linear": converted = AnimationCurve.Linear(0f, 0f, 1f, 1f); return true;
                case "easein": converted = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); return true;
                case "easeout": converted = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); return true;
                case "easeinout": converted = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); return true;
                case "constant": converted = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 0f)); return true;
                default: return false;
            }
        }
    }
}

namespace BatihanDev.UnityCliCommands.Transform
{
    public static class TransformCommands
    {
        private const string WorldSchema = "unity.transform.world@1";

        [CliCommand("transform.world", "Set independently optional world position and world Euler axes on one exact GameObject.",
            Tags = new[] { "unity-cli-commands", "gameobject" })]
        public static CommandResult<TransformWorldResult> World(
            [CliArg("target", "Exact GameObject ObjectRef string.")] string target,
            float? posX = null, float? posY = null, float? posZ = null,
            float? rotX = null, float? rotY = null, float? rotZ = null)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<TransformWorldResult>.Failure(WorldSchema, compatibility.Error);
            var gameObject = Editor.ExactObjectReference.Resolve<GameObject>(target);
            if (gameObject == null) return Failure<TransformWorldResult>(WorldSchema, "TARGET_NOT_FOUND", target);
            var transform = gameObject.transform;
            var beforePosition = transform.position;
            var beforeRotation = transform.eulerAngles;
            var changed = posX.HasValue || posY.HasValue || posZ.HasValue || rotX.HasValue || rotY.HasValue || rotZ.HasValue;
            if (changed) Undo.RecordObject(transform, "Set World Transform");
            if (posX.HasValue || posY.HasValue || posZ.HasValue)
                transform.position = new Vector3(posX ?? transform.position.x, posY ?? transform.position.y, posZ ?? transform.position.z);
            if (rotX.HasValue || rotY.HasValue || rotZ.HasValue)
                transform.eulerAngles = new Vector3(rotX ?? transform.eulerAngles.x, rotY ?? transform.eulerAngles.y, rotZ ?? transform.eulerAngles.z);
            return CommandResult<TransformWorldResult>.Success(WorldSchema, new TransformWorldResult
            {
                Target = Editor.ExactObjectReference.ExactId(gameObject),
                BeforePosition = Vector(beforePosition), AfterPosition = Vector(transform.position),
                BeforeEuler = Vector(beforeRotation), AfterEuler = Vector(transform.eulerAngles), Applied = changed
            });
        }

        private static Float3 Vector(Vector3 value) => new Float3 { X = value.x, Y = value.y, Z = value.z };
        internal static CommandResult<T> Failure<T>(string schema, string code, string target) =>
            CommandResult<T>.Failure(schema, code, "The exact target could not be resolved.",
                new Dictionary<string, object> { ["target"] = target ?? string.Empty });
    }

    public static class RectTransformCommands
    {
        private const string Schema = "unity.rect-transform.size@1";

        [CliCommand("rect-transform.size", "Set independently optional physical width and height on one exact RectTransform.",
            Tags = new[] { "unity-cli-commands", "gameobject", "ui" })]
        public static CommandResult<RectTransformSizeResult> Size(
            [CliArg("target", "Exact GameObject or RectTransform ObjectRef string.")] string target,
            float? width = null, float? height = null)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<RectTransformSizeResult>.Failure(Schema, compatibility.Error);
            var rect = Editor.ExactObjectReference.Resolve<RectTransform>(target);
            if (rect == null)
            {
                var gameObject = Editor.ExactObjectReference.Resolve<GameObject>(target);
                rect = gameObject == null ? null : gameObject.GetComponent<RectTransform>();
            }
            if (rect == null) return TransformCommands.Failure<RectTransformSizeResult>(Schema, "RECT_TRANSFORM_REQUIRED", target);
            var before = rect.rect.size;
            if (width.HasValue || height.HasValue) Undo.RecordObject(rect, "Set RectTransform Physical Size");
            if (width.HasValue) rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width.Value);
            if (height.HasValue) rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height.Value);
            return CommandResult<RectTransformSizeResult>.Success(Schema, new RectTransformSizeResult
            {
                Target = Editor.ExactObjectReference.ExactId(rect),
                Before = new Float2 { X = before.x, Y = before.y },
                After = new Float2 { X = rect.rect.width, Y = rect.rect.height },
                Applied = width.HasValue || height.HasValue
            });
        }
    }

    [Serializable] public sealed class Float2 { public float X { get; set; } public float Y { get; set; } }
    [Serializable] public sealed class Float3 { public float X { get; set; } public float Y { get; set; } public float Z { get; set; } }
    [Serializable] public sealed class TransformWorldResult
    {
        public string Target { get; set; }
        public Float3 BeforePosition { get; set; }
        public Float3 AfterPosition { get; set; }
        public Float3 BeforeEuler { get; set; }
        public Float3 AfterEuler { get; set; }
        public bool Applied { get; set; }
    }
    [Serializable] public sealed class RectTransformSizeResult
    {
        public string Target { get; set; }
        public Float2 Before { get; set; }
        public Float2 After { get; set; }
        public bool Applied { get; set; }
    }
}

namespace BatihanDev.UnityCliCommands.Component
{
    public static class ComponentAuthoringCommands
    {
        private const string CopySchema = "unity.component.copy@1";
        private const string RemoveSchema = "unity.component.remove@1";
        private const string MemberSchema = "unity.component.member-set@1";
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

        [CliCommand("component.copy", "Copy one exact selected component to one exact destination GameObject.",
            Tags = new[] { "unity-cli-commands", "component" })]
        public static CommandResult<ComponentCopyResult> Copy(string source, string destination)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<ComponentCopyResult>.Failure(CopySchema, compatibility.Error);
            var component = Editor.ExactObjectReference.Resolve<UnityEngine.Component>(source);
            var gameObject = Editor.ExactObjectReference.Resolve<GameObject>(destination);
            if (component == null || gameObject == null)
                return Failure<ComponentCopyResult>(CopySchema, "TARGET_NOT_FOUND", source, destination);
            if (component is UnityEngine.Transform)
                return Failure<ComponentCopyResult>(CopySchema, "COMPONENT_NOT_COPYABLE", source, destination);
            var group = Undo.GetCurrentGroup();
            try
            {
                Undo.SetCurrentGroupName("Copy Component");
                var copy = Undo.AddComponent(gameObject, component.GetType());
                EditorUtility.CopySerialized(component, copy);
                EditorUtility.SetDirty(copy);
                Undo.CollapseUndoOperations(group);
                return CommandResult<ComponentCopyResult>.Success(CopySchema, new ComponentCopyResult
                {
                    Source = Editor.ExactObjectReference.ExactId(component),
                    Destination = Editor.ExactObjectReference.ExactId(gameObject),
                    Component = component.GetType().FullName,
                    Created = Editor.ExactObjectReference.ExactId(copy)
                });
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(group);
                return Failure<ComponentCopyResult>(CopySchema, "COMPONENT_COPY_FAILED", source, destination, exception);
            }
        }

        [CliCommand("component.remove", "Safely remove one exact component after RequireComponent dependency preflight.",
            Tags = new[] { "unity-cli-commands", "component" })]
        public static CommandResult<ComponentRemoveResult> Remove(string component)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<ComponentRemoveResult>.Failure(RemoveSchema, compatibility.Error);
            var selected = Editor.ExactObjectReference.Resolve<UnityEngine.Component>(component);
            if (selected == null)
                return Failure<ComponentRemoveResult>(RemoveSchema, "COMPONENT_NOT_FOUND", component, null);
            if (selected is UnityEngine.Transform)
                return Failure<ComponentRemoveResult>(RemoveSchema, "COMPONENT_NOT_REMOVABLE", component, null);
            var dependents = RequiredDependents(selected).ToArray();
            if (dependents.Length != 0)
                return CommandResult<ComponentRemoveResult>.Failure(RemoveSchema, "REQUIRED_COMPONENT",
                    "Another component requires the selected component.", new Dictionary<string, object>
                    {
                        ["component"] = component, ["dependents"] = dependents
                    });
            var gameObject = selected.gameObject;
            var selectedType = selected.GetType().FullName;
            var group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Remove Component");
            Undo.DestroyObjectImmediate(selected);
            if (selected != null)
            {
                Undo.RevertAllDownToGroup(group);
                return Failure<ComponentRemoveResult>(RemoveSchema, "REMOVAL_NOT_APPLIED", component, null);
            }
            Undo.CollapseUndoOperations(group);
            return CommandResult<ComponentRemoveResult>.Success(RemoveSchema, new ComponentRemoveResult
            {
                Component = component, ComponentType = selectedType,
                GameObject = Editor.ExactObjectReference.ExactId(gameObject), Removed = true
            });
        }

        [CliCommand("component.member-set", "Set a writable public member that has no established serialized owner.",
            Tags = new[] { "unity-cli-commands", "component" })]
        public static CommandResult<ComponentMemberSetResult> MemberSet(
            string target, string member, string value = null, string reference = null)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<ComponentMemberSetResult>.Failure(MemberSchema, compatibility.Error);
            var component = Editor.ExactObjectReference.Resolve<UnityEngine.Component>(target);
            if (component == null) return Failure<ComponentMemberSetResult>(MemberSchema, "COMPONENT_NOT_FOUND", target, null);
            var members = component.GetType().GetMembers(PublicInstance)
                .Where(item => (item is PropertyInfo property && property.GetSetMethod(false) != null &&
                    property.GetIndexParameters().Length == 0) ||
                    (item is FieldInfo field && !field.IsInitOnly && !field.IsLiteral))
                .Where(item => string.Equals(item.Name, member, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (members.Length != 1)
                return CommandResult<ComponentMemberSetResult>.Failure(MemberSchema,
                    members.Length == 0 ? "MEMBER_NOT_FOUND" : "MEMBER_AMBIGUOUS",
                    "The writable public member must resolve uniquely.", new Dictionary<string, object>
                    { ["member"] = member ?? string.Empty, ["matches"] = members.Select(item => item.Name).ToArray() });
            var selected = members[0];
            var targetType = selected is PropertyInfo selectedProperty ? selectedProperty.PropertyType : ((FieldInfo)selected).FieldType;
            if (HasEstablishedSerializedOwner(component, selected))
                return CommandResult<ComponentMemberSetResult>.Failure(MemberSchema, "USE_NATIVE_SERIALIZED_OWNER",
                    "This public field has an exact native SerializedProperty owner.",
                    new Dictionary<string, object> { ["member"] = selected.Name });
            if (!Editor.PublicValueConversion.TryConvert(value, targetType, reference, out var converted))
                return CommandResult<ComponentMemberSetResult>.Failure(MemberSchema, "VALUE_CONVERSION_FAILED",
                    "The value could not be converted to the selected public member type.",
                    new Dictionary<string, object> { ["member"] = selected.Name, ["valueType"] = targetType.FullName });
            Undo.RecordObject(component, "Set Component Member");
            try
            {
                if (selected is PropertyInfo property) property.SetValue(component, converted);
                else ((FieldInfo)selected).SetValue(component, converted);
                EditorUtility.SetDirty(component);
                return CommandResult<ComponentMemberSetResult>.Success(MemberSchema, new ComponentMemberSetResult
                {
                    GameObject = component.gameObject.name,
                    Component = component.GetType().FullName,
                    Property = selected.Name,
                    ValueSet = converted == null ? "null" : Convert.ToString(converted, CultureInfo.InvariantCulture),
                    ValueType = targetType.Name
                });
            }
            catch (Exception exception)
            {
                Undo.PerformUndo();
                return Failure<ComponentMemberSetResult>(MemberSchema, "MEMBER_SET_FAILED", target, null, exception);
            }
        }

        private static bool HasEstablishedSerializedOwner(UnityEngine.Component component, MemberInfo member)
        {
            if (!(member is FieldInfo)) return false;
            return new SerializedObject(component).FindProperty(member.Name) != null;
        }

        private static IEnumerable<string> RequiredDependents(UnityEngine.Component selected)
        {
            var components = selected.gameObject.GetComponents<UnityEngine.Component>().Where(item => item != null).ToArray();
            foreach (var candidate in components.Where(item => item != selected))
            foreach (var attribute in candidate.GetType().GetCustomAttributes(typeof(RequireComponent), true).Cast<RequireComponent>())
            foreach (var required in RequiredTypes(attribute))
            {
                if (!required.IsAssignableFrom(selected.GetType())) continue;
                if (components.Count(item => required.IsAssignableFrom(item.GetType())) <= 1)
                    yield return candidate.GetType().FullName;
            }
        }

        private static IEnumerable<Type> RequiredTypes(RequireComponent attribute)
        {
            foreach (var name in new[] { "m_Type0", "m_Type1", "m_Type2" })
            {
                var field = typeof(RequireComponent).GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var value = field?.GetValue(attribute) as Type;
                if (value != null) yield return value;
            }
        }

        private static CommandResult<T> Failure<T>(string schema, string code, string first, string second, Exception exception = null) =>
            CommandResult<T>.Failure(schema, code, "The component operation could not be completed.",
                new Dictionary<string, object>
                {
                    ["first"] = first ?? string.Empty, ["second"] = second ?? string.Empty,
                    ["exceptionType"] = exception?.GetType().Name ?? string.Empty
                });
    }

    [Serializable] public sealed class ComponentCopyResult { public string Source { get; set; } public string Destination { get; set; } public string Component { get; set; } public string Created { get; set; } }
    [Serializable] public sealed class ComponentRemoveResult { public string Component { get; set; } public string ComponentType { get; set; } public string GameObject { get; set; } public bool Removed { get; set; } }
    [Serializable] public sealed class ComponentMemberSetResult { public string GameObject { get; set; } public string Component { get; set; } public string Property { get; set; } public string ValueSet { get; set; } public string ValueType { get; set; } }
}

namespace BatihanDev.UnityCliCommands.Asset
{
    public static class AssetAuthoringCommands
    {
        private const string LabelsSchema = "unity.asset.labels@1";
        private const string ReimportSchema = "unity.asset.reimport@1";
        private const string TrashSchema = "unity.asset.trash@1";

        [CliCommand("asset.labels", "Preview or replace all labels on one exact project asset.", Tags = new[] { "unity-cli-commands", "asset" })]
        public static CommandResult<AssetLabelsResult> Labels(string asset, string[] labels,
            bool dryRun = false, bool confirm = false, bool allowEmbeddedPackages = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<AssetLabelsResult>.Failure(LabelsSchema, compatibility.Error);
            var path = ProjectPathPolicy.Validate(asset, allowEmbeddedPackages);
            if (!path.Ok) return CommandResult<AssetLabelsResult>.Failure(LabelsSchema, path.Error);
            var value = AssetDatabase.LoadMainAssetAtPath(path.Result.Path);
            if (value == null) return Failure<AssetLabelsResult>(LabelsSchema, "ASSET_NOT_FOUND", asset);
            var before = AssetDatabase.GetLabels(value);
            var proposed = (labels ?? Array.Empty<string>()).Where(label => !string.IsNullOrWhiteSpace(label))
                .Distinct(StringComparer.Ordinal).OrderBy(label => label, StringComparer.Ordinal).ToArray();
            if (!dryRun && !confirm) return Failure<AssetLabelsResult>(LabelsSchema, "CONFIRMATION_REQUIRED", asset);
            if (!dryRun) AssetDatabase.SetLabels(value, proposed);
            var after = AssetDatabase.GetLabels(value).OrderBy(label => label, StringComparer.Ordinal).ToArray();
            if (!dryRun && !after.SequenceEqual(proposed)) return Failure<AssetLabelsResult>(LabelsSchema, "READBACK_MISMATCH", asset);
            return CommandResult<AssetLabelsResult>.Success(LabelsSchema, new AssetLabelsResult
            { Path = path.Result.Path, Before = before, Proposed = proposed, After = after, Applied = !dryRun, DryRun = dryRun, Undoable = false });
        }

        [CliCommand("asset.reimport", "Preview or force reimport one exact project-relative asset path.", Tags = new[] { "unity-cli-commands", "asset", "importer" })]
        public static CommandResult<AssetReimportResult> Reimport(string asset,
            bool dryRun = false, bool confirm = false, bool allowEmbeddedPackages = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<AssetReimportResult>.Failure(ReimportSchema, compatibility.Error);
            var path = ProjectPathPolicy.Validate(asset, allowEmbeddedPackages);
            if (!path.Ok) return CommandResult<AssetReimportResult>.Failure(ReimportSchema, path.Error);
            if (!path.Result.Exists) return Failure<AssetReimportResult>(ReimportSchema, "ASSET_NOT_FOUND", asset);
            if (!dryRun && !confirm) return Failure<AssetReimportResult>(ReimportSchema, "CONFIRMATION_REQUIRED", asset);
            var beforeGuid = AssetDatabase.AssetPathToGUID(path.Result.Path);
            if (!dryRun) AssetDatabase.ImportAsset(path.Result.Path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            var afterGuid = AssetDatabase.AssetPathToGUID(path.Result.Path);
            if (!dryRun && string.IsNullOrEmpty(afterGuid)) return Failure<AssetReimportResult>(ReimportSchema, "IMPORT_READBACK_FAILED", asset);
            return CommandResult<AssetReimportResult>.Success(ReimportSchema, new AssetReimportResult
            { Path = path.Result.Path, BeforeGuid = beforeGuid, AfterGuid = afterGuid, Applied = !dryRun, DryRun = dryRun, Undoable = false });
        }

        [CliCommand("asset.trash", "Preview or move one exact project asset and its meta file to recoverable OS trash.", Tags = new[] { "unity-cli-commands", "asset" })]
        public static CommandResult<AssetTrashResult> Trash(string asset,
            bool dryRun = false, bool confirm = false, bool allowEmbeddedPackages = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<AssetTrashResult>.Failure(TrashSchema, compatibility.Error);
            var path = ProjectPathPolicy.Validate(asset, allowEmbeddedPackages);
            if (!path.Ok) return CommandResult<AssetTrashResult>.Failure(TrashSchema, path.Error);
            if (!path.Result.Exists || AssetDatabase.LoadMainAssetAtPath(path.Result.Path) == null)
                return Failure<AssetTrashResult>(TrashSchema, "ASSET_NOT_FOUND", asset);
            if (!dryRun && !confirm) return Failure<AssetTrashResult>(TrashSchema, "CONFIRMATION_REQUIRED", asset);
            var guid = AssetDatabase.AssetPathToGUID(path.Result.Path);
            if (!dryRun && !AssetDatabase.MoveAssetToTrash(path.Result.Path))
                return Failure<AssetTrashResult>(TrashSchema, "TRASH_FAILED", asset);
            if (!dryRun && AssetDatabase.LoadMainAssetAtPath(path.Result.Path) != null)
                return Failure<AssetTrashResult>(TrashSchema, "READBACK_MISMATCH", asset);
            return CommandResult<AssetTrashResult>.Success(TrashSchema, new AssetTrashResult
            { Path = path.Result.Path, Guid = guid, Applied = !dryRun, DryRun = dryRun, Recoverable = true, Undoable = false });
        }

        private static CommandResult<T> Failure<T>(string schema, string code, string path) =>
            CommandResult<T>.Failure(schema, code, "The asset operation could not be completed.",
                new Dictionary<string, object> { ["path"] = path ?? string.Empty });
    }

    [Serializable] public sealed class AssetLabelsResult { public string Path { get; set; } public string[] Before { get; set; } public string[] Proposed { get; set; } public string[] After { get; set; } public bool Applied { get; set; } public bool DryRun { get; set; } public bool Undoable { get; set; } }
    [Serializable] public sealed class AssetReimportResult { public string Path { get; set; } public string BeforeGuid { get; set; } public string AfterGuid { get; set; } public bool Applied { get; set; } public bool DryRun { get; set; } public bool Undoable { get; set; } }
    [Serializable] public sealed class AssetTrashResult { public string Path { get; set; } public string Guid { get; set; } public bool Applied { get; set; } public bool DryRun { get; set; } public bool Recoverable { get; set; } public bool Undoable { get; set; } }
}

namespace BatihanDev.UnityCliCommands.Audio
{
    public static class AudioAuthoringCommands
    {
        private const string Schema = "unity.audio.mixer-create@1";

        [CliCommand("audio.mixer-create", "Create a proper AudioMixer with the exact Unity Editor factory.", Tags = new[] { "unity-cli-commands", "audio", "asset" })]
        public static CommandResult<AudioMixerCreateResult> CreateMixer(
            string mixerName = "NewAudioMixer", string folder = "Assets", bool dryRun = false,
            bool confirm = false, bool allowEmbeddedPackages = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<AudioMixerCreateResult>.Failure(Schema, compatibility.Error);
            if (string.IsNullOrWhiteSpace(mixerName) || mixerName.IndexOfAny(new[] { '/', '\\' }) >= 0 || mixerName.Contains(".."))
                return Failure("MIXER_NAME_INVALID", mixerName, null);
            var path = (folder ?? "Assets").TrimEnd('/', '\\') + "/" + mixerName + ".mixer";
            var validated = ProjectPathPolicy.Validate(path, allowEmbeddedPackages);
            if (!validated.Ok) return CommandResult<AudioMixerCreateResult>.Failure(Schema, validated.Error);
            if (validated.Result.Exists) return Failure("TARGET_EXISTS", mixerName, validated.Result.Path);
            if (!dryRun && !confirm) return Failure("CONFIRMATION_REQUIRED", mixerName, validated.Result.Path);
            if (dryRun) return CommandResult<AudioMixerCreateResult>.Success(Schema, new AudioMixerCreateResult
            { Success = true, Name = mixerName, Path = validated.Result.Path, Applied = false, DryRun = true, Undoable = false });
            try
            {
                EnsureAssetFolder(Path.GetDirectoryName(validated.Result.Path)?.Replace('\\', '/'));
                var type = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(assembly => assembly.GetType("UnityEditor.Audio.AudioMixerController", false)).FirstOrDefault(item => item != null);
                var method = type?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .SingleOrDefault(candidate => candidate.Name == "CreateMixerControllerAtPath" &&
                        candidate.GetParameters().Length == 1 && candidate.GetParameters()[0].ParameterType == typeof(string));
                if (method == null) return Failure("AUDIO_MIXER_FACTORY_UNAVAILABLE", mixerName, validated.Result.Path);
                var created = method.Invoke(null, new object[] { validated.Result.Path });
                if (created == null) return CleanupFailure("AUDIO_MIXER_FACTORY_FAILED", mixerName, validated.Result.Path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(validated.Result.Path);
                var masters = mixer?.FindMatchingGroups("Master").Where(group => group.name == "Master").ToArray();
                if (mixer == null || masters == null || masters.Length != 1)
                    return CleanupFailure("AUDIO_MIXER_INVALID", mixerName, validated.Result.Path);
                return CommandResult<AudioMixerCreateResult>.Success(Schema, new AudioMixerCreateResult
                {
                    Success = true, Name = mixerName, Path = validated.Result.Path,
                    Guid = AssetDatabase.AssetPathToGUID(validated.Result.Path), MasterGroupCount = masters.Length,
                    Factory = "UnityEditor.Audio.AudioMixerController.CreateMixerControllerAtPath(System.String)",
                    Applied = true, DryRun = false, Undoable = false
                });
            }
            catch (Exception exception)
            {
                return CleanupFailure("AUDIO_MIXER_FACTORY_FAILED", mixerName, validated.Result.Path, exception);
            }
        }

        private static void EnsureAssetFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder)) return;
            var segments = folder.Split('/');
            var current = segments[0];
            for (var index = 1; index < segments.Length; index++)
            {
                var next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, segments[index]);
                current = next;
            }
        }

        private static CommandResult<AudioMixerCreateResult> CleanupFailure(string code, string name, string path, Exception exception = null)
        {
            if (!string.IsNullOrEmpty(path) && AssetDatabase.LoadMainAssetAtPath(path) != null)
                AssetDatabase.MoveAssetToTrash(path);
            return Failure(code, name, path, exception);
        }

        private static CommandResult<AudioMixerCreateResult> Failure(string code, string name, string path, Exception exception = null) =>
            CommandResult<AudioMixerCreateResult>.Failure(Schema, code, "The AudioMixer was not created.",
                new Dictionary<string, object> { ["name"] = name ?? string.Empty, ["path"] = path ?? string.Empty,
                    ["exceptionType"] = exception?.GetType().Name ?? string.Empty });
    }

    [Serializable] public sealed class AudioMixerCreateResult
    {
        public bool Success { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }
        public string Guid { get; set; }
        public int MasterGroupCount { get; set; }
        public string Factory { get; set; }
        public bool Applied { get; set; }
        public bool DryRun { get; set; }
        public bool Undoable { get; set; }
    }
}

namespace BatihanDev.UnityCliCommands.ScriptableObject
{
    public static class ScriptableObjectAuthoringCommands
    {
        private const string MemberSchema = "unity.scriptableobject.member-set@1";
        private const string FieldsSchema = "unity.scriptableobject.fields-set@1";
        private const string JsonSchema = "unity.scriptableobject.json-import@1";
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

        [CliCommand("scriptableobject.member-set", "Set one writable public ScriptableObject field or property.", Tags = new[] { "unity-cli-commands", "scriptableobject", "asset" })]
        public static CommandResult<ScriptableObjectMemberResult> MemberSet(
            string asset, string member, string value = null, string reference = null,
            bool dryRun = false, bool confirm = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<ScriptableObjectMemberResult>.Failure(MemberSchema, compatibility.Error);
            var confined = ProjectPathPolicy.Validate(asset);
            if (!confined.Ok) return CommandResult<ScriptableObjectMemberResult>.Failure(MemberSchema, confined.Error);
            asset = confined.Result.Path;
            var resolved = AssetDatabase.LoadMainAssetAtPath(asset) as UnityEngine.ScriptableObject;
            if (resolved == null) return Failure<ScriptableObjectMemberResult>(MemberSchema, "SCRIPTABLE_OBJECT_NOT_FOUND", asset);
            var members = resolved.GetType().GetMembers(PublicInstance)
                .Where(item => (item is PropertyInfo property && property.GetSetMethod(false) != null && property.GetIndexParameters().Length == 0) ||
                    (item is FieldInfo field && !field.IsInitOnly && !field.IsLiteral))
                .Where(item => string.Equals(item.Name, member, StringComparison.Ordinal)).ToArray();
            if (members.Length != 1) return Failure<ScriptableObjectMemberResult>(MemberSchema, "MEMBER_NOT_FOUND", member);
            var selected = members[0];
            var targetType = selected is PropertyInfo propertyInfo ? propertyInfo.PropertyType : ((FieldInfo)selected).FieldType;
            if (!Editor.PublicValueConversion.TryConvert(value, targetType, reference, out var converted))
                return Failure<ScriptableObjectMemberResult>(MemberSchema, "VALUE_CONVERSION_FAILED", member);
            if (!dryRun && !confirm) return Failure<ScriptableObjectMemberResult>(MemberSchema, "CONFIRMATION_REQUIRED", member);
            if (!dryRun)
            {
                Undo.RecordObject(resolved, "Set ScriptableObject Member");
                if (selected is PropertyInfo property) property.SetValue(resolved, converted);
                else ((FieldInfo)selected).SetValue(resolved, converted);
                EditorUtility.SetDirty(resolved);
                AssetDatabase.SaveAssets();
            }
            return CommandResult<ScriptableObjectMemberResult>.Success(MemberSchema, new ScriptableObjectMemberResult
            {
                Asset = asset, Member = selected.Name, ValueSet = converted == null ? "null" : Convert.ToString(converted, CultureInfo.InvariantCulture),
                ValueType = targetType.Name, Applied = !dryRun, DryRun = dryRun
            });
        }

        [CliCommand("scriptableobject.fields-set", "Set ordered mutable public ScriptableObject fields.", Tags = new[] { "unity-cli-commands", "scriptableobject", "asset" })]
        public static CommandResult<ScriptableObjectFieldsSetResult> FieldsSet(
            string asset, ScriptableObjectMemberUpdate[] updates, bool dryRun = false, bool confirm = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<ScriptableObjectFieldsSetResult>.Failure(FieldsSchema, compatibility.Error);
            var confined = ProjectPathPolicy.Validate(asset);
            if (!confined.Ok) return CommandResult<ScriptableObjectFieldsSetResult>.Failure(FieldsSchema, confined.Error);
            asset = confined.Result.Path;
            var resolved = AssetDatabase.LoadMainAssetAtPath(asset) as UnityEngine.ScriptableObject;
            if (resolved == null) return Failure<ScriptableObjectFieldsSetResult>(FieldsSchema, "SCRIPTABLE_OBJECT_NOT_FOUND", asset);
            if (updates == null || updates.Length == 0) return Failure<ScriptableObjectFieldsSetResult>(FieldsSchema, "UPDATES_REQUIRED", asset);
            if (!dryRun && !confirm) return Failure<ScriptableObjectFieldsSetResult>(FieldsSchema, "CONFIRMATION_REQUIRED", asset);

            var results = new List<ScriptableObjectMemberUpdateResult>();
            var undoRecorded = false;
            var serializedChanged = false;
            foreach (var update in updates)
            {
                if (update == null)
                {
                    results.Add(new ScriptableObjectMemberUpdateResult { ErrorCode = "UPDATE_REQUIRED" });
                    continue;
                }
                var field = resolved.GetType().GetField(update.Name ?? string.Empty, PublicInstance);
                if (field == null || field.IsInitOnly || field.IsLiteral)
                {
                    results.Add(new ScriptableObjectMemberUpdateResult { Name = update.Name, ErrorCode = "MEMBER_NOT_FOUND" });
                    continue;
                }
                if (!Editor.PublicValueConversion.TryConvert(update.Value, field.FieldType, update.Reference, out var converted))
                {
                    results.Add(new ScriptableObjectMemberUpdateResult { Name = update.Name, ErrorCode = "VALUE_CONVERSION_FAILED" });
                    continue;
                }
                var changed = !Equals(field.GetValue(resolved), converted);
                if (!dryRun && changed)
                {
                    if (!undoRecorded)
                    {
                        Undo.RecordObject(resolved, "Set ScriptableObject Fields");
                        undoRecorded = true;
                    }
                    field.SetValue(resolved, converted);
                    serializedChanged |= SerializedField(resolved.GetType(), field.Name) != null;
                }
                results.Add(new ScriptableObjectMemberUpdateResult { Name = update.Name, Success = true, Applied = !dryRun && changed });
            }
            if (!dryRun && serializedChanged)
            {
                EditorUtility.SetDirty(resolved);
                AssetDatabase.SaveAssetIfDirty(resolved);
            }
            var succeeded = results.Count(item => item.Success);
            var applied = results.Count(item => item.Applied);
            return CommandResult<ScriptableObjectFieldsSetResult>.Success(FieldsSchema, new ScriptableObjectFieldsSetResult
            {
                Asset = asset,
                Total = results.Count,
                Succeeded = succeeded,
                Applied = applied,
                Failed = results.Count - succeeded,
                DryRun = dryRun,
                Results = results.ToArray()
            });
        }

        [CliCommand("scriptableobject.json-import", "Preview or partially overwrite one ScriptableObject from exactly one JSON source.", Tags = new[] { "unity-cli-commands", "scriptableobject", "asset" })]
        public static CommandResult<ScriptableObjectJsonImportResult> ImportJson(
            string asset, string json = null, string jsonFilePath = null,
            bool dryRun = false, bool confirm = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<ScriptableObjectJsonImportResult>.Failure(JsonSchema, compatibility.Error);
            if (json != null && jsonFilePath != null) return Failure<ScriptableObjectJsonImportResult>(JsonSchema, "JSON_SOURCE_AMBIGUOUS", asset);
            if (json == null && jsonFilePath == null) return Failure<ScriptableObjectJsonImportResult>(JsonSchema, "JSON_SOURCE_REQUIRED", asset);
            var confined = ProjectPathPolicy.Validate(asset);
            if (!confined.Ok) return CommandResult<ScriptableObjectJsonImportResult>.Failure(JsonSchema, confined.Error);
            asset = confined.Result.Path;
            var resolved = AssetDatabase.LoadMainAssetAtPath(asset) as UnityEngine.ScriptableObject;
            if (resolved == null) return Failure<ScriptableObjectJsonImportResult>(JsonSchema, "SCRIPTABLE_OBJECT_NOT_FOUND", asset);
            if (jsonFilePath != null)
            {
                var file = ProjectPathPolicy.Validate(jsonFilePath);
                if (!file.Ok) return CommandResult<ScriptableObjectJsonImportResult>.Failure(JsonSchema, file.Error);
                var root = Directory.GetParent(Application.dataPath)?.FullName;
                json = File.ReadAllText(Path.Combine(root ?? string.Empty, file.Result.Path));
            }
            if (!TryGetEditorJsonPayload(json, out var payload))
                return Failure<ScriptableObjectJsonImportResult>(JsonSchema, "JSON_ROOT_INVALID", asset);
            if (!ValidateEditorJsonPayload(resolved.GetType(), payload, out var invalidPath))
                return CommandResult<ScriptableObjectJsonImportResult>.Failure(JsonSchema, "JSON_INVALID",
                    "The JSON source contains a value that does not match the serialized field type.",
                    new Dictionary<string, object>
                    {
                        ["asset"] = asset,
                        ["propertyPath"] = invalidPath
                    });
            var before = Snapshot(resolved);
            UnityEngine.ScriptableObject probe = null;
            var dirtyBeforeSave = false;
            try
            {
                probe = UnityObject.Instantiate(resolved);
                probe.name = resolved.name;
                var jsonToApply = json;
                var completedPayload = (JObject)payload.DeepClone();
                if (TryGetEditorJsonPayload(EditorJsonUtility.ToJson(probe), out var baselinePayload) &&
                    CompleteEngineOwnedJsonValues(resolved.GetType(), completedPayload, baselinePayload))
                    jsonToApply = new JObject(new JProperty("MonoBehaviour", completedPayload)).ToString(Formatting.None);
                EditorJsonUtility.FromJsonOverwrite(jsonToApply, probe);
                var proposed = Snapshot(probe);
                var changed = proposed.Where(item => !before.TryGetValue(item.Key, out var prior) || prior != item.Value)
                    .Select(item => item.Key).OrderBy(item => item, StringComparer.Ordinal).ToArray();
                if (!dryRun && !confirm) return Failure<ScriptableObjectJsonImportResult>(JsonSchema, "CONFIRMATION_REQUIRED", asset);
                if (!dryRun)
                {
                    var resolvedPath = AssetDatabase.GetAssetPath(resolved);
                    if (!EditorUtility.IsPersistent(resolved) || !string.Equals(resolvedPath, asset, StringComparison.Ordinal))
                        return Failure<ScriptableObjectJsonImportResult>(JsonSchema, "ASSET_IDENTITY_MISMATCH", resolvedPath);
                    if (changed.Length != 0) Undo.RecordObject(resolved, "Import JSON to ScriptableObject");
                    foreach (var rootMember in changed.Select(RootMember).Distinct(StringComparer.Ordinal))
                    {
                        var field = SerializedField(resolved.GetType(), rootMember);
                        if (field != null) field.SetValue(resolved, field.GetValue(probe));
                    }
                    EditorUtility.SetDirty(resolved);
                    dirtyBeforeSave = EditorUtility.IsDirty(resolved);
                    AssetDatabase.SaveAssets();
                    var dirtyAfterSave = EditorUtility.IsDirty(resolved);
                    var reloaded = AssetDatabase.LoadMainAssetAtPath(asset) as UnityEngine.ScriptableObject;
                    if (reloaded == null)
                        return Failure<ScriptableObjectJsonImportResult>(JsonSchema, "READBACK_MISMATCH", asset);
                    var after = Snapshot(reloaded);
                    var mismatch = proposed.Keys.Union(before.Keys).OrderBy(path => path, StringComparer.Ordinal)
                        .FirstOrDefault(path =>
                        {
                            var expectedPresent = proposed.TryGetValue(path, out var expectedValue);
                            var actualPresent = after.TryGetValue(path, out var actualValue);
                            return expectedPresent != actualPresent ||
                                expectedPresent && actualValue != expectedValue;
                        });
                    if (mismatch != null)
                        return CommandResult<ScriptableObjectJsonImportResult>.Failure(JsonSchema, "READBACK_MISMATCH",
                            "The saved ScriptableObject did not match the complete proposed serialized state.",
                            new Dictionary<string, object>
                            {
                                ["asset"] = asset,
                                ["propertyPath"] = mismatch,
                                ["expected"] = proposed.TryGetValue(mismatch, out var expected) ? expected : "<absent>",
                                ["actual"] = after.TryGetValue(mismatch, out var actual) ? actual : "<absent>"
                            });
                }
                return CommandResult<ScriptableObjectJsonImportResult>.Success(JsonSchema, new ScriptableObjectJsonImportResult
                { Asset = asset, ChangedMembers = changed, Applied = !dryRun, DryRun = dryRun, PartialOverwrite = true,
                    Persistent = EditorUtility.IsPersistent(resolved), DirtyBeforeSave = dirtyBeforeSave,
                    DirtyAfterSave = EditorUtility.IsDirty(resolved), InstanceId = Editor.ExactObjectReference.ExactId(resolved) });
            }
            catch (Exception exception)
            {
                return CommandResult<ScriptableObjectJsonImportResult>.Failure(JsonSchema, "JSON_INVALID",
                    "The JSON source could not be applied.", new Dictionary<string, object>
                    { ["asset"] = asset ?? string.Empty, ["exceptionType"] = exception.GetType().Name });
            }
            finally
            {
                if (probe != null) UnityObject.DestroyImmediate(probe);
            }
        }

        private static Dictionary<string, string> Snapshot(UnityObject value)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            var serialized = new SerializedObject(value);
            serialized.UpdateIfRequiredOrScript();
            var property = serialized.GetIterator();
            var enterChildren = true;
            while (property.Next(enterChildren))
            {
                enterChildren = true;
                if (property.propertyPath == "m_Script" || property.propertyPath == "m_Name" ||
                    property.propertyPath == "m_ObjectHideFlags" ||
                    property.propertyPath == "m_CorrespondingSourceObject" ||
                    property.propertyPath == "m_PrefabInstance" || property.propertyPath == "m_PrefabAsset")
                {
                    enterChildren = false;
                    continue;
                }
                result[property.propertyPath] = PropertyValue(property);
                if (property.propertyType == SerializedPropertyType.String ||
                    property.propertyType == SerializedPropertyType.ObjectReference) enterChildren = false;
            }
            return result;
        }

        private static bool TryGetEditorJsonPayload(string json, out JObject payload)
        {
            payload = null;
            try
            {
                var root = JObject.Parse(json, new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
                });
                if (root.Count != 1 || !root.TryGetValue("MonoBehaviour", StringComparison.Ordinal, out var token) ||
                    token.Type != JTokenType.Object) return false;
                payload = (JObject)token;
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool ValidateEditorJsonPayload(Type targetType, JObject payload, out string invalidPath)
        {
            foreach (var property in payload.Properties())
            {
                var field = SerializedField(targetType, property.Name);
                if (field != null)
                {
                    if (!ValidateEditorJsonValue(field.FieldType, property.Value, property.Name, out invalidPath))
                        return false;
                    continue;
                }
                if (!ValidateEditorMetadata(property, out invalidPath)) return false;
            }
            invalidPath = null;
            return true;
        }

        private static bool ValidateEditorMetadata(JProperty property, out string invalidPath)
        {
            invalidPath = property.Name;
            switch (property.Name)
            {
                case "m_Enabled":
                    return property.Value.Type == JTokenType.Boolean;
                case "m_ObjectHideFlags":
                case "m_EditorHideFlags":
                    return ValidateInteger(property.Value, typeof(int));
                case "m_Name":
                case "m_EditorClassIdentifier":
                    return property.Value.Type == JTokenType.String;
                case "m_Script":
                case "m_CorrespondingSourceObject":
                case "m_PrefabInstance":
                case "m_PrefabAsset":
                    return ValidateObjectReference(typeof(UnityObject), property.Value);
                default:
                    return false;
            }
        }

        private static bool ValidateEditorJsonValue(Type targetType, JToken token, string path, out string invalidPath)
        {
            invalidPath = path;
            var nullable = Nullable.GetUnderlyingType(targetType);
            if (nullable != null)
            {
                if (token.Type == JTokenType.Null) return true;
                targetType = nullable;
            }
            if (typeof(UnityObject).IsAssignableFrom(targetType))
                return ValidateObjectReference(targetType, token);
            if (token.Type == JTokenType.Null)
                return targetType == typeof(string) || !targetType.IsValueType;
            if (targetType == typeof(string)) return token.Type == JTokenType.String;
            if (targetType == typeof(bool)) return token.Type == JTokenType.Boolean;
            if (targetType.IsEnum)
                return ValidateInteger(token, Enum.GetUnderlyingType(targetType));
            if (IsIntegerType(targetType)) return ValidateInteger(token, targetType);
            if (targetType == typeof(float) || targetType == typeof(double))
                return ValidateFloatingPoint(token, targetType);
            if (targetType == typeof(Rect)) return ValidateRectJson(token, path, out invalidPath);
            if (targetType == typeof(Bounds)) return ValidateBoundsJson(token, path, out invalidPath);
            if (targetType == typeof(AnimationCurve)) return ValidateAnimationCurveJson(token, path, out invalidPath);
            if (targetType == typeof(Gradient)) return ValidateGradientJson(token, path, out invalidPath);
            if (targetType == typeof(LayerMask)) return ValidateLayerMaskJson(token, path, out invalidPath);
            if (targetType.IsArray)
                return ValidateEditorJsonArray(targetType.GetElementType(), token, path, out invalidPath);
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
                return ValidateEditorJsonArray(targetType.GetGenericArguments()[0], token, path, out invalidPath);
            if (token.Type != JTokenType.Object) return false;
            foreach (var property in ((JObject)token).Properties())
            {
                var field = SerializedField(targetType, property.Name);
                var childPath = path + "." + property.Name;
                if (field == null || !ValidateEditorJsonValue(field.FieldType, property.Value, childPath, out invalidPath))
                {
                    invalidPath = childPath;
                    return false;
                }
            }
            invalidPath = null;
            return true;
        }

        private static bool ValidateRectJson(JToken token, string path, out string invalidPath)
        {
            invalidPath = path;
            if (token.Type != JTokenType.Object) return false;
            foreach (var property in ((JObject)token).Properties())
            {
                var childPath = path + "." + property.Name;
                var valid = property.Name == "serializedVersion"
                    ? property.Value.Type == JTokenType.String
                    : property.Name == "x" || property.Name == "y" || property.Name == "width" || property.Name == "height"
                        ? ValidateFloatingPoint(property.Value, typeof(float))
                        : false;
                if (valid) continue;
                invalidPath = childPath;
                return false;
            }
            invalidPath = null;
            return true;
        }

        private static bool ValidateBoundsJson(JToken token, string path, out string invalidPath)
        {
            invalidPath = path;
            if (token.Type != JTokenType.Object) return false;
            foreach (var property in ((JObject)token).Properties())
            {
                var childPath = path + "." + property.Name;
                if ((property.Name == "m_Center" || property.Name == "m_Extent") &&
                    ValidateVector3Json(property.Value, childPath, out invalidPath)) continue;
                invalidPath = invalidPath ?? childPath;
                return false;
            }
            invalidPath = null;
            return true;
        }

        private static bool ValidateAnimationCurveJson(JToken token, string path, out string invalidPath)
        {
            invalidPath = path;
            if (token.Type != JTokenType.Object) return false;
            foreach (var property in ((JObject)token).Properties())
            {
                var childPath = path + "." + property.Name;
                if (property.Name == "serializedVersion" && property.Value.Type == JTokenType.String) continue;
                if (property.Name == "m_Curve" && ValidateKeyframeArrayJson(property.Value, childPath, out invalidPath)) continue;
                if ((property.Name == "m_PreInfinity" || property.Name == "m_PostInfinity" ||
                    property.Name == "m_RotationOrder") && ValidateInteger(property.Value, typeof(int))) continue;
                invalidPath = invalidPath ?? childPath;
                return false;
            }
            invalidPath = null;
            return true;
        }

        private static bool ValidateKeyframeArrayJson(JToken token, string path, out string invalidPath)
        {
            invalidPath = path;
            if (token.Type != JTokenType.Array) return false;
            var index = 0;
            foreach (var item in (JArray)token)
            {
                var itemPath = path + "[" + index.ToString(CultureInfo.InvariantCulture) + "]";
                if (!ValidateKeyframeJson(item, itemPath, out invalidPath)) return false;
                index++;
            }
            invalidPath = null;
            return true;
        }

        private static bool ValidateKeyframeJson(JToken token, string path, out string invalidPath)
        {
            invalidPath = path;
            if (token.Type != JTokenType.Object) return false;
            foreach (var property in ((JObject)token).Properties())
            {
                var childPath = path + "." + property.Name;
                var valid = property.Name == "serializedVersion"
                    ? property.Value.Type == JTokenType.String
                    : property.Name == "tangentMode" || property.Name == "weightedMode"
                        ? ValidateInteger(property.Value, typeof(int))
                        : property.Name == "time" || property.Name == "value" || property.Name == "inSlope" ||
                            property.Name == "outSlope" || property.Name == "inWeight" || property.Name == "outWeight"
                            ? ValidateFloatingPoint(property.Value, typeof(float))
                            : false;
                if (valid) continue;
                invalidPath = childPath;
                return false;
            }
            invalidPath = null;
            return true;
        }

        private static bool ValidateGradientJson(JToken token, string path, out string invalidPath)
        {
            invalidPath = path;
            if (token.Type != JTokenType.Object) return false;
            foreach (var property in ((JObject)token).Properties())
            {
                var childPath = path + "." + property.Name;
                if (property.Name == "serializedVersion" && property.Value.Type == JTokenType.String) continue;
                if (IsIndexedProperty(property.Name, "key") &&
                    ValidateColorJson(property.Value, childPath, out invalidPath)) continue;
                if ((IsIndexedProperty(property.Name, "ctime") || IsIndexedProperty(property.Name, "atime")) &&
                    ValidateInteger(property.Value, typeof(ushort))) continue;
                if ((property.Name == "m_Mode" || property.Name == "m_ColorSpace") &&
                    ValidateInteger(property.Value, typeof(int))) continue;
                if ((property.Name == "m_NumColorKeys" || property.Name == "m_NumAlphaKeys") &&
                    ValidateInteger(property.Value, typeof(int)))
                {
                    var count = property.Value.Value<int>();
                    if (count >= 0 && count <= 8) continue;
                }
                invalidPath = invalidPath ?? childPath;
                return false;
            }
            invalidPath = null;
            return true;
        }

        private static bool ValidateLayerMaskJson(JToken token, string path, out string invalidPath)
        {
            invalidPath = path;
            if (token.Type != JTokenType.Object) return false;
            foreach (var property in ((JObject)token).Properties())
            {
                var childPath = path + "." + property.Name;
                if (property.Name == "serializedVersion" && property.Value.Type == JTokenType.String) continue;
                if (property.Name == "m_Bits" && ValidateInteger(property.Value, typeof(uint))) continue;
                invalidPath = childPath;
                return false;
            }
            invalidPath = null;
            return true;
        }

        private static bool ValidateVector3Json(JToken token, string path, out string invalidPath)
        {
            invalidPath = path;
            if (token.Type != JTokenType.Object) return false;
            foreach (var property in ((JObject)token).Properties())
            {
                var childPath = path + "." + property.Name;
                if ((property.Name == "x" || property.Name == "y" || property.Name == "z") &&
                    ValidateFloatingPoint(property.Value, typeof(float))) continue;
                invalidPath = childPath;
                return false;
            }
            invalidPath = null;
            return true;
        }

        private static bool ValidateColorJson(JToken token, string path, out string invalidPath)
        {
            invalidPath = path;
            if (token.Type != JTokenType.Object) return false;
            foreach (var property in ((JObject)token).Properties())
            {
                var childPath = path + "." + property.Name;
                if ((property.Name == "r" || property.Name == "g" || property.Name == "b" || property.Name == "a") &&
                    ValidateFloatingPoint(property.Value, typeof(float))) continue;
                invalidPath = childPath;
                return false;
            }
            invalidPath = null;
            return true;
        }

        private static bool IsIndexedProperty(string value, string prefix) =>
            value.Length == prefix.Length + 1 && value.StartsWith(prefix, StringComparison.Ordinal) &&
            value[value.Length - 1] >= '0' && value[value.Length - 1] <= '7';

        private static bool CompleteEngineOwnedJsonValues(Type targetType, JObject supplied, JObject baseline)
        {
            var completed = false;
            foreach (var property in supplied.Properties().ToArray())
            {
                var field = SerializedField(targetType, property.Name);
                if (field == null || property.Value.Type != JTokenType.Object ||
                    !baseline.TryGetValue(property.Name, StringComparison.Ordinal, out var baselineToken) ||
                    baselineToken.Type != JTokenType.Object) continue;
                var suppliedObject = (JObject)property.Value;
                var baselineObject = (JObject)baselineToken;
                if (CompleteEngineOwnedJsonValue(field.FieldType, suppliedObject, baselineObject))
                {
                    completed = true;
                    continue;
                }
                if (CanContainEngineOwnedJson(field.FieldType) &&
                    CompleteEngineOwnedJsonValues(field.FieldType, suppliedObject, baselineObject)) completed = true;
            }
            return completed;
        }

        private static bool CompleteEngineOwnedJsonValue(Type targetType, JObject supplied, JObject baseline)
        {
            if (targetType == typeof(Rect))
            {
                CopyMissingJsonProperties(supplied, baseline, "serializedVersion", "x", "y", "width", "height");
                return true;
            }
            if (targetType == typeof(Bounds))
            {
                CompleteNestedJsonObject(supplied, baseline, "m_Center", "x", "y", "z");
                CompleteNestedJsonObject(supplied, baseline, "m_Extent", "x", "y", "z");
                CopyMissingJsonProperties(supplied, baseline, "m_Center", "m_Extent");
                return true;
            }
            if (targetType == typeof(AnimationCurve))
            {
                CopyMissingJsonProperties(supplied, baseline, "serializedVersion", "m_Curve", "m_PreInfinity",
                    "m_PostInfinity", "m_RotationOrder");
                return true;
            }
            if (targetType == typeof(Gradient))
            {
                for (var index = 0; index < 8; index++)
                    CompleteNestedJsonObject(supplied, baseline, "key" + index.ToString(CultureInfo.InvariantCulture),
                        "r", "g", "b", "a");
                CopyMissingJsonProperties(supplied, baseline, "serializedVersion",
                    "key0", "key1", "key2", "key3", "key4", "key5", "key6", "key7",
                    "ctime0", "ctime1", "ctime2", "ctime3", "ctime4", "ctime5", "ctime6", "ctime7",
                    "atime0", "atime1", "atime2", "atime3", "atime4", "atime5", "atime6", "atime7",
                    "m_Mode", "m_ColorSpace", "m_NumColorKeys", "m_NumAlphaKeys");
                return true;
            }
            if (targetType == typeof(LayerMask))
            {
                CopyMissingJsonProperties(supplied, baseline, "serializedVersion", "m_Bits");
                return true;
            }
            return false;
        }

        private static void CompleteNestedJsonObject(
            JObject supplied, JObject baseline, string propertyName, params string[] memberNames)
        {
            if (!supplied.TryGetValue(propertyName, StringComparison.Ordinal, out var suppliedToken) ||
                suppliedToken.Type != JTokenType.Object ||
                !baseline.TryGetValue(propertyName, StringComparison.Ordinal, out var baselineToken) ||
                baselineToken.Type != JTokenType.Object) return;
            CopyMissingJsonProperties((JObject)suppliedToken, (JObject)baselineToken, memberNames);
        }

        private static void CopyMissingJsonProperties(JObject supplied, JObject baseline, params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                if (supplied.TryGetValue(propertyName, StringComparison.Ordinal, out _) ||
                    !baseline.TryGetValue(propertyName, StringComparison.Ordinal, out var baselineValue)) continue;
                supplied.Add(propertyName, baselineValue.DeepClone());
            }
        }

        private static bool CanContainEngineOwnedJson(Type type) =>
            !type.IsPrimitive && !type.IsEnum && type != typeof(string) && !typeof(UnityObject).IsAssignableFrom(type) &&
            !type.IsArray && !(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>));

        private static bool ValidateEditorJsonArray(Type elementType, JToken token, string path, out string invalidPath)
        {
            invalidPath = path;
            if (token.Type == JTokenType.Null) return true;
            if (token.Type != JTokenType.Array) return false;
            var index = 0;
            foreach (var item in (JArray)token)
            {
                if (!ValidateEditorJsonValue(elementType, item, path + "[" + index.ToString(CultureInfo.InvariantCulture) + "]", out invalidPath))
                    return false;
                index++;
            }
            invalidPath = null;
            return true;
        }

        private static bool ValidateObjectReference(Type targetType, JToken token)
        {
            if (token.Type == JTokenType.Null) return true;
            if (token.Type != JTokenType.Object) return false;
            var reference = (JObject)token;
            if (reference.TryGetValue("instanceID", StringComparison.Ordinal, out var instanceToken))
            {
                if (reference.Count != 1 || !ValidateInteger(instanceToken, typeof(int))) return false;
                var instanceId = instanceToken.Value<int>();
                var instance = instanceId == 0 ? null :
                    EditorUtility.EntityIdToObject(EntityId.FromULong(unchecked((ulong)(long)instanceId)));
                return instanceId == 0 || instance != null && targetType.IsInstanceOfType(instance);
            }
            if (!reference.TryGetValue("fileID", StringComparison.Ordinal, out var fileToken) ||
                !ValidateInteger(fileToken, typeof(long))) return false;
            if (reference.Properties().Any(item => item.Name != "fileID" && item.Name != "guid" && item.Name != "type"))
                return false;
            if (reference.TryGetValue("type", StringComparison.Ordinal, out var typeToken) &&
                !ValidateInteger(typeToken, typeof(int))) return false;
            var fileId = fileToken.Value<long>();
            var guid = reference.TryGetValue("guid", StringComparison.Ordinal, out var guidToken)
                ? guidToken.Type == JTokenType.String ? guidToken.Value<string>() : null
                : null;
            if (fileId == 0)
                return string.IsNullOrEmpty(guid) || guid.All(character => character == '0');
            if (string.IsNullOrEmpty(guid)) return false;
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath)) return false;
            foreach (var candidate in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (candidate == null || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(candidate, out string candidateGuid, out long candidateFileId))
                    continue;
                if (candidateFileId == fileId && string.Equals(candidateGuid, guid, StringComparison.OrdinalIgnoreCase))
                    return targetType.IsInstanceOfType(candidate);
            }
            return false;
        }

        private static bool ValidateInteger(JToken token, Type targetType)
        {
            if (token.Type != JTokenType.Integer) return false;
            try
            {
                Convert.ChangeType(((JValue)token).Value, targetType, CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception exception) when (exception is InvalidCastException || exception is OverflowException || exception is FormatException)
            {
                return false;
            }
        }

        private static bool ValidateFloatingPoint(JToken token, Type targetType)
        {
            if (token.Type != JTokenType.Integer && token.Type != JTokenType.Float) return false;
            if (!double.TryParse(token.ToString(Formatting.None), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                double.IsNaN(value) || double.IsInfinity(value)) return false;
            return targetType == typeof(double) || Math.Abs(value) <= float.MaxValue;
        }

        private static bool IsIntegerType(Type type) =>
            type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) ||
            type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong) ||
            type == typeof(char);

        private static string RootMember(string propertyPath)
        {
            var separator = propertyPath.IndexOf('.');
            return separator < 0 ? propertyPath : propertyPath.Substring(0, separator);
        }

        private static FieldInfo SerializedField(Type type, string name)
        {
            for (var cursor = type; cursor != null && cursor != typeof(UnityEngine.ScriptableObject); cursor = cursor.BaseType)
            {
                var field = cursor.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null && !field.IsStatic && !field.IsNotSerialized &&
                    (field.IsPublic || field.GetCustomAttribute<SerializeField>() != null)) return field;
            }
            return null;
        }

        private static string PropertyValue(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer: return property.longValue.ToString(CultureInfo.InvariantCulture);
                case SerializedPropertyType.Boolean: return property.boolValue.ToString();
                case SerializedPropertyType.Float: return property.doubleValue.ToString("R", CultureInfo.InvariantCulture);
                case SerializedPropertyType.String: return property.stringValue ?? string.Empty;
                case SerializedPropertyType.ObjectReference: return Editor.ExactObjectReference.ExactId(property.objectReferenceValue);
                case SerializedPropertyType.Enum: return property.enumValueIndex.ToString(CultureInfo.InvariantCulture);
                case SerializedPropertyType.Vector2: return property.vector2Value.ToString("R");
                case SerializedPropertyType.Vector3: return property.vector3Value.ToString("R");
                case SerializedPropertyType.Vector4: return property.vector4Value.ToString("R");
                case SerializedPropertyType.Color: return property.colorValue.ToString("R");
                default: return property.propertyType + ":" + property.isArray +
                    (property.isArray ? ":" + property.arraySize.ToString(CultureInfo.InvariantCulture) : string.Empty);
            }
        }

        private static CommandResult<T> Failure<T>(string schema, string code, string value) =>
            CommandResult<T>.Failure(schema, code, "The ScriptableObject operation could not be completed.",
                new Dictionary<string, object> { ["value"] = value ?? string.Empty });

    }

    [Serializable] public sealed class ScriptableObjectMemberResult { public string Asset { get; set; } public string Member { get; set; } public string ValueSet { get; set; } public string ValueType { get; set; } public bool Applied { get; set; } public bool DryRun { get; set; } }
    [Serializable] public sealed class ScriptableObjectMemberUpdate { public string Name { get; set; } public string Value { get; set; } public string Reference { get; set; } }
    [Serializable] public sealed class ScriptableObjectMemberUpdateResult { public string Name { get; set; } public bool Success { get; set; } public bool Applied { get; set; } public string ErrorCode { get; set; } }
    [Serializable] public sealed class ScriptableObjectFieldsSetResult { public string Asset { get; set; } public int Total { get; set; } public int Succeeded { get; set; } public int Applied { get; set; } public int Failed { get; set; } public bool DryRun { get; set; } public ScriptableObjectMemberUpdateResult[] Results { get; set; } }
    [Serializable] public sealed class ScriptableObjectJsonImportResult { public string Asset { get; set; } public string[] ChangedMembers { get; set; } public bool Applied { get; set; } public bool DryRun { get; set; } public bool PartialOverwrite { get; set; } public bool Persistent { get; set; } public bool DirtyBeforeSave { get; set; } public bool DirtyAfterSave { get; set; } public string InstanceId { get; set; } }
}

namespace BatihanDev.UnityCliCommands.Scene
{
    public static class SceneAuthoringCommands
    {
        private const string UnloadSchema = "unity.scene.unload@1";
        private const string SaveAsSchema = "unity.scene.save-as@1";

        [CliCommand("scene.unload", "Unload one exact scene with an explicit save/discard decision when dirty.", Tags = new[] { "unity-cli-commands", "scene" })]
        public static CommandResult<SceneUnloadResult> Unload(string scene, string dirtyAction = null)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<SceneUnloadResult>.Failure(UnloadSchema, compatibility.Error);
            var candidates = LoadedScenes().Where(item => string.Equals(item.path, scene, StringComparison.Ordinal) ||
                string.Equals(item.name, scene, StringComparison.Ordinal) ||
                item.handle.GetRawData().ToString(CultureInfo.InvariantCulture) == scene).ToArray();
            if (candidates.Length != 1)
                return Failure(candidates.Length == 0 ? "SCENE_NOT_FOUND" : "SCENE_AMBIGUOUS", scene);
            var selected = candidates[0];
            if (SceneManager.sceneCount <= 1) return Failure("ONLY_LOADED_SCENE", scene);
            var wasDirty = selected.isDirty;
            if (selected.isDirty)
            {
                if (!string.Equals(dirtyAction, "save", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(dirtyAction, "discard", StringComparison.OrdinalIgnoreCase))
                    return Failure("DIRTY_SCENE_DECISION_REQUIRED", scene);
                if (string.Equals(dirtyAction, "save", StringComparison.OrdinalIgnoreCase) &&
                    string.IsNullOrEmpty(selected.path))
                    return Failure("UNSAVED_SCENE", scene);
                if (string.Equals(dirtyAction, "save", StringComparison.OrdinalIgnoreCase) &&
                    !EditorSceneManager.SaveScene(selected))
                    return Failure("SCENE_SAVE_FAILED", scene);
            }
            var path = selected.path;
            var name = selected.name;
            if (!EditorSceneManager.CloseScene(selected, true)) return Failure("SCENE_UNLOAD_FAILED", scene);
            var stillLoaded = LoadedScenes().Any(item => item.handle == selected.handle);
            return CommandResult<SceneUnloadResult>.Success(UnloadSchema, new SceneUnloadResult
            { Path = path, Name = name, DirtyAction = wasDirty ? dirtyAction : null, Unloaded = !stillLoaded, Undoable = false });
        }

        [CliCommand("scene.save-as", "Save one exact loaded scene to a different project scene path.", Tags = new[] { "unity-cli-commands", "scene" })]
        public static CommandResult<SceneSaveAsResult> SaveAs(
            string scene, string destination, bool dryRun = false, bool confirm = false,
            bool allowEmbeddedPackages = false, string expectedDestinationSha256 = null)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<SceneSaveAsResult>.Failure(SaveAsSchema, compatibility.Error);
            var candidates = LoadedScenes().Where(item => string.Equals(item.path, scene, StringComparison.Ordinal) ||
                item.handle.GetRawData().ToString(CultureInfo.InvariantCulture) == scene).ToArray();
            if (candidates.Length != 1)
                return SaveAsFailure(candidates.Length == 0 ? "SCENE_NOT_FOUND" : "SCENE_AMBIGUOUS", scene, destination);
            var selected = candidates[0];
            var sourcePath = selected.path;
            var sceneHandle = selected.handle.GetRawData().ToString(CultureInfo.InvariantCulture);

            var validated = ProjectPathPolicy.Validate(destination, allowEmbeddedPackages);
            if (!validated.Ok) return CommandResult<SceneSaveAsResult>.Failure(SaveAsSchema, validated.Error);
            var path = validated.Result.Path;
            if (!string.Equals(Path.GetExtension(path), ".unity", StringComparison.OrdinalIgnoreCase))
                return SaveAsFailure("SCENE_EXTENSION_REQUIRED", scene, path);
            if (SameProjectPath(path, sourcePath))
                return SaveAsFailure("DESTINATION_MATCHES_SOURCE", scene, path);
            if (LoadedScenes().Any(item => item.handle != selected.handle &&
                SameProjectPath(item.path, path)))
                return SaveAsFailure("DESTINATION_SCENE_LOADED", scene, path);

            var fullPath = ProjectFullPath(path);
            var parent = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrEmpty(parent) || !Directory.Exists(parent))
                return SaveAsFailure("DESTINATION_PARENT_MISSING", scene, path,
                    "Create the destination folder with create_folder before saving the scene.");
            if (Directory.Exists(fullPath))
                return SaveAsFailure("DESTINATION_NOT_REGULAR_FILE", scene, path);

            var exists = File.Exists(fullPath);
            string priorGuid = null;
            string expectedHash = null;
            try
            {
                if (exists)
                {
                    if (!confirm) return SaveAsFailure("CONFIRMATION_REQUIRED", scene, path);
                    if (string.IsNullOrEmpty(expectedDestinationSha256))
                        return SaveAsFailure("EXPECTED_DESTINATION_SHA256_REQUIRED", scene, path);
                    expectedHash = FileSha256(fullPath);
                    if (!string.Equals(expectedHash, expectedDestinationSha256, StringComparison.OrdinalIgnoreCase))
                        return SaveAsFailure("DESTINATION_SHA256_MISMATCH", scene, path);
                    priorGuid = AssetDatabase.AssetPathToGUID(path);
                    if (string.IsNullOrEmpty(priorGuid) || AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                        return SaveAsFailure("DESTINATION_SCENE_INVALID", scene, path);
                }
                else if (expectedDestinationSha256 != null)
                {
                    return SaveAsFailure("EXPECTED_DESTINATION_SHA256_UNEXPECTED", scene, path);
                }
            }
            catch (Exception exception) when (ProjectPathPolicy.IsExpectedPathException(exception))
            {
                return ProjectPathPolicy.FileSystemFailure<SceneSaveAsResult>(SaveAsSchema, path, exception);
            }

            if (dryRun)
                return CommandResult<SceneSaveAsResult>.Success(SaveAsSchema, new SceneSaveAsResult
                {
                    Saved = false, Applied = false, Path = path, SourcePath = sourcePath,
                    SceneHandle = sceneHandle, Guid = priorGuid, Exists = exists,
                    DryRun = true, Undoable = false
                });

            var current = LoadedScenes().Where(item => item.handle == selected.handle &&
                SameProjectPath(item.path, sourcePath)).ToArray();
            if (current.Length != 1) return SaveAsFailure("SOURCE_SCENE_CHANGED", scene, path);
            var revalidated = ProjectPathPolicy.Validate(path, allowEmbeddedPackages);
            if (!revalidated.Ok) return CommandResult<SceneSaveAsResult>.Failure(SaveAsSchema, revalidated.Error);
            if (!SameProjectPath(revalidated.Result.Path, path))
                return SaveAsFailure("DESTINATION_PATH_CHANGED", scene, path);
            if (LoadedScenes().Any(item => item.handle != selected.handle &&
                SameProjectPath(item.path, path)))
                return SaveAsFailure("DESTINATION_SCENE_LOADED", scene, path);

            var saveAttempted = false;
            var saveApplied = false;
            try
            {
                var existsNow = File.Exists(fullPath);
                if (existsNow != exists)
                    return SaveAsFailure("DESTINATION_STATE_CHANGED", scene, path);
                if (exists && !string.Equals(FileSha256(fullPath), expectedHash, StringComparison.Ordinal))
                    return SaveAsFailure("DESTINATION_SHA256_MISMATCH", scene, path);
                saveAttempted = true;
                if (!EditorSceneManager.SaveScene(current[0], path, false))
                    return SaveAsFailure("SCENE_SAVE_FAILED", scene, path,
                        "SaveScene reported failure; the destination state may have changed.",
                        false, null, true);
                saveApplied = true;

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                var savedScene = LoadedScenes().SingleOrDefault(item => item.handle == selected.handle);
                var guid = AssetDatabase.AssetPathToGUID(path);
                var loadable = AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null;
                if (!savedScene.IsValid() || !SameProjectPath(savedScene.path, path) ||
                    savedScene.isDirty || !File.Exists(fullPath) || !loadable || string.IsNullOrEmpty(guid))
                    return SaveAsFailure("SAVE_READBACK_FAILED", scene, path,
                        "The save was applied, but its resulting scene state could not be verified.", true);
                if (exists && !string.Equals(guid, priorGuid, StringComparison.Ordinal))
                    return SaveAsFailure("DESTINATION_GUID_CHANGED", scene, path,
                        "The destination was overwritten, but its GUID was not preserved.", true);

                return CommandResult<SceneSaveAsResult>.Success(SaveAsSchema, new SceneSaveAsResult
                {
                    Saved = true, Applied = true, Path = path, SourcePath = sourcePath,
                    SceneHandle = sceneHandle, Guid = guid, Exists = true,
                    DryRun = false, Undoable = false
                });
            }
            catch (Exception exception)
            {
                return SaveAsFailure(
                    ProjectPathPolicy.IsExpectedPathException(exception) ? "SCENE_SAVE_IO_FAILED" : "SCENE_SAVE_FAILED",
                    scene, path,
                    saveApplied
                        ? "The save was applied, but its resulting scene state could not be verified."
                        : "The scene save failed before successful completion.",
                    saveApplied, exception, saveAttempted);
            }
        }

        private static string ProjectFullPath(string path) => Path.GetFullPath(Path.Combine(
            Directory.GetParent(Application.dataPath).FullName,
            path.Replace('/', Path.DirectorySeparatorChar)));

        private static bool SameProjectPath(string left, string right)
        {
            if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
                return string.Equals(left, right, StringComparison.Ordinal);
            return string.Equals(ProjectFullPath(left), ProjectFullPath(right), ProjectPathPolicy.PathComparison());
        }

        private static string FileSha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static IEnumerable<UnityEngine.SceneManagement.Scene> LoadedScenes()
        {
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (scene.IsValid() && scene.isLoaded) yield return scene;
            }
        }

        private static CommandResult<SceneUnloadResult> Failure(string code, string scene) =>
            CommandResult<SceneUnloadResult>.Failure(UnloadSchema, code, "The selected scene could not be unloaded.",
                new Dictionary<string, object> { ["scene"] = scene ?? string.Empty });

        private static CommandResult<SceneSaveAsResult> SaveAsFailure(
            string code, string scene, string destination, string message = null,
            bool saveApplied = false, Exception exception = null, bool stateMayHaveChanged = false) =>
            CommandResult<SceneSaveAsResult>.Failure(SaveAsSchema, code,
                message ?? "The selected scene could not be saved to the destination.",
                new Dictionary<string, object>
                {
                    ["scene"] = scene ?? string.Empty,
                    ["destination"] = destination ?? string.Empty,
                    ["saveApplied"] = saveApplied,
                    ["stateMayHaveChanged"] = stateMayHaveChanged,
                    ["exceptionType"] = exception?.GetType().Name ?? string.Empty
                });
    }

    [Serializable] public sealed class SceneUnloadResult { public string Path { get; set; } public string Name { get; set; } public string DirtyAction { get; set; } public bool Unloaded { get; set; } public bool Undoable { get; set; } }
    [Serializable] public sealed class SceneSaveAsResult { public bool Saved { get; set; } public bool Applied { get; set; } public string Path { get; set; } public string SourcePath { get; set; } public string SceneHandle { get; set; } public string Guid { get; set; } public bool Exists { get; set; } public bool DryRun { get; set; } public bool Undoable { get; set; } }
}
