using JetBrains.Annotations;
using jp.ootr.common;
using jp.ootr.UdonBase64RLE;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;
using VRC.Udon.Common.Enums;

namespace jp.ootr.ImageDeviceController
{
    public class ETILoader : ZipLoader {
        private readonly string[] _etiLoaderPrefixes = { "ETILoader" };
        [SerializeField] private UdonBase64CSVRLE base64Rle;
        
        private const int EtiDelayFrames = 1;
        
        private string _etiSourceUrl;
        private DataDictionary _etiCurrentManifest;
        private DataList _etiCurrentFiles;
        private string _etiCurrentContent;
        private int _etiCurrentIndex;
        
        private string[] _etiCurrentFileUrls = new string[0];
        
        private string[] _etiParsedFileUrls = new string[0];
        private string[] _etiParsedFileBuffers = new string[0];
        private DataDictionary[] _etiParsedFileManifests = new DataDictionary[0];
        
        protected void OnETILoadSuccess(IVRCStringDownload result)
        {
            if (base64Rle == null)
            {
                ConsoleError("UdonBase64CSVRLE component is not set.", _etiLoaderPrefixes);
                ETIOnLoadError(result.Url.ToString(), LoadError.MissingBase64RLE);
                return;
            }
            ConsoleLog($"download success from {result.Url}", _etiLoaderPrefixes);
            if (!result.IsValidETI())
            {
                ETIOnLoadError(result.Url.ToString(), LoadError.InvalidETIFile);
                ConsoleError("invalid ETI file.", _etiLoaderPrefixes);
                return;
            }
            
            _etiSourceUrl = result.Url.ToString();
            var content = result.Result;

            var etiManifestStart = content.IndexOf("^", System.StringComparison.Ordinal);
            var etiManifestEnd = content.IndexOf("$", System.StringComparison.Ordinal);
            
            if (etiManifestStart < 0 || etiManifestEnd < 0 || etiManifestStart > etiManifestEnd)
            {
                ConsoleError("Invalid ETI manifest format", _etiLoaderPrefixes);
                ETIOnLoadError(_etiSourceUrl, LoadError.InvalidETIFile);
                return;
            }
            
            var etiManifestStr = content.Substring(etiManifestStart + 1, etiManifestEnd - etiManifestStart - 1);
            if (!VRCJson.TryDeserializeFromJson(etiManifestStr, out var etiManifest) || etiManifest.TokenType != TokenType.DataDictionary)
            {
                ConsoleError("Failed to parse ETI manifest", _etiLoaderPrefixes);
                ETIOnLoadError(_etiSourceUrl, LoadError.InvalidETIFile);
                return;
            }
            
            _etiCurrentManifest = etiManifest.DataDictionary;
            if (!_etiCurrentManifest.TryGetValue("i", TokenType.DataList, out var etiCurrentFiles))
            {
                ConsoleError("ETI manifest does not contain 'i' key", _etiLoaderPrefixes);
                ETIOnLoadError(_etiSourceUrl, LoadError.InvalidETIFile);
                return;
            }
            
            _etiCurrentFileUrls = new string[0];
            _etiCurrentContent = content.Substring(etiManifestEnd+1);
            _etiCurrentFiles = etiCurrentFiles.DataList;
            _etiCurrentIndex = 0;
            ConsoleLog($"success to load ETI manifest: {_etiCurrentManifest["i"].DataList.Count} files", _etiLoaderPrefixes);
            
            SendCustomEventDelayedFrames(nameof(ETIParseManifest), EtiDelayFrames, EventTiming.LateUpdate);
        }

        public void ETIParseManifest()
        {
            if (
                !_etiCurrentFiles.TryGetValue(_etiCurrentIndex, TokenType.DataDictionary, out var fileManifest)
                || !fileManifest.DataDictionary.TryGetValue("t", TokenType.String, out var fileType)
                || !fileManifest.DataDictionary.TryGetValue("n", TokenType.String, out var fileName)
                || !fileManifest.DataDictionary.TryGetValue("f", TokenType.String, out var fileFormat)
                || !fileManifest.DataDictionary.TryGetValue("w", TokenType.Double, out var fileWidth)
                || !fileManifest.DataDictionary.TryGetValue("h", TokenType.Double, out var fileHeight)
            )
            {
                ConsoleError("ETI manifest does not contain file manifest", _etiLoaderPrefixes);
                ETIOnLoadError(_etiSourceUrl, LoadError.InvalidETIFile);
                return;
            }
            
            var fileUrl = $"dynamic-eti{_etiSourceUrl.Substring(5)}/{fileName.String}";

            if (fileType.String == "m")
            {
                ETIParseMasterImage(fileManifest.DataDictionary, fileUrl, fileName.String, (int)fileWidth.Double, (int)fileHeight.Double);
                return;
            }
            
            ETIParseCroppedImage(fileManifest.DataDictionary, fileUrl, fileName.String, (int)fileWidth.Double, (int)fileHeight.Double);
        }

        private void ETIParseMasterImage(DataDictionary fileManifest, string fileUrl, string fileName, int fileWidth, int fileHeight)
        {
            if (
                !fileManifest.TryGetValue("s", TokenType.Double, out var fileBufferStartToken)
                || !fileManifest.TryGetValue("l", TokenType.Double, out var fileBufferLengthToken)
            )
            {
                ConsoleError("ETI manifest does not contain file buffer", _etiLoaderPrefixes);
                ETIOnLoadError(_etiSourceUrl, LoadError.InvalidETIFile);
                return;
            }
            var fileBufferStart = (int)fileBufferStartToken.Double;
            var fileBufferLength = (int)fileBufferLengthToken.Double;

            _etiCurrentFileUrls = _etiCurrentFileUrls.Append(fileUrl);
            _etiParsedFileUrls = _etiParsedFileUrls.Append(fileUrl);
            _etiParsedFileBuffers = _etiParsedFileBuffers.Append(_etiCurrentContent.Substring(fileBufferStart, fileBufferLength));
            _etiParsedFileManifests = _etiParsedFileManifests.Append(fileManifest);
                
            ConsoleLog($"ETI file manifest: {fileName} ({fileWidth}x{fileHeight})", _etiLoaderPrefixes);
            _etiCurrentIndex++;
            if (_etiCurrentIndex >= _etiCurrentFiles.Count)
            {
                ConsoleLog("ETI file manifest parsing complete", _etiLoaderPrefixes);
                ETIOnLoadSuccess(_etiSourceUrl, _etiCurrentFileUrls);
                return;
            }
                
            SendCustomEventDelayedFrames(nameof(ETIParseManifest), EtiDelayFrames, EventTiming.LateUpdate);
        }


        private void ETIParseCroppedImage(DataDictionary _fileManifest, string fileUrl, string fileName, int fileWidth, int fileHeight)
        {
            var fileManifest = _fileManifest.DeepClone();
            if (
                !fileManifest.TryGetValue("r", TokenType.DataList, out var fileRectToken)
                || fileRectToken.DataList.Count == 0
                || !fileRectToken.DataList.TryGetValue(0, TokenType.DataDictionary, out var firstRectToken)
                || !firstRectToken.DataDictionary.TryGetValue("s", TokenType.Double, out var fileBufferFirstStartToken)
                || !fileRectToken.DataList.TryGetValue(fileRectToken.DataList.Count - 1, TokenType.DataDictionary, out var lastRectToken)
                || !lastRectToken.DataDictionary.TryGetValue("s", TokenType.Double, out var fileBufferLastStartToken)
                || !lastRectToken.DataDictionary.TryGetValue("l", TokenType.Double, out var fileBufferLastLengthToken)
            )
            {
                ConsoleError("ETI manifest does not contain file rect", _etiLoaderPrefixes);
                ETIOnLoadError(_etiSourceUrl, LoadError.InvalidETIFile);
                return;
            }
            
            var fileBufferFirstStart = (int)fileBufferFirstStartToken.Double;
            var fileBufferLastEnd = (int)fileBufferLastStartToken.Double + (int)fileBufferLastLengthToken.Double;
            var fileBufferLength = fileBufferLastEnd - fileBufferFirstStart;

            for (int i = 0; i < fileRectToken.DataList.Count; i++)
            {
                if (
                    !fileRectToken.DataList.TryGetValue(i, TokenType.DataDictionary, out var rect)
                    || !rect.DataDictionary.TryGetValue("s", TokenType.Double, out var rectStartToken)
                )
                {
                    ConsoleError("ETI manifest does not contain file rect", _etiLoaderPrefixes);
                    ETIOnLoadError(_etiSourceUrl, LoadError.InvalidETIFile);
                    return;
                }
                var rectStart = (int)rectStartToken.Double;
                if (rectStart < fileBufferFirstStart || rectStart >= fileBufferLastEnd)
                {
                    ConsoleError("ETI manifest does not contain file rect", _etiLoaderPrefixes);
                    ETIOnLoadError(_etiSourceUrl, LoadError.InvalidETIFile);
                    return;
                }
                rect.DataDictionary.SetValue("s", rectStart - fileBufferFirstStart);
            }
            
            _etiCurrentFileUrls = _etiCurrentFileUrls.Append(fileUrl);
            _etiParsedFileUrls = _etiParsedFileUrls.Append(fileUrl);
            _etiParsedFileBuffers = _etiParsedFileBuffers.Append(_etiCurrentContent.Substring(fileBufferFirstStart, fileBufferLength));
            _etiParsedFileManifests = _etiParsedFileManifests.Append(fileManifest);
            
            ConsoleLog($"ETI file manifest: {fileName} ({fileWidth}x{fileHeight}) with {fileRectToken.DataList.Count} rects", _etiLoaderPrefixes);
            _etiCurrentIndex++;
            if (_etiCurrentIndex >= _etiCurrentFiles.Count)
            {
                ConsoleLog("ETI file manifest parsing complete", _etiLoaderPrefixes);
                ETIOnLoadSuccess(_etiSourceUrl, _etiCurrentFileUrls);
                return;
            }
            SendCustomEventDelayedFrames(nameof(ETIParseManifest), EtiDelayFrames, EventTiming.LateUpdate);
        }
        
        protected virtual void ETIOnLoadProgress([CanBeNull] string sourceUrl, float progress)
        {
            ConsoleError("ETIOnLoadProgress should not be called from base class", _etiLoaderPrefixes);
        }

        protected virtual void ETIOnLoadSuccess([CanBeNull] string sourceUrl, [CanBeNull] string[] fileUrls)
        {
            ConsoleError("ETIOnLoadSuccess should not be called from base class", _etiLoaderPrefixes);
        }

        protected virtual void ETIOnLoadError([CanBeNull] string sourceUrl, LoadError error)
        {
            ConsoleError("ETIOnLoadError should not be called from base class", _etiLoaderPrefixes);
        }
    }
}
