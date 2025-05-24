using System;
using JetBrains.Annotations;
using jp.ootr.common;
using UnityEngine;
using VRC.SDK3.Data;

namespace jp.ootr.ImageDeviceController
{
    public class EIAFileLoader : LocalSourceLoader {
        private readonly string[] _eiaFileLoaderPrefixes = { "EIAFileLoader" };
        
        private string[] _eiaQueuedSourceUrls = new string[0];
        private string[] _eiaQueuedFileUrls = new string[0];
        private int[] _eiaQueuedFilePriorities = new int[0];
        
        private int _eiaCurrentMaxPriority = 0;
        
        private bool _eiaFileIsLoading = false;
        
        private string _eiaCurrentSourceUrl = string.Empty;
        private string _eiaCurrentFileUrl = string.Empty;
        private int _eiaCurrentFileIndex = -1;
        
        private byte[] _eiaCurrentDecodedData = null;
        private DataDictionary _eiaCurrentFile = null;
        private int _eiaCurrentFileBytesPerPixel = 0;
        private int _eiaCurrentFileWidth = 0;
        private int _eiaCurrentFileHeight = 0;
        private DataList _eiaCurrentFileRects = null;
        private int _eiaCurrentFileRectsIndex = 0;
        private byte[] _eiaCurrentFileBinary = null;
        private string _eiaCurrentFileBaseUrl = string.Empty;
        private TextureFormat _eiaCurrentFileFormat = TextureFormat.RGB24;
        
        private float _eiaProcessStartTime = 0;
        
        private const int _eiaProcessIntervalFrame = 1;
        
        private void EIAClear()
        {
            _eiaCurrentSourceUrl = string.Empty;
            _eiaCurrentFileUrl = string.Empty;
            _eiaCurrentFileIndex = -1;
            _eiaCurrentDecodedData = null;
            _eiaCurrentFile = null;
            _eiaCurrentFileBytesPerPixel = 0;
            _eiaCurrentFileWidth = 0;
            _eiaCurrentFileHeight = 0;
            _eiaCurrentFileRects = null;
            _eiaCurrentFileRectsIndex = 0;
            _eiaCurrentFileBinary = null;
            _eiaCurrentFileBaseUrl = string.Empty;
            _eiaCurrentFileFormat = TextureFormat.RGB24;
        }
        
        protected void EIALoadFile([CanBeNull] string sourceUrl, [CanBeNull] string fileUrl, int priority) {
            if (string.IsNullOrEmpty(fileUrl) || string.IsNullOrEmpty(sourceUrl)) {
                return;
            }

            if (!EiaParsedFileUrls.Has(fileUrl, out var index))
            {
                ConsoleDebug($"File not found in cache: {fileUrl}", _eiaFileLoaderPrefixes);
                OnFileLoadError(fileUrl, LoadError.InvalidFileURL);
                SendCustomEvent(nameof(EIALoadFIleNext));
                return;
            }

            var toLoadUrls = new string[0];

            while (
                EiaParsedFileManifests[index].TryGetValue("t", TokenType.String, out var type)
                && type.String == "c"
                && EiaParsedFileManifests[index].TryGetValue("b", TokenType.String, out var baseUrl)
                && EiaParsedFileUrls.Has(baseUrl.String, out index)
                && !_eiaQueuedFileUrls.Has(baseUrl.String)
            )
            {
                toLoadUrls = toLoadUrls.Append(baseUrl.String);
                ConsoleDebug($"Loading base URL: {baseUrl.String} {type.String}", _eiaFileLoaderPrefixes);
            }

            for (int i = toLoadUrls.Length - 1; i >= 0; i--)
            {
                _eiaQueuedSourceUrls = _eiaQueuedSourceUrls.Append(sourceUrl);
                _eiaQueuedFileUrls = _eiaQueuedFileUrls.Append(toLoadUrls[i]);
                var itemPriority = priority + ( toLoadUrls.Length - i);
                _eiaQueuedFilePriorities = _eiaQueuedFilePriorities.Append(itemPriority);
                if (_eiaCurrentMaxPriority < itemPriority)
                {
                    _eiaCurrentMaxPriority = itemPriority;
                }
            }

            _eiaQueuedSourceUrls = _eiaQueuedSourceUrls.Append(sourceUrl);
            _eiaQueuedFileUrls = _eiaQueuedFileUrls.Append(fileUrl);
            _eiaQueuedFilePriorities = _eiaQueuedFilePriorities.Append(priority);
            if (_eiaCurrentMaxPriority < priority)
            {
                _eiaCurrentMaxPriority = priority;
            }
            
            if (_eiaFileIsLoading)
            {
                ConsoleDebug($"Loading already in progress, queuing {fileUrl}", _eiaFileLoaderPrefixes);
                return;
            }
            
            ConsoleDebug($"Loading {fileUrl}", _eiaFileLoaderPrefixes);
            _eiaFileIsLoading = true;
            EIALoadFIleNext();
        }
        
        private int EIAGetHeightPriorityIndex()
        {
            if (_eiaQueuedFilePriorities.Length == 0) return -1;
            var maxPriority = Int32.MinValue;
            var maxPriorityIndex = -1;
            
            for (var i = 0; i < _eiaQueuedFilePriorities.Length; i++)
            {
                if (_eiaQueuedFilePriorities[i] > maxPriority)
                {
                    maxPriority = _eiaQueuedFilePriorities[i];
                    maxPriorityIndex = i;
                }
            }

            return maxPriorityIndex;
        }

        public void EIALoadFIleNext()
        {
            if (_eiaQueuedFileUrls.Length == 0)
            {
                ConsoleDebug("No more URLs to load", _eiaFileLoaderPrefixes);
                _eiaFileIsLoading = false;
                EIAClear();
                return;
            }

            var index = EIAGetHeightPriorityIndex();
            if (index == -1)
            {
                ConsoleDebug("No more URLs to load", _eiaFileLoaderPrefixes);
                _eiaFileIsLoading = false;
                EIAClear();
                return;
            }

            _eiaQueuedFileUrls = _eiaQueuedFileUrls.Remove(index, out _eiaCurrentFileUrl);
            _eiaQueuedSourceUrls = _eiaQueuedSourceUrls.Remove(index, out _eiaCurrentSourceUrl);
            _eiaQueuedFilePriorities = _eiaQueuedFilePriorities.Remove(index, out var priority);

            if (string.IsNullOrEmpty(_eiaCurrentFileUrl) || string.IsNullOrEmpty(_eiaCurrentSourceUrl))
            {
                ConsoleDebug("Empty URL, skipping", _eiaFileLoaderPrefixes);
                SendCustomEvent(nameof(EIALoadFIleNext));
                return;
            }

            if (!EiaParsedFileUrls.Has(_eiaCurrentFileUrl, out _eiaCurrentFileIndex))
            {
                ConsoleDebug($"File not found in cache: {_eiaCurrentFileUrl}", _eiaFileLoaderPrefixes);
                OnFileLoadError(_eiaCurrentFileUrl, LoadError.InvalidFileURL);
                SendCustomEvent(nameof(EIALoadFIleNext));
                return;
            }
            
            ConsoleInfo($"Loading file: {_eiaCurrentFileUrl} from source: {_eiaCurrentSourceUrl} priority: {priority}", _eiaFileLoaderPrefixes);
            
            var file = EiaParsedFileManifests[_eiaCurrentFileIndex];
            if (!file.TryGetValue("u", out var uncompressedToken) || (uncompressedToken.TokenType != TokenType.Double && uncompressedToken.TokenType != TokenType.Int))
            {
                ConsoleError($"Uncompressed size not found: {_eiaCurrentFileUrl}", _eiaFileLoaderPrefixes);
                OnFileLoadError(_eiaCurrentFileUrl, LoadError.InvalidFileURL);
                SendCustomEvent(nameof(EIALoadFIleNext));
                return;
            }
            
            var uncompressedSize = uncompressedToken.TokenType == TokenType.Double ? (int)uncompressedToken.Double : uncompressedToken.Int;
            
            ConsoleDebug($"Decompressing {_eiaCurrentFileUrl} compressed size: {EiaParsedFileBuffers[_eiaCurrentFileIndex].Length} uncompressed size: {uncompressedSize}", _eiaFileLoaderPrefixes);
            
            udonLZ4.DecompressAsync(this, EiaParsedFileBuffers[_eiaCurrentFileIndex], uncompressedSize);
        }

        public void OnLZ4Decompress()
        {
            ConsoleDebug($"File loaded successfully: {_eiaCurrentFileUrl}", _eiaFileLoaderPrefixes);
            _eiaProcessStartTime = Time.realtimeSinceStartup;
            var fileUrl = EiaParsedFileUrls[_eiaCurrentFileIndex];
            var file = EiaParsedFileManifests[_eiaCurrentFileIndex];

            if (
                !file.TryGetValue("t", TokenType.String, out var type)
                || !file.TryGetValue("w", TokenType.Double, out var width)
                || !file.TryGetValue("h", TokenType.Double, out var height)
                || !file.TryGetValue("f", TokenType.String, out var formatToken)
                || !TextZipUtils.ParseTextureFormatString(formatToken.String, out _eiaCurrentFileFormat)
            )
            {
                ConsoleError($"File type not found: {fileUrl}", _eiaFileLoaderPrefixes);
                OnFileLoadError(fileUrl, LoadError.InvalidFileURL);
                SendCustomEvent(nameof(EIALoadFIleNext));
                return;
            }
            
            _eiaCurrentDecodedData = udonLZ4.GetDecompressedData();
            _eiaCurrentFile = file;

            ConsoleDebug($"OnLZ4Decompress: {Time.realtimeSinceStartup - _eiaProcessStartTime}", _eiaFileLoaderPrefixes);
            SendCustomEventDelayedFrames(nameof(EIAGenerateImageBytesAsyncInit), _eiaProcessIntervalFrame);
        }

        public void OnLZ4DecompressError()
        {
            ConsoleDebug($"Failed to decode file: {_eiaCurrentFileUrl}", _eiaFileLoaderPrefixes);
            OnFileLoadError(_eiaCurrentFileUrl, LoadError.InvalidFileURL);
            
            SendCustomEventDelayedSeconds(nameof(EIALoadFIleNext), 1f);
        }

        public void EIAGenerateImageBytesAsyncInit()
        {
            _eiaProcessStartTime = Time.realtimeSinceStartup;
            ConsoleDebug($"EIA Generate Image Bytes Async Init: {_eiaCurrentFileUrl}", _eiaFileLoaderPrefixes);
            if (!_eiaCurrentFile.TryGetValue("t", TokenType.String, out var type))
            {
                EIAOnGenerateImageBytesError();
                return;
            }
            
            if (type.String == "m")
            {
                _eiaCurrentFileBinary = _eiaCurrentDecodedData;
                EIAGenerateImageBytesAsyncMaster();
                return;
            }

            if (
                !_eiaCurrentFile.TryGetValue("b", TokenType.String, out var baseUrl)
                || !_eiaCurrentFile.TryGetValue("w", TokenType.Double, out var width)
                || !_eiaCurrentFile.TryGetValue("h", TokenType.Double, out var height)
                || !_eiaCurrentFile.TryGetValue("r", TokenType.DataList, out var rects)
            )
            {
                VRCJson.TrySerializeToJson(_eiaCurrentFile, JsonExportType.Beautify, out var json);
                ConsoleError(json.String, _eiaFileLoaderPrefixes);
                ConsoleError($"Base URL not found: {_eiaCurrentFile}", _eiaFileLoaderPrefixes);
                EIAOnGenerateImageBytesError();
                return;
            }
            
            _eiaCurrentFileWidth = (int)width.Double;
            _eiaCurrentFileHeight = (int)height.Double;
            _eiaCurrentFileRects = rects.DataList;
            _eiaCurrentFileRectsIndex = 0;
            _eiaCurrentFileBaseUrl = baseUrl.String;
            _eiaCurrentFileBytesPerPixel = TextureFormat.RGB24.GetBytePerPixel();

            
            ConsoleDebug($"EIAGenerateImageBytesAsyncInit: {Time.realtimeSinceStartup - _eiaProcessStartTime}", _eiaFileLoaderPrefixes);
            SendCustomEventDelayedFrames(nameof(EIAGenerateImageBytesAsyncInitCopy),_eiaProcessIntervalFrame);
        }

        public void EIAGenerateImageBytesAsyncMaster()
        {
            if (
                !_eiaCurrentFile.TryGetValue("w", TokenType.Double, out var width)
                || !_eiaCurrentFile.TryGetValue("h", TokenType.Double, out var height)
            )
            {
                VRCJson.TrySerializeToJson(_eiaCurrentFile, JsonExportType.Beautify, out var json);
                ConsoleError(json.String, _eiaFileLoaderPrefixes);
                EIAOnGenerateImageBytesError();
                return;
            }
            _eiaCurrentFileWidth = (int)width.Double;
            _eiaCurrentFileHeight = (int)height.Double;
            _eiaCurrentFileBytesPerPixel = TextureFormat.RGB24.GetBytePerPixel();
            
            ConsoleDebug($"EIAGenerateImageBytesAsyncInit,EIAGenerateImageBytesAsyncMaster: {Time.realtimeSinceStartup - _eiaProcessStartTime}", _eiaFileLoaderPrefixes);
            EIAOnGenerateImageBytesSuccess();
        }

        public void EIAGenerateImageBytesAsyncInitCopy()
        {
            _eiaProcessStartTime = Time.realtimeSinceStartup;
            var sourceBinary = CcGetBinary(_eiaCurrentSourceUrl, _eiaCurrentFileBaseUrl);
            if (sourceBinary == null)
            {
                ConsoleError($"Base binary not found: {_eiaCurrentFileBaseUrl}", _eiaFileLoaderPrefixes);
                EIAOnGenerateImageBytesError();
                return;
            }
            _eiaCurrentFileBinary = new byte[_eiaCurrentFileWidth * _eiaCurrentFileHeight * _eiaCurrentFileBytesPerPixel];
            Array.Copy(sourceBinary, _eiaCurrentFileBinary, sourceBinary.Length);
            
            ConsoleDebug($"EIAGenerateImageBytesAsyncInitCopy: {Time.realtimeSinceStartup - _eiaProcessStartTime}", _eiaFileLoaderPrefixes);
            SendCustomEventDelayedFrames(nameof(EIAGenerateImageBytesAsync), _eiaProcessIntervalFrame);
        }

        public void EIAGenerateImageBytesAsync()
        {
            _eiaProcessStartTime = Time.realtimeSinceStartup;
            if (!_eiaCurrentFileRects.TryGetValue(_eiaCurrentFileRectsIndex, TokenType.DataDictionary, out var rect))
            {
                ConsoleError($"Rect not found: {_eiaCurrentFile}", _eiaFileLoaderPrefixes);
                EIAOnGenerateImageBytesError();
                return;
            }

            if (
                (!rect.DataDictionary.TryGetValue("x", out var baseXToken) && baseXToken.TokenType != TokenType.Double && baseXToken.TokenType != TokenType.Int)
                || (!rect.DataDictionary.TryGetValue("y", out var baseYToken) && baseYToken.TokenType != TokenType.Double && baseYToken.TokenType != TokenType.Int)
                || (!rect.DataDictionary.TryGetValue("w", out var rectWidthToken) && rectWidthToken.TokenType != TokenType.Double && rectWidthToken.TokenType != TokenType.Int)
                || (!rect.DataDictionary.TryGetValue("h", out var rectHeightToken) && rectHeightToken.TokenType != TokenType.Double && rectHeightToken.TokenType != TokenType.Int)
                || (!rect.DataDictionary.TryGetValue("s", out var startToken) && startToken.TokenType != TokenType.Double && startToken.TokenType != TokenType.Int)
            )
            {
                VRCJson.TrySerializeToJson(rect, JsonExportType.Beautify, out var json);
                ConsoleError(json.String, _eiaFileLoaderPrefixes);
                ConsoleError($"x Rect metadata not found: {_eiaCurrentFile}", _eiaFileLoaderPrefixes);
                EIAOnGenerateImageBytesError();
                return;
            }
            
            var baseX = baseXToken.TokenType == TokenType.Double ? (int) baseXToken.Double : baseXToken.Int;
            var baseY = baseYToken.TokenType == TokenType.Double ? (int) baseYToken.Double : baseYToken.Int;
            var rectWidth = rectWidthToken.TokenType == TokenType.Double ? (int) rectWidthToken.Double : rectWidthToken.Int;
            var rectHeight = rectHeightToken.TokenType == TokenType.Double ? (int) rectHeightToken.Double : rectHeightToken.Int;
            var start = startToken.TokenType == TokenType.Double ? (int) startToken.Double : startToken.Int;

            var rectLineByteLength = rectWidth * _eiaCurrentFileBytesPerPixel;
            var imageLineByteLength = _eiaCurrentFileWidth * _eiaCurrentFileBytesPerPixel;
            for (var y = 0; y < rectHeight; y++)
            {
                Array.Copy(
                    _eiaCurrentDecodedData,
                    start + y * rectLineByteLength,
                    _eiaCurrentFileBinary,
                    (baseY + y) * imageLineByteLength + baseX * _eiaCurrentFileBytesPerPixel,
                    rectLineByteLength
                );
            }
            
            _eiaCurrentFileRectsIndex++;
            
            ConsoleDebug($"EIAGenerateImageBytesAsync: {Time.realtimeSinceStartup - _eiaProcessStartTime}", _eiaFileLoaderPrefixes);
            if (_eiaCurrentFileRectsIndex < _eiaCurrentFileRects.Count)
            {
                SendCustomEventDelayedFrames(nameof(EIAGenerateImageBytesAsync), _eiaProcessIntervalFrame);
                return;
            }
            
            EIAOnGenerateImageBytesSuccess();
        }
        
        private void EIAOnGenerateImageBytesError()
        {
            ConsoleDebug($"Failed to decode file: {_eiaCurrentFileUrl}", _eiaFileLoaderPrefixes);
            OnFileLoadError(_eiaCurrentFileUrl, LoadError.InvalidFileURL);

            EIAClear();
            SendCustomEventDelayedSeconds(nameof(EIALoadFIleNext), 1f);
        }

        private void EIAOnGenerateImageBytesSuccess()
        {
            _eiaProcessStartTime = Time.realtimeSinceStartup;
            var texture = new Texture2D(_eiaCurrentFileWidth, _eiaCurrentFileHeight, _eiaCurrentFileFormat, false);
            texture.LoadRawTextureData(_eiaCurrentFileBinary);
            texture.Apply();
            
            CcSetTexture(_eiaCurrentSourceUrl, _eiaCurrentFileUrl, texture, _eiaCurrentFile, _eiaCurrentFileBinary, _eiaCurrentFileFormat);
            OnFileLoadSuccess(_eiaCurrentFileUrl);
            
            _eiaCurrentFileUrl = string.Empty;
            _eiaCurrentSourceUrl = string.Empty;
            udonLZ4.ClearDecompressedData();
            
            EIAClear();
            ConsoleDebug($"EIAOnGenerateImageBytesSuccess: {Time.realtimeSinceStartup - _eiaProcessStartTime}", _eiaFileLoaderPrefixes);
            
            SendCustomEventDelayedFrames(nameof(EIALoadFIleNext), 1);
        }
    }
}
