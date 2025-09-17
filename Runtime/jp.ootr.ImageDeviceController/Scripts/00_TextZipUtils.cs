using System;
using JetBrains.Annotations;
using jp.ootr.common;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;

namespace jp.ootr.ImageDeviceController
{
    public enum ParseResult
    {
        Success,
        UnknownVersion,
        InvalidValueType,
        InvalidTextureFormat
    }

    public static class TextZipUtils
    {
        public static ParseResult ValidateManifest(DataToken manifest, [CanBeNull] out DataList files,
            out int manifestVersion,
            [CanBeNull] out string[] requiredFeatures, [CanBeNull] out string[] extension)
        {
            switch (manifest.TokenType)
            {
                case TokenType.DataList:
                    manifestVersion = 0;
                    requiredFeatures = null;
                    extension = null;
                    return ValidateManifestV0(manifest.DataList, out files);
                case TokenType.DataDictionary:
                    return ValidateManifestV1(manifest.DataDictionary, out files, out manifestVersion,
                        out requiredFeatures, out extension);
            }

            files = null;
            manifestVersion = -1;
            requiredFeatures = null;
            extension = null;
            return ParseResult.UnknownVersion;
        }

        private static ParseResult ValidateManifestV0([CanBeNull] DataList manifest, [CanBeNull] out DataList files)
        {
            if (manifest == null)
            {
                files = null;
                return ParseResult.InvalidValueType;
            }

            files = manifest;
            var length = files.Count;
            for (var i = 0; i < length; i++)
                if (
                    !files.TryGetValue(i, TokenType.DataDictionary, out var file) ||
                    !file.DataDictionary.TryGetValue("path", TokenType.String, out var void1) ||
                    !file.DataDictionary.TryGetValue("rect", TokenType.DataDictionary, out var rect) ||
                    !rect.DataDictionary.TryGetValue("width", TokenType.Double, out var void2) ||
                    !rect.DataDictionary.TryGetValue("height", TokenType.Double, out var void3)
                )
                    return ParseResult.InvalidValueType;

            return ParseResult.Success;
        }

        private static ParseResult ValidateManifestV1(DataToken manifest, [CanBeNull] out DataList files,
            out int manifestVersion,
            [CanBeNull] out string[] requiredFeatures, [CanBeNull] out string[] extension)
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
                return ParseResult.InvalidValueType;
            }

            files = filesToken.DataList;
            manifestVersion = (int)manifestVersionToken.Double;

            return ParseResult.Success;
        }

        private static bool IsValidFilesV1([CanBeNull] DataList files)
        {
            if (files == null) return false;
            var length = files.Count;
            for (var i = 0; i < length; i++)
                if (
                    !files.TryGetValue(i, TokenType.DataDictionary, out var file) ||
                    !file.DataDictionary.TryGetValue("path", TokenType.String, out var void1) ||
                    !file.DataDictionary.TryGetValue("format", TokenType.String, out var void2) ||
                    !file.DataDictionary.TryGetValue("rect", TokenType.DataDictionary, out var rect) ||
                    !rect.DataDictionary.TryGetValue("width", TokenType.Double, out var void3) ||
                    !rect.DataDictionary.TryGetValue("height", TokenType.Double, out var void4) ||
                    !file.DataDictionary.TryGetValue("extensions", TokenType.DataDictionary, out var ext)
                )
                    return false;

            return true;
        }

        public static ParseResult TryGetFileMetadata([CanBeNull] this DataDictionary file, [CanBeNull] out string path,
            out TextureFormat format,
            out int width, out int height, [CanBeNull] out DataDictionary ext)
        {
            if (
                file == null ||
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
                return ParseResult.InvalidValueType;
            }

            ext = file.TryGetValue("extensions", TokenType.DataDictionary, out var extToken)
                ? extToken.DataDictionary
                : new DataDictionary();

            path = pathToken.String;
            if (!file.TryGetValue("format", TokenType.String, out var formatToken))
            {
                format = TextureFormat.RGBA32;
            }
            else if (!ParseTextureFormatString(formatToken.String, out format))
            {
                path = null;
                format = TextureFormat.RGBA32;
                width = -1;
                height = -1;
                ext = null;
                return ParseResult.InvalidTextureFormat;
            }

            width = (int)widthToken.Double;
            height = (int)heightToken.Double;
            return ParseResult.Success;
        }

        public static ParseResult TryGetCroppedMetadata([CanBeNull] this DataDictionary extensions, out string basePath,
            out DataList rects)
        {
            if (extensions == null || !extensions.TryGetValue("cropped", TokenType.DataDictionary, out var cropped) ||
                !cropped.DataDictionary.TryGetValue("basePath", TokenType.String, out var basePathToken) ||
                !cropped.DataDictionary.TryGetValue("rects", TokenType.DataList, out var rectsToken))
            {
                basePath = null;
                rects = null;
                return ParseResult.InvalidValueType;
            }

            basePath = basePathToken.String;
            rects = rectsToken.DataList;
            return ParseResult.Success;
        }

        public static ParseResult TryGetRectMetadata([CanBeNull] this DataDictionary rect, out int x, out int y,
            out int width, out int height, out string path)
        {
            if (
                rect == null ||
                !rect.TryGetValue("x", TokenType.Double, out var xToken) ||
                !rect.TryGetValue("y", TokenType.Double, out var yToken) ||
                !rect.TryGetValue("width", TokenType.Double, out var widthToken) ||
                !rect.TryGetValue("height", TokenType.Double, out var heightToken) ||
                !rect.TryGetValue("path", TokenType.String, out var pathToken)
            )
            {
                x = -1;
                y = -1;
                width = -1;
                height = -1;
                path = null;
                return ParseResult.InvalidValueType;
            }

            x = (int)xToken.Double;
            y = (int)yToken.Double;
            width = (int)widthToken.Double;
            height = (int)heightToken.Double;
            path = pathToken.String;
            return ParseResult.Success;
        }

        public static bool IsValidTextZip([CanBeNull] this IVRCStringDownload result)
        {
            if (result == null) return false;
            return result.Result.Substring(0, 6) == "UEsDBA";
        }

        public static bool ParseTextureFormatString([CanBeNull] string input, out TextureFormat result)
        {
            if (input == null)
            {
                result = TextureFormat.RGBA32;
                return false;
            }

            var textureFormats = new[]
            {
                "-",
                "Alpha8", "ARGB4444", "RGB24", "RGBA32", "ARGB32",
                "-", "RGB565", "-", "R16", "DXT1", //10
                "-", "DXT5", "RGBA4444", "BGRA32", "RHalf",
                "RGHalf", "RGBAHalf", "RFloat", "RGFloat", "RGBAFloat", //20
                "YUY2", "RGB9e5Float", "-", "BC6H", "BC7",
                "BC4", "BC5", "DXT1Crunched", "DXT5Crunched", "PVRTC_RGB2", //30
                "PVRTC_RGBA2", "PVRTC_RGB4", "PVRTC_RGBA4", "ETC_RGB4", "-",
                "-", "-", "-", "-", "-", //40
                "EAC_R", "EAC_R_SIGNED", "EAC_RG", "EAC_RG_SIGNED", "ETC2_RGB",
                "ETC2_RGBA1", "ETC2_RGBA8", "ASTC_4x4", "ASTC_5x5", "ASTC_6x6", //50
                "ASTC_8x8", "ASTC_10x10", "ASTC_12x12", "-", "-",
                "-", "-", "-", "-", "-", //60
                "-", "RG16", "R8", "ETC_RGB4Crunched", "ETC2_RGBA8Crunched",
                "ASTC_HDR_4x4", "ASTC_HDR_5x5", "ASTC_HDR_6x6", "ASTC_HDR_8x8", "ASTC_HDR_10x10", //70
                "ASTC_HDR_12x12", "RG32", "RGB48", "RGBA64"
            };
            var index = Array.IndexOf(textureFormats, input);
            if (index == -1)
            {
                result = TextureFormat.RGBA32;
                return false;
            }

            result = (TextureFormat)index;
            return true;
        }
        public static int GetBytePerPixel(this TextureFormat input)
        {
            switch (input)
            {
                case TextureFormat.RGB24:
                    return 3;
                case TextureFormat.RGBA32:
                case TextureFormat.ARGB32:
                    return 4;
                case TextureFormat.Alpha8:
                    return 1;
                case TextureFormat.ARGB4444:
                case TextureFormat.RGB565:
                case TextureFormat.R16:
                    return 2;
                case TextureFormat.DXT1:
                case TextureFormat.DXT1Crunched:
                case TextureFormat.PVRTC_RGB2:
                case TextureFormat.PVRTC_RGBA2:
                case TextureFormat.ETC_RGB4:
                case TextureFormat.ETC_RGB4Crunched:
                case TextureFormat.EAC_R:
                case TextureFormat.EAC_R_SIGNED:
                    return 8; // Compressed formats, approximate value
                case TextureFormat.DXT5:
                case TextureFormat.DXT5Crunched:
                case TextureFormat.RGBA4444:
                case TextureFormat.BGRA32:
                case TextureFormat.RHalf:
                case TextureFormat.RGHalf:
                case TextureFormat.RGBAHalf:
                case TextureFormat.RFloat:
                case TextureFormat.RGFloat:
                case TextureFormat.RGBAFloat:
                case TextureFormat.YUY2:
                case TextureFormat.RGB9e5Float:
                case TextureFormat.BC4:
                case TextureFormat.BC5:
                case TextureFormat.BC6H:
                case TextureFormat.BC7:
                case TextureFormat.PVRTC_RGB4:
                case TextureFormat.PVRTC_RGBA4:
                case TextureFormat.ETC2_RGBA1:
                case TextureFormat.ETC2_RGBA8:
                case TextureFormat.ETC2_RGBA8Crunched:
                case TextureFormat.ASTC_4x4:
                case TextureFormat.ASTC_5x5:
                case TextureFormat.ASTC_6x6:
                case TextureFormat.ASTC_8x8:
                case TextureFormat.ASTC_10x10:
                case TextureFormat.ASTC_12x12:
                case TextureFormat.ASTC_HDR_4x4:
                case TextureFormat.ASTC_HDR_5x5:
                case TextureFormat.ASTC_HDR_6x6:
                case TextureFormat.ASTC_HDR_8x8:
                case TextureFormat.ASTC_HDR_10x10:
                case TextureFormat.ASTC_HDR_12x12:
                    return 16; // Compressed formats, approximate value
                case TextureFormat.RG16:
                    return 2;
                case TextureFormat.R8:
                    return 1;
                case TextureFormat.RG32:
                    return 4;
                case TextureFormat.RGB48:
                    return 6;
                case TextureFormat.RGBA64:
                    return 8;
                default:
                    return -1;
            }
        }
    }
}
