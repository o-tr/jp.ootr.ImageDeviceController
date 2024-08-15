using System;
using System.Text;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;
using VRC.Udon.Common.Interfaces;
using static jp.ootr.common.ArrayUtils;
using static jp.ootr.common.String;

namespace jp.ootr.ImageDeviceController
{
    public class ZipLoader : URLStore
    {
        [SerializeField] protected UdonZip.UdonZip zlUdonZip;

        [SerializeField][Range(1024,1024000)]public int zlPartLength = 102400;
        [SerializeField][Range(1,100)]public int zlDelayFrames = 1;
        protected int ZlDecodedBytes;
        protected byte[] ZlDecodedData;
        protected string[] ZlFilenames;
        protected DataList ZlMetadata;
        protected object ZlObject;
        protected int ZlProcessIndex;
        protected string[] ZlQueuedUrlStrings = new string[0];
        protected string[] ZlSource;
        protected string ZlSourceUrl;
        protected bool ZlIsLoading;

        public virtual void ZlLoadZip(string url)
        {
            if (zlUdonZip == null)
            {
                ConsoleError("ZipLoader: UdonZip component is not set.");
                ZlOnLoadError(url, LoadError.MissingUdonZip);
                return;
            }

            if (ZlIsLoading)
            {
                ZlQueuedUrlStrings = ZlQueuedUrlStrings.Append(url);
                return;
            }

            ZlSourceUrl = url;
            ZlIsLoading = true;
            VRCStringDownloader.LoadUrl(UsGetUrl(url), (IUdonEventReceiver)this);
        }


        public virtual void ZlLoadNext()
        {
            if (ZlQueuedUrlStrings.Length < 1)
            {
                ZlIsLoading = false;
                return;
            }
            ZlQueuedUrlStrings = ZlQueuedUrlStrings.__Shift(out ZlSourceUrl);
            VRCStringDownloader.LoadUrl(UsGetUrl(ZlSourceUrl), (IUdonEventReceiver)this);
        }

        public override void OnStringLoadSuccess(IVRCStringDownload result)
        {
            ConsoleDebug($"ZipLoader: text-zip loaded successfully from {result.Url}.");
            if (!result.IsValidTextZip())
            {
                ZlOnLoadError(result.Url.ToString(), LoadError.InvalidZipFile);
                SendCustomEventDelayedFrames(nameof(ZlLoadNext), zlDelayFrames);
                return;
            }

            ZlSource = result.Result.Split(zlPartLength);
            ZlDecodedData = new byte[ZlSource.Length * zlPartLength];
            ZlProcessIndex = 0;
            ZlDecodedBytes = 0;
            SendCustomEventDelayedFrames(nameof(ZlDecodePart), zlDelayFrames);
        }

        public override void OnStringLoadError(IVRCStringDownload result)
        {
            ConsoleError($"Error loading string: {result.ErrorCode} - {result.Error}");
            ZlOnLoadError(ZlSourceUrl, ParseStringDownloadError(result.Result, result.ErrorCode));
            SendCustomEventDelayedFrames(nameof(ZlLoadNext), zlDelayFrames);
        }

        public virtual void ZlDecodePart()
        {
            var data = Convert.FromBase64String(ZlSource[ZlProcessIndex]);
            if (ZlDecodedBytes + data.Length >= ZlDecodedData.Length)
            {
                var tmp = new byte[ZlDecodedBytes + data.Length];
                Array.Copy(ZlDecodedData, tmp, ZlDecodedData.Length);
                ZlDecodedData = tmp;
            }
            else
            {
                Array.Copy(data, 0, ZlDecodedData, ZlDecodedBytes, data.Length);
            }

            ZlDecodedBytes += data.Length;
            ZlProcessIndex++;
            ZlOnLoadProgress(ZlSourceUrl, (float)ZlProcessIndex / ZlSource.Length / 2);
            if (ZlProcessIndex < ZlSource.Length)
            {
                SendCustomEventDelayedFrames(nameof(ZlDecodePart), zlDelayFrames);
            }
            else
            {
                ZlDecodedData = ZlDecodedData.Resize(ZlDecodedBytes);
                SendCustomEventDelayedFrames(nameof(ZlExtractData), zlDelayFrames);
            }
        }

        public virtual void ZlExtractData()
        {
            ConsoleDebug("ZipLoader: extracting data.");
            ZlObject = zlUdonZip.Extract(ZlDecodedData);
            var file = zlUdonZip.GetFile(ZlObject, "metadata.json");
            var metadata = zlUdonZip.GetFileData(file);
            VRCJson.TryDeserializeFromJson(Encoding.UTF8.GetString(metadata), out var metadataToken);
            if(!TextZipUtils.ValidateManifest(metadataToken, out ZlMetadata, out var manifestVersion, out var requiredFeatures, out var extension))
            {
                ZlOnLoadError(ZlSourceUrl, LoadError.InvalidManifest);
                SendCustomEventDelayedFrames(nameof(ZlLoadNext), zlDelayFrames);
                return;
            }

            if (manifestVersion > SupportedManifestVersion)
            {
                ZlOnLoadError(ZlSourceUrl, LoadError.UnsupportedManifestVersion);
                SendCustomEventDelayedFrames(nameof(ZlLoadNext), zlDelayFrames);
                return;
            }

            foreach (var feature in requiredFeatures)
            {
                if (SupportedFeatures.Has(feature)) continue;
                ZlOnLoadError(ZlSourceUrl, LoadError.UnsupportedFeature);
                SendCustomEventDelayedFrames(nameof(ZlLoadNext), zlDelayFrames);
                return;
            }
            
            ZlProcessIndex = 0;
            ZlFilenames = new string[ZlMetadata.Count];
            SendCustomEventDelayedFrames(nameof(ZlExtractItem), zlDelayFrames);
        }

        public virtual void ZlExtractItem()
        {
            if (
                !ZlMetadata.TryGetValue(ZlProcessIndex, TokenType.DataDictionary, out var metadataItem) ||
                !metadataItem.DataDictionary.TryGetFileMetadata(out var path, out var format, out var width,
                    out var height, out var ext)
            )
            {
                ZlOnLoadError(ZlSourceUrl, LoadError.InvalidMetadata);
                SendCustomEventDelayedFrames(nameof(ZlLoadNext), zlDelayFrames);
                return;
            }
            var imageFile = zlUdonZip.GetFile(ZlObject, path);
            var imageBytes = zlUdonZip.GetFileData(imageFile);
            var texture = new Texture2D(width, height, format, false);
            texture.LoadRawTextureData(imageBytes);
            texture.Apply();
            var fileName = $"zip://{ZlSourceUrl.Substring(8)}/{path}";
            ZlFilenames[ZlProcessIndex] = fileName;
            CcSetTexture(ZlSourceUrl, fileName, texture, metadataItem.DataDictionary, imageBytes);
            ZlProcessIndex++;
            ZlOnLoadProgress(ZlSourceUrl, 0.5f + (float)ZlProcessIndex / ZlMetadata.Count / 2);
            if (ZlProcessIndex < ZlMetadata.Count)
            {
                SendCustomEventDelayedFrames(nameof(ZlExtractItem), zlDelayFrames);
                return;
            }

            ZlOnLoadSuccess(ZlSourceUrl, ZlFilenames);
            SendCustomEventDelayedFrames(nameof(ZlLoadNext), zlDelayFrames);
        }

        protected virtual void ZlOnLoadProgress(string source, float progress)
        {
            ConsoleError("ZipLoader: ZipOnLoadProgress should not be called from base class");
        }

        protected virtual void ZlOnLoadSuccess(string source, string[] fileNames)
        {
            ConsoleError("ZipLoader: ZipOnLoadSuccess should not be called from base class");
        }

        protected virtual void ZlOnLoadError(string source, LoadError error)
        {
            ConsoleError("ZipLoader: ZipOnLoadError should not be called from base class");
        }
    }
}