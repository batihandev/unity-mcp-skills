using UnityEngine;

namespace BatihanDev.UnityCliCommands.Tests
{
    public enum FixtureMode { Off, On }

    public sealed class CoreComponentFixture : MonoBehaviour
    {
        [SerializeField] private float number;
        [SerializeField] private int integer;
        [SerializeField] private bool boolean;
        [SerializeField] private Vector2 vector2;
        [SerializeField] private Vector3 vector3;
        [SerializeField] private Vector4 vector4;
        [SerializeField] private Color color;
        [SerializeField] private Quaternion rotation;
        [SerializeField] private FixtureMode mode;
        [SerializeField] private LayerMask mask;
        [SerializeField] private AnimationCurve curve;
        [SerializeField] private UnityEngine.Object reference;
        public float Number { get => number; set => number = value; }
        public int Integer { get => integer; set => integer = value; }
        public bool Boolean { get => boolean; set => boolean = value; }
        public Vector2 Vector2Value { get => vector2; set => vector2 = value; }
        public Vector3 Vector3Value { get => vector3; set => vector3 = value; }
        public Vector4 Vector4Value { get => vector4; set => vector4 = value; }
        public Color Tint { get => color; set => color = value; }
        public Quaternion Rotation { get => rotation; set => rotation = value; }
        public FixtureMode Mode { get => mode; set => mode = value; }
        public LayerMask Mask { get => mask; set => mask = value; }
        public AnimationCurve Curve { get => curve; set => curve = value; }
        public UnityEngine.Object Reference { get => reference; set => reference = value; }
        public int PublicSerialized;
        [System.NonSerialized] public int Transient;
        public readonly int Immutable = 7;
        public int PrivateSetter { get; private set; } = 5;
        public int Collision { get => integer; set => integer = value; }
        public int collision { get => integer; set => integer = value; }
        public int ThrowingGetter => throw new System.InvalidOperationException("Fixture getter failure");
        public int this[int index] { get => integer; set => integer = value; }
        public static int StaticValue;
    }
}
