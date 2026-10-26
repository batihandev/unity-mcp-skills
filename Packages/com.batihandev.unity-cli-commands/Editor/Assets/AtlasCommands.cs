using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BatihanDev.UnityCliCommands.Foundation;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using UnityObject = UnityEngine.Object;

namespace BatihanDev.UnityCliCommands.Assets
{
    public static class AtlasCommands
    {
        private const string MembershipSchema = "unity.atlas.set-packables@1";
        private const string PackSchema = "unity.atlas.pack@1";
        private const string SourceTypeName = "UnityEditor.U2D.SpriteAtlasAsset";

        [CliCommand("atlas.create", "Create one Sprite Atlas V2 source at a vacant asset path.", Tags = new[] { "unity-cli-commands", "asset", "importer" })]
        public static CommandResult<AtlasCreateResult> Create(string atlas, bool dryRun = false, bool confirm = false)
        {
            const string schema = "unity.atlas.create@1";
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<AtlasCreateResult>.Failure(schema, compatibility.Error);
            var validated = ProjectPathPolicy.Validate(atlas);
            if (!validated.Ok) return CommandResult<AtlasCreateResult>.Failure(schema, validated.Error);
            var path = validated.Result.Path;
            if (!path.EndsWith(".spriteatlasv2", StringComparison.OrdinalIgnoreCase))
                return Failure<AtlasCreateResult>(schema, "ATLAS_EXTENSION_REQUIRED", path, "Use an explicit .spriteatlasv2 path.");
            var metaPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath, "..", path + ".meta"));
            if (validated.Result.Exists || System.IO.File.Exists(metaPath) || System.IO.Directory.Exists(metaPath))
                return Failure<AtlasCreateResult>(schema, "ASSET_EXISTS", path, "Atlas creation requires a vacant destination.");
            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(parent) || !AssetDatabase.IsValidFolder(parent))
                return Failure<AtlasCreateResult>(schema, "PARENT_FOLDER_REQUIRED", path, "Create the selected parent folder through the native folder owner first.");
            var api = ResolveSourceApi();
            if (api == null) return Failure<AtlasCreateResult>(schema, "ATLAS_SOURCE_API_UNAVAILABLE", path, "Sprite Atlas V2 source APIs are unavailable.");
            if (dryRun) return CommandResult<AtlasCreateResult>.Success(schema, new AtlasCreateResult { Atlas = path, Applied = false, DryRun = true });
            if (!confirm) return Failure<AtlasCreateResult>(schema, "CONFIRMATION_REQUIRED", path, "Set confirm=true to create the atlas source.");
            UnityObject source = null;
            try
            {
                source = api.Create();
                api.Save(source, path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                var imported = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
                if (imported == null) throw new InvalidOperationException("Saved source did not import as a Sprite Atlas.");
                return CommandResult<AtlasCreateResult>.Success(schema, new AtlasCreateResult
                { Atlas = path, Guid = AssetDatabase.AssetPathToGUID(path), Applied = true, DryRun = false });
            }
            catch (Exception exception)
            {
                return CommandResult<AtlasCreateResult>.Failure(schema, "WRITE_MAY_HAVE_APPLIED",
                    "Atlas creation may have written the selected path; inspect it before cleanup or retry.",
                    new Dictionary<string, object> { ["atlas"] = path, ["cause"] = exception.GetBaseException().Message });
            }
            finally
            {
                if (source != null && !AssetDatabase.Contains(source)) UnityObject.DestroyImmediate(source);
            }
        }

        [CliCommand("atlas.set-packables", "Replace or restore the complete membership of one existing Sprite Atlas V2 asset.", Tags = new[] { "unity-cli-commands", "asset", "importer" })]
        public static CommandResult<AtlasMembershipResult> SetPackables(
            [CliArg("atlas", "Exact project-relative .spriteatlasv2 asset path.", Required = true)] string atlas,
            string[] packables = null,
            string[] restore = null,
            bool dryRun = false,
            bool confirm = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<AtlasMembershipResult>.Failure(MembershipSchema, compatibility.Error);
            var validatedPath = ProjectPathPolicy.Validate(atlas);
            if (!validatedPath.Ok) return CommandResult<AtlasMembershipResult>.Failure(MembershipSchema, validatedPath.Error);
            var path = validatedPath.Result.Path;
            if (!path.EndsWith(".spriteatlasv2", StringComparison.OrdinalIgnoreCase) || !validatedPath.Result.Exists)
                return Failure<AtlasMembershipResult>(MembershipSchema, "ATLAS_REQUIRED", path, "An existing .spriteatlasv2 asset is required.");
            var target = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            if (target == null) return Failure<AtlasMembershipResult>(MembershipSchema, "ATLAS_REQUIRED", path, "The selected asset is not an imported Sprite Atlas V2.");
            if (IsVariant(target)) return Failure<AtlasMembershipResult>(MembershipSchema, "ATLAS_VARIANT_UNSUPPORTED", path, "Membership changes require a master atlas.");
            if ((packables == null) == (restore == null))
                return Failure<AtlasMembershipResult>(MembershipSchema, "MEMBERSHIP_INPUT_REQUIRED", path, "Supply exactly one of packables or restore.");
            var requested = packables ?? restore;
            var sourceApi = ResolveSourceApi();
            if (sourceApi == null) return Failure<AtlasMembershipResult>(MembershipSchema, "ATLAS_SOURCE_API_UNAVAILABLE", path, "Sprite Atlas V2 source APIs are unavailable in this Editor.");

            var loaded = sourceApi.Load(path);
            if (loaded == null) return Failure<AtlasMembershipResult>(MembershipSchema, "ATLAS_SOURCE_LOAD_FAILED", path, "The Sprite Atlas V2 source could not be loaded.");
            try
            {
                var current = GetPackables(target);
                if (current == null) return Failure<AtlasMembershipResult>(MembershipSchema, "ATLAS_MEMBERSHIP_READ_FAILED", path, "Public Sprite Atlas membership could not be read.");
                var before = current.Select(PackableReference).ToArray();
                if (before.Any(string.IsNullOrEmpty)) return Failure<AtlasMembershipResult>(MembershipSchema, "ATLAS_MEMBERSHIP_REFERENCE_FAILED", path, "Current membership contains an object without a durable reference.");
                var resolved = new List<UnityObject>(requested.Length);
                var canonical = new HashSet<string>(StringComparer.Ordinal);
                for (var index = 0; index < requested.Length; index++)
                {
                    var reference = requested[index];
                    var item = ResolvePackable(reference);
                    if (item == null) return Failure<AtlasMembershipResult>(MembershipSchema, "PACKABLE_UNSUPPORTED", reference, "Each packable must resolve to an existing Sprite, Sprite Texture2D, or asset folder.");
                    var key = AssetDatabase.IsValidFolder(reference) ? "folder:" + reference : PackableReference(item);
                    if (string.IsNullOrEmpty(key) || !canonical.Add(key))
                        return Failure<AtlasMembershipResult>(MembershipSchema, "PACKABLE_DUPLICATE", reference, "Packable references must be unique.");
                    resolved.Add(item);
                }
                var proposed = resolved.Select(PackableReference).ToArray();
                if (proposed.Any(string.IsNullOrEmpty))
                    return Failure<AtlasMembershipResult>(MembershipSchema, "PACKABLE_REFERENCE_FAILED", path, "Every requested packable must have a durable reference.");
                if (!dryRun && !confirm) return Failure<AtlasMembershipResult>(MembershipSchema, "CONFIRMATION_REQUIRED", path, "Set confirm=true to write the requested membership.");
                if (!dryRun)
                {
                    try
                    {
                        sourceApi.Remove(loaded, current);
                        sourceApi.Add(loaded, resolved.ToArray());
                        sourceApi.Save(loaded, path);
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                    }
                    catch (Exception exception)
                    {
                        return WriteMayHaveApplied(path, before, exception);
                    }
                    var reloaded = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
                    var afterObjects = reloaded == null ? null : GetPackables(reloaded);
                    var after = afterObjects?.Select(PackableReference).ToArray();
                    if (after == null || !after.SequenceEqual(proposed, StringComparer.Ordinal))
                        return WriteMayHaveApplied(path, before, null);
                    return CommandResult<AtlasMembershipResult>.Success(MembershipSchema, new AtlasMembershipResult
                    { Atlas = path, Before = before, After = after, Applied = true, DryRun = false, Undoable = false });
                }
                return CommandResult<AtlasMembershipResult>.Success(MembershipSchema, new AtlasMembershipResult
                { Atlas = path, Before = before, After = proposed, Applied = false, DryRun = true, Undoable = false });
            }
            finally
            {
                if (loaded != null) UnityObject.DestroyImmediate(loaded);
            }
        }

        [CliCommand("atlas.pack", "Pack one explicit Sprite Atlas V2 for a valid build target and report measured bindings.", Tags = new[] { "unity-cli-commands", "asset", "importer" })]
        public static CommandResult<AtlasPackResult> Pack(
            [CliArg("atlas", "Exact project-relative .spriteatlasv2 asset path.", Required = true)] string atlas,
            string buildTarget = null,
            string[] sprites = null)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<AtlasPackResult>.Failure(PackSchema, compatibility.Error);
            var validatedPath = ProjectPathPolicy.Validate(atlas);
            if (!validatedPath.Ok) return CommandResult<AtlasPackResult>.Failure(PackSchema, validatedPath.Error);
            var path = validatedPath.Result.Path;
            var selected = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            if (!path.EndsWith(".spriteatlasv2", StringComparison.OrdinalIgnoreCase) || selected == null)
                return Failure<AtlasPackResult>(PackSchema, "ATLAS_REQUIRED", path, "Select an existing imported Sprite Atlas V2.");
            if (IsVariant(selected)) return Failure<AtlasPackResult>(PackSchema, "ATLAS_VARIANT_UNSUPPORTED", path, "Pack a master atlas explicitly.");
            if (EditorSettings.spritePackerMode != SpritePackerMode.SpriteAtlasV2)
                return Failure<AtlasPackResult>(PackSchema, "ATLAS_PACKER_DISABLED", path, "Enable Sprite Atlas V2 in project settings before packing.");
            var target = EditorUserBuildSettings.activeBuildTarget;
            if (!string.IsNullOrWhiteSpace(buildTarget) &&
                (!Enum.TryParse(buildTarget, false, out BuildTarget parsed) || !Enum.IsDefined(typeof(BuildTarget), parsed)))
                return Failure<AtlasPackResult>(PackSchema, "BUILD_TARGET_INVALID", buildTarget, "Supply an exact BuildTarget name.");
            else if (!string.IsNullOrWhiteSpace(buildTarget)) target = (BuildTarget)Enum.Parse(typeof(BuildTarget), buildTarget, false);
            if (target == BuildTarget.NoTarget || !BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target), target))
                return Failure<AtlasPackResult>(PackSchema, "BUILD_TARGET_UNSUPPORTED", target.ToString(), "The selected build target is not installed and supported.");

            var requested = new List<string>();
            var requestedIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var reference in sprites ?? Array.Empty<string>())
            {
                var sprite = Editor.ExactObjectReference.Resolve<Sprite>(reference);
                if (sprite == null) return Failure<AtlasPackResult>(PackSchema, "SPRITE_REQUIRED", reference, "Each binding observation must resolve to one exact Sprite.");
                var exact = PackableReference(sprite);
                if (string.IsNullOrEmpty(exact) || !requestedIds.Add(exact))
                    return Failure<AtlasPackResult>(PackSchema, "SPRITE_DUPLICATE", reference, "Binding observations must be unique.");
                requested.Add(reference);
            }

            try
            {
                PackSelectedAtlas(selected, target);
            }
            catch (Exception exception)
            {
                return Failure<AtlasPackResult>(PackSchema, "ATLAS_PACK_FAILED", path, exception.GetBaseException().Message);
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            selected = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            if (selected == null) return Failure<AtlasPackResult>(PackSchema, "ATLAS_RELOAD_FAILED", path, "The atlas could not be reloaded after packing.");
            var observations = new List<SpriteBindingResult>(requested.Count);
            foreach (var reference in requested)
            {
                var sprite = Editor.ExactObjectReference.Resolve<Sprite>(reference);
                if (sprite == null) return Failure<AtlasPackResult>(PackSchema, "SPRITE_RELOAD_FAILED", reference, "An exact Sprite binding target could not be re-resolved after packing.");
                observations.Add(new SpriteBindingResult { Sprite = PackableReference(sprite), CanBind = selected.CanBindTo(sprite) });
            }
            var packed = GetPackedSpriteCount(selected);
            return CommandResult<AtlasPackResult>.Success(PackSchema, new AtlasPackResult
            { Atlas = path, BuildTarget = target.ToString(), PackedSpriteCount = packed, Bindings = observations.ToArray() });
        }

        private static AtlasSourceApi ResolveSourceApi()
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(SourceTypeName, false)).FirstOrDefault(value => value != null);
            if (type == null) return null;
            var load = type.GetMethod("Load", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
            var add = type.GetMethod("Add", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(UnityObject[]) }, null);
            var remove = type.GetMethod("Remove", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(UnityObject[]) }, null);
            var save = type.GetMethod("Save", BindingFlags.Public | BindingFlags.Static, null, new[] { type, typeof(string) }, null);
            return load == null || add == null || remove == null || save == null ? null : new AtlasSourceApi(type, load, add, remove, save);
        }

        private static UnityObject[] GetPackables(SpriteAtlas atlas)
        {
            var extension = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("UnityEditor.U2D.SpriteAtlasExtensions", false))
                .FirstOrDefault(type => type != null);
            var method = extension?.GetMethod("GetPackables", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(SpriteAtlas) }, null);
            return method?.Invoke(null, new object[] { atlas }) as UnityObject[];
        }

        private static UnityObject ResolvePackable(string reference)
        {
            if (AssetDatabase.IsValidFolder(reference)) return AssetDatabase.LoadMainAssetAtPath(reference);
            var value = Editor.ExactObjectReference.Resolve<UnityObject>(reference);
            if (value is Sprite) return value;
            if (value is Texture2D texture)
            {
                var texturePath = AssetDatabase.GetAssetPath(texture);
                var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                return importer != null && importer.textureType == TextureImporterType.Sprite ? value : null;
            }
            return null;
        }

        private static string PackableReference(UnityObject value)
        {
            if (value == null) return null;
            var path = AssetDatabase.GetAssetPath(value);
            if (string.IsNullOrEmpty(path)) return null;
            if (AssetDatabase.IsValidFolder(path)) return path;
            var global = GlobalObjectId.GetGlobalObjectIdSlow(value);
            return global.identifierType == 0 ? null : global.ToString();
        }

        private static bool IsVariant(SpriteAtlas atlas)
        {
            var extension = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("UnityEditor.U2D.SpriteAtlasExtensions", false))
                .FirstOrDefault(type => type != null);
            var method = extension?.GetMethod("IsVariant", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(SpriteAtlas) }, null);
            return method != null && (bool)method.Invoke(null, new object[] { atlas });
        }

        private static void PackSelectedAtlas(SpriteAtlas atlas, BuildTarget target)
        {
            var methods = typeof(SpriteAtlasUtility).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => method.Name == "PackAtlases").ToArray();
            foreach (var method in methods)
            {
                var parameters = method.GetParameters();
                if (parameters.Length < 2 || parameters[0].ParameterType != typeof(SpriteAtlas[]) || parameters[1].ParameterType != typeof(BuildTarget)) continue;
                var arguments = new object[parameters.Length];
                arguments[0] = new[] { atlas };
                arguments[1] = target;
                var supported = true;
                for (var index = 2; index < parameters.Length; index++)
                {
                    if (!parameters[index].IsOptional) { supported = false; break; }
                    arguments[index] = parameters[index].DefaultValue;
                }
                if (supported) { method.Invoke(null, arguments); return; }
            }
            throw new MissingMethodException("A public PackAtlases overload for one exact atlas and build target is unavailable.");
        }

        private static int GetPackedSpriteCount(SpriteAtlas atlas)
        {
            var method = typeof(SpriteAtlas).GetMethod("GetSprites", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Sprite[]) }, null);
            if (method == null) return 0;
            var capacity = 16;
            while (capacity <= 65536)
            {
                var sprites = new Sprite[capacity];
                var count = Convert.ToInt32(method.Invoke(atlas, new object[] { sprites }), CultureInfo.InvariantCulture);
                if (count < capacity) return count;
                capacity *= 2;
            }
            throw new InvalidOperationException("The packed sprite count exceeded the supported readback capacity.");
        }

        private static CommandResult<AtlasMembershipResult> WriteMayHaveApplied(string path, string[] before, Exception exception) =>
            CommandResult<AtlasMembershipResult>.Failure(MembershipSchema, "WRITE_MAY_HAVE_APPLIED",
                "Atlas source membership may have changed; restore the captured references explicitly after inspecting the asset.",
                new Dictionary<string, object> { ["atlas"] = path, ["before"] = before, ["cause"] = exception?.GetBaseException().Message ?? "Persisted membership readback did not match." });

        private static CommandResult<T> Failure<T>(string schema, string code, string target, string message) =>
            CommandResult<T>.Failure(schema, code, message, new Dictionary<string, object> { ["target"] = target ?? string.Empty });

        private sealed class AtlasSourceApi
        {
            private readonly Type _type;
            private readonly MethodInfo _load, _add, _remove, _save;
            internal AtlasSourceApi(Type type, MethodInfo load, MethodInfo add, MethodInfo remove, MethodInfo save) { _type = type; _load = load; _add = add; _remove = remove; _save = save; }
            internal UnityObject Create() => Activator.CreateInstance(_type) as UnityObject;
            internal UnityObject Load(string path) => _load.Invoke(null, new object[] { path }) as UnityObject;
            internal void Add(UnityObject source, UnityObject[] values) => _add.Invoke(source, new object[] { values });
            internal void Remove(UnityObject source, UnityObject[] values) { if (values.Length > 0) _remove.Invoke(source, new object[] { values }); }
            internal void Save(UnityObject source, string path) => _save.Invoke(null, new object[] { source, path });
        }
    }

    [Serializable] public sealed class AtlasCreateResult { public string Atlas { get; set; } public string Guid { get; set; } public bool Applied { get; set; } public bool DryRun { get; set; } public bool Undoable { get; set; } }
    [Serializable] public sealed class AtlasMembershipResult { public string Atlas { get; set; } public string[] Before { get; set; } public string[] After { get; set; } public bool Applied { get; set; } public bool DryRun { get; set; } public bool Undoable { get; set; } }
    [Serializable] public sealed class AtlasPackResult { public string Atlas { get; set; } public string BuildTarget { get; set; } public int PackedSpriteCount { get; set; } public SpriteBindingResult[] Bindings { get; set; } }
    [Serializable] public sealed class SpriteBindingResult { public string Sprite { get; set; } public bool CanBind { get; set; } }
}
