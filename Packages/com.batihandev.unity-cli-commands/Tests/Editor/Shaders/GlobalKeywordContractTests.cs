using BatihanDev.UnityCliCommands.ShaderAuthoring;
using NUnit.Framework;
using UnityEngine;
using System.Linq;

namespace BatihanDev.UnityCliCommands.Tests.ShaderAuthoring
{
    public sealed class GlobalKeywordContractTests
    {
        private const string Target = "BATIH_SHADER3062_TARGET";
        private const string Sentinel = "BATIH_SHADER3062_SENTINEL";
        private bool _targetBefore;
        private bool _sentinelBefore;

        [SetUp]
        public void Capture()
        {
            _targetBefore = Shader.IsKeywordEnabled(Target);
            _sentinelBefore = Shader.IsKeywordEnabled(Sentinel);
        }

        [TearDown]
        public void Restore()
        {
            if (_targetBefore) Shader.EnableKeyword(Target); else Shader.DisableKeyword(Target);
            if (_sentinelBefore) Shader.EnableKeyword(Sentinel); else Shader.DisableKeyword(Sentinel);
        }

        [Test]
        public void RegisteredCommandHasGuardedInputContract()
        {
            var method = typeof(GlobalKeywordCommands).GetMethod("Set");
            var command = method.GetCustomAttributes(false).SingleOrDefault(attribute =>
                attribute.GetType().FullName == "Unity.Pipeline.Commands.CliCommandAttribute");
            Assert.That(command, Is.Not.Null);
            Assert.That((string)command.GetType().GetProperty("Name").GetValue(command),
                Is.EqualTo("shader.global-keyword-set"));
            var arguments = method.GetParameters();
            Assert.That(arguments.Select(argument => argument.Name).ToArray(),
                Is.EqualTo(new[] { "name", "enabled", "confirm", "dryRun" }));
            Assert.That(arguments[1].DefaultValue, Is.EqualTo(true));
            Assert.That(arguments[2].DefaultValue, Is.EqualTo(false));
            Assert.That(arguments[3].DefaultValue, Is.EqualTo(false));
        }

        [Test]
        public void InvalidNamesAndMissingConfirmationLeaveStateUntouched()
        {
            foreach (var name in new[] { null, "", " ", "bad\nname", "bad\u0001name" })
            {
                var invalid = GlobalKeywordCommands.Set(name, !_targetBefore, true);
                Assert.That(invalid.Ok, Is.False);
                Assert.That(invalid.Error.Code, Is.EqualTo("KEYWORD_NAME_INVALID"));
            }
            var unconfirmed = GlobalKeywordCommands.Set(Target, !_targetBefore);
            Assert.That(unconfirmed.Ok, Is.False);
            Assert.That(unconfirmed.Error.Code, Is.EqualTo("CONFIRMATION_REQUIRED"));
            Assert.That(Shader.IsKeywordEnabled(Target), Is.EqualTo(_targetBefore));
        }

        [Test]
        public void DryRunWinsOverConfirmAndReportsActualState()
        {
            var preview = GlobalKeywordCommands.Set(Target, !_targetBefore, true, true);
            Assert.That(preview.Ok, Is.True);
            Assert.That(preview.Result.Before, Is.EqualTo(_targetBefore));
            Assert.That(preview.Result.Requested, Is.EqualTo(!_targetBefore));
            Assert.That(preview.Result.After, Is.EqualTo(_targetBefore));
            Assert.That(preview.Result.Applied, Is.False);
            Assert.That(preview.Result.DryRun, Is.True);
            Assert.That(Shader.IsKeywordEnabled(Target), Is.EqualTo(_targetBefore));
        }

        [Test]
        public void ConfirmedToggleReadsBackAndRestoresWithoutTouchingSentinel()
        {
            var change = GlobalKeywordCommands.Set(Target, !_targetBefore, true);
            Assert.That(change.Ok, Is.True);
            Assert.That(change.Result.After, Is.EqualTo(!_targetBefore));
            Assert.That(change.Result.Applied, Is.True);
            Assert.That(change.Result.Undoable, Is.False);
            Assert.That(Shader.IsKeywordEnabled(Sentinel), Is.EqualTo(_sentinelBefore));
            var restore = GlobalKeywordCommands.Set(Target, _targetBefore, true);
            Assert.That(restore.Ok, Is.True);
            Assert.That(Shader.IsKeywordEnabled(Target), Is.EqualTo(_targetBefore));
        }
    }
}
