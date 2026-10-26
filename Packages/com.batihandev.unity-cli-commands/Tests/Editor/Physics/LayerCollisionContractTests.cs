using BatihanDev.UnityCliCommands.Physics3D;
using NUnit.Framework;
using UnityEngine;

namespace BatihanDev.UnityCliCommands.Tests.Physics3D
{
    public sealed class LayerCollisionContractTests
    {
        private bool _before;

        [SetUp]
        public void Capture()
        {
            _before = !UnityEngine.Physics.GetIgnoreLayerCollision(27, 28);
        }

        [TearDown]
        public void Restore()
        {
            UnityEngine.Physics.IgnoreLayerCollision(27, 28, !_before);
        }

        [Test]
        public void InvalidLayerAndMissingConfirmationLeavePairUntouched()
        {
            var invalid = LayerCollisionCommands.Set(-1, 28, !_before, false, true);
            Assert.That(invalid.Ok, Is.False);
            Assert.That(invalid.Error.Code, Is.EqualTo("LAYER_INDEX_INVALID"));
            var unconfirmed = LayerCollisionCommands.Set(27, 28, !_before);
            Assert.That(unconfirmed.Ok, Is.False);
            Assert.That(unconfirmed.Error.Code, Is.EqualTo("CONFIRMATION_REQUIRED"));
            Assert.That(!UnityEngine.Physics.GetIgnoreLayerCollision(27, 28), Is.EqualTo(_before));
        }

        [Test]
        public void DryRunReportsPriorAndRequestedWithoutChangingPair()
        {
            var preview = LayerCollisionCommands.Set(27, 28, !_before, false, true);
            Assert.That(preview.Ok, Is.True);
            Assert.That(preview.Result.Before, Is.EqualTo(_before));
            Assert.That(preview.Result.Requested, Is.EqualTo(!_before));
            Assert.That(preview.Result.After, Is.EqualTo(_before));
            Assert.That(preview.Result.Applied, Is.False);
            Assert.That(!UnityEngine.Physics.GetIgnoreLayerCollision(27, 28), Is.EqualTo(_before));
        }

        [Test]
        public void ConfirmedChangeReadsBackAndCanBeRestored()
        {
            var change = LayerCollisionCommands.Set(27, 28, !_before, true);
            Assert.That(change.Ok, Is.True);
            Assert.That(change.Result.Before, Is.EqualTo(_before));
            Assert.That(change.Result.After, Is.EqualTo(!_before));
            Assert.That(change.Result.Applied, Is.True);
            Assert.That(change.Result.Undoable, Is.False);
            Assert.That(!UnityEngine.Physics.GetIgnoreLayerCollision(27, 28), Is.EqualTo(!_before));
            var restore = LayerCollisionCommands.Set(27, 28, _before, true);
            Assert.That(restore.Ok, Is.True);
            Assert.That(restore.Result.After, Is.EqualTo(_before));
        }
    }
}
