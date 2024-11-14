using jp.ootr.common;

namespace jp.ootr.ImageDeviceController
{
    public static class UrlUtil
    {
        public static bool GetUrlAndArgs(string url, out string rawUrl, out string[] args)
        {
            var urls = url.Split(@"\\\");
            rawUrl = null;
            args = null;
            if (urls.Length != 2) return false;

            rawUrl = urls[0];
            args = urls[1].Split(",");
            return true;
        }

        public static bool IsValidUrl(this string url, out LoadError error)
        {
            if (url.IsInsecureUrl())
            {
                error = LoadError.InsecureURL;
                return false;
            }

            if (!url.IsValidUrl())
            {
                error = LoadError.InvalidURL;
                return false;
            }

            error = LoadError.Unknown;
            return true;
        }

        public static void ParseSourceOptions(this string options, out URLType type, out float offset,
            out float interval)
        {
            var split = options.Split(',');
            type = (URLType)int.Parse(split[0]);
            if (split.Length < 3)
            {
                offset = 0;
                interval = 0;
                return;
            }

            offset = float.Parse(split[1]);
            interval = float.Parse(split[2]);
        }

        public static void ParseSourceOptions(this string options, out URLType type)
        {
            options.ParseSourceOptions(out type, out var v1, out var v2);
        }

        public static void ParseFileName(this string fileName, out URLType type,  out string options)
        {
            options = "";
            if (fileName.StartsWith("zip://"))
            {
                type = URLType.TextZip;
                return;
            }
            if (fileName.StartsWith("video://"))
            {
                options = fileName.Substring(8).Split("@")[0];
                type = URLType.Video;
                return;
            }
            type = URLType.Image;
        }

        public static string BuildSourceOptions(URLType type, float offset, float interval)
        {
            return $"{(int)type},{offset},{interval}";
        }
    }
}
