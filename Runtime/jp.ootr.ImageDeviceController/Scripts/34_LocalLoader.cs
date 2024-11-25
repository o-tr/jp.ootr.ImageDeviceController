using JetBrains.Annotations;
using jp.ootr.common;
using UnityEngine;

namespace jp.ootr.ImageDeviceController
{
    public class LocalLoader : ImageLoader
    {
        [SerializeField] protected Texture2D[] localTextures;
        [SerializeField] protected string[] localTextureUrls;

        private readonly string[] _localLoaderPrefixes = { "LocalLoader" };

        protected void LlLoadImage([CanBeNull]string url)
        {
            if (!url.IsValidLocalUrl())
            {
                LlOnLoadError(url, LoadError.InvalidURL);
                return;
            }
            if (!localTextureUrls.Has(url, out var index)) LlOnLoadError(url, LoadError.HttpNotFound);

            if (!CcHasCache(url)) CcSetTexture(url, url, localTextures[index]);

            LlOnLoadSuccess(url, new[] { url });
        }

        protected virtual void LlOnLoadSuccess([CanBeNull]string source, [CanBeNull]string[] fileNames)
        {
            ConsoleError("LlOnLoadSuccess should not be called from base class", _localLoaderPrefixes);
        }

        protected virtual void LlOnLoadError([CanBeNull]string source, LoadError error)
        {
            ConsoleError("LlOnLoadError should not be called from base class", _localLoaderPrefixes);
        }
    }
}
