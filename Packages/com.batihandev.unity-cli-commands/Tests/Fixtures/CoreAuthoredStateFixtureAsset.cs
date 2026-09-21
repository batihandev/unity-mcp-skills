using UnityEngine;

namespace BatihanDev.UnityCliCommands.Tests
{
    public sealed class CoreAuthoredStateFixtureAsset : ScriptableObject
    {
        public int Number;
        public string Text;
        public Object Reference;
        public string TextValue { get; set; }
    }
}
