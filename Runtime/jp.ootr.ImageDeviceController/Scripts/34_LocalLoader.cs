using JetBrains.Annotations;
using jp.ootr.common;
using UnityEngine;
using UnityEngine.Serialization;

namespace jp.ootr.ImageDeviceController
{
    public class LocalLoader : ImageLoader
    {
        [SerializeField] protected Texture2D[] localTextures;
        [FormerlySerializedAs("localTextureUrls")] [SerializeField] protected string[] localTextureSourceUrls;

        private readonly string[] _localLoaderPrefixes = { "LocalLoader" };

        protected void LlLoadLocal([CanBeNull] string sourceUrl)
        {
            if (!sourceUrl.IsValidLocalUrl())
            {
                LlOnLoadError(sourceUrl, LoadError.InvalidURL);
                return;
            }

            if (!localTextureSourceUrls.Has(sourceUrl, out var index)) LlOnLoadError(sourceUrl, LoadError.HttpNotFound);

            if (!CcHasCache(sourceUrl)) CcSetTexture(sourceUrl, sourceUrl, localTextures[index]);

            LlOnLoadSuccess(sourceUrl, new[] { sourceUrl });
        }

        protected virtual void LlOnLoadSuccess([CanBeNull] string sourceUrl, [CanBeNull] string[] fileUrls)
        {
            ConsoleError("LlOnLoadSuccess should not be called from base class", _localLoaderPrefixes);
        }

        protected virtual void LlOnLoadError([CanBeNull] string sourceUrl, LoadError error)
        {
            ConsoleError("LlOnLoadError should not be called from base class", _localLoaderPrefixes);
        }
    }
}
