using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace jp.ootr.ImageDeviceController.CommonDevice.PickupDetector
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PickupDetector : UdonSharpBehaviour
    {
        [SerializeField][CanBeNull] internal CommonDevice commonDevice;

        public override void OnPickup()
        {
            if (commonDevice == null) return;
            commonDevice.OnPickup();
        }

        public override void OnDrop()
        {
            if (commonDevice == null) return;
            commonDevice.OnDrop();
        }
    }
}
