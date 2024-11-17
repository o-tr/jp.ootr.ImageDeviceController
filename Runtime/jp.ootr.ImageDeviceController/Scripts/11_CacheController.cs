using jp.ootr.common;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace jp.ootr.ImageDeviceController
{
    public class CacheController : CommonClass
    {
        private readonly string[] _cacheControllerPrefixes = { "CacheController" };

        /**
         * type CacheFiles = {
         * [source: string]: {
         * files: {
         * [fileName: string]: {
         * texture: Texture2D;
         * usedCount: number;
         * }
         * }
         * usedCount: number;
         * }
         * }
         */
        private readonly DataDictionary _oCacheFiles = new DataDictionary();

        private byte[][] _cacheBinary = new byte[0][]; //DataDictionaryにbyte[]が入らないので別で取り扱う
        private string[] _cacheBinaryNames = new string[0];
        private Cache CacheFiles => (Cache)_oCacheFiles;

        public virtual Texture2D CcGetTexture(string source, string fileName)
        {
            if (!CcHasTexture(source, fileName)) return null;
            var files = CacheFiles.GetSource(source);
            var file = files.GetFile(fileName);
            files.IncreaseUsedCount();
            file.IncreaseUsedCount();
            var texture = file.GetTexture();
            if (!Utilities.IsValid(texture)) return TryRegenerateTexture(file);
            return texture;
        }

        private Texture2D TryRegenerateTexture(File file)
        {
            var key = file.GetCacheKey();
            if (!_cacheBinaryNames.Has(key, out var index)) return null;
            ConsoleDebug($"regenerate texture: {key}", _cacheControllerPrefixes);
            var format = file.GetTextureFormat();
            var bytes = _cacheBinary[index];
            var texture = new Texture2D(file["width"].Int, file["height"].Int, format, false);
            texture.LoadRawTextureData(bytes);
            texture.Apply();
            file.SetTexture(texture);
            return texture;
        }

        protected bool CcHasCache(string source)
        {
            return CacheFiles.HasSource(source);
        }

        protected Source CcGetCache(string source)
        {
            return CacheFiles.GetSource(source);
        }

        protected virtual bool CcHasTexture(string source, string fileName)
        {
            return CacheFiles.HasSource(source) &&
                   CacheFiles.GetSource(source).HasFile(fileName);
        }

        public virtual void CcReleaseTexture(string sourceName, string fileName)
        {
            if (!CcHasTexture(sourceName, fileName)) return;
            var source = CacheFiles.GetSource(sourceName);
            var file = source.GetFile(fileName);
            if (source.DecreaseUsedCount() < 1)
            {
                foreach (var tmpFileName in source.GetFileNames()) source.GetFile(tmpFileName).DestroyTexture();
                var keys = CacheFiles.RemoveSource(sourceName);
                foreach (var key in keys)
                {
                    if (!_cacheBinaryNames.Has(key, out var index)) continue;
                    _cacheBinary = _cacheBinary.Remove(index);
                    _cacheBinaryNames = _cacheBinaryNames.Remove(index);
                }

                return;
            }

            if (file.DecreaseUsedCount() < 1)
            {
                var key = file.GetCacheKey();
                if (key.IsNullOrEmpty() && _cacheBinaryNames.Has(key))
                {
                    ConsoleInfo($"release texture: {sourceName}/{fileName}", _cacheControllerPrefixes);
                    file.DestroyTexture();
                }
                else
                {
                    ConsoleInfo($"cannot release texture: {sourceName}/{fileName}", _cacheControllerPrefixes);
                }
            }
        }

        public virtual Metadata CcGetMetadata(string source, string fileName)
        {
            if (!CcHasTexture(source, fileName)) return null;
            return CacheFiles.GetSource(source).GetFile(fileName).GetMetadata();
        }

        protected virtual void CcSetTexture(string source, string fileName, Texture2D texture, byte[] bytes = null,
            TextureFormat format = TextureFormat.RGBA32)
        {
            CcSetTexture(source, fileName, texture, new DataDictionary(), bytes, format);
        }

        protected virtual void CcSetTexture(string source, string fileName, Texture2D texture, DataDictionary metadata,
            byte[] bytes = null, TextureFormat format = TextureFormat.RGBA32)
        {
            if (!CacheFiles.HasSource(source)) CacheFiles.AddSource(source);

            var files = CacheFiles.GetSource(source);
            if (files.HasFile(fileName))
            {
                ConsoleError($"file already exists: {source}/{fileName}", _cacheControllerPrefixes);
                return;
            }

            string cacheKey = null;

            if (Utilities.IsValid(bytes))
            {
                cacheKey = $"cache://{source}/{fileName}";
                _cacheBinary = _cacheBinary.Append(bytes);
                _cacheBinaryNames = _cacheBinaryNames.Append(cacheKey);
            }

            files.AddFile(fileName, texture, metadata, cacheKey, format);
        }

        protected virtual void CcOnRelease(string source)
        {
        }

        public virtual string DumpCache()
        {
            var result = "";
            var cacheKeys = CacheFiles.GetKeys().ToStringArray();
            for (var i = 0; i < cacheKeys.Length; i++)
            {
                var source = CacheFiles.GetSource(cacheKeys[i]);
                var fileNames = source.GetFileNames();
                result += $"{cacheKeys[i]}: {source["usedCount"].Int}\n";
                for (var j = 0; j < fileNames.Length; j++)
                {
                    var file = source.GetFile(fileNames[j]);
                    result += $"{cacheKeys[i]}/{fileNames[j]}: {file["usedCount"].Int}\n";
                }

                result += "\n";
            }

            return result;
        }
    }
}
