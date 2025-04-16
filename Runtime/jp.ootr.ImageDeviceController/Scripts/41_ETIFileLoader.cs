using System;
using JetBrains.Annotations;
using jp.ootr.common;
using UnityEngine;
using VRC.SDK3.Data;

namespace jp.ootr.ImageDeviceController
{
    public class ETIFileLoader : LocalSourceLoader {
        private readonly string[] _etiFileLoaderPrefixes = { "ETIFileLoader" };
        
        private string[] _etiQueuedFileUrls = new string[0];
        private bool _etiFileIsLoading = false;
        
        private string _etiCurrentSourceUrl = string.Empty;
        private string _etiCurrentFileUrl = string.Empty;
        private int _etiCurrentFileIndex = -1;
        
        protected void ETILoadFile([CanBeNull] string fileUrl) {
            if (string.IsNullOrEmpty(fileUrl)) {
                return;
            }

            if (!EtiParsedFileUrls.Has(_etiCurrentFileUrl, out var index))
            {
                ConsoleDebug($"File not found in cache: {_etiCurrentFileUrl}", _etiFileLoaderPrefixes);
                OnFileLoadError(_etiCurrentFileUrl, LoadError.InvalidFileURL);
                SendCustomEvent(nameof(ETILoadFIleNext));
                return;
            }

            var toLoadUrls = new string[0];

            while (
                EtiParsedFileManifests[index].TryGetValue("t", TokenType.String, out var type)
                && type.String == "c"
                && EtiParsedFileManifests[index].TryGetValue("b", TokenType.String, out var baseUrl)
                && EtiParsedFileUrls.Has(baseUrl.String, out index)
                && !_etiQueuedFileUrls.Has(baseUrl.String)
            )
            {
                toLoadUrls = toLoadUrls.Append(baseUrl.String);
            }

            for (int i = toLoadUrls.Length - 1; i >= 0; i--)
            {
                _etiQueuedFileUrls = _etiQueuedFileUrls.Append(toLoadUrls[i]);
            }

            _etiQueuedFileUrls = _etiQueuedFileUrls.Append(fileUrl);
            
            if (_etiFileIsLoading)
            {
                ConsoleDebug($"Loading already in progress, queuing {fileUrl}", _etiFileLoaderPrefixes);
                return;
            }
            
            ConsoleDebug($"Loading {fileUrl}", _etiFileLoaderPrefixes);
            _etiFileIsLoading = true;
            ETILoadFIleNext();
        }

        public void ETILoadFIleNext()
        {
            if (_etiQueuedFileUrls.Length == 0)
            {
                ConsoleDebug("No more URLs to load", _etiFileLoaderPrefixes);
                _etiFileIsLoading = false;
                return;
            }

            _etiQueuedFileUrls = _etiQueuedFileUrls.Shift(out _etiCurrentFileUrl, out var success);
            if (!success)
            {
                _etiFileIsLoading = false;
                ConsoleDebug("no more URLs to load", _etiFileLoaderPrefixes);
                return;
            }

            if (string.IsNullOrEmpty(_etiCurrentFileUrl))
            {
                ConsoleDebug("Empty URL, skipping", _etiFileLoaderPrefixes);
                SendCustomEvent(nameof(ETILoadFIleNext));
                return;
            }

            if (!EtiParsedFileUrls.Has(_etiCurrentFileUrl, out _etiCurrentFileIndex))
            {
                ConsoleDebug($"File not found in cache: {_etiCurrentFileUrl}", _etiFileLoaderPrefixes);
                OnFileLoadError(_etiCurrentFileUrl, LoadError.InvalidFileURL);
                SendCustomEvent(nameof(ETILoadFIleNext));
                return;
            }
            var sourceStart = _etiCurrentFileUrl.IndexOf(":", StringComparison.Ordinal);
            var sourceEnd = _etiCurrentFileUrl.LastIndexOf("/", StringComparison.Ordinal);
            
            _etiCurrentSourceUrl = $"https{_etiCurrentFileUrl.Substring(sourceStart, sourceEnd - sourceStart)}"; 
            
            base64Rle.DecodeAsync(this, EtiParsedFileBuffers[_etiCurrentFileIndex]);
        }

        public void OnDecodeComplete()
        {
            ConsoleDebug($"File loaded successfully: {_etiCurrentFileUrl}", _etiFileLoaderPrefixes);
            var fileUrl = EtiParsedFileUrls[_etiCurrentFileIndex];
            var file = EtiParsedFileManifests[_etiCurrentFileIndex];

            if (
                !file.TryGetValue("t", TokenType.String, out var type)
                || !file.TryGetValue("w", TokenType.Double, out var width)
                || !file.TryGetValue("h", TokenType.Double, out var height)
                || !file.TryGetValue("f", TokenType.String, out var formatToken)
                || !TextZipUtils.ParseTextureFormatString(formatToken.String, out var format)
            )
            {
                ConsoleError($"File type not found: {fileUrl}", _etiFileLoaderPrefixes);
                OnFileLoadError(fileUrl, LoadError.InvalidFileURL);
                SendCustomEvent(nameof(ETILoadFIleNext));
                return;
            }

            var decoded = Convert.FromBase64String(base64Rle.GetDecoded());

            var bytes = ETIGenerateImageBytes(file, decoded);
            var texture = new Texture2D((int)width.Double, (int)height.Double, format, false);
            texture.LoadRawTextureData(bytes);
            texture.Apply();
            
            CcSetTexture(_etiCurrentSourceUrl, _etiCurrentFileUrl, texture, file, bytes, format);
            _etiCurrentFileUrl = string.Empty;
            _etiCurrentSourceUrl = string.Empty;
            base64Rle.ClearStoredData();
            
            OnFileLoadSuccess(_etiCurrentFileUrl, bytes);
            
            SendCustomEvent(nameof(ETILoadFIleNext));
        }

        public void OnDecodeFailed()
        {
            ConsoleDebug($"Failed to decode file: {_etiCurrentFileUrl}", _etiFileLoaderPrefixes);
            OnFileLoadError(_etiCurrentFileUrl, LoadError.InvalidFileURL);
            
            SendCustomEvent(nameof(ETILoadFIleNext));
        }

        [CanBeNull]
        private byte[] ETIGenerateImageBytes(
            DataDictionary file,
            byte[] decodedData 
        )
        {
            if (!file.TryGetValue("t", TokenType.String, out var type))
            {
                ConsoleError($"File type not found: {file}", _etiFileLoaderPrefixes);
                return null;
            }

            if (type.String == "m")
            {
                return decodedData;
            }

            if (
                !file.TryGetValue("b", TokenType.String, out var baseUrl)
                || !file.TryGetValue("w", TokenType.Double, out var width)
                || !file.TryGetValue("r", TokenType.Double, out var rects)
            )
            {
                ConsoleError($"Base URL not found: {file}", _etiFileLoaderPrefixes);
                return null;
            }

            var baseBinary = CcGetBinary(_etiCurrentSourceUrl, baseUrl.String);
            if (baseBinary == null)
            {
                ConsoleError($"Base binary not found: {baseUrl}", _etiFileLoaderPrefixes);
                return null;
            }
            var binary = new byte[baseBinary.Length];
            Array.Copy(baseBinary, binary, baseBinary.Length);
            
            var bytePerPixel = TextureFormat.RGBA32.GetBytePerPixel();
            var baseWidth = (int)width.Double;

            var rectsCount = rects.DataList.Count;
            
            for (int i = 0; i < rectsCount; i++)
            {
                if (!rects.DataList.TryGetValue(i, TokenType.DataDictionary, out var rect))
                {
                    ConsoleError($"Rect not found: {file}", _etiFileLoaderPrefixes);
                    return null;
                }

                if (
                    !rect.DataDictionary.TryGetValue("x", TokenType.Double, out var baseXToken)
                    || !rect.DataDictionary.TryGetValue("y", TokenType.Double, out var baseYToken)
                    || !rect.DataDictionary.TryGetValue("w", TokenType.Double, out var rectWidthToken)
                    || !rect.DataDictionary.TryGetValue("h", TokenType.Double, out var rectHeightToken)
                    || !rect.DataDictionary.TryGetValue("s", TokenType.Double, out var startToken)
                )
                {
                    ConsoleError($"Rect metadata not found: {file}", _etiFileLoaderPrefixes);
                    return null;
                }
                
                var baseX = (int) baseXToken.Double;
                var baseY = (int) baseYToken.Double;
                var rectWidth = (int)rectWidthToken.Double;
                var rectHeight = (int)rectHeightToken.Double;
                var start = (int)startToken.Double;
                
                for (var y = 0; y < rectHeight; y++)
                {
                    //Array.Copy(rectBytes, y * w * bytePerPixel, baseImage, (baseY + y) * width * bytePerPixel + baseX * bytePerPixel, w * bytePerPixel);
                    
                    Array.Copy(
                        decodedData,
                        start + y * rectWidth * bytePerPixel,
                        binary,
                        (baseY + y) * baseWidth * bytePerPixel + baseX * bytePerPixel,
                        rectWidth * bytePerPixel
                    );
                }
            }

            return binary;
        }
    }
}
