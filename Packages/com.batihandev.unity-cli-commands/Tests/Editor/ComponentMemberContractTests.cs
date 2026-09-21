using System.Collections;
using BatihanDev.UnityCliCommands.Component;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace BatihanDev.UnityCliCommands.Tests
{
    public sealed class ComponentMemberContractTests
    {
        private GameObject owner;
        private CoreComponentFixture component;
        private string target;

        [SetUp]
        public void SetUp()
        {
            owner = new GameObject("ComponentMemberContract", typeof(CoreComponentFixture));
            component = owner.GetComponent<CoreComponentFixture>();
            target = EntityId.ToULong(component.GetEntityId()).ToString();
            Undo.IncrementCurrentGroup();
        }

        [TearDown]
        public void TearDown()
        {
            UnityObject.DestroyImmediate(owner);
        }

        private static IEnumerable Conversions()
        {
            yield return new TestCaseData("Number", "0.375", 0.375f);
            yield return new TestCaseData("Integer", "12", 12);
            yield return new TestCaseData("Boolean", "1", true);
            yield return new TestCaseData("Boolean", "0", false);
            yield return new TestCaseData("Boolean", "true", true);
            yield return new TestCaseData("Vector2Value", "1,2", new Vector2(1, 2));
            yield return new TestCaseData("Vector3Value", "1,2,3", new Vector3(1, 2, 3));
            yield return new TestCaseData("Vector4Value", "1,2,3,4", new Vector4(1, 2, 3, 4));
            yield return new TestCaseData("Tint", "1,0,0,1", Color.red);
            yield return new TestCaseData("Tint", "#FF0000", Color.red);
            yield return new TestCaseData("Tint", "red", Color.red);
            yield return new TestCaseData("Rotation", "0,90,0", Quaternion.Euler(0, 90, 0));
            yield return new TestCaseData("Rotation", "0,0,0,1", Quaternion.identity);
            yield return new TestCaseData("Mode", "On", FixtureMode.On);
            yield return new TestCaseData("Mode", "FixtureMode.On", FixtureMode.On);
            yield return new TestCaseData("Mode", "BatihanDev.UnityCliCommands.Tests.FixtureMode.On", FixtureMode.On);
            yield return new TestCaseData("Mask", "UI", (LayerMask)(1 << 5));
            yield return new TestCaseData("Mask", "3", (LayerMask)3);
        }

        [TestCaseSource(nameof(Conversions))]
        public void ConvertsPublicPropertyAndRestoresSerializedBackingWithUndo(string member, string value, object expected)
        {
            var property = typeof(CoreComponentFixture).GetProperty(member);
            var before = property.GetValue(component);
            var result = ComponentAuthoringCommands.MemberSet(target, member.ToUpperInvariant(), value);
            Assert.That(result.Ok, Is.True, result.Error?.Code);
            Assert.That(result.Result.Property, Is.EqualTo(member));
            Assert.That(property.GetValue(component), Is.EqualTo(expected));
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert.That(property.GetValue(component), Is.EqualTo(before));
        }

        [TestCase("Immutable")]
        [TestCase("PrivateSetter")]
        [TestCase("StaticValue")]
        [TestCase("Item")]
        [TestCase("Missing")]
        [TestCase("ThrowingGetter")]
        public void RejectsMembersWithoutAPublicWritableInstanceSlot(string member)
        {
            var before = EditorJsonUtility.ToJson(component);
            var result = ComponentAuthoringCommands.MemberSet(target, member, "9");
            Assert.That(result.Ok, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo("MEMBER_NOT_FOUND"));
            Assert.That(component.Immutable, Is.EqualTo(7));
            Assert.That(component.PrivateSetter, Is.EqualTo(5));
            Assert.That(EditorJsonUtility.ToJson(component), Is.EqualTo(before));
        }

        [TestCase("Number", "bad")]
        [TestCase("Boolean", "maybe")]
        [TestCase("Vector3Value", "1,2")]
        [TestCase("Mode", "OtherEnum.On")]
        public void RefusesInvalidConversionWithoutChangingState(string member, string value)
        {
            var before = EditorJsonUtility.ToJson(component);
            var result = ComponentAuthoringCommands.MemberSet(target, member, value);
            Assert.That(result.Ok, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo("VALUE_CONVERSION_FAILED"));
            Assert.That(EditorJsonUtility.ToJson(component), Is.EqualTo(before));
        }

        [Test]
        public void RefusesCaseCollisionAndDefersSerializedFieldToNativeOwner()
        {
            Assert.That(ComponentAuthoringCommands.MemberSet(target, "COLLISION", "4").Error.Code,
                Is.EqualTo("MEMBER_AMBIGUOUS"));
            Assert.That(ComponentAuthoringCommands.MemberSet(target, "PublicSerialized", "4").Error.Code,
                Is.EqualTo("USE_NATIVE_SERIALIZED_OWNER"));
            Assert.That(component.Integer, Is.Zero);
            Assert.That(component.PublicSerialized, Is.Zero);
        }

        [Test]
        public void SetsMutableNonserializedFieldAndAllowsExplicitRestoration()
        {
            var before = component.Transient;
            var result = ComponentAuthoringCommands.MemberSet(target, "TRANSIENT", "9");
            Assert.That(result.Ok, Is.True, result.Error?.Code);
            Assert.That(result.Result.Property, Is.EqualTo("Transient"));
            Assert.That(component.Transient, Is.EqualTo(9));
            var restored = ComponentAuthoringCommands.MemberSet(target, "Transient", before.ToString());
            Assert.That(restored.Ok, Is.True, restored.Error?.Code);
            Assert.That(component.Transient, Is.EqualTo(before));
        }

        [TestCase("linear")]
        [TestCase("easein")]
        [TestCase("easeout")]
        [TestCase("easeinout")]
        [TestCase("constant")]
        public void ConvertsCurveShape(string shape)
        {
            var result = ComponentAuthoringCommands.MemberSet(target, "Curve", shape);
            Assert.That(result.Ok, Is.True, result.Error?.Code);
            Assert.That(component.Curve.length, Is.EqualTo(2));
            Assert.That(component.Curve.Evaluate(0), Is.Zero.Within(0.001f));
            Assert.That(component.Curve.Evaluate(1), Is.EqualTo(shape == "constant" ? 0 : 1).Within(0.001f));
        }

        [Test]
        public void SetsExactObjectReferenceAndExplicitNull()
        {
            var referenceId = EntityId.ToULong(owner.GetEntityId()).ToString();
            var set = ComponentAuthoringCommands.MemberSet(target, "Reference", reference: referenceId);
            Assert.That(set.Ok, Is.True, set.Error?.Code);
            Assert.That(component.Reference, Is.SameAs(owner));
            var cleared = ComponentAuthoringCommands.MemberSet(target, "Reference", reference: "null");
            Assert.That(cleared.Ok, Is.True, cleared.Error?.Code);
            Assert.That(component.Reference, Is.Null);
        }
    }
}
