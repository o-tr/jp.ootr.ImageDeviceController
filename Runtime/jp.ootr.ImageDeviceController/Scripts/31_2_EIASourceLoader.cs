using System;
using System.Text;
using JetBrains.Annotations;
using jp.ootr.common;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;
using VRC.Udon.Common.Enums;

namespace jp.ootr.ImageDeviceController
{
    public class EiaSourceLoader : ZipSourceLoader {
        private readonly string[] _eiaSourceLoaderPrefixes = { "EIASourceLoader" };
        [SerializeField] protected UdonLZ4.UdonLZ4 udonLZ4;
        
        protected const int EiaDelayFrames = 1;
        
        private string _eiaSourceUrl;
        private DataDictionary _eiaCurrentManifest;
        private DataList _eiaCurrentFiles;
        private byte[] _eiaCurrentContent;
        private int _eiaCurrentContentBufferStart;
        private int _eiaCurrentIndex;
        
        private string[] _eiaCurrentFileUrls = new string[0];
        
        protected string[] EiaParsedFileUrls = new string[0];
        protected byte[][] EiaParsedFileBuffers = new byte[0][];
        protected DataDictionary[] EiaParsedFileManifests = new DataDictionary[0];
        
        protected void OnEIALoadSuccess(IVRCStringDownload result)
        {
            if (udonLZ4 == null)
            {
                ConsoleError("UdonBase64CSVRLE component is not set.", _eiaSourceLoaderPrefixes);
                EIAOnLoadError(result.Url.ToString(), LoadError.MissingBase64RLE);
                return;
            }
            ConsoleLog($"download success from {result.Url}", _eiaSourceLoaderPrefixes);
            if (!result.IsValidEIA())
            {
                EIAOnLoadError(result.Url.ToString(), LoadError.InvalidEIAFile);
                ConsoleError("invalid EIA file.", _eiaSourceLoaderPrefixes);
                return;
            }
            
            _eiaSourceUrl = result.Url.ToString();
            var content = result.ResultBytes;

            var eiaManifestStart = Array.IndexOf(content, (byte)'^');
            var eiaManifestEnd = Array.IndexOf(content, (byte)'$');
            
            if (eiaManifestStart < 0 || eiaManifestEnd < 0 || eiaManifestStart > eiaManifestEnd)
            {
                ConsoleError("Invalid EIA manifest format", _eiaSourceLoaderPrefixes);
                EIAOnLoadError(_eiaSourceUrl, LoadError.InvalidEIAFile);
                return;
            }

            var eiaManifestStr =
                Encoding.UTF8.GetString(content, eiaManifestStart + 1, eiaManifestEnd - eiaManifestStart - 1);
            if (!VRCJson.TryDeserializeFromJson(eiaManifestStr, out var eiaManifest) || eiaManifest.TokenType != TokenType.DataDictionary)
            {
                ConsoleError("Failed to parse EIA manifest", _eiaSourceLoaderPrefixes);
                EIAOnLoadError(_eiaSourceUrl, LoadError.InvalidEIAFile);
                return;
            }
            
            _eiaCurrentManifest = eiaManifest.DataDictionary;
            if (!_eiaCurrentManifest.TryGetValue("i", TokenType.DataList, out var eiaCurrentFiles))
            {
                ConsoleError("EIA manifest does not contain 'i' key", _eiaSourceLoaderPrefixes);
                EIAOnLoadError(_eiaSourceUrl, LoadError.InvalidEIAFile);
                return;
            }
            
            _eiaCurrentFileUrls = new string[0];
            _eiaCurrentContent = content;
            _eiaCurrentContentBufferStart = eiaManifestEnd + 1;
            _eiaCurrentFiles = eiaCurrentFiles.DataList;
            _eiaCurrentIndex = 0;
            ConsoleLog($"success to load EIA manifest: {_eiaCurrentManifest["i"].DataList.Count} files", _eiaSourceLoaderPrefixes);
            
            SendCustomEventDelayedFrames(nameof(EIAParseManifest), EiaDelayFrames, EventTiming.LateUpdate);
        }

        public void EIAParseManifest()
        {
            if (
                !_eiaCurrentFiles.TryGetValue(_eiaCurrentIndex, TokenType.DataDictionary, out var fileManifest)
                || !fileManifest.DataDictionary.TryGetValue("t", TokenType.String, out var fileType)
                || !fileManifest.DataDictionary.TryGetValue("n", TokenType.String, out var fileName)
                || !fileManifest.DataDictionary.TryGetValue("f", TokenType.String, out var fileFormat)
                || !fileManifest.DataDictionary.TryGetValue("w", TokenType.Double, out var fileWidth)
                || !fileManifest.DataDictionary.TryGetValue("h", TokenType.Double, out var fileHeight)
            )
            {
                ConsoleError("EIA manifest does not contain file manifest", _eiaSourceLoaderPrefixes);
                EIAOnLoadError(_eiaSourceUrl, LoadError.InvalidEIAFile);
                return;
            }
            
            var fileUrl = EIABuildFileName(_eiaSourceUrl, fileName.String);

            if (fileType.String == "m")
            {
                EIAParseMasterImage(fileManifest.DataDictionary, fileUrl, fileName.String, (int)fileWidth.Double, (int)fileHeight.Double);
                return;
            }
            
            EIAParseCroppedImage(fileManifest.DataDictionary, fileUrl, fileName.String, (int)fileWidth.Double, (int)fileHeight.Double);
        }

        private void EIAParseMasterImage(DataDictionary fileManifest, string fileUrl, string fileName, int fileWidth, int fileHeight)
        {
            if (
                !fileManifest.TryGetValue("s", TokenType.Double, out var fileBufferStartToken)
                || !fileManifest.TryGetValue("l", TokenType.Double, out var fileBufferLengthToken)
            )
            {
                ConsoleError("EIA manifest does not contain file buffer", _eiaSourceLoaderPrefixes);
                EIAOnLoadError(_eiaSourceUrl, LoadError.InvalidEIAFile);
                return;
            }
            var fileBufferStart = (int)fileBufferStartToken.Double;
            var fileBufferLength = (int)fileBufferLengthToken.Double;

            _eiaCurrentFileUrls = _eiaCurrentFileUrls.Append(fileUrl);
            EiaParsedFileUrls = EiaParsedFileUrls.Append(fileUrl);
            EiaParsedFileBuffers = EiaParsedFileBuffers.Append(_eiaCurrentContent.Slice(_eiaCurrentContentBufferStart+fileBufferStart, fileBufferLength));
            EiaParsedFileManifests = EiaParsedFileManifests.Append(fileManifest);
                
            ConsoleLog($"EIA file manifest: {fileName} ({fileWidth}x{fileHeight})", _eiaSourceLoaderPrefixes);
            _eiaCurrentIndex++;
            if (_eiaCurrentIndex >= _eiaCurrentFiles.Count)
            {
                ConsoleLog("EIA file manifest parsing complete", _eiaSourceLoaderPrefixes);
                EIAOnLoadSuccess(_eiaSourceUrl, _eiaCurrentFileUrls);
                return;
            }
                
            SendCustomEventDelayedFrames(nameof(EIAParseManifest), EiaDelayFrames, EventTiming.LateUpdate);
        }


        private void EIAParseCroppedImage(DataDictionary _fileManifest, string fileUrl, string fileName, int fileWidth, int fileHeight)
        {
            var fileManifest = _fileManifest.DeepClone();
            if (
                !fileManifest.TryGetValue("b", TokenType.String, out var basePathToken)
                || !fileManifest.TryGetValue("r", TokenType.DataList, out var fileRectToken)
                || fileRectToken.DataList.Count == 0
                || !fileRectToken.DataList.TryGetValue(0, TokenType.DataDictionary, out var firstRectToken)
                || !firstRectToken.DataDictionary.TryGetValue("s", TokenType.Double, out var fileBufferFirstStartToken)
                || !fileRectToken.DataList.TryGetValue(fileRectToken.DataList.Count - 1, TokenType.DataDictionary, out var lastRectToken)
                || !lastRectToken.DataDictionary.TryGetValue("s", TokenType.Double, out var fileBufferLastStartToken)
                || !lastRectToken.DataDictionary.TryGetValue("l", TokenType.Double, out var fileBufferLastLengthToken)
                || !fileManifest.TryGetValue("s", TokenType.Double, out var fileStartToken)
                || !fileManifest.TryGetValue("l", TokenType.Double, out var fileLengthToken)
            )
            {
                ConsoleError("EIA manifest does not contain file rect", _eiaSourceLoaderPrefixes);
                EIAOnLoadError(_eiaSourceUrl, LoadError.InvalidEIAFile);
                return;
            }
            
            var fileBufferFirstStart = (int)fileBufferFirstStartToken.Double;
            var fileBufferLastEnd = (int)fileBufferLastStartToken.Double + (int)fileBufferLastLengthToken.Double;
            var fileBufferLength = fileBufferLastEnd - fileBufferFirstStart;
            
            var fileStart = (int)fileStartToken.Double;
            var fileLength = (int)fileLengthToken.Double;

            for (int i = 0; i < fileRectToken.DataList.Count; i++)
            {
                if (
                    !fileRectToken.DataList.TryGetValue(i, TokenType.DataDictionary, out var rect)
                    || !rect.DataDictionary.TryGetValue("s", TokenType.Double, out var rectStartToken)
                )
                {
                    ConsoleError("EIA manifest does not contain file rect", _eiaSourceLoaderPrefixes);
                    EIAOnLoadError(_eiaSourceUrl, LoadError.InvalidEIAFile);
                    return;
                }
                var rectStart = (int)rectStartToken.Double;
                if (rectStart < fileBufferFirstStart || rectStart >= fileBufferLastEnd)
                {
                    ConsoleError("EIA manifest does not contain file rect", _eiaSourceLoaderPrefixes);
                    EIAOnLoadError(_eiaSourceUrl, LoadError.InvalidEIAFile);
                    return;
                }
                rect.DataDictionary.SetValue("s", rectStart - fileBufferFirstStart);
            }
            
            fileManifest.SetValue("b", EIABuildFileName(_eiaSourceUrl, basePathToken.String));
            
            _eiaCurrentFileUrls = _eiaCurrentFileUrls.Append(fileUrl);
            EiaParsedFileUrls = EiaParsedFileUrls.Append(fileUrl);
            EiaParsedFileBuffers = EiaParsedFileBuffers.Append(_eiaCurrentContent.Slice(_eiaCurrentContentBufferStart + fileStart, fileLength));
            EiaParsedFileManifests = EiaParsedFileManifests.Append(fileManifest);
            
            ConsoleLog($"EIA file manifest: {fileName} ({fileWidth}x{fileHeight}) with {fileRectToken.DataList.Count} rects", _eiaSourceLoaderPrefixes);
            _eiaCurrentIndex++;
            if (_eiaCurrentIndex >= _eiaCurrentFiles.Count)
            {
                ConsoleLog("EIA file manifest parsing complete", _eiaSourceLoaderPrefixes);
                EIAOnLoadSuccess(_eiaSourceUrl, _eiaCurrentFileUrls);
                return;
            }
            SendCustomEventDelayedFrames(nameof(EIAParseManifest), EiaDelayFrames, EventTiming.LateUpdate);
        }

        private string EIABuildFileName(string sourceUrl, string fileName)
        {
            return $"dynamic-eia{sourceUrl.Substring(5)}/{fileName}";
        }
        
        protected virtual void EIAOnLoadProgress([CanBeNull] string sourceUrl, float progress)
        {
            ConsoleError("EIAOnLoadProgress should not be called from base class", _eiaSourceLoaderPrefixes);
        }

        protected virtual void EIAOnLoadSuccess([CanBeNull] string sourceUrl, [CanBeNull] string[] fileUrls)
        {
            ConsoleError("EIAOnLoadSuccess should not be called from base class", _eiaSourceLoaderPrefixes);
        }

        protected virtual void EIAOnLoadError([CanBeNull] string sourceUrl, LoadError error)
        {
            ConsoleError("EIAOnLoadError should not be called from base class", _eiaSourceLoaderPrefixes);
        }
    }
}
