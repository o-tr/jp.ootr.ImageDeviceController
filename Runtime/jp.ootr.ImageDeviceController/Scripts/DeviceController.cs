using UnityEngine;

namespace jp.ootr.ImageDeviceController
{
    public class DeviceController : FileController
    {
        [SerializeField] public CommonDevice.CommonDevice[] devices = new CommonDevice.CommonDevice[0];

        protected virtual void Start()
        {
            Init();
        }

        private void Init()
        {
            foreach (var device in devices)
            {
                if (device == null) continue;
                device.InitController();
            }
        }
    }
}