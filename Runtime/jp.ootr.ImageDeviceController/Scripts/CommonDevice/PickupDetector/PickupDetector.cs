using UdonSharp;
using UnityEngine;

namespace jp.ootr.ImageDeviceController.CommonDevice.PickupDetector
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PickupDetector : UdonSharpBehaviour
    {
        [SerializeField] internal CommonDevice commonDevice;

        public override void OnPickup()
        {
            commonDevice.OnPickup();
        }

        public override void OnDrop()
        {
            commonDevice.OnDrop();
        }
    }
}
