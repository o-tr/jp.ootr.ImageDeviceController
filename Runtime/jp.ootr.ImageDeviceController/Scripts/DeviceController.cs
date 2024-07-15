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
            for (var i = 0; i < devices.Length; i++)
            {
                var device = devices[i];
                if (device == null) continue;
                device.InitController(this, i, devices);
            }
        }
    }
}