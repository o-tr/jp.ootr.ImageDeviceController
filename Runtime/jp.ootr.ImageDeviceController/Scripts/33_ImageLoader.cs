using UnityEngine;
using VRC.SDK3.Image;
using VRC.Udon.Common.Interfaces;
using static jp.ootr.common.ArrayUtils;

namespace jp.ootr.ImageDeviceController
{
    public class ImageLoader : VideoLoader
    {
        protected const int IlDelayFrames = 10;
        protected bool IlInited;

        protected bool IlIsLoading;

        protected string[] IlQueuedUrlStrings = new string[0];
        protected string IlSourceUrl;
        protected VRCImageDownloader ImageDownloader;
        protected TextureInfo TextureInfo;

        private void IlInit()
        {
            ImageDownloader = new VRCImageDownloader();
            TextureInfo = new TextureInfo();
            TextureInfo.GenerateMipMaps = true;
            TextureInfo.WrapModeU = TextureWrapMode.Clamp;
            TextureInfo.WrapModeV = TextureWrapMode.Clamp;
            TextureInfo.AnisoLevel = 16;
        }

        public virtual void IlLoadImage(string url)
        {
            if (!IlInited)
            {
                IlInited = true;
                IlInit();
            }

            if (IlQueuedUrlStrings.Has(url))
            {
                ConsoleWarn($"[ILLoadImage] already in queue: {url}");
                return;
            }

            IlQueuedUrlStrings = IlQueuedUrlStrings.Append(url);
            if (IlIsLoading)
            {
                ConsoleDebug($"[ILLoadImage] added to queue: {url}");
                return;
            }

            IlIsLoading = true;
            IlLoadNext();
        }

        public virtual void IlLoadNext()
        {
            if (IlQueuedUrlStrings.Length == 0)
            {
                IlIsLoading = false;
                return;
            }

            IlSourceUrl = IlQueuedUrlStrings[0];
            ConsoleDebug($"[ILLoadNext] Loading next image: {IlSourceUrl}");
            ImageDownloader.DownloadImage(UsGetUrl(IlSourceUrl), null, (IUdonEventReceiver)this, TextureInfo);
        }

        public override void OnImageLoadSuccess(IVRCImageDownload result)
        {
            var texture = result.Result;
            CcSetTexture(IlSourceUrl, IlSourceUrl, texture);
            IlOnLoadSuccess(IlSourceUrl, new[] { IlSourceUrl });
            IlQueuedUrlStrings = IlQueuedUrlStrings.Remove(0);
            SendCustomEventDelayedFrames(nameof(IlLoadNext), IlDelayFrames);
        }

        public override void OnImageLoadError(IVRCImageDownload result)
        {
            ConsoleError(
                $"[ImageLoader] Error loading image: url: {IlSourceUrl}, error code: {result.Error}, message: {result.ErrorMessage}");
            IlOnLoadError(IlSourceUrl, ParseImageDownloadError((LoadError)result.Error, result.ErrorMessage));
            IlQueuedUrlStrings = IlQueuedUrlStrings.Remove(0);
            SendCustomEventDelayedFrames(nameof(IlLoadNext), IlDelayFrames);
        }

        protected virtual void IlOnLoadSuccess(string source, string[] fileNames)
        {
            ConsoleError("ZipLoader: ZipOnLoadSuccess should not be called from base class");
        }

        protected virtual void IlOnLoadError(string source, LoadError error)
        {
            ConsoleError("ZipLoader: ZipOnLoadError should not be called from base class");
        }
    }
}