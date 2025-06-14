using JetBrains.Annotations;
using jp.ootr.common;
using UnityEngine;
using VRC.SDK3.Image;
using VRC.Udon.Common.Interfaces;

namespace jp.ootr.ImageDeviceController
{
    public class ImageSourceLoader : VideoSourceLoader
    {
        private const int IlDelayFrames = 10;

        private readonly string[] _imageLoaderPrefixes = { "ImageLoader" };
        private bool _ilInited;

        private bool _ilIsLoading;

        [ItemNotNull] private string[] _ilQueuedSourceUrls = new string[0];
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

        protected virtual void IlLoadImage([CanBeNull] string sourceUrl)
        {
            if (!_ilInited)
            {
                _ilInited = true;
                IlInit();
            }

            if (sourceUrl.IsNullOrEmpty())
            {
                ConsoleWarn("source is null", _imageLoaderPrefixes);
                return;
            }

            if (!sourceUrl.IsValidUrl())
            {
                ConsoleWarn($"invalid source: {sourceUrl}", _imageLoaderPrefixes);
                return;
            }

            if (_ilQueuedSourceUrls.Has(sourceUrl))
            {
                ConsoleWarn($"already in queue: {sourceUrl}", _imageLoaderPrefixes);
                return;
            }

            _ilQueuedSourceUrls = _ilQueuedSourceUrls.Append(sourceUrl);
            if (_ilIsLoading)
            {
                ConsoleDebug($"added to queue: {sourceUrl}", _imageLoaderPrefixes);
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
            if (_ilQueuedSourceUrls.Length == 0 || _ilQueuedSourceUrls[0].IsNullOrEmpty())
            {
                _ilIsLoading = false;
                return;
            }

            _ilSourceUrl = _ilQueuedSourceUrls[0];
            ConsoleDebug($"Loading next image: {_ilSourceUrl}", _imageLoaderPrefixes);
            _imageDownloader.DownloadImage(UsGetUrl(_ilSourceUrl), null, (IUdonEventReceiver)this, _textureInfo);
        }

        public override void OnImageLoadSuccess(IVRCImageDownload result)
        {
            var texture = result.Result;
            var fileName = $"{PROTOCOL_IMAGE}://{_ilSourceUrl.Substring(8)}";
            CcSetTexture(_ilSourceUrl, fileName, texture);
            OnSourceLoadSuccess(_ilSourceUrl, new[] { fileName });
            _ilQueuedSourceUrls = _ilQueuedSourceUrls.Remove(0);
            SendCustomEventDelayedFrames(nameof(IlLoadNext), IlDelayFrames);
        }

        public override void OnImageLoadError(IVRCImageDownload result)
        {
            ConsoleError(
                $"Error loading image: source: {_ilSourceUrl}, error code: {result.Error}, message: {result.ErrorMessage}",
                _imageLoaderPrefixes);
            OnSourceLoadError(_ilSourceUrl, ParseImageDownloadError((LoadError)result.Error, result.ErrorMessage));
            _ilQueuedSourceUrls = _ilQueuedSourceUrls.Remove(0);
            SendCustomEventDelayedFrames(nameof(IlLoadNext), IlDelayFrames);
        }
    }
}
