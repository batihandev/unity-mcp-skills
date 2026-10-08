using System;
using System.Collections.Generic;
using UnityEngine;
namespace BatihanDev.UnityCliCommands.Tests
{
    public sealed class SceneContextReferenceFixture : MonoBehaviour
    {
        public int Number = 37;
        public string Text = "fixture | <value>";
        public UnityEngine.Object Assigned;
        public UnityEngine.Object OtherAssigned;
        public UnityEngine.Object LegitimateNull;
        public Vector3 Offset = new Vector3(1, 2, 3);
        [SerializeField] private SceneContextGraphB privateDependency;
        public SceneContextGraphB[] ArrayDependency;
        public List<SceneContextGraphC> GenericDependency;
        public static int GetterCalls;
        public int DangerousGetter { get { GetterCalls++; throw new InvalidOperationException("Getter must not execute."); } }
        private int PrivateProperty { get { GetterCalls++; return 9; } }
        private void PrivateMethod() { }
        private void Awake() { }
    }
    [Serializable] public class SceneContextGraphBase { public SceneContextGraphC Inherited; }
    [Serializable] public class SceneContextGraphA : SceneContextGraphBase
    {
        public SceneContextGraphB[] Array;
        public List<SceneContextGraphC> Generic;
        [SerializeField] private SceneContextGraphB PrivateSerialized;
        private SceneContextGraphD PrivateUnserialized;
        public static SceneContextGraphD PublicStatic;
        [SerializeField] private static SceneContextGraphE PrivateStatic;
        public SceneContextGraphA Self;
    }
    [Serializable] public class SceneContextGraphB { public SceneContextGraphC Forward; }
    [Serializable] public class SceneContextGraphC { public SceneContextGraphA Cycle; }
    [Serializable] public class SceneContextGraphD { public SceneContextGraphA Incoming; }
    [Serializable] public class SceneContextGraphE { }
    public class ScriptAnalysisPlainFixture
    {
        public int Public;
        [SerializeField] private int Private;
        public static int Static;
        public readonly int ReadOnly;
        [NonSerialized] public int Excluded;
        public int Getter { get { SceneContextReferenceFixture.GetterCalls++; throw new Exception(); } }
        private int HiddenProperty { get { SceneContextReferenceFixture.GetterCalls++; return 1; } }
        public void Method() { }
        private void HiddenMethod() { }
    }
}
namespace ScriptAnalysisCollisionOne { public class ScriptAnalysisCollisionFixture { } }
namespace ScriptAnalysisCollisionTwo { public class ScriptAnalysisCollisionFixture { } }
namespace Microsoft.ScriptAnalysisFixtures { public class ScriptAnalysisInspectOnlyFixture { } }
