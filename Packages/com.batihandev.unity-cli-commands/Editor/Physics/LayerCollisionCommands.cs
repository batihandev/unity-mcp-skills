using System;
using System.Collections.Generic;
using BatihanDev.UnityCliCommands.Foundation;
using Unity.Pipeline.Commands;

namespace BatihanDev.UnityCliCommands.Physics3D
{
    public static class LayerCollisionCommands
    {
        private const string Schema = "unity.physics.layer-collision-set@1";

        [CliCommand("physics.layer-collision-set", "Preview or set collision for one exact pair of layer indices.",
            Tags = new[] { "unity-cli-commands", "physics" })]
        public static CommandResult<LayerCollisionResult> Set(
            [CliArg("layer1", "First layer index, from 0 through 31.", Required = true)] int layer1,
            [CliArg("layer2", "Second layer index, from 0 through 31.", Required = true)] int layer2,
            [CliArg("enableCollision", "Enable collision for the pair.")] bool enableCollision = true,
            [CliArg("confirm", "Apply the requested collision setting.")] bool confirm = false,
            [CliArg("dryRun", "Preview without writing; wins over confirmation.")] bool dryRun = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok)
                return CommandResult<LayerCollisionResult>.Failure(Schema, compatibility.Error);
            if (layer1 < 0 || layer1 > 31 || layer2 < 0 || layer2 > 31)
                return Failure("LAYER_INDEX_INVALID", "Both layer indices must be between 0 and 31.", layer1, layer2);

            var before = !UnityEngine.Physics.GetIgnoreLayerCollision(layer1, layer2);
            if (!dryRun && !confirm)
                return Failure("CONFIRMATION_REQUIRED", "Set confirm=true or dryRun=true.", layer1, layer2);

            if (!dryRun)
                UnityEngine.Physics.IgnoreLayerCollision(layer1, layer2, !enableCollision);
            var after = !UnityEngine.Physics.GetIgnoreLayerCollision(layer1, layer2);
            if (!dryRun && after != enableCollision)
                return Failure("READBACK_MISMATCH", "The collision matrix did not match the requested setting.", layer1, layer2);

            return CommandResult<LayerCollisionResult>.Success(Schema, new LayerCollisionResult
            {
                Layer1 = layer1,
                Layer2 = layer2,
                Before = before,
                Requested = enableCollision,
                After = after,
                Applied = !dryRun,
                DryRun = dryRun,
                Undoable = false
            });
        }

        private static CommandResult<LayerCollisionResult> Failure(string code, string message, int layer1, int layer2) =>
            CommandResult<LayerCollisionResult>.Failure(Schema, code, message,
                new Dictionary<string, object> { ["layer1"] = layer1, ["layer2"] = layer2 });
    }

    [Serializable]
    public sealed class LayerCollisionResult
    {
        public int Layer1 { get; set; }
        public int Layer2 { get; set; }
        public bool Before { get; set; }
        public bool Requested { get; set; }
        public bool After { get; set; }
        public bool Applied { get; set; }
        public bool DryRun { get; set; }
        public bool Undoable { get; set; }
    }
}
