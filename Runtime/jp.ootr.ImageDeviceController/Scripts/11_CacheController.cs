using jp.ootr.common;
using UnityEngine;
using VRC.SDK3.Data;

namespace jp.ootr.ImageDeviceController
{
    public class CacheController : CommonClass
    {
        protected Cache CacheFiles;//Hack: 初期値を入れるとコンパイルエラーになるので、Startで初期化する
        
        protected byte[][] CacheBinary = new byte[0][];//DataDictionaryにbyte[]が入らないので別で取り扱う
        protected string[] CacheBinaryNames = new string[0];

        public virtual void Start()
        {
            CacheFiles = (Cache)new DataDictionary();
        }

        public virtual Texture2D CcGetTexture(string source, string fileName)
        {
            if (!CcHasTexture(source, fileName)) return null;
            var files = CacheFiles.GetSource(source);
            var file = files.GetFile(fileName);
            files.IncreaseUsedCount();
            file.IncreaseUsedCount();
            var texture = file.GetTexture();
            if (texture == null)
            {
                return TryRegenerateTexture(file);
            }
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
                !CacheFiles.HasSource(source)||
                !CacheFiles.GetSource(source).HasFile(fileName)
            ) return false;
            return true;
        }

        public virtual void CcReleaseTexture(string source, string fileName)
        {
            if (!CcHasTexture(source, fileName)) return;
            var files = CacheFiles.GetSource(source);
            var file = files.GetFile(fileName);
            if (files.DecreaseUsedCount() < 1)
            {
                CacheFiles.RemoveSource(source);
                return;
            }

            if (file.DecreaseUsedCount() < 1)
            {
                file.DestroyTexture();
            }
        }

        protected virtual void CcSetTexture(string source, string fileName, Texture2D texture, byte[] bytes = null)
        {
            if (!CacheFiles.HasSource(source))
            {
                CacheFiles.AddSource(source);
            }

            var files = CacheFiles.GetSource(source);
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
            
            files.AddFile(fileName, texture, cacheKey);
        }

        protected virtual void CcOnRelease(string source)
        {
        }
    }
}