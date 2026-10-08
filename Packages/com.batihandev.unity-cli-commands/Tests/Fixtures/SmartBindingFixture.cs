using System;
using System.Collections.Generic;
using UnityEngine;
namespace BatihanDev.UnityCliCommands.Tests
{
    public sealed class SmartBindingFixture : MonoBehaviour
    {
        public GameObject External;
        public int Marker;
        public GameObject[] Objects = Array.Empty<GameObject>();
        public List<Light> Lights = new List<Light>();
        public GameObject[] m_Aliased = Array.Empty<GameObject>();
        public GameObject[] _other = Array.Empty<GameObject>();
        [NonSerialized] public GameObject[] VolatileField = Array.Empty<GameObject>();
        [SerializeField] private GameObject[] backing = Array.Empty<GameObject>();
        [NonSerialized] public int GetterCalls;
        public GameObject[] Backed { get { GetterCalls++; return backing; } set { backing = value; } }
        public GameObject[] Volatile { get; set; } = Array.Empty<GameObject>();
        public GameObject[] ThrowGetter { get { GetterCalls++; throw new InvalidOperationException("Getter failed"); } set { backing = value; } }
        public GameObject[] ThrowSetter { get { return backing; } set { backing = value; throw new InvalidOperationException("Setter failed after target write"); } }
        public GameObject[] MutatingGetter { get { GetterCalls++; Marker++; backing = Array.Empty<GameObject>(); Objects = Array.Empty<GameObject>(); Lights.Clear(); return backing; } set { backing = value; } }
        public GameObject[] MutatingThrowGetter { get { GetterCalls++; Marker++; backing = Array.Empty<GameObject>(); Objects = Array.Empty<GameObject>(); Lights.Clear(); throw new InvalidOperationException("Getter failed after target write"); } set { backing = value; } }
        public GameObject[] ReferenceOnlyGetter { get { GetterCalls++; External = null; return backing; } set { backing = value; } }
        public GameObject[] ReferenceOnlySetter { get { return backing; } set { External = value[0]; } }
        public GameObject[] ExternalThrowSetter { get { return backing; } set { backing = value; Marker++; Objects = Array.Empty<GameObject>(); Lights.Clear(); External.name = "External setter effect"; throw new InvalidOperationException("Setter failed after external write"); } }
        public GameObject[] Readonly => backing;
        public GameObject[] PrivateGetter { private get { GetterCalls++; return backing; } set { backing = value; } }
        public static GameObject[] Static { get; set; }
        public GameObject[] this[int index] { get { return backing; } set { backing = value; } }
        public GameObject[] ReadBacking() => backing;
    }
}
