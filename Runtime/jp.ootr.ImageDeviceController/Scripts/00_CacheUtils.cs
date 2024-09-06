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
        public static Source GetSource(this Cache sources, string source)
        {
            return sources.ContainsKey(source) ? (Source)sources[source].DataDictionary : null;
        }

        public static bool HasSource(this Cache sources, string source)
        {
            return sources.ContainsKey(source);
        }

        public static Source AddSource(this Cache sources, string source)
        {
            var sourceData = new DataDictionary();
            sourceData["files"] = new DataDictionary();
            sourceData["usedCount"] = 0;
            sources[source] = sourceData;
            return (Source)sourceData;
        }

        public static File GetFile(this Source source, string fileName)
        {
            return source.HasFile(fileName) ? (File)source["files"].DataDictionary[fileName].DataDictionary : null;
        }

        public static bool HasFile(this Source files, string fileName)
        {
            return files["files"].DataDictionary.ContainsKey(fileName);
        }

        public static File AddFile(this Source files, string fileName, Texture2D texture, DataDictionary metadata,
            string cacheKey = null, TextureFormat format = TextureFormat.RGBA32)
        {
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

        public static string[] RemoveSource(this Cache sources, string source)
        {
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

        public static string[] GetFileNames(this Source files)
        {
            return files["files"].DataDictionary.GetKeys().ToStringArray();
        }

        public static int IncreaseUsedCount(this Source files)
        {
            var val = files["usedCount"].Int + 1;
            files["usedCount"] = val;
            return val;
        }

        public static int DecreaseUsedCount(this Source files)
        {
            var val = files["usedCount"].Int - 1;
            files["usedCount"] = val;
            return val;
        }

        public static void RemoveFile(this Source files, string fileName)
        {
            var file = files.GetFile(fileName);
            file.DestroyTexture();
            files["files"].DataDictionary.Remove(fileName);
        }

        public static int IncreaseUsedCount(this File file)
        {
            var val = file["usedCount"].Int + 1;
            file["usedCount"] = val;
            return val;
        }

        public static int DecreaseUsedCount(this File file)
        {
            var val = file["usedCount"].Int - 1;
            file["usedCount"] = val;
            return val;
        }

        public static Texture2D GetTexture(this File file)
        {
            return (Texture2D)file["texture"].Reference;
        }

        public static void SetTexture(this File file, Texture2D texture)
        {
            file["texture"] = texture;
        }

        public static void DestroyTexture(this File file)
        {
            Object.Destroy(file.GetTexture());
        }

        public static string GetCacheKey(this File file)
        {
            return file["cacheKey"].String;
        }

        public static TextureFormat GetTextureFormat(this File file)
        {
            return (TextureFormat)(int)file["format"].Double;
        }

        public static Metadata GetMetadata(this File file)
        {
            return (Metadata)file["metadata"].DataDictionary;
        }

        public static DataDictionary GetExtensions(this Metadata metadata)
        {
            return metadata.TryGetValue("extensions", TokenType.DataDictionary, out var ext)
                ? ext.DataDictionary
                : new DataDictionary();
        }
    }
}
