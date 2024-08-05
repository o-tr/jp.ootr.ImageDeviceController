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

        protected readonly float VlDelaySeconds = 0.05f;
        protected RenderTexture VlTmpRenderTexture;
        protected float VlCurrentTime;
        protected float VlDuration;

        protected string[] VlFilenames;
        protected float VlInterval;
        protected bool VlIsLoading;
        protected Texture2D VlMainTexture;
        protected float VlOffset;
        protected int VlPageCount;

        protected byte[] VlPreviousTextureBuffer;
        protected int VlProcessIndex;

        protected string[] VlQueuedUrls = new string[0];
        protected string[] VlQueuedOptions = new string[0];
        protected int VlRetryCount;
        protected string VlSourceUrl;
        protected string VlSourceRawUrl;
        protected int VlTextureHeight;

        protected int VlTextureWidth;

        protected virtual void VlLoadVideo(string url, string options = "")
        {
            if (vlVideoPlayer == null)
            {
                ConsoleError("VideoLoader: VRCAVProVideoPlayer component is not set.");
                VlOnLoadError(url, LoadError.MissingVRCAVProVideoPlayer);
                return;
            }
            VlQueuedUrls = VlQueuedUrls.Append(url);
            VlQueuedOptions = VlQueuedOptions.Append(options);
            if (VlIsLoading) return;
            VlLoadNext();
        }

        public virtual void VlLoadNext()
        {
            if (VlQueuedUrls.Length < 1)
            {
                ConsoleDebug("[VLLoadVideoInternal] No video to load.");
                VlIsLoading = false;
                return;
            }

            VlQueuedUrls = VlQueuedUrls.__Shift(out var url);
            VlQueuedOptions = VlQueuedOptions.__Shift(out var options);
            
            options.ParseSourceOptions(out var type, out var offset, out var duration);

            ConsoleDebug($"[VLLoadVideoInternal] Loading video: {url}");
            VlIsLoading = true;
            VlInterval = duration;
            VlCurrentTime = offset;
            VlOffset = offset;
            VlSourceUrl = url;
            VlSourceRawUrl = url;
            vlVideoPlayer.Stop();
            vlVideoPlayer.LoadURL(UsGetUrl(url));
        }

        public override void OnVideoReady()
        {
            ConsoleDebug($"[VlOnVideoReady] Video is ready. {VlSourceUrl}");
            base.OnVideoReady();
            VlDuration = vlVideoPlayer.GetDuration();
            VlPageCount = Mathf.CeilToInt((VlDuration - VlOffset) / VlInterval);
            VlFilenames = new string[VlPageCount];
            VlProcessIndex = 0;
            VlRetryCount = 0;
            VlWaitForVideLoad();
        }

        public override void OnVideoError(VideoError videoError)
        {
            VlOnLoadError(VlSourceUrl, ToLoadError(videoError));
            SendCustomEventDelayedSeconds(nameof(VlLoadNext), VlDelaySeconds);
        }

        public virtual void VlWaitForVideLoad()
        {
            if (!VlIsLoading) return;
            VlMainTexture = (Texture2D)vlVideoRenderer.material.mainTexture;
            if (VlMainTexture == null)
            {
                SendCustomEventDelayedSeconds(nameof(VlWaitForVideLoad), 1);
                return;
            }

            vlVideoPlayer.SetTime(VlCurrentTime);
            SendCustomEventDelayedSeconds(nameof(VlOnVideoReady), 1);
        }

        public void VlOnVideoReady()
        {
            if (!VlIsLoading)
            {
                ConsoleWarn("[VlOnVideoReady] Video is not loading.");
                return;
            }

            if (VlDuration < VlCurrentTime)
            {
                ConsoleDebug($"end of video: {VlCurrentTime}");
                return;
            }

            ConsoleDebug($"[VlOnVideoReady] {VlCurrentTime} / {VlDuration}");
            VlTextureHeight = VlMainTexture.height;
            VlTextureWidth = VlMainTexture.width;
            CopyToRenderTexture(VlMainTexture, false, true);
        }

        protected virtual void CopyToRenderTexture(Texture2D texture, bool flipHorizontal = false,
            bool flipVertical = false)
        {
            VlTmpRenderTexture = new RenderTexture(texture.width, texture.height, 0, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear);
            VlTmpRenderTexture.Create();
            VRCGraphics.Blit(texture, VlTmpRenderTexture, new Vector2(flipHorizontal ? -1 : 1, flipVertical ? -1 : 1),
                new Vector2(flipHorizontal ? 1 : 0, flipVertical ? 1 : 0));
            VRCAsyncGPUReadback.Request(VlTmpRenderTexture, 0, (IUdonEventReceiver)this);
        }

        public override void OnAsyncGpuReadbackComplete(VRCAsyncGPUReadbackRequest request)
        {
            var data = new byte[VlTextureWidth * VlTextureHeight * 4];
            request.TryGetData(data);
            if (VlRetryCount * VlDelaySeconds < vlLoadTimeout)
            {
                if (data.Similar(VlPreviousTextureBuffer, 5000))
                {
                    VlRetryCount++;
                    ConsoleDebug($"[VlOnVideoReady] Texture is same as previous. wait for {VlDelaySeconds}s");
                    SendCustomEventDelayedFrames(nameof(VlOnVideoReady), 1);
                    return;
                }
                if (data.MayBlank(1000))
                {
                    VlRetryCount++;
                    ConsoleDebug($"[VlOnVideoReady] Texture may blank. wait for {VlDelaySeconds}s");
                    SendCustomEventDelayedFrames(nameof(VlOnVideoReady), 1);
                    return;
                }
            }

            var readableText = new Texture2D(VlTextureWidth, VlTextureHeight, TextureFormat.RGBA32, false);
            readableText.LoadRawTextureData(data);
            readableText.Apply();
            VlTmpRenderTexture.Release();
            VlPreviousTextureBuffer = data;
            var fileName = $"video://{VlSourceUrl.Substring(8)}/{VlCurrentTime:0.00}";
            VlFilenames[VlProcessIndex] = fileName;
            CcSetTexture(VlSourceRawUrl, fileName, readableText);
            VlProcessIndex++;
            VlOnLoadProgress(VlSourceRawUrl, (float)VlProcessIndex / VlPageCount);
            if (VlProcessIndex < VlPageCount)
            {
                VlCurrentTime += VlInterval;
                VlRetryCount = 0;
                vlVideoPlayer.SetTime(VlCurrentTime);
                SendCustomEventDelayedSeconds(nameof(VlOnVideoReady), VlDelaySeconds);
                return;
            }

            ConsoleDebug($"[VlOnVideoReady] Video load complete: {VlSourceUrl}");
            VlOnLoadSuccess(VlSourceRawUrl, VlFilenames);
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