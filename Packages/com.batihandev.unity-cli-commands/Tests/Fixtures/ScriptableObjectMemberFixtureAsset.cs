using System;
using UnityEngine;

namespace BatihanDev.UnityCliCommands.Tests
{
    public sealed class ScriptableObjectMemberFixtureAsset : ScriptableObject
    {
        public int Number;
        public string Text;
        public UnityEngine.Object Reference;
        public Material MaterialReference;
        [NonSerialized] public int Transient;
        public readonly int ReadOnly = 7;
        public int PublicProperty { get; set; }
        public int PrivateSetter { get; private set; } = 5;
        public int this[int index] { get => Number; set => Number = value; }
        public static int StaticValue;
    }
}
