using jp.ootr.common;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Rendering;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace jp.ootr.ImageDeviceController
{
    public class VideoLoader : ZipLoader
    {
        [SerializeField] protected VRCAVProVideoPlayer vlVideoPlayer;
        [SerializeField] protected MeshRenderer vlVideoRenderer;
        [SerializeField] protected float vlLoadTimeout = 5;

        private const float VlDelaySeconds = 0.05f;
        private float _vlCurrentTime;
        private float _vlDuration;

        private string[] _vlFilenames;
        private float _vlInterval;
        private bool _vlIsLoading;
        private Texture2D _vlMainTexture;
        private float _vlOffset;
        private int _vlPageCount;

        private byte[] _vlPreviousTextureBuffer;
        private int _vlProcessIndex;
        private string[] _vlQueuedOptions = new string[0];

        private string[] _vlQueuedUrls = new string[0];
        private int _vlRetryCount;
        private string _vlSourceRawUrl;
        private string _vlSourceUrl;
        private int _vlTextureHeight;

        private int _vlTextureWidth;
        private RenderTexture _vlTmpRenderTexture;

        protected virtual void VlLoadVideo(string url, string options = "")
        {
            if (vlVideoPlayer == null)
            {
                ConsoleError("VideoLoader: VRCAVProVideoPlayer component is not set.");
                VlOnLoadError(url, LoadError.MissingVRCAVProVideoPlayer);
                return;
            }

            _vlQueuedUrls = _vlQueuedUrls.Append(url);
            _vlQueuedOptions = _vlQueuedOptions.Append(options);
            if (_vlIsLoading) return;
            VlLoadNext();
        }

        /**
         * @private
         * コールバック用にpublicにしているが、外部から直接呼び出さないこと
         */
        public virtual void VlLoadNext()
        {
            if (_vlQueuedUrls.Length < 1)
            {
                ConsoleDebug("[VLLoadVideoInternal] No video to load.");
                _vlIsLoading = false;
                return;
            }

            _vlQueuedUrls = _vlQueuedUrls.__Shift(out var url);
            _vlQueuedOptions = _vlQueuedOptions.__Shift(out var options);

            options.ParseSourceOptions(out var type, out var offset, out var duration);

            ConsoleDebug($"[VLLoadVideoInternal] Loading video: {url}");
            _vlIsLoading = true;
            _vlInterval = duration;
            _vlCurrentTime = offset;
            _vlOffset = offset;
            _vlSourceUrl = url;
            _vlSourceRawUrl = url;
            vlVideoPlayer.Stop();
            vlVideoPlayer.LoadURL(UsGetUrl(url));
        }

        public override void OnVideoReady()
        {
            ConsoleDebug($"[VlOnVideoReady] Video is ready. {_vlSourceUrl}");
            base.OnVideoReady();
            _vlDuration = vlVideoPlayer.GetDuration();
            _vlPageCount = Mathf.CeilToInt((_vlDuration - _vlOffset) / _vlInterval);
            _vlFilenames = new string[_vlPageCount];
            _vlProcessIndex = 0;
            _vlRetryCount = 0;
            VlWaitForVideLoad();
        }

        public override void OnVideoError(VideoError videoError)
        {
            VlOnLoadError(_vlSourceUrl, ToLoadError(videoError));
            SendCustomEventDelayedSeconds(nameof(VlLoadNext), VlDelaySeconds);
        }

        /**
         * @private
         * コールバック用にpublicにしているが、外部から直接呼び出さないこと
         */
        public virtual void VlWaitForVideLoad()
        {
            if (!_vlIsLoading) return;
            _vlMainTexture = (Texture2D)vlVideoRenderer.material.mainTexture;
            if (_vlMainTexture == null)
            {
                SendCustomEventDelayedSeconds(nameof(VlWaitForVideLoad), 1);
                return;
            }

            vlVideoPlayer.SetTime(_vlCurrentTime);
            SendCustomEventDelayedSeconds(nameof(VlOnVideoReady), 1);
        }

        /**
         * @private
         * コールバック用にpublicにしているが、外部から直接呼び出さないこと
         */
        public void VlOnVideoReady()
        {
            if (!_vlIsLoading)
            {
                ConsoleWarn("[VlOnVideoReady] Video is not loading.");
                return;
            }

            if (_vlDuration < _vlCurrentTime)
            {
                ConsoleDebug($"end of video: {_vlCurrentTime}");
                return;
            }

            ConsoleDebug($"[VlOnVideoReady] {_vlCurrentTime} / {_vlDuration}");
            _vlTextureHeight = _vlMainTexture.height;
            _vlTextureWidth = _vlMainTexture.width;
            CopyToRenderTexture(_vlMainTexture, false, true);
        }

        protected virtual void CopyToRenderTexture(Texture2D texture, bool flipHorizontal = false,
            bool flipVertical = false)
        {
            _vlTmpRenderTexture = new RenderTexture(texture.width, texture.height, 0, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear);
            _vlTmpRenderTexture.Create();
            VRCGraphics.Blit(texture, _vlTmpRenderTexture, new Vector2(flipHorizontal ? -1 : 1, flipVertical ? -1 : 1),
                new Vector2(flipHorizontal ? 1 : 0, flipVertical ? 1 : 0));
            VRCAsyncGPUReadback.Request(_vlTmpRenderTexture, 0, (IUdonEventReceiver)this);
        }

        public override void OnAsyncGpuReadbackComplete(VRCAsyncGPUReadbackRequest request)
        {
            var data = new byte[_vlTextureWidth * _vlTextureHeight * 4];
            request.TryGetData(data);
            if (data.MayBlank(100))
            {
                _vlRetryCount++;
                ConsoleDebug($"[VlOnVideoReady] Texture may blank. wait for {VlDelaySeconds}s");
                SendCustomEventDelayedFrames(nameof(VlOnVideoReady), 1);
                return;
            }

            if (_vlRetryCount * VlDelaySeconds < vlLoadTimeout)
                if (data.Similar(_vlPreviousTextureBuffer, 5000))
                {
                    _vlRetryCount++;
                    ConsoleDebug($"[VlOnVideoReady] Texture is same as previous. wait for {VlDelaySeconds}s");
                    SendCustomEventDelayedSeconds(nameof(VlOnVideoReady), VlDelaySeconds);
                    return;
                }

            var readableText = new Texture2D(_vlTextureWidth, _vlTextureHeight, TextureFormat.RGBA32, false);
            readableText.LoadRawTextureData(data);
            readableText.Apply();
            _vlTmpRenderTexture.Release();
            _vlPreviousTextureBuffer = data;
            var fileName = $"video://{_vlSourceUrl.Substring(8)}/{_vlCurrentTime:0.00}";
            _vlFilenames[_vlProcessIndex] = fileName;
            CcSetTexture(_vlSourceRawUrl, fileName, readableText);
            _vlProcessIndex++;
            VlOnLoadProgress(_vlSourceRawUrl, (float)_vlProcessIndex / _vlPageCount);
            if (_vlProcessIndex < _vlPageCount)
            {
                _vlCurrentTime += _vlInterval;
                _vlRetryCount = 0;
                vlVideoPlayer.SetTime(_vlCurrentTime);
                SendCustomEventDelayedSeconds(nameof(VlOnVideoReady), VlDelaySeconds);
                return;
            }

            ConsoleDebug($"[VlOnVideoReady] Video load complete: {_vlSourceUrl}");
            VlOnLoadSuccess(_vlSourceRawUrl, _vlFilenames);
            SendCustomEventDelayedSeconds(nameof(VlLoadNext), VlDelaySeconds);
        }

        protected virtual void VlOnLoadProgress(string source, float progress)
        {
            ConsoleError("VideoLoader: VideoOnLoadProgress should not be called from base class");
        }

        protected virtual void VlOnLoadSuccess(string source, string[] fileNames)
        {
            ConsoleError("VideoLoader: VideoOnLoadSuccess should not be called from base class");
        }

        protected virtual void VlOnLoadError(string source, LoadError error)
        {
            ConsoleError("VideoLoader: VideoOnLoadError should not be called from base class");
        }


        public static LoadError ToLoadError(VideoError error)
        {
            switch (error)
            {
                case VideoError.InvalidURL:
                    return LoadError.InvalidURL;
                case VideoError.AccessDenied:
                    return LoadError.AccessDenied;
                case VideoError.PlayerError:
                    return LoadError.PlayerError;
                case VideoError.RateLimited:
                    return LoadError.RateLimited;
                default:
                    return LoadError.Unknown;
            }
        }
    }
}