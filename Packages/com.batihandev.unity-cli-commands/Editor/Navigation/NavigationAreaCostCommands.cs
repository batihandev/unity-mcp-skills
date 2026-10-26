using System;
using System.Collections.Generic;
using BatihanDev.UnityCliCommands.Foundation;
using Unity.Pipeline.Commands;
using UnityEngine.AI;

namespace BatihanDev.UnityCliCommands.Navigation
{
    public static class NavigationAreaCostCommands
    {
        private const string Schema = "unity.navmesh.area-cost-set@1";

        [CliCommand("navmesh.area-cost-set", "Preview or set the global traversal cost for one NavMesh area index.",
            Tags = new[] { "unity-cli-commands", "navmesh" })]
        public static CommandResult<NavigationAreaCostResult> Set(
            [CliArg("areaIndex", "NavMesh area index, 0 through 31.", Required = true)] int areaIndex,
            [CliArg("cost", "Finite nonnegative traversal cost.", Required = true)] float cost,
            [CliArg("confirm", "Apply the requested cost.")] bool confirm = false,
            [CliArg("dryRun", "Preview without writing; wins over confirmation.")] bool dryRun = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok)
                return CommandResult<NavigationAreaCostResult>.Failure(Schema, compatibility.Error);
            if (areaIndex < 0 || areaIndex > 31)
                return Failure("AREA_INDEX_INVALID", "areaIndex must be between 0 and 31.", areaIndex, cost);
            if (float.IsNaN(cost) || float.IsInfinity(cost) || cost < 0f)
                return Failure("COST_INVALID", "cost must be finite and nonnegative.", areaIndex, cost);
            if (!dryRun && !confirm)
                return Failure("CONFIRMATION_REQUIRED", "Set confirm=true or dryRun=true.", areaIndex, cost);

            float before;
            try { before = NavMesh.GetAreaCost(areaIndex); }
            catch (Exception exception)
            {
                return Failure("AREA_READ_FAILED", "The current NavMesh area cost could not be read.", areaIndex, cost, exception);
            }
            if (dryRun)
                return CommandResult<NavigationAreaCostResult>.Success(Schema, Result(areaIndex, cost, before, before, false, true));

            try
            {
                NavMesh.SetAreaCost(areaIndex, cost);
                var after = NavMesh.GetAreaCost(areaIndex);
                if (!Equal(after, cost))
                    return Failure("COST_READBACK_MISMATCH", "The engine did not retain the requested area cost; inspect the observed value before restoring.",
                        areaIndex, cost, null, before, after);
                return CommandResult<NavigationAreaCostResult>.Success(Schema, Result(areaIndex, cost, before, after, true, false));
            }
            catch (Exception exception)
            {
                float? after = null;
                try { after = NavMesh.GetAreaCost(areaIndex); } catch (Exception) { }
                return Failure("COST_WRITE_FAILED", "The engine rejected the requested area cost; inspect the observed value before restoring.",
                    areaIndex, cost, exception, before, after);
            }
        }

        private static bool Equal(float a, float b) => Math.Abs(a - b) <= Math.Max(0.00001f, Math.Abs(b) * 0.00001f);
        private static NavigationAreaCostResult Result(int index, float requested, float before, float after, bool applied, bool dryRun) =>
            new NavigationAreaCostResult { AreaIndex = index, Requested = requested, Before = before, After = after,
                Applied = applied, DryRun = dryRun, Undoable = false };
        private static CommandResult<NavigationAreaCostResult> Failure(string code, string message, int index, float requested,
            Exception exception = null, float? before = null, float? after = null) =>
            CommandResult<NavigationAreaCostResult>.Failure(Schema, code, message, new Dictionary<string, object>
            { ["areaIndex"] = index, ["requested"] = requested, ["before"] = before, ["after"] = after,
              ["exceptionType"] = exception?.GetType().Name ?? string.Empty });
    }

    [Serializable]
    public sealed class NavigationAreaCostResult
    {
        public int AreaIndex { get; set; }
        public float Requested { get; set; }
        public float Before { get; set; }
        public float After { get; set; }
        public bool Applied { get; set; }
        public bool DryRun { get; set; }
        public bool Undoable { get; set; }
    }
}
