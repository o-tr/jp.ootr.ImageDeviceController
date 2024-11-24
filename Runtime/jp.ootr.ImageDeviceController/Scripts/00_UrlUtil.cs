using JetBrains.Annotations;
using jp.ootr.common;

namespace jp.ootr.ImageDeviceController
{
    public static class UrlUtil
    {
        public static bool GetUrlAndArgs([CanBeNull]string url, out string rawUrl, out string[] args)
        {
            if (url == null)
            {
                rawUrl = null;
                args = null;
                return false;
            }
            var urls = url.Split(@"\\\");
            rawUrl = null;
            args = null;
            if (urls.Length != 2) return false;

            rawUrl = urls[0];
            args = urls[1].Split(",");
            return true;
        }

        public static bool IsValidUrl([CanBeNull]this string url, out LoadError error)
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

        public static bool ParseSourceOptions([CanBeNull]this string options, out URLType type, out float offset,
            out float interval)
        {
            if (options == null)
            {
                type = URLType.Image;
                offset = 0;
                interval = 0;
                return false;
            }
            var split = options.Split(',');
            type = (URLType)int.Parse(split[0]);
            if (split.Length < 3)
            {
                offset = 0;
                interval = 0;
                return false;
            }

            offset = float.Parse(split[1]);
            interval = float.Parse(split[2]);
            return true;
        }

        public static void ParseSourceOptions([CanBeNull]this string options, out URLType type)
        {
            options.ParseSourceOptions(out type, out var v1, out var v2);
        }

        public static void ParseFileName([CanBeNull]this string fileName, out URLType type, out string options)
        {
            if (fileName == null)
            {
                type = URLType.Image;
                options = "";
                return;
            }
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
