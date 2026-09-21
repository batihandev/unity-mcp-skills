using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BatihanDev.UnityCliCommands.ScriptableObject;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BatihanDev.UnityCliCommands.Tests
{
    public sealed class ScriptableObjectMemberContractTests
    {
        private const string Root = "Assets/ScriptableObjectMemberContractTests";
        private ScriptableObjectMemberFixtureAsset asset;
        private string assetPath;

        [SetUp]
        public void SetUp()
        {
            if (!AssetDatabase.IsValidFolder(Root))
                AssetDatabase.CreateFolder("Assets", "ScriptableObjectMemberContractTests");
            assetPath = Root + "/Target.asset";
            asset = UnityEngine.ScriptableObject.CreateInstance<ScriptableObjectMemberFixtureAsset>();
            asset.Number = 1;
            asset.Text = "before";
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            Undo.IncrementCurrentGroup();
        }

        [TearDown]
        public void TearDown()
        {
            ScriptableObjectMemberFixtureAsset.StaticValue = 0;
            AssetDatabase.DeleteAsset(Root);
            AssetDatabase.Refresh();
        }

        [Test]
        public void MemberSetAcceptsMutableSerializedFieldAndPublicProperty()
        {
            var field = ScriptableObjectAuthoringCommands.MemberSet(assetPath, "Number", "8", confirm: true);
            var property = ScriptableObjectAuthoringCommands.MemberSet(assetPath, "PublicProperty", "9", confirm: true);

            Assert.That(field.Ok, Is.True, field.Error?.Code);
            Assert.That(property.Ok, Is.True, property.Error?.Code);
            Assert.That(asset.Number, Is.EqualTo(8));
            Assert.That(asset.PublicProperty, Is.EqualTo(9));
        }

        [TestCase("PrivateSetter")]
        [TestCase("ReadOnly")]
        [TestCase("Item")]
        [TestCase("StaticValue")]
        [TestCase("Missing")]
        [TestCase("number")]
        public void MemberSetRefusesNonPublicWritableOrNonExactMembers(string member)
        {
            var result = ScriptableObjectAuthoringCommands.MemberSet(assetPath, member, "9", confirm: true);

            Assert.That(result.Ok, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo("MEMBER_NOT_FOUND"));
            Assert.That(asset.Number, Is.EqualTo(1));
            Assert.That(asset.ReadOnly, Is.EqualTo(7));
            Assert.That(asset.PrivateSetter, Is.EqualTo(5));
        }

        [Test]
        public void FieldsSetDryRunAndMissingConfirmationLeaveTheAssetUnchanged()
        {
            var dryRun = FieldsSet(assetPath, true, false, Update("Number", "8"), Update("Text", "after"));
            AssertBatchOk(dryRun);
            Assert.That(asset.Number, Is.EqualTo(1));
            Assert.That(asset.Text, Is.EqualTo("before"));

            var refused = FieldsSet(assetPath, false, false, Update("Number", "8"));
            AssertBatchError(refused, "CONFIRMATION_REQUIRED");
            Assert.That(asset.Number, Is.EqualTo(1));
        }

        [Test]
        public void FieldsSetRefusesNullUpdatesBeforeMutation()
        {
            var result = InvokeFieldsSet(assetPath, null, false, true);

            AssertBatchError(result, "UPDATES_REQUIRED");
            Assert.That(asset.Number, Is.EqualTo(1));
            Assert.That(asset.Text, Is.EqualTo("before"));
        }

        [Test]
        public void FieldsSetRefusesEmptyUpdatesBeforeMutation()
        {
            var updateType = BatchMethod().GetParameters()[1].ParameterType.GetElementType();
            var result = InvokeFieldsSet(assetPath, Array.CreateInstance(updateType, 0), false, true);

            AssertBatchError(result, "UPDATES_REQUIRED");
            Assert.That(asset.Number, Is.EqualTo(1));
            Assert.That(asset.Text, Is.EqualTo("before"));
        }

        [Test]
        public void FieldsSetReportsSequentialSuccessFailureSuccessWithoutBlockingLaterItems()
        {
            var result = FieldsSet(assetPath, false, true,
                Update("Number", "8"), Update("Missing", "ignored"), Update("Text", "after"));

            AssertBatchOk(result);
            Assert.That(Outcomes(result), Has.Count.EqualTo(3));
            AssertOutcome(Outcomes(result)[0], "Number", true, true, null);
            AssertOutcome(Outcomes(result)[1], "Missing", false, false, "MEMBER_NOT_FOUND");
            AssertOutcome(Outcomes(result)[2], "Text", true, true, null);
            Assert.That(Value(Result(result), "Total"), Is.EqualTo(3));
            Assert.That(Value(Result(result), "Succeeded"), Is.EqualTo(2));
            Assert.That(Value(Result(result), "Applied"), Is.EqualTo(2));
            Assert.That(Value(Result(result), "Failed"), Is.EqualTo(1));
            Assert.That(asset.Number, Is.EqualTo(8));
            Assert.That(asset.Text, Is.EqualTo("after"));
        }

        [Test]
        public void FieldsSetAppliesDuplicateFieldUpdatesInInputOrder()
        {
            var result = FieldsSet(assetPath, false, true, Update("Number", "4"), Update("Number", "9"));

            AssertBatchOk(result);
            AssertOutcome(Outcomes(result)[0], "Number", true, true, null);
            AssertOutcome(Outcomes(result)[1], "Number", true, true, null);
            Assert.That(asset.Number, Is.EqualTo(9));
        }

        [Test]
        public void FieldsSetRefusesPropertiesAndContinuesWithMutableFields()
        {
            var result = FieldsSet(assetPath, false, true, Update("PublicProperty", "9"), Update("Number", "8"));

            AssertBatchOk(result);
            AssertOutcome(Outcomes(result)[0], "PublicProperty", false, false, "MEMBER_NOT_FOUND");
            AssertOutcome(Outcomes(result)[1], "Number", true, true, null);
            Assert.That(asset.PublicProperty, Is.Zero);
            Assert.That(asset.Number, Is.EqualTo(8));
        }

        [Test]
        public void FieldsSetAcceptsExactObjectReferencesAndRefusesWrongReferenceTypes()
        {
            var materialPath = Root + "/Reference.mat";
            var texturePath = Root + "/ReferenceTexture.asset";
            var material = new Material(Shader.Find("Standard"));
            var texture = new Texture2D(2, 2);
            AssetDatabase.CreateAsset(material, materialPath);
            AssetDatabase.CreateAsset(texture, texturePath);
            AssetDatabase.SaveAssets();

            var result = FieldsSet(assetPath, false, true,
                Update("Reference", null, materialPath), Update("MaterialReference", null, materialPath),
                Update("MaterialReference", null, texturePath));

            AssertBatchOk(result);
            AssertOutcome(Outcomes(result)[0], "Reference", true, true, null);
            AssertOutcome(Outcomes(result)[1], "MaterialReference", true, true, null);
            AssertOutcome(Outcomes(result)[2], "MaterialReference", false, false, "VALUE_CONVERSION_FAILED");
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var reloaded = AssetDatabase.LoadAssetAtPath<ScriptableObjectMemberFixtureAsset>(assetPath);
            AssertSamePersistentAsset(material, reloaded.Reference);
            AssertSamePersistentAsset(material, reloaded.MaterialReference);
        }

        private static void AssertSamePersistentAsset(Material expected, UnityEngine.Object actual)
        {
            Assert.That(actual, Is.TypeOf<Material>());
            Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(expected, out string expectedGuid, out long expectedLocalId), Is.True);
            Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(actual, out string actualGuid, out long actualLocalId), Is.True);
            Assert.That(actualGuid, Is.EqualTo(expectedGuid));
            Assert.That(actualLocalId, Is.EqualTo(expectedLocalId));
        }

        [Test]
        public void FieldsSetAllowsTransientFieldsWithAnExplicitInverse()
        {
            var changed = FieldsSet(assetPath, false, true, Update("Transient", "8"));
            AssertBatchOk(changed);
            AssertOutcome(Outcomes(changed)[0], "Transient", true, true, null);
            Assert.That(asset.Transient, Is.EqualTo(8));

            var restored = FieldsSet(assetPath, false, true, Update("Transient", "0"));
            AssertBatchOk(restored);
            Assert.That(asset.Transient, Is.Zero);
        }

        [Test]
        public void FieldsSetContinuesAfterMalformedAndNullUpdates()
        {
            var result = FieldsSet(assetPath, false, true,
                Update("Number", "not-a-number"), null, Update("Text", "after"));

            AssertBatchOk(result);
            AssertOutcome(Outcomes(result)[0], "Number", false, false, "VALUE_CONVERSION_FAILED");
            Assert.That(Value(Outcomes(result)[1], "Success"), Is.EqualTo(false));
            Assert.That(Value(Outcomes(result)[1], "Applied"), Is.EqualTo(false));
            Assert.That(Value(Outcomes(result)[1], "ErrorCode"), Is.Not.Empty);
            AssertOutcome(Outcomes(result)[2], "Text", true, true, null);
            Assert.That(asset.Number, Is.EqualTo(1));
            Assert.That(asset.Text, Is.EqualTo("after"));
        }

        [Test]
        public void FieldsSetRecordsOneUndoThatRestoresSerializedFieldWrites()
        {
            var result = FieldsSet(assetPath, false, true, Update("Number", "8"), Update("Text", "after"));
            AssertBatchOk(result);
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();

            Assert.That(asset.Number, Is.EqualTo(1));
            Assert.That(asset.Text, Is.EqualTo("before"));
        }

        [Test]
        public void FieldsSetSavesItsAssetWithoutSavingAnUnrelatedDirtyAsset()
        {
            var otherPath = Root + "/Other.asset";
            var other = UnityEngine.ScriptableObject.CreateInstance<ScriptableObjectMemberFixtureAsset>();
            other.Number = 1;
            AssetDatabase.CreateAsset(other, otherPath);
            AssetDatabase.SaveAssets();
            other.Number = 13;
            EditorUtility.SetDirty(other);

            var result = FieldsSet(assetPath, false, true, Update("Number", "8"));
            AssertBatchOk(result);

            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            StringAssert.Contains("Number: 8", File.ReadAllText(Path.Combine(projectRoot, assetPath)));
            StringAssert.Contains("Number: 1", File.ReadAllText(Path.Combine(projectRoot, otherPath)));
            Assert.That(EditorUtility.IsDirty(other), Is.True);
        }

        private static MethodInfo BatchMethod()
        {
            return typeof(ScriptableObjectAuthoringCommands).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .SingleOrDefault(method => method.GetCustomAttributes(false).Any(attribute =>
                    attribute.GetType().FullName == "Unity.Pipeline.Commands.CliCommandAttribute" &&
                    (string)attribute.GetType().GetProperty("Name").GetValue(attribute) == "scriptableobject.fields-set"));
        }

        private static object FieldsSet(string path, bool dryRun, bool confirm, params UpdateInput[] inputs)
        {
            var method = BatchMethod();
            Assert.That(method, Is.Not.Null, "The scriptableobject.fields-set command must be registered.");
            var parameters = method.GetParameters();
            Assert.That(parameters.Select(parameter => parameter.ParameterType).ToArray(), Is.EqualTo(new[]
            {
                typeof(string), parameters[1].ParameterType, typeof(bool), typeof(bool)
            }));
            Assert.That(parameters[1].ParameterType.IsArray, Is.True);
            var updates = Array.CreateInstance(parameters[1].ParameterType.GetElementType(), inputs.Length);
            for (var index = 0; index < inputs.Length; index++)
            {
                if (inputs[index] == null)
                {
                    updates.SetValue(null, index);
                    continue;
                }
                var update = Activator.CreateInstance(parameters[1].ParameterType.GetElementType());
                Set(update, "Name", inputs[index].Name);
                Set(update, "Value", inputs[index].Value);
                Set(update, "Reference", inputs[index].Reference);
                updates.SetValue(update, index);
            }
            return InvokeFieldsSet(path, updates, dryRun, confirm);
        }

        private static object InvokeFieldsSet(string path, Array updates, bool dryRun, bool confirm)
        {
            var method = BatchMethod();
            Assert.That(method, Is.Not.Null, "The scriptableobject.fields-set command must be registered.");
            return method.Invoke(null, new object[] { path, updates, dryRun, confirm });
        }

        private static UpdateInput Update(string name, string value, string reference = null) =>
            new UpdateInput { Name = name, Value = value, Reference = reference };

        private static void AssertBatchOk(object command)
        {
            Assert.That(Value(command, "Ok"), Is.EqualTo(true), ErrorCode(command));
        }

        private static void AssertBatchError(object command, string code)
        {
            Assert.That(Value(command, "Ok"), Is.EqualTo(false));
            Assert.That(ErrorCode(command), Is.EqualTo(code));
        }

        private static object Result(object command) => Value(command, "Result");

        private static List<object> Outcomes(object command) => ((IEnumerable)Value(Result(command), "Results")).Cast<object>().ToList();

        private static void AssertOutcome(object outcome, string name, bool success, bool applied, string errorCode)
        {
            Assert.That(Value(outcome, "Name"), Is.EqualTo(name));
            Assert.That(Value(outcome, "Success"), Is.EqualTo(success));
            Assert.That(Value(outcome, "Applied"), Is.EqualTo(applied));
            Assert.That(Value(outcome, "ErrorCode"), Is.EqualTo(errorCode));
        }

        private static string ErrorCode(object command)
        {
            var error = Value(command, "Error");
            return error == null ? null : (string)Value(error, "Code");
        }

        private static object Value(object target, string name)
        {
            Assert.That(target, Is.Not.Null);
            var property = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null, target.GetType().FullName + " must expose " + name + ".");
            return property.GetValue(target);
        }

        private static void Set(object target, string name, object value)
        {
            var property = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null, target.GetType().FullName + " must expose " + name + ".");
            property.SetValue(target, value);
        }

        private sealed class UpdateInput
        {
            public string Name;
            public string Value;
            public string Reference;
        }
    }
}
