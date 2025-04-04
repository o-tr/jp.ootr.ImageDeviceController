using JetBrains.Annotations;
using jp.ootr.UdonBase64RLE;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;

namespace jp.ootr.ImageDeviceController
{
    public class ETILoader : ZipLoader {
        private readonly string[] _etiLoaderPrefixes = { "ETILoader" };
        [SerializeField] private UdonBase64CSVRLE base64Rle;
        
        private string _etiSourceUrl;
        private DataDictionary _etiCurrentManifest;
        
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

            var etiManifestStart = result.Result.IndexOf("^", System.StringComparison.Ordinal);
            var etiManifestEnd = result.Result.IndexOf("$", System.StringComparison.Ordinal);
            
            if (etiManifestStart < 0 || etiManifestEnd < 0 || etiManifestStart > etiManifestEnd)
            {
                ConsoleError("Invalid ETI manifest format", _etiLoaderPrefixes);
                ETIOnLoadError(result.Url.ToString(), LoadError.InvalidETIFile);
                return;
            }
            
            var etiManifestStr = result.Result.Substring(etiManifestStart + 1, etiManifestEnd - etiManifestStart - 1);
            if (!VRCJson.TryDeserializeFromJson(etiManifestStr, out var etiManifest) || etiManifest.TokenType != TokenType.DataDictionary)
            {
                ConsoleError("Failed to parse ETI manifest", _etiLoaderPrefixes);
                ETIOnLoadError(result.Url.ToString(), LoadError.InvalidETIFile);
                return;
            }
            
            _etiCurrentManifest = etiManifest.DataDictionary;
            ConsoleLog($"success to load ETI manifest: {_etiCurrentManifest["i"].DataList.Count} files", _etiLoaderPrefixes);
            ETIOnLoadSuccess(result.Url.ToString(), new string[0]);
        }
        
        
        
        protected virtual void ETIOnLoadProgress([CanBeNull] string source, float progress)
        {
            ConsoleError("ETIOnLoadProgress should not be called from base class", _etiLoaderPrefixes);
        }

        protected virtual void ETIOnLoadSuccess([CanBeNull] string source, [CanBeNull] string[] fileNames)
        {
            ConsoleError("ETIOnLoadSuccess should not be called from base class", _etiLoaderPrefixes);
        }

        protected virtual void ETIOnLoadError([CanBeNull] string source, LoadError error)
        {
            ConsoleError("ETIOnLoadError should not be called from base class", _etiLoaderPrefixes);
        }
    }
}
