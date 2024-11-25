using JetBrains.Annotations;
using jp.ootr.common;
using UnityEngine;
using VRC.SDK3.Data;

namespace jp.ootr.ImageDeviceController
{
    public abstract class Cache : DataDictionary
    {
    }

    public abstract class Source : DataDictionary
    {
    }

    public abstract class File : DataDictionary
    {
    }

    public abstract class Metadata : DataDictionary
    {
    }

    public static class CacheUtils
    {
        [CanBeNull]
        public static Source GetSource([CanBeNull] this Cache sources, [CanBeNull] string source)
        {
            if (sources == null || source == null || !sources.ContainsKey(source)) return null;
            return (Source)sources[source].DataDictionary;
        }

        public static bool HasSource([CanBeNull] this Cache sources, [CanBeNull] string source)
        {
            if (sources == null || source == null) return false;
            return sources.ContainsKey(source);
        }

        [CanBeNull]
        public static Source AddSource([CanBeNull] this Cache sources, [CanBeNull] string source)
        {
            if (sources == null || source == null) return null;
            var sourceData = new DataDictionary();
            sourceData["files"] = new DataDictionary();
            sourceData["usedCount"] = 0;
            sources[source] = sourceData;
            return (Source)sourceData;
        }

        [CanBeNull]
        public static File GetFile([CanBeNull] this Source source, [CanBeNull] string fileName)
        {
            if (source == null || fileName == null || !source.HasFile(fileName)) return null;
            return (File)source["files"].DataDictionary[fileName].DataDictionary;
        }

        public static bool HasFile([CanBeNull] this Source files, [CanBeNull] string fileName)
        {
            if (files == null || fileName == null) return false;
            return files["files"].DataDictionary.ContainsKey(fileName);
        }

        [CanBeNull]
        public static File AddFile([CanBeNull] this Source files, [CanBeNull] string fileName,
            [CanBeNull] Texture2D texture, [CanBeNull] DataDictionary metadata,
            [CanBeNull] string cacheKey = null, TextureFormat format = TextureFormat.RGBA32)
        {
            if (files == null || fileName == null || texture == null || metadata == null) return null;
            var fileData = new DataDictionary();
            fileData["texture"] = texture;
            fileData["usedCount"] = 0;
            fileData["cacheKey"] = cacheKey ?? "";
            fileData["width"] = texture.width;
            fileData["height"] = texture.height;
            fileData["metadata"] = metadata;
            fileData["format"] = (int)format;
            files["files"].DataDictionary[fileName] = fileData;
            return (File)fileData;
        }

        [NotNull]
        public static string[] RemoveSource([CanBeNull] this Cache sources, [CanBeNull] string source)
        {
            if (sources == null || source == null || !sources.HasSource(source)) return new string[0];
            var sourceData = sources.GetSource(source);
            var fileNames = sourceData.GetFileNames();
            var keys = new string[fileNames.Length];
            var index = 0;
            foreach (var fileName in fileNames)
            {
                keys[index++] = sourceData.GetFile(fileName).GetCacheKey();
                sourceData.RemoveFile(fileName);
            }

            sources.Remove(source);
            return keys;
        }

        [NotNull]
        public static string[] GetFileNames([CanBeNull] this Source files)
        {
            if (files == null) return new string[0];
            return files["files"].DataDictionary.GetKeys().ToStringArray();
        }

        public static int IncreaseUsedCount([CanBeNull] this Source files)
        {
            if (files == null) return 0;
            var val = files["usedCount"].Int + 1;
            files["usedCount"] = val;
            return val;
        }

        public static int DecreaseUsedCount([CanBeNull] this Source files)
        {
            if (files == null) return 0;
            var val = files["usedCount"].Int - 1;
            files["usedCount"] = val;
            return val;
        }

        public static void RemoveFile([CanBeNull] this Source files, [CanBeNull] string fileName)
        {
            if (files == null || fileName == null || !files.HasFile(fileName)) return;
            var file = files.GetFile(fileName);
            file.DestroyTexture();
            files["files"].DataDictionary.Remove(fileName);
        }

        public static int IncreaseUsedCount([CanBeNull] this File file)
        {
            if (file == null) return 0;
            var val = file["usedCount"].Int + 1;
            file["usedCount"] = val;
            return val;
        }

        public static int DecreaseUsedCount([CanBeNull] this File file)
        {
            if (file == null) return 0;
            var val = file["usedCount"].Int - 1;
            file["usedCount"] = val;
            return val;
        }

        [CanBeNull]
        public static Texture2D GetTexture([CanBeNull] this File file)
        {
            if (file == null) return null;
            return (Texture2D)file["texture"].Reference;
        }

        public static void SetTexture([CanBeNull] this File file, [CanBeNull] Texture2D texture)
        {
            if (file == null) return;
            file["texture"] = texture;
        }

        public static void DestroyTexture([CanBeNull] this File file)
        {
            if (file == null) return;
            var texture = file.GetTexture();
            if (texture == null) return;
            Object.Destroy(texture);
        }

        [NotNull]
        public static string GetCacheKey([CanBeNull] this File file)
        {
            if (file == null) return "";
            return file["cacheKey"].String;
        }

        public static TextureFormat GetTextureFormat([CanBeNull] this File file)
        {
            if (file == null) return TextureFormat.RGBA32;
            return (TextureFormat)(int)file["format"].Double;
        }

        [CanBeNull]
        public static Metadata GetMetadata([CanBeNull] this File file)
        {
            if (file == null) return null;
            return (Metadata)file["metadata"].DataDictionary;
        }

        public static int GetWidth([CanBeNull] this File file)
        {
            if (file == null) return 0;
            return file["width"].Int;
        }

        public static int GetHeight([CanBeNull] this File file)
        {
            if (file == null) return 0;
            return file["height"].Int;
        }

        public static int GetUsedCount([CanBeNull] this Source file)
        {
            if (file == null) return 0;
            return file["usedCount"].Int;
        }

        public static int GetUsedCount([CanBeNull] this File file)
        {
            if (file == null) return 0;
            return file["usedCount"].Int;
        }

        [CanBeNull]
        public static DataDictionary GetExtensions([CanBeNull] this Metadata metadata)
        {
            if (metadata == null || !metadata.TryGetValue("extensions", TokenType.DataDictionary, out var ext))
                return new DataDictionary();
            return ext.DataDictionary;
        }
    }
}
