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
        public virtual Texture2D CcGetTexture([CanBeNull] string sourceName, [CanBeNull] string fileName)
        {
            if (!CcHasTexture(sourceName, fileName))
            {
                ConsoleError($"texture not found: {sourceName}/{fileName}", _cacheControllerPrefixes);
                return null;
            }
            var source = CacheFiles.GetSource(sourceName);
            var file = source.GetFile(fileName);
            var sourceCount = source.IncreaseUsedCount();
            var fileCount = file.IncreaseUsedCount();
            var texture = file.GetTexture();
            ConsoleInfo($"get texture: {sourceName}/{fileName} ({sourceCount}/{fileCount})", _cacheControllerPrefixes);
            if (texture == null) return TryRegenerateTexture(file);
            return texture;
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

        protected bool CcHasCache([CanBeNull] string source)
        {
            return CacheFiles.HasSource(source);
        }

        [CanBeNull]
        protected Source CcGetCache([CanBeNull] string source)
        {
            return CacheFiles.GetSource(source);
        }

        protected virtual bool CcHasTexture([CanBeNull] string source, [CanBeNull] string fileName)
        {
            return CacheFiles.HasSource(source) &&
                   CacheFiles.GetSource(source).HasFile(fileName);
        }

        public virtual void CcReleaseTexture([CanBeNull] string sourceName, [CanBeNull] string fileName)
        {
            if (!CcHasTexture(sourceName, fileName)) return;
            var source = CacheFiles.GetSource(sourceName);
            var file = source.GetFile(fileName);
            var sourceCount = source.DecreaseUsedCount();
            if (sourceCount < 1)
            {
                foreach (var tmpFileName in source.GetFileNames()) source.GetFile(tmpFileName).DestroyTexture();
                var keys = CacheFiles.RemoveSource(sourceName);
                foreach (var key in keys)
                {
                    if (!_cacheBinaryNames.Has(key, out var index)) continue;
                    _cacheBinary = _cacheBinary.Remove(index);
                    _cacheBinaryNames = _cacheBinaryNames.Remove(index);
                }
                ConsoleInfo($"destroy source: {sourceName}", _cacheControllerPrefixes);

                return;
            }
            
            var fileCount = file.DecreaseUsedCount();
            if (fileCount < 1)
            {
                var key = file.GetCacheKey();
                if (key.IsNullOrEmpty() && _cacheBinaryNames.Has(key))
                {
                    ConsoleInfo($"destroy texture: {sourceName}/{fileName}", _cacheControllerPrefixes);
                    file.DestroyTexture();
                }
                else
                {
                    ConsoleInfo($"cannot release texture: {sourceName}/{fileName}", _cacheControllerPrefixes);
                }
            }
            ConsoleInfo($"release texture: {sourceName}/{fileName} ({sourceCount}/{fileCount})", _cacheControllerPrefixes);
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
            return source.GetFileNames();
        }

        [NotNull]
        public virtual string DumpCache()
        {
            var result = "";
            var cacheKeys = CacheFiles.GetKeys().ToStringArray();
            foreach (var key in cacheKeys)
            {
                var source = CacheFiles.GetSource(key);
                var fileNames = source.GetFileNames();
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
