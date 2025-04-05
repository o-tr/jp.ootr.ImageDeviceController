using System;
using System.Text;
using JetBrains.Annotations;
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
        private string[] _zlContent;
        private string _zlSourceUrl;

        protected virtual void OnZipLoadSuccess(IVRCStringDownload result)
        {
            if (zlUdonZip == null)
            {
                ConsoleError("UdonZip component is not set.", _zipLoaderPrefixes);
                ZlOnLoadError(result.Url.ToString(), LoadError.MissingUdonZip);
                return;
            }
            ConsoleLog($"download success from {result.Url}", _zipLoaderPrefixes);
            if (!result.IsValidTextZip())
            {
                ZlOnLoadError(result.Url.ToString(), LoadError.InvalidZipFile);
                ConsoleError("invalid text-zip file.", _zipLoaderPrefixes);
                return;
            }

            _zlSourceUrl = result.Url.ToString();
            _zlContent = result.Result.Split(zlPartLength);
            _zlDecodedData = new byte[_zlContent.Length * zlPartLength];
            _zlProcessIndex = 0;
            _zlDecodedBytes = 0;
            SendCustomEventDelayedFrames(nameof(ZlDecodePart), zlDelayFrames);
        }

        /**
         * @private
         * コールバック用にpublicにしているが、外部から直接呼び出さないこと
         */
        public virtual void ZlDecodePart()
        {
            var data = Convert.FromBase64String(_zlContent[_zlProcessIndex]);
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
            ZlOnLoadProgress(_zlSourceUrl, (float)_zlProcessIndex / _zlContent.Length / 2);
            if (_zlProcessIndex < _zlContent.Length)
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
                    out var requiredFeatures, out var void1) != ParseResult.Success || requiredFeatures == null ||
                _zlMetadata == null)
            {
                ZlOnLoadError(_zlSourceUrl, LoadError.InvalidManifest);
                ConsoleError($"invalid manifest. {_zlSourceUrl}", _zipLoaderPrefixes);
                return;
            }

            if (manifestVersion > SupportedManifestVersion)
            {
                ZlOnLoadError(_zlSourceUrl, LoadError.UnsupportedManifestVersion);
                ConsoleError($"unsupported manifest version. {_zlSourceUrl}", _zipLoaderPrefixes);
                return;
            }

            foreach (var feature in requiredFeatures)
            {
                if (SupportedFeatures.Has(feature)) continue;
                ZlOnLoadError(_zlSourceUrl, LoadError.UnsupportedFeature);
                ConsoleError($"unsupported feature: {feature}. {_zlSourceUrl}", _zipLoaderPrefixes);
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
                    out var height, out var extensions) != ParseResult.Success || path == null || extensions == null
            )
            {
                ZlOnLoadError(_zlSourceUrl, LoadError.InvalidMetadata);
                ConsoleError($"invalid metadata. {_zlSourceUrl} - {_zlProcessIndex}", _zipLoaderPrefixes);
                return;
            }

            var imageBytes = GenerateImageBytes(extensions, width, format, path);
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
        }

        [CanBeNull]
        private byte[] GenerateImageBytes(DataDictionary extensions, int width, TextureFormat format, string path)
        {

            if (extensions.TryGetCroppedMetadata(out var basePath, out var rects) == ParseResult.Success)
            {
                var fileName = $"zip://{_zlSourceUrl.Substring(8)}/{basePath}";
                var baseImage = CcGetBinary(_zlSourceUrl, fileName);
                if (baseImage == null)
                {
                    ZlOnLoadError(_zlSourceUrl, LoadError.InvalidMetadata);
                    ConsoleError($"missing base image: {basePath}", _zipLoaderPrefixes);
                    return null;
                }

                var bytePerPixel = format.GetBytePerPixel();

                for (int i = 0; i < rects.Count; i++)
                {
                    if (!rects.TryGetValue(i, TokenType.DataDictionary, out var rect)) continue;
                    if (rect.DataDictionary.TryGetRectMetadata(out var baseX, out var baseY, out var w, out var h, out var rectPath) != ParseResult.Success)continue;
                    var rectFile = zlUdonZip.GetFile(_zlObject, rectPath);
                    var rectBytes = zlUdonZip.GetFileData(rectFile);
                    for(var y = 0; y < h; y++)
                    {
                        Array.Copy(rectBytes, y * w * bytePerPixel, baseImage, (baseY + y) * width * bytePerPixel + baseX * bytePerPixel, w * bytePerPixel);
                    }                    
                }
                
                return baseImage;
            }
            var imageFile = zlUdonZip.GetFile(_zlObject, path);
            return zlUdonZip.GetFileData(imageFile);
        }

        protected virtual void ZlOnLoadProgress([CanBeNull] string sourceUrl, float progress)
        {
            ConsoleError("ZipOnLoadProgress should not be called from base class", _zipLoaderPrefixes);
        }

        protected virtual void ZlOnLoadSuccess([CanBeNull] string sourceUrl, [CanBeNull] string[] fileUrls)
        {
            ConsoleError("ZipOnLoadSuccess should not be called from base class", _zipLoaderPrefixes);
        }

        protected virtual void ZlOnLoadError([CanBeNull] string sourceUrl, LoadError error)
        {
            ConsoleError("ZipOnLoadError should not be called from base class", _zipLoaderPrefixes);
        }
    }
}
