using System;
using System.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using Unity.Pipeline.Commands;
using UnityEditor;

namespace BatihanDev.UnityCliCommands.Editor
{
    public static class EditorCommands
    {
        private const string Schema = "unity.editor.context-details@1";

        [CliCommand("editor.context.details", "Return focused-window and current-selection details keyed by exact identities.",
            Tags = new[] { "unity-cli-commands", "editor" })]
        public static CommandResult<EditorContextDetailsResult> ContextDetails()
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok)
                return CommandResult<EditorContextDetailsResult>.Failure(Schema, compatibility.Error);

            var gameObjects = Selection.gameObjects.Select(gameObject =>
            {
                var globalId = GlobalObjectId.GetGlobalObjectIdSlow(gameObject);
                return new SelectedGameObjectDetails
                {
                    GlobalObjectId = globalId.identifierType == 0 ? null : globalId.ToString(),
                    InstanceId = ExactObjectReference.ExactId(gameObject),
                    Name = gameObject.name,
                    Tag = gameObject.tag,
                    LayerIndex = gameObject.layer,
                    LayerName = UnityEngine.LayerMask.LayerToName(gameObject.layer),
                    ActiveSelf = gameObject.activeSelf,
                    ActiveInHierarchy = gameObject.activeInHierarchy
                };
            }).ToArray();
            var assets = Selection.assetGUIDs.Select(guid =>
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                return new SelectedAssetDetails
                {
                    Guid = guid,
                    Path = path,
                    InstanceId = ExactObjectReference.ExactId(asset),
                    IsFolder = AssetDatabase.IsValidFolder(path)
                };
            }).ToArray();

            return CommandResult<EditorContextDetailsResult>.Success(Schema, new EditorContextDetailsResult
            {
                FocusedWindow = EditorWindow.focusedWindow == null ? "None" : EditorWindow.focusedWindow.GetType().Name,
                SelectedGameObjects = gameObjects,
                SelectedAssets = assets
            });
        }

    }

    [Serializable]
    public sealed class EditorContextDetailsResult
    {
        public string FocusedWindow { get; set; }
        public SelectedGameObjectDetails[] SelectedGameObjects { get; set; }
        public SelectedAssetDetails[] SelectedAssets { get; set; }
    }

    [Serializable]
    public sealed class SelectedGameObjectDetails
    {
        public string GlobalObjectId { get; set; }
        public string InstanceId { get; set; }
        public string Name { get; set; }
        public string Tag { get; set; }
        public int LayerIndex { get; set; }
        public string LayerName { get; set; }
        public bool ActiveSelf { get; set; }
        public bool ActiveInHierarchy { get; set; }
    }

    [Serializable]
    public sealed class SelectedAssetDetails
    {
        public string Guid { get; set; }
        public string Path { get; set; }
        public string InstanceId { get; set; }
        public bool IsFolder { get; set; }
    }
}
