using jp.ootr.common;
using UnityEngine;

namespace jp.ootr.ImageDeviceController.CommonDevice
{
    public class BaseMethods : BaseClass
    {
        [SerializeField] public string deviceName;
        [SerializeField] public Texture2D deviceIcon;
        [SerializeField] protected Animator animator;
        [SerializeField] public CommonDevice[] devices;
        [SerializeField] public DeviceController controller;

        public string deviceUuid = null;

        public virtual string GetName()
        {
            return deviceName;
        }

        public virtual string GetDeviceUuid()
        {
            return deviceUuid;
        }

        public virtual void InitController()
        {
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