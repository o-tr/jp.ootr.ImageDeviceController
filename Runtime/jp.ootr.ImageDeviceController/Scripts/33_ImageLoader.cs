using UnityEngine;
using VRC.SDK3.Image;
using VRC.Udon.Common.Interfaces;
using static jp.ootr.common.ArrayUtils;

namespace jp.ootr.ImageDeviceController
{
    public class ImageLoader : VideoLoader
    {
        private const int IlDelayFrames = 10;
        private bool _ilInited;

        private bool _ilIsLoading;

        private string[] _ilQueuedUrlStrings = new string[0];
        private string _ilSourceUrl;
        private VRCImageDownloader _imageDownloader;
        private TextureInfo _textureInfo;

        private void IlInit()
        {
            _imageDownloader = new VRCImageDownloader();
            _textureInfo = new TextureInfo();
            _textureInfo.GenerateMipMaps = true;
            _textureInfo.WrapModeU = TextureWrapMode.Clamp;
            _textureInfo.WrapModeV = TextureWrapMode.Clamp;
            _textureInfo.AnisoLevel = 16;
        }

        protected virtual void IlLoadImage(string url)
        {
            if (!_ilInited)
            {
                _ilInited = true;
                IlInit();
            }

            if (_ilQueuedUrlStrings.Has(url))
            {
                ConsoleWarn($"[ILLoadImage] already in queue: {url}");
                return;
            }

            _ilQueuedUrlStrings = _ilQueuedUrlStrings.Append(url);
            if (_ilIsLoading)
            {
                ConsoleDebug($"[ILLoadImage] added to queue: {url}");
                return;
            }

            _ilIsLoading = true;
            IlLoadNext();
        }

        /**
         * @private
         * コールバック用にpublicにしているが、外部から直接呼び出さないこと
         */
        public virtual void IlLoadNext()
        {
            if (_ilQueuedUrlStrings.Length == 0)
            {
                _ilIsLoading = false;
                return;
            }

            _ilSourceUrl = _ilQueuedUrlStrings[0];
            ConsoleDebug($"[ILLoadNext] Loading next image: {_ilSourceUrl}");
            _imageDownloader.DownloadImage(UsGetUrl(_ilSourceUrl), null, (IUdonEventReceiver)this, _textureInfo);
        }

        public override void OnImageLoadSuccess(IVRCImageDownload result)
        {
            var texture = result.Result;
            CcSetTexture(_ilSourceUrl, _ilSourceUrl, texture);
            IlOnLoadSuccess(_ilSourceUrl, new[] { _ilSourceUrl });
            _ilQueuedUrlStrings = _ilQueuedUrlStrings.Remove(0);
            SendCustomEventDelayedFrames(nameof(IlLoadNext), IlDelayFrames);
        }

        public override void OnImageLoadError(IVRCImageDownload result)
        {
            ConsoleError(
                $"[ImageLoader] Error loading image: url: {_ilSourceUrl}, error code: {result.Error}, message: {result.ErrorMessage}");
            IlOnLoadError(_ilSourceUrl, ParseImageDownloadError((LoadError)result.Error, result.ErrorMessage));
            _ilQueuedUrlStrings = _ilQueuedUrlStrings.Remove(0);
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