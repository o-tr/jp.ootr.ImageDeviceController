﻿using System;
using JetBrains.Annotations;
using jp.ootr.common;
using jp.ootr.common.Base;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Rendering;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace jp.ootr.ImageDeviceController
{
    public class VideoSourceLoader : StringSourceLoader
    {
        private const float VlDelaySeconds = 0.05f;
        [SerializeField] protected VRCAVProVideoPlayer vlVideoPlayer;
        [SerializeField] protected MeshRenderer vlVideoRenderer;
        [SerializeField] [Range(1, 60)] protected internal float vlLoadTimeout = 5;

        private readonly string[] _videoLoaderPrefixes = { "VideoLoader" };

        private readonly int _vlLoadMinInterval = 5;
        private float _vlCurrentTime;
        private float _vlDuration;

        private string[] _vlFilenames;
        private float _vlInterval;
        private bool _vlIsLoading;

        private DateTime _vlLastLoadTime;
        private Texture2D _vlMainTexture;
        private float _vlOffset;
        private int _vlPageCount;

        private byte[] _vlPreviousTextureBuffer;
        private int _vlProcessIndex;

        [ItemNotNull] private string[] _vlQueuedOptions = new string[0];
        [ItemNotNull] private string[] _vlQueuedSourceUrls = new string[0];

        private int _vlRetryCount;
        private string _vlSourceOptions;
        private string _vlSourceUrl;
        private int _vlTextureHeight;

        private int _vlTextureWidth;
        private RenderTexture _vlTmpRenderTexture;

        protected virtual void VlLoadVideo([CanBeNull] string sourceUrl, [CanBeNull] string options = "")
        {
            if (vlVideoPlayer == null)
            {
                ConsoleError("VRCAVProVideoPlayer component is not set.", _videoLoaderPrefixes);
                OnSourceLoadError(sourceUrl, LoadError.MissingVRCAVProVideoPlayer);
                return;
            }

            if (!sourceUrl.IsValidUrl(out var error))
            {
                ConsoleError($"Invalid URL: {sourceUrl}", _videoLoaderPrefixes);
                OnSourceLoadError(sourceUrl, error);
                return;
            }

            if (string.IsNullOrEmpty(options) ||
                !options.ParseSourceOptions(out var void1, out var void2, out var void3))
            {
                ConsoleWarn("Options is empty or invalid.", _videoLoaderPrefixes);
                OnSourceLoadError(sourceUrl, LoadError.InvalidOptions);
                return;
            }

            if (_vlQueuedSourceUrls.Has(sourceUrl, out var index) && _vlQueuedOptions[index] == options)
            {
                ConsoleDebug("Already queued.", _videoLoaderPrefixes);
                return;
            }

            _vlQueuedSourceUrls = _vlQueuedSourceUrls.Append(sourceUrl);
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
            if (_vlQueuedSourceUrls.Length < 1)
            {
                ConsoleDebug("no more urls to load.", _videoLoaderPrefixes);
                _vlIsLoading = false;
                return;
            }

            if (_vlLastLoadTime.AddSeconds(_vlLoadMinInterval) > DateTime.Now)
            {
                ConsoleDebug($"Rate limited. retry after {_vlLoadMinInterval}s", _videoLoaderPrefixes);
                SendCustomEventDelayedSeconds(nameof(VlLoadNext), _vlLoadMinInterval);
                return;
            }

            _vlLastLoadTime = DateTime.Now;

            _vlQueuedSourceUrls = _vlQueuedSourceUrls.Shift(out var sourceUrl);
            _vlQueuedOptions = _vlQueuedOptions.Shift(out var options);

            options.ParseSourceOptions(out var void1, out var offset, out var duration);

            ConsoleDebug($"Loading video: {sourceUrl}", _videoLoaderPrefixes);
            _vlIsLoading = true;
            _vlInterval = duration;
            _vlCurrentTime = offset;
            _vlOffset = offset;
            _vlSourceUrl = sourceUrl;
            _vlSourceOptions = options;
            VlLoadVideo();
        }

        public void VlLoadVideo()
        {
            vlVideoPlayer.Stop();
            vlVideoPlayer.LoadURL(UsGetUrl(_vlSourceUrl));
        }

        public override void OnVideoReady()
        {
            ConsoleDebug($"Video is ready. source: {_vlSourceUrl}", _videoLoaderPrefixes);
            base.OnVideoReady();
            _vlDuration = vlVideoPlayer.GetDuration();
            if (float.IsInfinity(_vlDuration))
            {
                ConsoleWarn($"Video duration is infinity. source: {_vlSourceUrl}", _videoLoaderPrefixes);

                OnSourceLoadError(_vlSourceUrl, LoadError.LiveVideoNotSupported);
                SendCustomEventDelayedSeconds(nameof(VlLoadNext), VlDelaySeconds);
                return;
            }

            _vlPageCount = Mathf.CeilToInt((_vlDuration - _vlOffset) / _vlInterval);
            _vlFilenames = new string[_vlPageCount];
            _vlProcessIndex = 0;
            _vlRetryCount = 0;
            VlWaitForVideLoad();
        }

        public override void OnVideoError(VideoError videoError)
        {
            if (videoError == VideoError.RateLimited)
            {
                ConsoleWarn($"Rate limited. retry after {_vlLoadMinInterval}s", _videoLoaderPrefixes);
                SendCustomEventDelayedSeconds(nameof(VlLoadVideo), _vlLoadMinInterval);
                return;
            }

            OnSourceLoadError(_vlSourceUrl, ToLoadError(videoError));
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
            if (!Utilities.IsValid(_vlMainTexture))
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
                ConsoleWarn($"Video is not loading. source: {_vlSourceUrl}", _videoLoaderPrefixes);
                return;
            }

            if (_vlDuration < _vlCurrentTime)
            {
                ConsoleDebug($"end of video: {_vlCurrentTime}", _videoLoaderPrefixes);
                return;
            }

            _vlTextureHeight = _vlMainTexture.height;
            _vlTextureWidth = _vlMainTexture.width;
            
            //HACK: クエスト以外は縦が反転しているので補正
            var flipVertical = CurrentPlatform != Platform.Android;
            
            CopyToRenderTexture(_vlMainTexture, false, flipVertical);
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
            Destroy(_vlTmpRenderTexture);
            _vlTmpRenderTexture.Release();
            var data = new byte[_vlTextureWidth * _vlTextureHeight * 4];
            request.TryGetData(data);
            if (data.MayBlank(100))
            {
                ConsoleDebug($"Texture may blank. wait for {VlDelaySeconds}s", _videoLoaderPrefixes);
                SendCustomEventDelayedFrames(nameof(VlOnVideoReady), 1);
                return;
            }

            if (_vlRetryCount * VlDelaySeconds < vlLoadTimeout)
                if (data.Similar(_vlPreviousTextureBuffer, 5000))
                {
                    _vlRetryCount++;
                    ConsoleDebug($"Texture may same as previous. wait for {VlDelaySeconds}s", _videoLoaderPrefixes);
                    SendCustomEventDelayedSeconds(nameof(VlOnVideoReady), VlDelaySeconds);
                    return;
                }

            ConsoleDebug($"Success to read texture {_vlProcessIndex + 1}/{_vlPageCount}", _videoLoaderPrefixes);
            var readableText = new Texture2D(_vlTextureWidth, _vlTextureHeight, TextureFormat.RGBA32, false);
            readableText.LoadRawTextureData(data);
            readableText.Apply();
            _vlPreviousTextureBuffer = data;
            var fileName = $"{PROTOCOL_VIDEO}://{_vlSourceOptions}@{_vlSourceUrl.Substring(8)}/{_vlCurrentTime:0.00}";
            _vlFilenames[_vlProcessIndex] = fileName;
            CcSetTexture(_vlSourceUrl, fileName, readableText, data);
            _vlProcessIndex++;
            OnSourceLoadProgress(_vlSourceUrl, (float)_vlProcessIndex / _vlPageCount);
            if (_vlProcessIndex < _vlPageCount)
            {
                _vlCurrentTime += _vlInterval;
                _vlRetryCount = 0;
                vlVideoPlayer.SetTime(_vlCurrentTime);
                SendCustomEventDelayedSeconds(nameof(VlOnVideoReady), VlDelaySeconds);
                return;
            }

            ConsoleDebug($"Video load complete: {_vlSourceUrl}", _videoLoaderPrefixes);
            OnSourceLoadSuccess(_vlSourceUrl, _vlFilenames);
            SendCustomEventDelayedSeconds(nameof(VlLoadNext), VlDelaySeconds);
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
