using jp.ootr.common;
using UnityEngine;
using VRC.SDK3.Data;

namespace jp.ootr.ImageDeviceController
{
    public class CacheController : CommonClass
    {
        protected byte[][] CacheBinary = new byte[0][]; //DataDictionaryにbyte[]が入らないので別で取り扱う
        protected string[] CacheBinaryNames = new string[0];

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
        protected DataDictionary CacheFiles = new DataDictionary();

        public virtual Texture2D CcGetTexture(string source, string fileName)
        {
            if (!CcHasTexture(source, fileName)) return null;
            var files = ((Cache)CacheFiles).GetSource(source);
            var file = files.GetFile(fileName);
            files.IncreaseUsedCount();
            file.IncreaseUsedCount();
            var texture = file.GetTexture();
            if (texture == null) return TryRegenerateTexture(file);
            return texture;
        }

        private Texture2D TryRegenerateTexture(File file)
        {
            var key = file.GetCacheKey();
            if (!CacheBinaryNames.Has(key, out var index)) return null;
            var bytes = CacheBinary[index];
            var texture = new Texture2D(file["width"].Int, file["height"].Int);
            texture.LoadRawTextureData(bytes);
            texture.Apply();
            file.SetTexture(texture);
            return texture;
        }


        public virtual bool CcHasTexture(string source, string fileName)
        {
            if (
                !((Cache)CacheFiles).HasSource(source) ||
                !((Cache)CacheFiles).GetSource(source).HasFile(fileName)
            ) return false;
            return true;
        }

        public virtual void CcReleaseTexture(string sourceName, string fileName)
        {
            if (!CcHasTexture(sourceName, fileName)) return;
            var source = ((Cache)CacheFiles).GetSource(sourceName);
            var file = source.GetFile(fileName);
            if (source.DecreaseUsedCount() < 1)
            {
                var keys = ((Cache)CacheFiles).RemoveSource(sourceName);
                foreach (var key in keys)
                {
                    if (!CacheBinaryNames.Has(key, out var index)) continue;
                    CacheBinary = CacheBinary.Remove(index);
                    CacheBinaryNames = CacheBinaryNames.Remove(index);
                }

                return;
            }

            if (file.DecreaseUsedCount() < 1) file.DestroyTexture();
        }

        public virtual Metadata CcGetMetadata(string source, string fileName)
        {
            if (!CcHasTexture(source, fileName)) return null;
            return ((Cache)CacheFiles).GetSource(source).GetFile(fileName).GetMetadata();
        }

        protected virtual void CcSetTexture(string source, string fileName, Texture2D texture, byte[] bytes = null)
        {
            CcSetTexture(source, fileName, texture, new DataDictionary(), bytes);
        }

        protected virtual void CcSetTexture(string source, string fileName, Texture2D texture, DataDictionary metadata,
            byte[] bytes = null)
        {
            if (!((Cache)CacheFiles).HasSource(source)) ((Cache)CacheFiles).AddSource(source);

            var files = ((Cache)CacheFiles).GetSource(source);
            if (files.HasFile(fileName))
            {
                ConsoleError($"CacheController: file already exists: {source}/{fileName}");
                return;
            }

            string cacheKey = null;

            if (bytes != null)
            {
                cacheKey = $"cache://{source}/{fileName}";
                CacheBinary = CacheBinary.Append(bytes);
                CacheBinaryNames = CacheBinaryNames.Append(cacheKey);
            }

            files.AddFile(fileName, texture, metadata, cacheKey);
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
                var source = ((Cache)CacheFiles).GetSource(cacheKeys[i]);
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