using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
namespace BatihanDev.UnityCliCommands.Tests.XR
{
    public sealed class XRTestHapticProvider : MonoBehaviour, IXRHapticImpulseProvider, IXRHapticImpulseChannelGroup, IXRHapticImpulseChannel
    {
        public int Calls;
        public float Amplitude, Duration, Frequency;
        public int channelCount => 1;
        public IXRHapticImpulseChannelGroup GetChannelGroup() => this;
        public IXRHapticImpulseChannel GetChannel(int channel = 0) => channel == 0 ? this : null;
        public bool SendHapticImpulse(float amplitude,float duration,float frequency=0)
        { Calls++;Amplitude=amplitude;Duration=duration;Frequency=frequency;return true; }
        public void Callback() => Calls++;
    }
}
