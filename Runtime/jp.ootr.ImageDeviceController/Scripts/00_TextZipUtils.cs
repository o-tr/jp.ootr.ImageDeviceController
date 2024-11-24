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
        public static ParseResult ValidateManifest(DataToken manifest, out DataList files, out int manifestVersion,
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
            return ParseResult.UnknownVersion;
        }

        private static ParseResult ValidateManifestV0([CanBeNull]DataList manifest, out DataList files)
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
                    !file.DataDictionary.TryGetValue("path", TokenType.String, out var path) ||
                    !file.DataDictionary.TryGetValue("rect", TokenType.DataDictionary, out var rect) ||
                    !rect.DataDictionary.TryGetValue("width", TokenType.Double, out var width) ||
                    !rect.DataDictionary.TryGetValue("height", TokenType.Double, out var height)
                )
                    return ParseResult.InvalidValueType;

            return ParseResult.Success;
        }

        private static ParseResult ValidateManifestV1(DataToken manifest, out DataList files, out int manifestVersion,
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
                return ParseResult.InvalidValueType;
            }

            files = filesToken.DataList;
            manifestVersion = (int)manifestVersionToken.Double;

            return ParseResult.Success;
        }

        private static bool IsValidFilesV1([CanBeNull]DataList files)
        {
            if (files == null) return false;
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

        public static ParseResult TryGetFileMetadata([CanBeNull]this DataDictionary file, out string path,
            out TextureFormat format,
            out int width, out int height, out DataDictionary ext)
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

        public static bool IsValidTextZip([CanBeNull]this IVRCStringDownload result)
        {
            if (result == null) return false;
            return result.Result.Substring(0, 6) == "UEsDBA";
        }

        public static bool ParseTextureFormatString([CanBeNull]string input, out TextureFormat result)
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
    }
}
