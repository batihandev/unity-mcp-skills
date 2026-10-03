using System;
using System.Collections.Generic;
using BatihanDev.UnityCliCommands.Foundation;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;

namespace BatihanDev.UnityCliCommands.TerrainAuthoring
{
    public static class TerrainTextureCommands
    {
        private const string Schema = "unity.terrain.paint-texture@1";

        [CliCommand("terrain.paint-texture", "Paint one existing TerrainLayer with normalized radial weights.",
            Tags = new[] { "unity-cli-commands", "terrain" })]
        public static CommandResult<object> Paint(
            [CliArg("target", "Exact Terrain component handle.", Required = true)] string target,
            [CliArg("centerX", "Normalized alphamap X, 0..1.")] float centerX = 0.5f,
            [CliArg("centerZ", "Normalized alphamap Z, 0..1.")] float centerZ = 0.5f,
            [CliArg("layer", "Existing non-null TerrainLayer index.")] int layer = 0,
            [CliArg("strength", "Paint strength, 0..1.")] float strength = 1f,
            [CliArg("brushSize", "Brush diameter in alphamap pixels, at least 1.")] int brushSize = 10,
            [CliArg("confirm", "Apply the paint.")] bool confirm = false,
            [CliArg("dryRun", "Preview without writes.")] bool dryRun = false)
        {
            var check = TerrainCore.PrepareWrite(Schema, target, confirm, dryRun, out var terrain);
            if (check != null) return check;
            var data = terrain.terrainData;
            if (!TerrainCore.Unit(centerX) || !TerrainCore.Unit(centerZ) || !TerrainCore.Unit(strength) || brushSize < 1 ||
                layer < 0 || layer >= data.terrainLayers.Length || data.terrainLayers[layer] == null ||
                data.alphamapWidth < 1 || data.alphamapHeight < 1)
                return TerrainCore.Fail(Schema, "INPUT_INVALID", "Center/strength must be 0..1; brushSize at least 1; layer must exist and be non-null.");
            double cx = centerX * (data.alphamapWidth - 1), cz = centerZ * (data.alphamapHeight - 1);
            double radius = brushSize == 1 ? 0d : brushSize * 0.5d;
            int x0 = Math.Max(0, (int)Math.Ceiling(cx - radius));
            int x1 = Math.Min(data.alphamapWidth - 1, (int)Math.Floor(cx + radius));
            int z0 = Math.Max(0, (int)Math.Ceiling(cz - radius));
            int z1 = Math.Min(data.alphamapHeight - 1, (int)Math.Floor(cz + radius));
            if (brushSize == 1)
            {
                x0 = x1 = TerrainCore.Pixel(centerX, data.alphamapWidth);
                z0 = z1 = TerrainCore.Pixel(centerZ, data.alphamapHeight);
            }
            if (x0 > x1 || z0 > z1)
                return TerrainCore.Fail(Schema, "BRUSH_EMPTY", "The brush does not touch an alphamap pixel.");
            int width = x1 - x0 + 1, length = z1 - z0 + 1;
            var weights = data.GetAlphamaps(x0, z0, width, length);
            int count = weights.GetLength(2);
            if (layer >= count) return TerrainCore.Fail(Schema, "LAYER_MISMATCH", "The selected layer has no alphamap channel.");
            for (int z = 0; z < length; z++) for (int x = 0; x < width; x++)
            {
                double distance = Math.Sqrt(Math.Pow(x0 + x - cx, 2) + Math.Pow(z0 + z - cz, 2));
                double falloff = radius == 0d ? 1d : Math.Max(0d, 1d - distance / radius);
                float blend = (float)(strength * falloff);
                if (blend <= 0f) continue;
                float otherSum = 0f;
                for (int i = 0; i < count; i++) if (i != layer) otherSum += weights[z, x, i];
                float chosen = Mathf.Lerp(weights[z, x, layer], 1f, blend);
                float remainder = 1f - chosen;
                if (count == 1) { weights[z, x, layer] = 1f; continue; }
                for (int i = 0; i < count; i++)
                    if (i != layer) weights[z, x, i] = otherSum > 0f ? weights[z, x, i] / otherSum * remainder : remainder / (count - 1);
                weights[z, x, layer] = chosen;
            }
            if (!dryRun)
            {
                var guard = TerrainCore.GuardData(Schema, data);
                if (guard != null) return guard;
                var textures = data.alphamapTextures;
                Undo.IncrementCurrentGroup();
                var group = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Paint Terrain texture");
                Undo.RegisterCompleteObjectUndo(data, "Paint Terrain texture");
                foreach (var texture in textures) if (texture != null) Undo.RegisterCompleteObjectUndo(texture, "Paint Terrain texture");
                data.SetAlphamaps(x0, z0, weights);
                Undo.CollapseUndoOperations(group);
                EditorUtility.SetDirty(data);
                foreach (var texture in textures) if (texture != null) EditorUtility.SetDirty(texture);
                if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(data))) AssetDatabase.SaveAssetIfDirty(data);
            }
            return CommandResult<object>.Success(Schema, new { identity = TerrainCore.Identity(terrain), layer,
                layerName = data.terrainLayers[layer].name, x = x0, z = z0, width, length, centerX, centerZ,
                brushSize, strength, applied = !dryRun, dryRun, undoable = !dryRun,
                saved = !dryRun && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(data)) });
        }
    }
}
