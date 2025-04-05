using JetBrains.Annotations;
using jp.ootr.common;
using UnityEngine;
using VRC.SDK3.Data;

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

        [CanBeNull]
        public virtual Texture2D CcGetTexture([CanBeNull] string sourceUrl, [CanBeNull] string fileUrl)
        {
            if (!CcHasTexture(sourceUrl, fileUrl))
            {
                ConsoleError($"texture not found: {sourceUrl}/{fileUrl}", _cacheControllerPrefixes);
                return null;
            }
            var source = CacheFiles.GetSource(sourceUrl);
            var file = source.GetFile(fileUrl);
            var sourceCount = source.IncreaseUsedCount();
            var fileCount = file.IncreaseUsedCount();
            var texture = file.GetTexture();
            ConsoleDebug($"get texture: {sourceUrl}/{fileUrl} ({sourceCount}/{fileCount})", _cacheControllerPrefixes);
            if (texture == null) return TryRegenerateTexture(file);
            return texture;
        }
        
        [CanBeNull]
        public virtual byte[] CcGetBinary([CanBeNull] string sourceUrl, [CanBeNull] string fileUrl)
        {
            ConsoleDebug($"get binary: {sourceUrl}/{fileUrl}", _cacheControllerPrefixes);
            if (!CcHasTexture(sourceUrl, fileUrl))
            {
                ConsoleError($"texture not found: {sourceUrl}/{fileUrl}", _cacheControllerPrefixes);
                return null;
            }
            var source = CacheFiles.GetSource(sourceUrl);
            var file = source.GetFile(fileUrl);
            var key = file.GetCacheKey();
            if (key.IsNullOrEmpty() || !_cacheBinaryNames.Has(key, out var index))
            {
                ConsoleError($"binary not found: {sourceUrl}/{fileUrl}", _cacheControllerPrefixes);
                return null;
            }
            ConsoleDebug($"get binary: {sourceUrl}/{fileUrl}", _cacheControllerPrefixes);
            return _cacheBinary[index];
        }

        [CanBeNull]
        private Texture2D TryRegenerateTexture([CanBeNull] File file)
        {
            if (file == null) return null;
            var key = file.GetCacheKey();
            if (!_cacheBinaryNames.Has(key, out var index)) return null;
            ConsoleDebug($"regenerate texture: {key}", _cacheControllerPrefixes);
            var format = file.GetTextureFormat();
            var bytes = _cacheBinary[index];
            var texture = new Texture2D(file.GetWidth(), file.GetHeight(), format, false);
            texture.LoadRawTextureData(bytes);
            texture.Apply();
            file.SetTexture(texture);
            return texture;
        }

        protected bool CcHasCache([CanBeNull] string sourceUrl)
        {
            return CacheFiles.HasSource(sourceUrl);
        }

        [CanBeNull]
        protected Source CcGetCache([CanBeNull] string sourceUrl)
        {
            return CacheFiles.GetSource(sourceUrl);
        }

        protected virtual bool CcHasTexture([CanBeNull] string sourceUrl, [CanBeNull] string fileUrl)
        {
            return CacheFiles.HasSource(sourceUrl) &&
                   CacheFiles.GetSource(sourceUrl).HasFile(fileUrl);
        }

        public virtual void CcReleaseTexture([CanBeNull] string sourceUrl, [CanBeNull] string fileUrl)
        {
            if (!CcHasTexture(sourceUrl, fileUrl)) return;
            var source = CacheFiles.GetSource(sourceUrl);
            var file = source.GetFile(fileUrl);
            var sourceCount = source.DecreaseUsedCount();
            if (sourceCount < 1)
            {
                foreach (var tmpFileUrl in source.GetFileUrls()) source.GetFile(tmpFileUrl).DestroyTexture();
                var keys = CacheFiles.RemoveSource(sourceUrl);
                foreach (var key in keys)
                {
                    if (!_cacheBinaryNames.Has(key, out var index)) continue;
                    _cacheBinary = _cacheBinary.Remove(index);
                    _cacheBinaryNames = _cacheBinaryNames.Remove(index);
                }
                ConsoleDebug($"destroy source: {sourceUrl}", _cacheControllerPrefixes);

                return;
            }
            
            var fileCount = file.DecreaseUsedCount();
            if (fileCount < 1)
            {
                var key = file.GetCacheKey();
                if (!key.IsNullOrEmpty() && _cacheBinaryNames.Has(key))
                {
                    ConsoleDebug($"destroy texture: {sourceUrl}/{fileUrl}", _cacheControllerPrefixes);
                    file.DestroyTexture();
                }
                else
                {
                    ConsoleWarn($"cannot release texture because it is not has binary cache: {sourceUrl}/{fileUrl}", _cacheControllerPrefixes);
                }
            }
            ConsoleDebug($"release texture: {sourceUrl}/{fileUrl} ({sourceCount}/{fileCount})", _cacheControllerPrefixes);
        }

        [CanBeNull]
        public virtual Metadata CcGetMetadata([CanBeNull] string source, [CanBeNull] string fileName)
        {
            if (!CcHasTexture(source, fileName)) return null;
            return CacheFiles.GetSource(source).GetFile(fileName).GetMetadata();
        }

        protected virtual void CcSetTexture([CanBeNull] string source, [CanBeNull] string fileName,
            [CanBeNull] Texture2D texture, [CanBeNull] byte[] bytes = null,
            TextureFormat format = TextureFormat.RGBA32)
        {
            CcSetTexture(source, fileName, texture, new DataDictionary(), bytes, format);
        }

        protected virtual void CcSetTexture([CanBeNull] string source, [CanBeNull] string fileName,
            [CanBeNull] Texture2D texture, [CanBeNull] DataDictionary metadata,
            [CanBeNull] byte[] bytes = null, TextureFormat format = TextureFormat.RGBA32)
        {
            if (!CacheFiles.HasSource(source)) CacheFiles.AddSource(source);

            var files = CacheFiles.GetSource(source);
            if (files.HasFile(fileName))
            {
                ConsoleError($"file already exists: {source}/{fileName}", _cacheControllerPrefixes);
                return;
            }

            string cacheKey = null;

            if (bytes != null)
            {
                cacheKey = $"cache://{source}/{fileName}";
                _cacheBinary = _cacheBinary.Append(bytes);
                _cacheBinaryNames = _cacheBinaryNames.Append(cacheKey);
            }
            ConsoleDebug($"add texture: {source}/{fileName}, {cacheKey}", _cacheControllerPrefixes);

            files.AddFile(fileName, texture, metadata, cacheKey, format);
        }

        protected virtual void CcOnRelease([CanBeNull] string source)
        {
        }
        
        [CanBeNull]
        public string[] CcGetFileNames([CanBeNull] string sourceName)
        {
            var source = CacheFiles.GetSource(sourceName);
            if (source == null) return null;
            return source.GetFileUrls();
        }

        [NotNull]
        public virtual string DumpCache()
        {
            var result = "";
            var cacheKeys = CacheFiles.GetKeys().ToStringArray();
            foreach (var key in cacheKeys)
            {
                var source = CacheFiles.GetSource(key);
                var fileNames = source.GetFileUrls();
                result += $"{key}: {source.GetUsedCount()}\n";
                foreach (var fileName in fileNames)
                {
                    var file = source.GetFile(fileName);
                    result += $"{key}/{fileName}: {file.GetUsedCount()}\n";
                }

                result += "\n";
            }

            return result;
        }
    }
}
