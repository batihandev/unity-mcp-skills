using System;
using System.Collections.Generic;
using System.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using UnityEditor;
using UnityEngine;
using NumericPose = BatihanDev.UnityCliCommands.Transform.TransformCommands;
using Exact = BatihanDev.UnityCliCommands.Editor.ExactObjectReference;
namespace BatihanDev.UnityCliCommands.Analysis
{
    public static class SmartTransformCommands
    {
        [CliCommand("smart.snap-grid", "Smart snap-grid operation.", Tags = new[] { "unity-cli-commands", "smart" })]
        public static CommandResult<SmartTransformReport> SnapGrid([CliArg("gridSize", "Finite positive world-coordinate grid spacing.")] float gridSize = 1, [CliArg("dryRun", "Preview only; wins over confirmation and preserves authored state.")] bool dryRun = true, [CliArg("confirm", "Commit the reviewed operation only when dryRun=false.")] bool confirm = false, [CliArg("targetsJson", "Optional JSON array of exact GameObject references; omitted uses current Selection.")] string targetsJson = null)
        {
            return Execute("snap-grid", targetsJson, dryRun, confirm, 1, false, selected =>
            {
                SmartSelectionFacts.Positive(gridSize, "gridSize");
                return selected.Select(go => { var item = Snapshot(go); var p = item.OriginalPosition; item.ProposedPosition = new Vector3(Mathf.Round(p.x / gridSize) * gridSize, Mathf.Round(p.y / gridSize) * gridSize, Mathf.Round(p.z / gridSize) * gridSize); return item; }).ToList();
            });
        }
        [CliCommand("smart.distribute", "Smart distribute operation.", Tags = new[] { "unity-cli-commands", "smart" })]
        public static CommandResult<SmartTransformReport> Distribute([CliArg("axis", "X, Y, Z, -X, -Y or -Z, case-insensitive.")] string axis = "X", [CliArg("dryRun", "Preview only; wins over confirmation and preserves authored state.")] bool dryRun = true, [CliArg("confirm", "Commit the reviewed operation only when dryRun=false.")] bool confirm = false, [CliArg("targetsJson", "Optional JSON array of exact GameObject references; omitted uses current Selection.")] string targetsJson = null)
        {
            return Execute("distribute", targetsJson, dryRun, confirm, 3, true, selected =>
            {
                var direction = SmartSelectionFacts.Axis(axis); var items = selected.Select(Snapshot).ToList();
                var start = Vector3.Dot(items[0].OriginalPosition, direction); var end = Vector3.Dot(items[items.Count - 1].OriginalPosition, direction);
                for (var i = 1; i < items.Count - 1; i++) { var desired = Mathf.Lerp(start, end, i / (float)(items.Count - 1)); var current = Vector3.Dot(items[i].OriginalPosition, direction); items[i].ProposedPosition += direction * (desired - current); }
                return items;
            });
        }
        [CliCommand("smart.align-ground", "Smart align-ground operation.", Tags = new[] { "unity-cli-commands", "smart" })]
        public static CommandResult<SmartTransformReport> AlignGround([CliArg("maxDistance", "Finite positive downward ray distance from world position plus Y0.1.")] float maxDistance = 100, [CliArg("alignRotation", "Align world up to the nearest external hit normal.")] bool alignRotation = false, [CliArg("dryRun", "Preview only; wins over confirmation and preserves authored state.")] bool dryRun = true, [CliArg("confirm", "Commit the reviewed operation only when dryRun=false.")] bool confirm = false, [CliArg("targetsJson", "Optional JSON array of exact GameObject references; omitted uses current Selection.")] string targetsJson = null)
        {
            return Execute("align-ground", targetsJson, dryRun, confirm, 1, false, selected =>
            {
                SmartSelectionFacts.Positive(maxDistance, "maxDistance"); var items = new List<SmartTransformState>();
                foreach (var go in selected)
                {
                    var item = Snapshot(go);
                    var hits = Physics.RaycastAll(item.OriginalPosition + Vector3.up * .1f, Vector3.down, maxDistance)
                        .Where(hit => hit.collider != null && !hit.collider.transform.IsChildOf(go.transform)).OrderBy(hit => hit.distance).ToArray();
                    if (hits.Length > 0)
                    {
                        var hit = hits[0]; item.Hit = true; item.Collider = Exact.ExactId(hit.collider); item.Normal = hit.normal; item.ProposedPosition = hit.point;
                        if (alignRotation) item.ProposedRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                    }
                    items.Add(item);
                }
                return items;
            });
        }
        [CliCommand("smart.random-transform", "Smart random-transform operation.", Tags = new[] { "unity-cli-commands", "smart" })]
        public static CommandResult<SmartTransformReport> RandomTransform([CliArg("posRange", "Finite nonnegative independent world-axis offset range.")] float posRange = 0, [CliArg("rotRange", "Finite nonnegative independent world Euler offset range.")] float rotRange = 0, [CliArg("scaleMin", "Finite positive minimum uniform local scale.")] float scaleMin = 1, [CliArg("scaleMax", "Finite positive maximum uniform local scale, at least scaleMin.")] float scaleMax = 1, [CliArg("dryRun", "Preview only; wins over confirmation and preserves authored state.")] bool dryRun = true, [CliArg("confirm", "Commit the reviewed operation only when dryRun=false.")] bool confirm = false, [CliArg("targetsJson", "Optional JSON array of exact GameObject references; omitted uses current Selection.")] string targetsJson = null)
        {
            return Execute("random-transform", targetsJson, dryRun, confirm, 1, false, selected =>
            {
                SmartSelectionFacts.Nonnegative(posRange, "posRange"); SmartSelectionFacts.Nonnegative(rotRange, "rotRange"); SmartSelectionFacts.Positive(scaleMin, "scaleMin"); SmartSelectionFacts.Positive(scaleMax, "scaleMax");
                if (scaleMin > scaleMax) throw new SmartFailure("SMART_INPUT_INVALID", "scaleMin must not exceed scaleMax.");
                var items = new List<SmartTransformState>();
                foreach (var go in selected)
                {
                    var item = Snapshot(go);
                    if (posRange > 0) item.ProposedPosition += new Vector3(UnityEngine.Random.Range(-posRange, posRange), UnityEngine.Random.Range(-posRange, posRange), UnityEngine.Random.Range(-posRange, posRange));
                    if (rotRange > 0) item.ProposedRotation = Quaternion.Euler(go.transform.eulerAngles + new Vector3(UnityEngine.Random.Range(-rotRange, rotRange), UnityEngine.Random.Range(-rotRange, rotRange), UnityEngine.Random.Range(-rotRange, rotRange)));
                    if (scaleMin != 1 || scaleMax != 1) item.ProposedScale = Vector3.one * UnityEngine.Random.Range(scaleMin, scaleMax);
                    items.Add(item);
                }
                return items;
            });
        }
        [CliCommand("smart.layout", "Smart layout operation.", Tags = new[] { "unity-cli-commands", "smart" })]
        public static CommandResult<SmartTransformReport> Layout([CliArg("layoutType", "Linear, Grid, Circle or Arc, case-insensitive.")] string layoutType = "Linear", [CliArg("axis", "X, Y, Z, -X, -Y or -Z, case-insensitive.")] string axis = "X", [CliArg("spacing", "Finite nonnegative spacing or Circle/Arc radius.")] float spacing = 2, [CliArg("columns", "Positive Grid column count; ignored by other layouts.")] int columns = 3, [CliArg("arcAngle", "Arc span in finite degrees 0..360; ignored by other layouts.")] float arcAngle = 180, [CliArg("lookAtCenter", "Face the original first position for nonzero Circle/Arc offsets.")] bool lookAtCenter = false, [CliArg("dryRun", "Preview only; wins over confirmation and preserves authored state.")] bool dryRun = true, [CliArg("confirm", "Commit the reviewed operation only when dryRun=false.")] bool confirm = false, [CliArg("targetsJson", "Optional JSON array of exact GameObject references; omitted uses current Selection.")] string targetsJson = null)
        {
            return Execute("layout", targetsJson, dryRun, confirm, 1, true, selected =>
            {
                var layout = layoutType?.ToLowerInvariant();
                if (layout != "linear" && layout != "grid" && layout != "circle" && layout != "arc") throw new SmartFailure("SMART_INPUT_INVALID", "Choose Linear, Grid, Circle or Arc.");
                SmartSelectionFacts.Nonnegative(spacing, "spacing");
                var direction = layout == "linear" ? SmartSelectionFacts.Axis(axis) : Vector3.zero;
                if (layout == "grid" && columns <= 0) throw new SmartFailure("SMART_INPUT_INVALID", "Grid columns must be positive.");
                if (layout == "arc" && (!SmartSelectionFacts.Finite(arcAngle) || arcAngle < 0 || arcAngle > 360)) throw new SmartFailure("SMART_INPUT_INVALID", "Arc angle must be finite and within 0..360.");
                var items = selected.Select(Snapshot).ToList(); var center = items[0].OriginalPosition;
                for (var i = 0; i < items.Count; i++)
                {
                    Vector3 offset;
                    switch (layout)
                    {
                        case "linear": offset = direction * (i * spacing); break;
                        case "grid": offset = new Vector3((i % columns) * spacing, 0, -(i / columns) * spacing); break;
                        case "circle": offset = Quaternion.Euler(0, i * (360f / items.Count), 0) * (Vector3.forward * spacing); break;
                        default: var angle = -arcAngle / 2 + (items.Count > 1 ? arcAngle / (items.Count - 1) : 0) * i; offset = Quaternion.Euler(0, angle, 0) * (Vector3.forward * spacing); break;
                    }
                    items[i].ProposedPosition = center + offset;
                    if (lookAtCenter && (layout == "circle" || layout == "arc") && offset.sqrMagnitude > 0)
                        items[i].ProposedRotation = Quaternion.LookRotation(-offset, Vector3.up);
                }
                return items;
            });
        }

        private sealed class SmartTransformState
        {
            internal string Target;
            internal Vector3 OriginalPosition, ProposedPosition, OriginalScale, ProposedScale, Normal;
            internal Quaternion OriginalRotation, ProposedRotation;
            internal bool Changed, Hit;
            internal string Collider;
        }
        private static SmartTransformItem Result(SmartTransformState state) => new SmartTransformItem
        {
            Target = state.Target,
            OriginalPosition = NumericPose.Vector(state.OriginalPosition), ProposedPosition = NumericPose.Vector(state.ProposedPosition),
            OriginalRotation = NumericPose.Rotation(state.OriginalRotation), ProposedRotation = NumericPose.Rotation(state.ProposedRotation),
            OriginalScale = NumericPose.Vector(state.OriginalScale), ProposedScale = NumericPose.Vector(state.ProposedScale),
            Normal = NumericPose.Vector(state.Normal), Changed = state.Changed, Hit = state.Hit, Collider = state.Collider
        };
        private static SmartTransformState Snapshot(GameObject go) => new SmartTransformState
        {
            Target = Exact.ExactId(go), OriginalPosition = go.transform.position, ProposedPosition = go.transform.position,
            OriginalRotation = go.transform.rotation, ProposedRotation = go.transform.rotation,
            OriginalScale = go.transform.localScale, ProposedScale = go.transform.localScale
        };
        private static CommandResult<SmartTransformReport> Execute(string operation, string targetsJson, bool dryRun, bool confirm, int minimum, bool sort, Func<List<GameObject>, List<SmartTransformState>> planner)
        {
            var schema = "unity.smart." + operation + "@1"; var randomBefore = UnityEngine.Random.state;
            try
            {
                SmartSelectionFacts.Authoring(dryRun, confirm);
                var targets = SmartSelectionFacts.Capture(targetsJson, minimum, sort); var items = planner(targets);
                for (var i = 0; i < items.Count; i++)
                {
                    var item = items[i]; SmartSelectionFacts.RequireFinite(item.ProposedPosition, item.ProposedRotation, item.ProposedScale);
                    item.Changed = !item.ProposedPosition.Equals(item.OriginalPosition) || !item.ProposedScale.Equals(item.OriginalScale) || !item.ProposedRotation.Equals(item.OriginalRotation);
                }
                var report = new SmartTransformReport { Operation = operation, Preview = dryRun, SelectedCount = targets.Count, ProcessedCount = targets.Count, ChangedCount = items.Count(item => item.Changed), HitCount = items.Count(item => item.Hit), UndoGroup = -1, Items = items.Select(Result).ToList() };
                if (dryRun || report.ChangedCount == 0) return CommandResult<SmartTransformReport>.Success(schema, report);
                using (var scope = new AuthoringUndoScope("Smart " + operation))
                {
                    report.UndoGroup = Undo.GetCurrentGroup();
                    try
                    {
                        Undo.RecordObjects(targets.Where((go, index) => items[index].Changed).Select(go => (UnityEngine.Object)go.transform).ToArray(), "Smart " + operation);
                        for (var i = 0; i < targets.Count; i++)
                        {
                            var item = items[i]; if (!item.Changed) continue;
                            var transform = targets[i].transform;
                            if (!item.ProposedPosition.Equals(item.OriginalPosition)) transform.position = item.ProposedPosition;
                            if (!item.ProposedRotation.Equals(item.OriginalRotation)) transform.rotation = item.ProposedRotation;
                            if (!item.ProposedScale.Equals(item.OriginalScale)) transform.localScale = item.ProposedScale;
                            SmartSelectionFacts.Changed(transform, targets[i]);
                        }
                        for (var i = 0; i < targets.Count; i++)
                        {
                            var item = items[i]; var target = targets[i];
                            if (SmartSelectionFacts.TransformMatches(target, item.ProposedPosition, item.ProposedRotation, item.ProposedScale)) continue;
                            throw new SmartFailure("SMART_TRANSFORM_READBACK_MISMATCH", "Actual transform state does not match the planned pose.", new Dictionary<string, object>
                            {
                                ["target"] = item.Target,
                                ["proposed"] = new { position = NumericPose.Vector(item.ProposedPosition), rotation = NumericPose.Rotation(item.ProposedRotation), localScale = NumericPose.Vector(item.ProposedScale) },
                                ["observed"] = target == null ? null : (object)new { position = NumericPose.Vector(target.transform.position), rotation = NumericPose.Rotation(target.transform.rotation), localScale = NumericPose.Vector(target.transform.localScale) },
                                ["positionAndScaleTolerance"] = SmartSelectionFacts.PositionTolerance,
                                ["rotationToleranceDegrees"] = SmartSelectionFacts.RotationToleranceDegrees
                            });
                        }
                        Undo.FlushUndoRecordObjects();
                        return CommandResult<SmartTransformReport>.Success(schema, report);
                    }
                    catch (Exception failure)
                    {
                        Undo.FlushUndoRecordObjects();
                        Exception rollbackFailure = null;
                        try { Undo.RevertAllDownToGroup(report.UndoGroup); scope.Cancel(); } catch (Exception rollback) { rollbackFailure = rollback; scope.Cancel(); }
                        var restored = rollbackFailure == null && targets.Select((go, index) => SmartSelectionFacts.TransformMatches(go, items[index].OriginalPosition, items[index].OriginalRotation, items[index].OriginalScale)).All(value => value);
                        var details = failure is SmartFailure refusal && refusal.Details != null ? new Dictionary<string, object>(refusal.Details) : new Dictionary<string, object>();
                        details["targetRollback"] = restored; details["failure"] = failure.Message; details["rollbackFailure"] = rollbackFailure?.Message;
                        throw new SmartFailure(failure is SmartFailure mismatch ? mismatch.Code : "SMART_MUTATION_FAILED", "The transform batch failed; inspect observed/proposed state and rollback before further authoring.", details);
                    }
                }
            }
            catch (Exception failure) { UnityEngine.Random.state = randomBefore; return SmartSelectionFacts.Failure<SmartTransformReport>(schema, failure); }
            finally { if (dryRun) UnityEngine.Random.state = randomBefore; }
        }
    }
}
