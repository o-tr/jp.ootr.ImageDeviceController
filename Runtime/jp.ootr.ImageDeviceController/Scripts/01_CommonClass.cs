using JetBrains.Annotations;
using jp.ootr.common;

namespace jp.ootr.ImageDeviceController
{
    public class CommonClass : BaseClass
    {
        public readonly string[] SupportedExtensions = { "note" };

        public readonly string[] SupportedFeatures =
        {
            "Format:Alpha8", "Format:ARGB4444", "Format:RGB24", "Format:RGBA32", "Format:ARGB32", "Format:RGB565",
            "Format:R16", "Format:DXT1", "Format:DXT5", "Format:RGBA4444", "Format:BGRA32", "Format:RHalf",
            "Format:RGHalf", "Format:RGBAHalf", "Format:RFloat", "Format:RGFloat", "Format:RGBAFloat", "Format:YUY2",
            "Format:RGB9e5Float", "Format:BC6H", "Format:BC7", "Format:BC4", "Format:BC5", "Format:DXT1Crunched",
            "Format:DXT5Crunched", "Format:PVRTC_RGB2", "Format:PVRTC_RGBA2", "Format:PVRTC_RGB4", "Format:PVRTC_RGBA4",
            "Format:ETC_RGB4", "Format:EAC_R", "Format:EAC_R_SIGNED", "Format:EAC_RG", "Format:EAC_RG_SIGNED",
            "Format:ETC2_RGB", "Format:ETC2_RGBA1", "Format:ETC2_RGBA8", "Format:ASTC_4x4", "Format:ASTC_5x5",
            "Format:ASTC_6x6", "Format:ASTC_8x8", "Format:ASTC_10x10", "Format:ASTC_12x12", "Format:RG16", "Format:R8",
            "Format:ETC_RGB4Crunched", "Format:ETC2_RGBA8Crunched", "Format:ASTC_HDR_4x4", "Format:ASTC_HDR_5x5",
            "Format:ASTC_HDR_6x6", "Format:ASTC_HDR_8x8", "Format:ASTC_HDR_10x10", "Format:ASTC_HDR_12x12",
            "Format:RG32", "Format:RGB48", "Format:RGBA64"
        };

        public readonly string PROTOCOL_ZIP = "zip";
        public readonly string PROTOCOL_EIA = "dynamic-eia";
        public readonly string PROTOCOL_VIDEO = "video";
        public readonly string PROTOCOL_IMAGE = "image";

        public readonly int SupportedManifestVersion = 1;

        protected virtual LoadError ParseStringDownloadError([CanBeNull] string message, int code)
        {
            if (message == "Client has too many requests (limit is 1000)." && code == 429)
                return LoadError.TooManyRequests;
            if (message == "Invalid URL" && code == 400) return LoadError.InvalidURL;
            if (message == "Strings may only be downloaded from HTTP(S) URLs." && code == 400)
                return LoadError.InvalidURL;
            if (message == "Content longer than 100MB" && code == 413) return LoadError.TooLarge;

            if (message == "Cannot resolve destination host" && code == 0) return LoadError.HostNotFound;

            if (message == "Redirect limit exceeded") return LoadError.RedirectNotAllowed;
            if (message == "Unable to write data") return LoadError.TooLarge;

            if (code > 300) return (LoadError)code;

            return LoadError.Unknown;
        }

        protected virtual LoadError ParseImageDownloadError(LoadError error, [CanBeNull] string message)
        {
            if (error == LoadError.DownloadError && message == "Redirect limit exceeded")
                return LoadError.RedirectNotAllowed;

            if (error == LoadError.DownloadError && message != null)
            {
                var split = message.Split(' ');
                if (split.Length > 2 && int.TryParse(split[1], out var code)) return (LoadError)code;
            }

            return error;
        }
        
        protected virtual void OnSourceLoadProgress([CanBeNull] string sourceUrl, float progress)
        {
            ConsoleError("OnSourceLoadProgress should not be called from base class");
        }
        
        protected virtual void OnSourceLoadSuccess([CanBeNull] string sourceUrl, [CanBeNull] string[] fileUrls)
        {
            ConsoleError("OnSourceLoadSuccess should not be called from base class");
        }
        
        protected virtual void OnSourceLoadError([CanBeNull] string sourceUrl, LoadError error)
        {
            ConsoleError("OnSourceLoadError should not be called from base class");
        }
        
        protected virtual void OnFileLoadProgress([CanBeNull] string fileUrl, float progress)
        {
            ConsoleError("OnFileLoadProgress should not be called from base class");
        }
        
        protected virtual void OnFileLoadSuccess([CanBeNull] string fileUrl)
        {
            ConsoleError("OnFileLoadSuccess should not be called from base class");
        }
        
        protected virtual void OnFileLoadError([CanBeNull] string fileUrl, LoadError error)
        {
            ConsoleError("OnFileLoadError should not be called from base class");
        }
    }
}
