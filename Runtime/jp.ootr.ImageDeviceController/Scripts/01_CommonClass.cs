using jp.ootr.common;

namespace jp.ootr.ImageDeviceController
{
    public class CommonClass : BaseClass
    {
        public readonly int SupportedManifestVersion = 1;
        public readonly string[] SupportedFeatures = {"textzip:rgb24", "textzip:rgba32"};
        public readonly string[] SupportedExtensions = {"note"};
        
        protected virtual LoadError ParseStringDownloadError(string message, int code)
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

        protected virtual LoadError ParseImageDownloadError(LoadError error, string message)
        {
            if (error == LoadError.DownloadError && message == "Redirect limit exceeded")
                return LoadError.RedirectNotAllowed;

            if (error == LoadError.DownloadError)
            {
                var split = message.Split(' ');
                if (split.Length > 2 && int.TryParse(split[1], out var code)) return (LoadError)code;
            }

            return error;
        }
    }
}