using System;
using System.Collections.Generic;
using UnityEngine;

namespace BatihanDev.UnityCliCommands.Tests
{
    public enum ScriptableObjectJsonValidationMode : ushort
    {
        Alpha = 3,
        Beta = 9
    }

    [Serializable]
    public sealed class ScriptableObjectJsonValidationNested
    {
        public int Count;
        public string Label;
    }

    public sealed class ScriptableObjectJsonValidationFixtureAsset : ScriptableObject
    {
        public int Number;
        public ushort SmallUnsigned;
        public Vector3 Vector;
        public ScriptableObjectJsonValidationMode Mode;
        public List<int> Numbers = new List<int>();
        public ScriptableObjectJsonValidationNested Nested = new ScriptableObjectJsonValidationNested();
        public ScriptableObject Reference;
        public Material MaterialReference;
    }
}
