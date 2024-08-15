using System;
using System.Text;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;
using VRC.Udon.Common.Interfaces;
using static jp.ootr.common.ArrayUtils;
using static jp.ootr.common.String;

namespace jp.ootr.ImageDeviceController
{
    public class ZipLoader : URLStore
    {
        [SerializeField] private UdonZip.UdonZip zlUdonZip;

        [SerializeField] [Range(1024, 1024000)]
        public int zlPartLength = 102400;

        [SerializeField] [Range(1, 100)] public int zlDelayFrames = 1;
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

        protected virtual void ZlLoadZip(string url)
        {
            if (zlUdonZip == null)
            {
                ConsoleError("ZipLoader: UdonZip component is not set.");
                ZlOnLoadError(url, LoadError.MissingUdonZip);
                return;
            }

            if (_zlIsLoading)
            {
                ConsoleDebug($"ZipLoader: {url} queued.");
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
                ConsoleDebug("ZipLoader: no more urls to load.");
                return;
            }

            _zlQueuedUrlStrings = _zlQueuedUrlStrings.__Shift(out _zlSourceUrl);
            VRCStringDownloader.LoadUrl(UsGetUrl(_zlSourceUrl), (IUdonEventReceiver)this);
        }

        /**
         * @private
         * コールバック用にpublicにしているが、外部から直接呼び出さないこと
         */
        public override void OnStringLoadSuccess(IVRCStringDownload result)
        {
            ConsoleDebug($"ZipLoader: text-zip loaded successfully from {result.Url}.");
            if (!result.IsValidTextZip())
            {
                ZlOnLoadError(result.Url.ToString(), LoadError.InvalidZipFile);
                ConsoleError("ZipLoader: invalid text-zip file.");
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
            ConsoleError($"ZipLoader: text-zip load error: {result.ErrorCode} - {result.Error}");
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
                ConsoleDebug($"ZipLoader: {_zlDecodedBytes} bytes decoded.");
                SendCustomEventDelayedFrames(nameof(ZlExtractData), zlDelayFrames);
            }
        }

        /**
         * @private
         * コールバック用にpublicにしているが、外部から直接呼び出さないこと
         */
        public virtual void ZlExtractData()
        {
            ConsoleDebug("ZipLoader: extracting data.");
            _zlObject = zlUdonZip.Extract(_zlDecodedData);
            var file = zlUdonZip.GetFile(_zlObject, "metadata.json");
            var metadata = zlUdonZip.GetFileData(file);
            VRCJson.TryDeserializeFromJson(Encoding.UTF8.GetString(metadata), out var metadataToken);
            if (!TextZipUtils.ValidateManifest(metadataToken, out _zlMetadata, out var manifestVersion,
                    out var requiredFeatures, out var extension))
            {
                ZlOnLoadError(_zlSourceUrl, LoadError.InvalidManifest);
                ConsoleError($"ZipLoader: invalid manifest. {_zlSourceUrl}");
                SendCustomEventDelayedFrames(nameof(ZlLoadNext), zlDelayFrames);
                return;
            }

            if (manifestVersion > SupportedManifestVersion)
            {
                ZlOnLoadError(_zlSourceUrl, LoadError.UnsupportedManifestVersion);
                ConsoleError($"ZipLoader: unsupported manifest version. {_zlSourceUrl}");
                SendCustomEventDelayedFrames(nameof(ZlLoadNext), zlDelayFrames);
                return;
            }

            foreach (var feature in requiredFeatures)
            {
                if (SupportedFeatures.Has(feature)) continue;
                ZlOnLoadError(_zlSourceUrl, LoadError.UnsupportedFeature);
                ConsoleError($"ZipLoader: unsupported feature: {feature}. {_zlSourceUrl}");
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
                !metadataItem.DataDictionary.TryGetFileMetadata(out var path, out var format, out var width,
                    out var height, out var ext)
            )
            {
                ZlOnLoadError(_zlSourceUrl, LoadError.InvalidMetadata);
                ConsoleError($"ZipLoader: invalid metadata. {_zlSourceUrl} - {_zlProcessIndex}");
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
            CcSetTexture(_zlSourceUrl, fileName, texture, metadataItem.DataDictionary, imageBytes);
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

        protected virtual void ZlOnLoadProgress(string source, float progress)
        {
            ConsoleError("ZipLoader: ZipOnLoadProgress should not be called from base class");
        }

        protected virtual void ZlOnLoadSuccess(string source, string[] fileNames)
        {
            ConsoleError("ZipLoader: ZipOnLoadSuccess should not be called from base class");
        }

        protected virtual void ZlOnLoadError(string source, LoadError error)
        {
            ConsoleError("ZipLoader: ZipOnLoadError should not be called from base class");
        }
    }
}