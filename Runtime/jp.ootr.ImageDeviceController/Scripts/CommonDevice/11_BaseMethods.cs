﻿using JetBrains.Annotations;
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
        [SerializeField] [CanBeNull] internal RawImage splashImage;
        [SerializeField] [CanBeNull] internal AspectRatioFitter splashImageFitter;
        [SerializeField] [CanBeNull] internal Texture2D splashImageTexture;

        public string deviceUuid;

        protected virtual void Start()
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

        public virtual void LoadImage([CanBeNull] string sourceUrl, [CanBeNull] string fileUrl,
            bool shouldPushHistory = false)
        {
        }

        public virtual void ShowScreenName()
        {
        }

        public virtual void OnFileLoadSuccess([CanBeNull] string source, [CanBeNull] string fileUrl, string channel) {}
        
        public virtual void OnFileLoadError([CanBeNull] string source, [CanBeNull] string fileUrl, string channel, LoadError error) {}

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
