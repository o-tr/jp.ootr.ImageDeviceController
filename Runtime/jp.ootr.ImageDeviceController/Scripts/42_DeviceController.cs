﻿using UnityEngine;

namespace jp.ootr.ImageDeviceController
{
    public class DeviceController : FileController
    {
        [SerializeField] protected internal CommonDevice.CommonDevice[] devices = new CommonDevice.CommonDevice[0];

        public virtual void Start()
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

        public bool IsUuidUsed(string uuid)
        {
            foreach (var device in devices)
                if (device.GetDeviceUuid() == uuid)
                    return true;

            return false;
        }
    }
}
