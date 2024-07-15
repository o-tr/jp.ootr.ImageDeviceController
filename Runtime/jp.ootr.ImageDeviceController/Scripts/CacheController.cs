using UnityEngine;
using static jp.ootr.common.ArrayUtils;

namespace jp.ootr.ImageDeviceController
{
    public class CacheController : CommonClass
    {
        protected string[][] FileNames = new string[0][];
        protected string[] FileSources = new string[0];
        protected Texture2D[][] FileTextures = new Texture2D[0][];
        protected int[] FileUsedCount = new int[0];

        public virtual Texture2D CcGetTexture(string source, string fileName)
        {
            if (!CcHasTexture(source, fileName, out var sourceIndex, out var fileIndex)) return null;
            FileUsedCount[sourceIndex]++;
            return FileTextures[sourceIndex][fileIndex];
        }


        public virtual bool CcHasTexture(string source, string fileName)
        {
            return CcHasTexture(source, fileName, out var sourceIndex, out var fileIndex);
        }

        public virtual bool CcHasTexture(string source, string fileName, out int sourceIndex, out int fileIndex)
        {
            fileIndex = -1;
            sourceIndex = -1;
            if (!FileSources.Has(source, out sourceIndex)) return false;
            return FileNames[sourceIndex].Has(fileName, out fileIndex);
        }

        public virtual void CcReleaseTexture(string source, string fileName)
        {
            if (!FileSources.Has(source, out var sourceIndex)) return;
            if (!FileNames[sourceIndex].Has(fileName)) return;
            FileUsedCount[sourceIndex]--;
            if (FileUsedCount[sourceIndex] <= 0)
            {
                FileSources = FileSources.Remove(sourceIndex);
                FileNames = FileNames.Remove(sourceIndex);
                FileTextures = FileTextures.Remove(sourceIndex);
                FileUsedCount = FileUsedCount.Remove(sourceIndex);
            }
        }

        protected virtual void CcSetTexture(string source, string fileName, Texture2D texture)
        {
            if (!FileSources.Has(source, out var sourceIndex))
            {
                FileSources = FileSources.Append(source);
                FileNames = FileNames.Append(new string[0]);
                FileTextures = FileTextures.Append(new Texture2D[0]);
                FileUsedCount = FileUsedCount.Append(0);
                sourceIndex = FileSources.Length - 1;
            }

            if (!FileNames[sourceIndex].Has(fileName, out var fileIndex))
            {
                FileNames[sourceIndex] = FileNames[sourceIndex].Append(fileName);
                FileTextures[sourceIndex] = FileTextures[sourceIndex].Append(texture);
            }
            else
            {
                FileTextures[sourceIndex][fileIndex] = texture;
            }
        }

        protected virtual void CcOnRelease(string source)
        {
        }
    }
}