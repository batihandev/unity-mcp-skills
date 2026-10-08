using BatihanDev.UnityCliCommands.Navigation;
using NUnit.Framework;
using UnityEngine.AI;

namespace BatihanDev.UnityCliCommands.Tests.Navigation
{
    public sealed class NavigationAreaCostContractTests
    {
        private const int Area = 27;
        private float _before;

        [SetUp] public void Capture() => _before = NavMesh.GetAreaCost(Area);
        [TearDown] public void Restore() => NavMesh.SetAreaCost(Area, _before);

        [Test]
        public void InvalidInputAndMissingConfirmationPreserveGlobalCost()
        {
            Assert.That(NavigationAreaCostCommands.Set(-1, 2f, true).Error.Code, Is.EqualTo("AREA_INDEX_INVALID"));
            Assert.That(NavigationAreaCostCommands.Set(Area, float.NaN, true).Error.Code, Is.EqualTo("COST_INVALID"));
            Assert.That(NavigationAreaCostCommands.Set(Area, float.PositiveInfinity, true).Error.Code, Is.EqualTo("COST_INVALID"));
            Assert.That(NavigationAreaCostCommands.Set(Area, -1f, true).Error.Code, Is.EqualTo("COST_INVALID"));
            Assert.That(NavigationAreaCostCommands.Set(Area, 2f).Error.Code, Is.EqualTo("CONFIRMATION_REQUIRED"));
            Assert.That(NavMesh.GetAreaCost(Area), Is.EqualTo(_before));
        }

        [Test]
        public void DryRunWinsOverConfirmationAndReportsObservedPriorCost()
        {
            var preview = NavigationAreaCostCommands.Set(Area, 2f, true, true);
            Assert.That(preview.Ok, Is.True);
            Assert.That(preview.Result.Before, Is.EqualTo(_before));
            Assert.That(preview.Result.After, Is.EqualTo(_before));
            Assert.That(preview.Result.Applied, Is.False);
            Assert.That(NavMesh.GetAreaCost(Area), Is.EqualTo(_before));
        }

        [Test]
        public void ConfirmedWriteReadsBackAndExplicitRestoreSucceeds()
        {
            var requested = _before == 2f ? 3f : 2f;
            var change = NavigationAreaCostCommands.Set(Area, requested, true);
            Assert.That(change.Ok, Is.True);
            Assert.That(change.Result.Before, Is.EqualTo(_before));
            Assert.That(change.Result.After, Is.EqualTo(requested));
            Assert.That(change.Result.Undoable, Is.False);
            var restore = NavigationAreaCostCommands.Set(Area, _before, true);
            Assert.That(restore.Ok, Is.True);
            Assert.That(NavMesh.GetAreaCost(Area), Is.EqualTo(_before));
        }
    }
}
