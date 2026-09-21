using UnityEngine;

namespace BatihanDev.UnityCliCommands.Tests
{
    public sealed class ScriptableObjectJsonEngineShapesFixtureAsset : ScriptableObject
    {
        public Rect Rect;
        public Bounds Bounds;
        public AnimationCurve Curve = new AnimationCurve();
        public Gradient ColorGradient = new Gradient();
        public LayerMask LayerMask;
    }
}
