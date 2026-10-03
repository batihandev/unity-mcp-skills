using UnityEngine;
namespace BatihanDev.UnityCliCommands.Tests
{
    [ExecuteAlways]
    public sealed class PrefabHiddenChildFixture : MonoBehaviour
    {
        private void OnEnable()
        {
            var child = transform.Find("HiddenMissingChild");
            if (child != null) child.gameObject.hideFlags = HideFlags.HideInHierarchy;
        }
    }
}
