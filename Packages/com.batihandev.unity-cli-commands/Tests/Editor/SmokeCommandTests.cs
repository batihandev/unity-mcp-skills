using BatihanDev.UnityCliCommands.Foundation;
using NUnit.Framework;

namespace BatihanDev.UnityCliCommands.Tests
{
    public sealed class SmokeCommandTests
    {
        [Test]
        public void SmokeCommandReturnsVersionedReadyResult()
        {
            var result = FoundationCommands.Smoke();

            Assert.That(result.Schema, Is.EqualTo("unity.foundation.compatibility@1"));
            Assert.That(result.Ok, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Result.PackageId, Is.EqualTo("com.batihandev.unity-cli-commands"));
            Assert.That(result.Result.PackageVersion, Is.EqualTo("0.1.0"));
            Assert.That(result.Result.Status, Is.EqualTo("ready"));
            Assert.That(result.Result.UnityVersion, Is.EqualTo("6000.6.2f1"));
            Assert.That(result.Result.PipelineVersion, Is.EqualTo("0.7.0-exp.1"));
            Assert.That(result.Result.InputSystemVersion, Is.EqualTo("1.20.0"));
        }
    }
}
