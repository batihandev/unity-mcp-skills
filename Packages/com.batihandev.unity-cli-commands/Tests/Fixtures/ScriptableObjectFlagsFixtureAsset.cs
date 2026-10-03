using System;
using UnityEngine;

namespace BatihanDev.UnityCliCommands.Tests
{
    [Flags]
    public enum SnapshotFlags { None = 0, One = 1, Four = 4, Eight = 8 }
    public enum SnapshotSparse { First = 3, Second = 17, Third = 91 }

    public sealed class ScriptableObjectFlagsFixtureAsset : ScriptableObject
    {
        public SnapshotFlags Flags = SnapshotFlags.One;
        public SnapshotSparse Sparse = SnapshotSparse.First;
        public int Omitted = 42;
    }
}
