using System;
using UnityEngine;
using UnityEngine.Events;

namespace BatihanDev.UnityCliCommands.Tests
{
    [Serializable] public sealed class EventInt : UnityEvent<int> { }

    public class EventProbeBase : MonoBehaviour
    {
        public UnityEvent Inherited = new UnityEvent();
    }

    public sealed class EventProbe : EventProbeBase
    {
        public UnityEvent Event = new UnityEvent();
        public EventInt Generic = new EventInt();
        public UnityEvent NullField = new UnityEvent();
        public UnityEvent NullProperty => null;
        [SerializeField] private UnityEvent propertyEvent = new UnityEvent();
        public UnityEvent PropertyEvent => propertyEvent;
        public UnityEvent Computed => new UnityEvent();
        public int this[int index] => index;
        public int Calls;
        public int IntValue;
        public float FloatValue;
        public string StringValue;
        public bool BoolValue;
        public int SetValue { get; set; }
        public void OnVoid() => Calls++;
        public void OnInt(int value) => IntValue = value;
        public void OnFloat(float value) => FloatValue = value;
        public void OnString(string value) => StringValue = value;
        public void OnBool(bool value) => BoolValue = value;
        public void OnOverload() => Calls++;
        public void OnOverload(int value) => IntValue = value;
        public void Throw() { Calls++; throw new InvalidOperationException("Event probe throw"); }
        public int WrongReturn() => 0;
        private void PrivateCallback() { }
    }
}
