using System;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
using static jp.ootr.common.ArrayUtils;
using static jp.ootr.common.String;

namespace jp.ootr.ImageDeviceController
{
    public class ZipLoader : URLStore
    {
        [SerializeField] private UdonZip.UdonZip zlUdonZip;

        [SerializeField] [Range(1024, 1024000)]
        protected internal int zlPartLength = 102400;

        [SerializeField] [Range(1, 100)] protected internal int zlDelayFrames = 1;

        private readonly string[] _zipLoaderPrefixes = { "ZipLoader" };
        private int _zlDecodedBytes;
        private byte[] _zlDecodedData;
        private string[] _zlFilenames;
        private bool _zlIsLoading;
        private DataList _zlMetadata;
        private object _zlObject;
        private int _zlProcessIndex;
        private string[] _zlQueuedUrlStrings = new string[0];
        private string[] _zlSource;
        private string _zlSourceUrl;

        protected virtual void ZlLoadZip([CanBeNull]string url)
        {
            if (zlUdonZip == null)
            {
                ConsoleError("UdonZip component is not set.", _zipLoaderPrefixes);
                ZlOnLoadError(url, LoadError.MissingUdonZip);
                return;
            }
            
            if (string.IsNullOrEmpty(url))
            {
                ConsoleError("url is empty.", _zipLoaderPrefixes);
                return;
            }

            if (_zlIsLoading)
            {
                ConsoleDebug($"{url} queued.", _zipLoaderPrefixes);
                _zlQueuedUrlStrings = _zlQueuedUrlStrings.Append(url);
                return;
            }

            _zlSourceUrl = url;
            _zlIsLoading = true;
            VRCStringDownloader.LoadUrl(UsGetUrl(url), (IUdonEventReceiver)this);
        }

        /**
         * @private
         * コールバック用にpublicにしているが、外部から直接呼び出さないこと
         */
        public virtual void ZlLoadNext()
        {
            if (_zlQueuedUrlStrings.Length < 1)
            {
                _zlIsLoading = false;
                ConsoleDebug("no more urls to load.", _zipLoaderPrefixes);
                return;
            }

            _zlQueuedUrlStrings = _zlQueuedUrlStrings.Shift(out _zlSourceUrl, out var success);
            if (!success)
            {
                _zlIsLoading = false;
                ConsoleDebug("no more urls to load.", _zipLoaderPrefixes);
                return;
            }
            VRCStringDownloader.LoadUrl(UsGetUrl(_zlSourceUrl), (IUdonEventReceiver)this);
        }

        /**
         * @private
         * コールバック用にpublicにしているが、外部から直接呼び出さないこと
         */
        public override void OnStringLoadSuccess(IVRCStringDownload result)
        {
            ConsoleLog($"download success from {result.Url}", _zipLoaderPrefixes);
            if (!result.IsValidTextZip())
            {
                ZlOnLoadError(result.Url.ToString(), LoadError.InvalidZipFile);
                ConsoleError("invalid text-zip file.", _zipLoaderPrefixes);
                SendCustomEventDelayedFrames(nameof(ZlLoadNext), zlDelayFrames);
                return;
            }

            _zlSource = result.Result.Split(zlPartLength);
            _zlDecodedData = new byte[_zlSource.Length * zlPartLength];
            _zlProcessIndex = 0;
            _zlDecodedBytes = 0;
            SendCustomEventDelayedFrames(nameof(ZlDecodePart), zlDelayFrames);
        }

        /**
         * @private
         * コールバック用にpublicにしているが、外部から直接呼び出さないこと
         */
        public override void OnStringLoadError(IVRCStringDownload result)
        {
            ConsoleError($"failed to download string from ${result.Url}: {result.ErrorCode} - {result.Error}",
                _zipLoaderPrefixes);
            ZlOnLoadError(_zlSourceUrl, ParseStringDownloadError(result.Result, result.ErrorCode));
            SendCustomEventDelayedFrames(nameof(ZlLoadNext), zlDelayFrames);
        }

        /**
         * @private
         * コールバック用にpublicにしているが、外部から直接呼び出さないこと
         */
        public virtual void ZlDecodePart()
        {
            var data = Convert.FromBase64String(_zlSource[_zlProcessIndex]);
            if (_zlDecodedBytes + data.Length >= _zlDecodedData.Length)
            {
                var tmp = new byte[_zlDecodedBytes + data.Length];
                Array.Copy(_zlDecodedData, tmp, _zlDecodedData.Length);
                _zlDecodedData = tmp;
            }
            else
            {
                Array.Copy(data, 0, _zlDecodedData, _zlDecodedBytes, data.Length);
            }

            _zlDecodedBytes += data.Length;
            _zlProcessIndex++;
            ZlOnLoadProgress(_zlSourceUrl, (float)_zlProcessIndex / _zlSource.Length / 2);
            if (_zlProcessIndex < _zlSource.Length)
            {
                SendCustomEventDelayedFrames(nameof(ZlDecodePart), zlDelayFrames);
            }
            else
            {
                _zlDecodedData = _zlDecodedData.Resize(_zlDecodedBytes);
                ConsoleDebug($"{_zlDecodedBytes} bytes decoded.", _zipLoaderPrefixes);
                SendCustomEventDelayedFrames(nameof(ZlExtractData), zlDelayFrames);
            }
        }

        /**
         * @private
         * コールバック用にpublicにしているが、外部から直接呼び出さないこと
         */
        public virtual void ZlExtractData()
        {
            ConsoleDebug("extracting data.", _zipLoaderPrefixes);
            _zlObject = zlUdonZip.Extract(_zlDecodedData);
            var file = zlUdonZip.GetFile(_zlObject, "metadata.json");
            var metadata = zlUdonZip.GetFileData(file);
            VRCJson.TryDeserializeFromJson(Encoding.UTF8.GetString(metadata), out var metadataToken);
            if (TextZipUtils.ValidateManifest(metadataToken, out _zlMetadata, out var manifestVersion,
                    out var requiredFeatures, out var extension) != ParseResult.Success)
            {
                ZlOnLoadError(_zlSourceUrl, LoadError.InvalidManifest);
                ConsoleError($"invalid manifest. {_zlSourceUrl}", _zipLoaderPrefixes);
                SendCustomEventDelayedFrames(nameof(ZlLoadNext), zlDelayFrames);
                return;
            }

            if (manifestVersion > SupportedManifestVersion)
            {
                ZlOnLoadError(_zlSourceUrl, LoadError.UnsupportedManifestVersion);
                ConsoleError($"unsupported manifest version. {_zlSourceUrl}", _zipLoaderPrefixes);
                SendCustomEventDelayedFrames(nameof(ZlLoadNext), zlDelayFrames);
                return;
            }

            foreach (var feature in requiredFeatures)
            {
                if (SupportedFeatures.Has(feature)) continue;
                ZlOnLoadError(_zlSourceUrl, LoadError.UnsupportedFeature);
                ConsoleError($"unsupported feature: {feature}. {_zlSourceUrl}", _zipLoaderPrefixes);
                SendCustomEventDelayedFrames(nameof(ZlLoadNext), zlDelayFrames);
                return;
            }

            _zlProcessIndex = 0;
            _zlFilenames = new string[_zlMetadata.Count];
            SendCustomEventDelayedFrames(nameof(ZlExtractItem), zlDelayFrames);
        }

        /**
         * @private
         * コールバック用にpublicにしているが、外部から直接呼び出さないこと
         */
        public virtual void ZlExtractItem()
        {
            if (
                !_zlMetadata.TryGetValue(_zlProcessIndex, TokenType.DataDictionary, out var metadataItem) ||
                metadataItem.DataDictionary.TryGetFileMetadata(out var path, out var format, out var width,
                    out var height, out var ext) != ParseResult.Success
            )
            {
                ZlOnLoadError(_zlSourceUrl, LoadError.InvalidMetadata);
                ConsoleError($"invalid metadata. {_zlSourceUrl} - {_zlProcessIndex}", _zipLoaderPrefixes);
                SendCustomEventDelayedFrames(nameof(ZlLoadNext), zlDelayFrames);
                return;
            }

            var imageFile = zlUdonZip.GetFile(_zlObject, path);
            var imageBytes = zlUdonZip.GetFileData(imageFile);
            var texture = new Texture2D(width, height, format, false);
            texture.LoadRawTextureData(imageBytes);
            texture.Apply();
            var fileName = $"zip://{_zlSourceUrl.Substring(8)}/{path}";
            _zlFilenames[_zlProcessIndex] = fileName;
            CcSetTexture(_zlSourceUrl, fileName, texture, metadataItem.DataDictionary, imageBytes, format);
            _zlProcessIndex++;
            ZlOnLoadProgress(_zlSourceUrl, 0.5f + (float)_zlProcessIndex / _zlMetadata.Count / 2);
            if (_zlProcessIndex < _zlMetadata.Count)
            {
                SendCustomEventDelayedFrames(nameof(ZlExtractItem), zlDelayFrames);
                return;
            }

            ZlOnLoadSuccess(_zlSourceUrl, _zlFilenames);
            SendCustomEventDelayedFrames(nameof(ZlLoadNext), zlDelayFrames);
        }

        protected virtual void ZlOnLoadProgress([CanBeNull]string source, float progress)
        {
            ConsoleError("ZipOnLoadProgress should not be called from base class", _zipLoaderPrefixes);
        }

        protected virtual void ZlOnLoadSuccess([CanBeNull]string source, [CanBeNull]string[] fileNames)
        {
            ConsoleError("ZipOnLoadSuccess should not be called from base class", _zipLoaderPrefixes);
        }

        protected virtual void ZlOnLoadError([CanBeNull]string source, LoadError error)
        {
            ConsoleError("ZipOnLoadError should not be called from base class", _zipLoaderPrefixes);
        }
    }
}
