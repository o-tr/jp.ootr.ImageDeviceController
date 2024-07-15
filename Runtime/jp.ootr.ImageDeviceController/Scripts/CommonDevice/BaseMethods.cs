using jp.ootr.common;
using UnityEngine;

namespace jp.ootr.ImageDeviceController.CommonDevice
{
    public class BaseMethods : BaseClass
    {
        [SerializeField] public string deviceName;
        [SerializeField] protected Animator animator;
        protected DeviceController Controller;
        protected int DeviceId;
        protected CommonDevice[] Devices;

        public virtual string GetName()
        {
            return deviceName;
        }

        public virtual int GetDeviceId()
        {
            return DeviceId;
        }

        public virtual void InitController(DeviceController controller, int deviceId, CommonDevice[] devices)
        {
            Controller = controller;
            DeviceId = deviceId;
            Devices = devices;
        }

        public virtual bool IsCastableDevice()
        {
            return false;
        }

        public virtual void LoadImage(string source, string fileName, bool shouldPushHistory = false)
        {
        }

        public virtual void ShowScreenName()
        {
        }

        protected override void ConsoleDebug(string message, string prefix = "")
        {
            base.ConsoleDebug(message, $" [<color=#7fffd4>{deviceName}</color>]{prefix}");
        }

        protected override void ConsoleError(string message, string prefix = "")
        {
            base.ConsoleError(message, $" [<color=#7fffd4>{deviceName}</color>]{prefix}");
        }

        protected override void ConsoleWarn(string message, string prefix = "")
        {
            base.ConsoleWarn(message, $" [<color=#7fffd4>{deviceName}</color>]{prefix}");
        }

        protected override void ConsoleLog(string message, string prefix = "")
        {
            base.ConsoleLog(message, $" [<color=#7fffd4>{deviceName}</color>]{prefix}");
        }

        protected override void ConsoleInfo(string message, string prefix = "")
        {
            base.ConsoleInfo(message, $" [<color=#7fffd4>{deviceName}</color>]{prefix}");
        }
    }
}