using jp.ootr.common;
using UnityEngine;
using UnityEngine.UI;

namespace jp.ootr.ImageDeviceController.CommonDevice
{
    public class BaseMethods : BaseClass
    {
        [SerializeField] public string deviceName;
        [SerializeField] public Texture2D deviceIcon;
        [SerializeField] protected Animator animator;
        [SerializeField] public CommonDevice[] devices;
        [SerializeField] protected internal DeviceController controller;
        [SerializeField] internal RawImage splashImage;
        [SerializeField] internal AspectRatioFitter splashImageFitter;
        [SerializeField] internal Texture2D splashImageTexture;
        
        public string deviceUuid;

        private void Start()
        {
            if (splashImage == null) return;
            splashImage.texture = splashImageTexture;
            if (splashImageTexture == null || splashImageFitter == null) return;
            splashImageFitter.aspectRatio = (float)splashImageTexture.width / splashImageTexture.height;
        }

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

        protected override void ConsoleDebug(string message, string[] prefix = null)
        {
            base.ConsoleDebug(message, LogBuilder.CombinePrefix(new[] { deviceName }, prefix));
        }

        protected override void ConsoleError(string message, string[] prefix = null)
        {
            base.ConsoleError(message, LogBuilder.CombinePrefix(new[] { deviceName }, prefix));
        }

        protected override void ConsoleWarn(string message, string[] prefix = null)
        {
            base.ConsoleWarn(message, LogBuilder.CombinePrefix(new[] { deviceName }, prefix));
        }

        protected override void ConsoleLog(string message, string[] prefix = null)
        {
            base.ConsoleLog(message, LogBuilder.CombinePrefix(new[] { deviceName }, prefix));
        }

        protected override void ConsoleInfo(string message, string[] prefix = null)
        {
            base.ConsoleInfo(message, LogBuilder.CombinePrefix(new[] { deviceName }, prefix));
        }
    }
}
