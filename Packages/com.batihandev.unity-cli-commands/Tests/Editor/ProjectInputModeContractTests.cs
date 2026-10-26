using System.Linq;
using BatihanDev.UnityCliCommands.Analysis;
using NUnit.Framework;
using UnityEditor;
namespace BatihanDev.UnityCliCommands.Tests.Analysis
{
    public sealed class ProjectInputModeContractTests : SmartFixtureSupport
    {
        [Test]
        public void StackReportsActualPublicSerializedInputSettingAndSource()
        {
            const string path = "ProjectSettings/ProjectSettings.asset";
            var owner = AssetDatabase.LoadAllAssetsAtPath(path).OfType<PlayerSettings>().FirstOrDefault();
            Assert.That(owner, Is.Not.Null, "Public PlayerSettings asset prerequisite");
            using (var serialized = new SerializedObject(owner))
            {
                var property = serialized.FindProperty("activeInputHandler"); Assert.That(property, Is.Not.Null, "Public serialized setting prerequisite"); Assert.That(property.propertyType, Is.EqualTo(SerializedPropertyType.Integer));
                var value = property.intValue; var expected = value == 0 ? "LegacyInputManager" : value == 1 ? "InputSystem" : value == 2 ? "Both" : "UnknownSetting:" + value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var report = ProjectAnalysisCommands.Stack(); Assert.That(report.Ok, Is.True); Assert.That(report.Result.InputMode, Is.EqualTo(expected)); Assert.That(report.Result.InputModeSource, Is.EqualTo(path + ":activeInputHandler"));
                Assert.That(report.Result.LegacyInputManagerAvailable, Is.EqualTo(value == 0 || value == 2 ? (bool?)true : value == 1 ? (bool?)false : null));
                Assert.That(serialized.FindProperty("activeInputHandler").intValue, Is.EqualTo(value));
            }
        }
    }
}
