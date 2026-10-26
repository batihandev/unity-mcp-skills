using System;
using System.Collections.Generic;
using UnityEngine;

namespace BatihanDev.UnityCliCommands.Examples
{
    public static class ObserverFraming
    {
        public static ObserverFramingResult TryFrame(
            Bounds measuredRoomBounds,
            Bounds subjectBounds,
            Camera observer,
            IReadOnlyList<Vector3> orderedCandidatePositions,
            LayerMask obstacleMask,
            float viewportMargin = 0.05f)
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));
            if (orderedCandidatePositions == null || orderedCandidatePositions.Count == 0)
                throw new ArgumentException("At least one explicit candidate position is required.", nameof(orderedCandidatePositions));
            if (!Finite(viewportMargin) || viewportMargin < 0f || viewportMargin >= 0.5f)
                throw new ArgumentOutOfRangeException(nameof(viewportMargin));
            if (!Contains(measuredRoomBounds, subjectBounds))
                return ObserverFramingResult.Refused("subject_outside_room", Array.Empty<ObserverCandidateResult>());

            var originalPosition = observer.transform.position;
            var originalRotation = observer.transform.rotation;
            var results = new List<ObserverCandidateResult>(orderedCandidatePositions.Count);
            try
            {
                for (var index = 0; index < orderedCandidatePositions.Count; index++)
                {
                    var candidate = orderedCandidatePositions[index];
                    var reason = ValidatePosition(candidate, measuredRoomBounds, subjectBounds, out var direction);
                    if (reason == null)
                    {
                        observer.transform.SetPositionAndRotation(candidate, Quaternion.LookRotation(direction, Vector3.up));
                        if (!FitsFrustum(observer, subjectBounds, viewportMargin)) reason = "subject_outside_frustum";
                        else
                        {
                            var sightLine = new Ray(candidate, direction);
                            if (!subjectBounds.IntersectRay(sightLine, out var subjectDistance)) reason = "subject_not_on_sight_line";
                            else if (Physics.Raycast(sightLine, subjectDistance, obstacleMask, QueryTriggerInteraction.Ignore)) reason = "line_of_sight_blocked";
                        }
                    }

                    var accepted = reason == null;
                    results.Add(new ObserverCandidateResult(index, candidate, accepted, reason ?? "selected"));
                    if (accepted)
                    {
                        observer.transform.SetPositionAndRotation(candidate, Quaternion.LookRotation(direction, Vector3.up));
                        return ObserverFramingResult.Selected(index, candidate, observer.transform.eulerAngles, results.ToArray());
                    }
                    observer.transform.SetPositionAndRotation(originalPosition, originalRotation);
                }
                return ObserverFramingResult.Refused("no_candidate_satisfied_framing_constraints", results.ToArray());
            }
            finally
            {
                if (results.Count == 0 || !results[results.Count - 1].Accepted)
                    observer.transform.SetPositionAndRotation(originalPosition, originalRotation);
            }
        }

        private static string ValidatePosition(Vector3 position, Bounds room, Bounds subject, out Vector3 direction)
        {
            direction = subject.center - position;
            if (!Finite(position.x) || !Finite(position.y) || !Finite(position.z)) return "candidate_not_finite";
            if (!Contains(room, position)) return "candidate_outside_room";
            if (direction.sqrMagnitude <= Mathf.Pow(subject.extents.magnitude + 0.01f, 2f)) return "candidate_too_close";
            return null;
        }

        private static bool FitsFrustum(Camera camera, Bounds bounds, float margin)
        {
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            foreach (var corner in Corners(bounds))
            {
                var viewport = camera.WorldToViewportPoint(corner);
                if (viewport.z <= camera.nearClipPlane || viewport.x < margin || viewport.x > 1f - margin || viewport.y < margin || viewport.y > 1f - margin)
                    return false;
                for (var plane = 0; plane < planes.Length; plane++)
                    if (planes[plane].GetDistanceToPoint(corner) < 0f) return false;
            }
            return true;
        }

        private static IEnumerable<Vector3> Corners(Bounds bounds)
        {
            var min = bounds.min;
            var max = bounds.max;
            for (var x = 0; x < 2; x++) for (var y = 0; y < 2; y++) for (var z = 0; z < 2; z++)
                yield return new Vector3(x == 0 ? min.x : max.x, y == 0 ? min.y : max.y, z == 0 ? min.z : max.z);
        }

        private static bool Contains(Bounds outer, Bounds inner) => Contains(outer, inner.min) && Contains(outer, inner.max);
        private static bool Contains(Bounds bounds, Vector3 point) => point.x >= bounds.min.x && point.x <= bounds.max.x && point.y >= bounds.min.y && point.y <= bounds.max.y && point.z >= bounds.min.z && point.z <= bounds.max.z;
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public sealed class ObserverFramingResult
    {
        public bool Applied { get; }
        public string Reason { get; }
        public int SelectedIndex { get; }
        public float[] Position { get; }
        public float[] Euler { get; }
        public ObserverCandidateResult[] Candidates { get; }

        private ObserverFramingResult(bool applied, string reason, int selectedIndex, Vector3 position, Vector3 euler, ObserverCandidateResult[] candidates)
        { Applied = applied; Reason = reason; SelectedIndex = selectedIndex; Position = Vector(position); Euler = Vector(euler); Candidates = candidates; }
        internal static ObserverFramingResult Selected(int index, Vector3 position, Vector3 euler, ObserverCandidateResult[] candidates) => new ObserverFramingResult(true, "selected", index, position, euler, candidates);
        internal static ObserverFramingResult Refused(string reason, ObserverCandidateResult[] candidates) => new ObserverFramingResult(false, reason, -1, default, default, candidates);
        private static float[] Vector(Vector3 value) => new[] { value.x, value.y, value.z };
    }

    public sealed class ObserverCandidateResult
    {
        public int Index { get; }
        public float[] Position { get; }
        public bool Accepted { get; }
        public string Reason { get; }
        internal ObserverCandidateResult(int index, Vector3 position, bool accepted, string reason)
        { Index = index; Position = new[] { position.x, position.y, position.z }; Accepted = accepted; Reason = reason; }
    }
}
