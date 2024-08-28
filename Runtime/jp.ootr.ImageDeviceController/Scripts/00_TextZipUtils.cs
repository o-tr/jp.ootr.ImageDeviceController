using jp.ootr.common;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;

namespace jp.ootr.ImageDeviceController
{
    public static class TextZipUtils
    {
        public static bool ValidateManifest(DataToken manifest, out DataList files, out int manifestVersion,
            out string[] requiredFeatures, out string[] extension)
        {
            switch (manifest.TokenType)
            {
                case TokenType.DataList:
                    manifestVersion = 0;
                    requiredFeatures = new string[0];
                    extension = new string[0];
                    return ValidateManifestV0(manifest.DataList, out files);
                case TokenType.DataDictionary:
                    return ValidateManifestV1(manifest.DataDictionary, out files, out manifestVersion,
                        out requiredFeatures, out extension);
            }

            files = null;
            manifestVersion = -1;
            requiredFeatures = null;
            extension = null;
            return false;
        }

        private static bool ValidateManifestV0(DataList manifest, out DataList files)
        {
            files = manifest;
            var length = files.Count;
            for (var i = 0; i < length; i++)
                if (
                    !files.TryGetValue(i, TokenType.DataDictionary, out var file) ||
                    !file.DataDictionary.TryGetValue("path", TokenType.String, out var path) ||
                    !file.DataDictionary.TryGetValue("rect", TokenType.DataDictionary, out var rect) ||
                    !rect.DataDictionary.TryGetValue("width", TokenType.Double, out var width) ||
                    !rect.DataDictionary.TryGetValue("height", TokenType.Double, out var height)
                )
                    return false;

            return true;
        }

        private static bool ValidateManifestV1(DataToken manifest, out DataList files, out int manifestVersion,
            out string[] requiredFeatures, out string[] extension)
        {
            if (
                !manifest.DataDictionary.TryGetValue("files", TokenType.DataList, out var filesToken) ||
                !manifest.DataDictionary.TryGetValue("manifestVersion", TokenType.Double,
                    out var manifestVersionToken) ||
                !manifest.DataDictionary.TryGetValue("requiredFeatures", TokenType.DataList,
                    out var requiredFeaturesToken) ||
                !requiredFeaturesToken.TryToStringArray(out requiredFeatures) ||
                !manifest.DataDictionary.TryGetValue("extensions", TokenType.DataList, out var extensionToken) ||
                !extensionToken.TryToStringArray(out extension) ||
                !IsValidFilesV1(filesToken.DataList)
            )
            {
                files = null;
                manifestVersion = -1;
                requiredFeatures = null;
                extension = null;
                return false;
            }

            files = filesToken.DataList;
            manifestVersion = (int)manifestVersionToken.Double;

            return true;
        }

        private static bool IsValidFilesV1(DataList files)
        {
            var length = files.Count;
            for (var i = 0; i < length; i++)
                if (
                    !files.TryGetValue(i, TokenType.DataDictionary, out var file) ||
                    !file.DataDictionary.TryGetValue("path", TokenType.String, out var path) ||
                    !file.DataDictionary.TryGetValue("format", TokenType.String, out var format) ||
                    !file.DataDictionary.TryGetValue("rect", TokenType.DataDictionary, out var rect) ||
                    !rect.DataDictionary.TryGetValue("width", TokenType.Double, out var width) ||
                    !rect.DataDictionary.TryGetValue("height", TokenType.Double, out var height) ||
                    (file.DataDictionary.TryGetValue("extensions", TokenType.DataDictionary, out var ext) &&
                     !ext.IsStringDictionary())
                )
                    return false;

            return true;
        }

        public static bool TryGetFileMetadata(this DataDictionary file, out string path, out TextureFormat format,
            out int width, out int height, out DataDictionary ext)
        {
            if (
                !file.TryGetValue("path", TokenType.String, out var pathToken) ||
                !file.TryGetValue("rect", TokenType.DataDictionary, out var rectToken) ||
                !rectToken.DataDictionary.TryGetValue("width", TokenType.Double, out var widthToken) ||
                !rectToken.DataDictionary.TryGetValue("height", TokenType.Double, out var heightToken)
            )
            {
                path = null;
                format = TextureFormat.RGBA32;
                width = -1;
                height = -1;
                ext = null;
                return false;
            }

            ext = file.TryGetValue("extensions", TokenType.DataDictionary, out var extToken)
                ? extToken.DataDictionary
                : new DataDictionary();

            path = pathToken.String;
            if (file.TryGetValue("format", TokenType.String, out var formatToken))
                switch (formatToken.String)
                {
                    case "RGB24":
                        format = TextureFormat.RGB24;
                        break;
                    case "RGBA32":
                        format = TextureFormat.RGBA32;
                        break;
                    case "DXT1":
                        format = TextureFormat.DXT1;
                        break;
                    default:
                        format = TextureFormat.RGBA32;
                        break;
                }
            else
                format = TextureFormat.RGBA32;

            width = (int)widthToken.Double;
            height = (int)heightToken.Double;
            return true;
        }

        public static bool IsValidTextZip(this IVRCStringDownload result)
        {
            return result.Result.Substring(0, 6) == "UEsDBA";
        }
    }
}