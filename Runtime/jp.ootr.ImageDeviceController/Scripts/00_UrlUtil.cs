using System;
using System.Globalization;
using JetBrains.Annotations;
using jp.ootr.common;

namespace jp.ootr.ImageDeviceController
{
    public static class UrlUtil
    {
        public static bool GetUrlAndArgs([CanBeNull] string url, [CanBeNull] out string rawUrl,
            [CanBeNull] out string[] args)
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

        public static bool IsValidUrl([CanBeNull] this string url, out LoadError error)
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

        public static bool ParseSourceOptions([CanBeNull] this string options, out SourceType type, out float offset,
            out float interval)
        {
            if (options == null)
            {
                type = SourceType.Image;
                offset = 0;
                interval = 0;
                return false;
            }

            var split = options.Split(',');
            if (split.Length < 3)
            {
                type = SourceType.Image;
                offset = 0;
                interval = 0;
                return false;
            }

            if (!int.TryParse(split[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var typeInt))
            {
                type = SourceType.Image;
                offset = 0;
                interval = 0;
                return false;
            }

            type = (SourceType)typeInt;

            if (!float.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out offset) ||
                !float.TryParse(split[2], NumberStyles.Float, CultureInfo.InvariantCulture, out interval))
            {
                offset = 0;
                interval = 0;
                return false;
            }

            return true;
        }

        public static bool ParseSourceOptions([CanBeNull] this string options, out SourceType type)
        {
            return options.ParseSourceOptions(out type, out var void1, out var void2);
        }

        public static bool ParseSourceOptions([CanBeNull] this string options)
        {
            return options.ParseSourceOptions(out var void1, out var void2, out var void3);
        }

        public static void ParseFileName([CanBeNull] this string fileName, out SourceType type,
            [NotNull] out string options)
        {
            if (fileName == null)
            {
                type = SourceType.Image;
                options = "";
                return;
            }

            options = "";
            // ref: 01_CommonClass.cs
            if (fileName.StartsWith("zip://") || fileName.StartsWith("dynamic-eia://"))
            {
                type = SourceType.StringKind;
                return;
            }

            if (fileName.StartsWith("video://"))
            {
                options = fileName.Substring(8).Split("@")[0];
                type = SourceType.Video;
                return;
            }

            type = SourceType.Image;
        }

        public static string BuildSourceOptions(SourceType type, float offset, float interval)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0},{1},{2}", (int)type, offset, interval);
        }
    }
}
