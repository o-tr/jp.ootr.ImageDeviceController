using JetBrains.Annotations;
using jp.ootr.common;
using UnityEngine;
using UnityEngine.Serialization;

namespace jp.ootr.ImageDeviceController
{
    public class LocalSourceLoader : ImageSourceLoader
    {
        [SerializeField] protected Texture2D[] localTextures;
        [FormerlySerializedAs("localTextureUrls")][SerializeField] protected string[] localTextureSourceUrls;

        private readonly string[] _localLoaderPrefixes = { "LocalLoader" };

        protected void LlLoadLocal([CanBeNull] string sourceUrl)
        {
            if (!sourceUrl.IsValidLocalUrl())
            {
                OnSourceLoadError(sourceUrl, LoadError.InvalidURL);
                return;
            }

            if (!localTextureSourceUrls.Has(sourceUrl, out var index)) OnSourceLoadError(sourceUrl, LoadError.HttpNotFound);

            if (!CcHasCache(sourceUrl)) CcSetTexture(sourceUrl, sourceUrl, localTextures[index]);

            OnSourceLoadSuccess(sourceUrl, new[] { sourceUrl });
        }
    }
}
