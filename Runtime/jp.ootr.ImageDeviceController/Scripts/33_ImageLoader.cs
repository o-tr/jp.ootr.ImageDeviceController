using JetBrains.Annotations;
using jp.ootr.common;
using UnityEngine;
using VRC.SDK3.Image;
using VRC.Udon.Common.Interfaces;

namespace jp.ootr.ImageDeviceController
{
    public class ImageLoader : VideoLoader
    {
        private const int IlDelayFrames = 10;

        private readonly string[] _imageLoaderPrefixes = { "ImageLoader" };
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

        protected virtual void IlLoadImage([CanBeNull]string url)
        {
            if (!_ilInited)
            {
                _ilInited = true;
                IlInit();
            }
            
            if (url.IsNullOrEmpty())
            {
                ConsoleWarn("url is null", _imageLoaderPrefixes);
                return;
            }

            if (_ilQueuedUrlStrings.Has(url))
            {
                ConsoleWarn($"already in queue: {url}", _imageLoaderPrefixes);
                return;
            }

            _ilQueuedUrlStrings = _ilQueuedUrlStrings.Append(url);
            if (_ilIsLoading)
            {
                ConsoleDebug($"added to queue: {url}", _imageLoaderPrefixes);
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
            ConsoleDebug($"Loading next image: {_ilSourceUrl}", _imageLoaderPrefixes);
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
                $"Error loading image: url: {_ilSourceUrl}, error code: {result.Error}, message: {result.ErrorMessage}",
                _imageLoaderPrefixes);
            IlOnLoadError(_ilSourceUrl, ParseImageDownloadError((LoadError)result.Error, result.ErrorMessage));
            _ilQueuedUrlStrings = _ilQueuedUrlStrings.Remove(0);
            SendCustomEventDelayedFrames(nameof(IlLoadNext), IlDelayFrames);
        }

        protected virtual void IlOnLoadSuccess([CanBeNull]string source, [CanBeNull]string[] fileNames)
        {
            ConsoleError("IlOnLoadSuccess should not be called from base class", _imageLoaderPrefixes);
        }

        protected virtual void IlOnLoadError([CanBeNull]string source, LoadError error)
        {
            ConsoleError("IlOnLoadError should not be called from base class", _imageLoaderPrefixes);
        }
    }
}
