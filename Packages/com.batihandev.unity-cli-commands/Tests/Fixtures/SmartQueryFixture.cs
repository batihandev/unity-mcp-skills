using System;
using UnityEngine;
namespace BatihanDev.UnityCliCommands.Tests
{
    public sealed class SmartQueryFixture : MonoBehaviour
    {
        public float Number;
        public string Text;
        public float? Optional;
        public float Throwing => throw new InvalidOperationException("Query getter failed");
    }
}
