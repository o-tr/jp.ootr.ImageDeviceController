using JetBrains.Annotations;
using UnityEngine;
using static jp.ootr.common.ArrayUtils;

namespace jp.ootr.ImageDeviceController
{
    public class SourceController : EIAFileLoader
    {
        private readonly string[] _SourceControllerPrefixes = { "SourceController" };

        private string[] _loadedSourceUrls = new string[0];
        private string[][] _loadedSourceFileNames = new string[0][];
        private CommonDevice.CommonDevice[][] _loadingDevices = new CommonDevice.CommonDevice[0][];
        private string[] _loadingSourceUrls = new string[0];

        private string[] _loadedSourceQueueUrls = new string[0];
        private string[][] _loadedSourceQueueFileNames = new string[0][];
        private CommonDevice.CommonDevice[] _loadedSourceQueueDevices = new CommonDevice.CommonDevice[0];
        private int[] _loadedSourceQueueFrameCounts = new int[0];

        public virtual bool LoadSource([CanBeNull] CommonDevice.CommonDevice self, [CanBeNull] string sourceUrl,
            SourceType type, string options = "")
        {
            if (self == null || sourceUrl == null)
            {
                ConsoleError("self or source is null.", _SourceControllerPrefixes);
                return false;
            }

            if (!UsHasUrl(sourceUrl))
            {
                ConsoleError($"source url not found in store: {sourceUrl}", _SourceControllerPrefixes);
                return false;
            }

            // ロード中でも部分的なキャッシュが見える場合があるため、ロード中かの判定を先に行う
            if (_loadingSourceUrls.Has(sourceUrl, out var loadingIndex))
            {
                ConsoleDebug($"already loading. {sourceUrl}", _SourceControllerPrefixes);
                _loadingDevices[loadingIndex] = _loadingDevices[loadingIndex].Append(self);
                return true;
            }

            if (_loadedSourceUrls.Has(sourceUrl, out var loadedIndex))
            {
                if (!CcHasCache(sourceUrl))
                {
                    ConsoleDebug($"cached source lost, force reload: {sourceUrl}", _SourceControllerPrefixes);
                    _loadedSourceUrls = _loadedSourceUrls.Remove(loadedIndex);
                    _loadedSourceFileNames = _loadedSourceFileNames.Remove(loadedIndex);
                }
                else
                {
                    ConsoleDebug($"already loaded. read from loaded source. {sourceUrl}",
                        _SourceControllerPrefixes);
                    // self.OnSourceLoadSuccess(sourceUrl, _loadedSourceFileNames[loadedIndex]);
                    _loadedSourceQueueUrls = _loadedSourceQueueUrls.Append(sourceUrl);
                    _loadedSourceQueueFileNames = _loadedSourceQueueFileNames.Append(_loadedSourceFileNames[loadedIndex]);
                    _loadedSourceQueueDevices = _loadedSourceQueueDevices.Append(self);
                    _loadedSourceQueueFrameCounts = _loadedSourceQueueFrameCounts.Append(Time.frameCount);
                    SendCustomEventDelayedFrames(nameof(SendLoadedSourceNotification), 1);
                    return true;
                }
            }

            if (CcHasCache(sourceUrl))
            {
                var files = CcGetCache(sourceUrl);
                var fileNames = files.GetFileUrls();
                if (fileNames.Length > 0)
                {
                    ConsoleDebug($"already loaded. read from cache. {fileNames.Length} files, {sourceUrl}",
                        _SourceControllerPrefixes);
                    // self.OnSourceLoadSuccess(sourceUrl, fileNames);
                    _loadedSourceQueueUrls = _loadedSourceQueueUrls.Append(sourceUrl);
                    _loadedSourceQueueFileNames = _loadedSourceQueueFileNames.Append(fileNames);
                    _loadedSourceQueueDevices = _loadedSourceQueueDevices.Append(self);
                    _loadedSourceQueueFrameCounts = _loadedSourceQueueFrameCounts.Append(Time.frameCount);
                    SendCustomEventDelayedFrames(nameof(SendLoadedSourceNotification), 1);
                    return true;
                }
                ConsoleDebug($"cache entry missing files, fallback to reload: {sourceUrl}", _SourceControllerPrefixes);
            }

            ConsoleDebug($"loading {sourceUrl}.", _SourceControllerPrefixes);
            _loadingSourceUrls = _loadingSourceUrls.Append(sourceUrl);
            _loadingDevices = _loadingDevices.Append(new[] { self });
            switch (type)
            {
                case SourceType.Image:
                    IlLoadImage(sourceUrl);
                    break;
                case SourceType.StringKind:
                    SlLoadString(sourceUrl);
                    break;
                case SourceType.Video:
                    VlLoadVideo(sourceUrl, options);
                    break;
                case SourceType.Local:
                    LlLoadLocal(sourceUrl);
                    break;
            }

            return true;
        }

        public virtual void SendLoadedSourceNotification()
        {
            if (_loadedSourceQueueUrls.Length == 0) return;
            for (var i = 0; i < _loadedSourceQueueUrls.Length; i++)
            {
                if (_loadedSourceQueueFrameCounts[i] == Time.frameCount) continue;
                _loadedSourceQueueUrls = _loadedSourceQueueUrls.Remove(i, out var sourceUrl);
                _loadedSourceQueueFileNames = _loadedSourceQueueFileNames.Remove(i, out var fileNames);
                _loadedSourceQueueDevices = _loadedSourceQueueDevices.Remove(i, out var device);
                _loadedSourceQueueFrameCounts = _loadedSourceQueueFrameCounts.Remove(i);
                i--;
                if (device == null || sourceUrl == null || fileNames == null || fileNames.Length == 0) continue;
                device.OnSourceLoadSuccess(sourceUrl, fileNames);
            }
            if (_loadedSourceQueueUrls.Length == 0) return;

            SendCustomEventDelayedFrames(nameof(SendLoadedSourceNotification), 1);
        }

        public virtual void UnloadSource([CanBeNull] CommonDevice.CommonDevice self, [CanBeNull] string sourceUrl)
        {
            if (self == null || sourceUrl == null)
            {
                ConsoleError("self or source is null.", _SourceControllerPrefixes);
                return;
            }

            if (_loadingSourceUrls.Has(sourceUrl, out var loadingIndex))
            {
                if (!_loadingDevices[loadingIndex].Has(self, out var deviceIndex)) return;
                {
                    _loadingDevices[loadingIndex] = _loadingDevices[loadingIndex].Remove(deviceIndex);
                    if (_loadingDevices[loadingIndex].Length == 0)
                    {
                        _loadingSourceUrls = _loadingSourceUrls.Remove(loadingIndex);
                        _loadingDevices = _loadingDevices.Remove(loadingIndex);
                    }
                }
            }

            CcOnRelease(sourceUrl);
        }

        public override string[] CcGetFileNames(string sourceName)
        {
            if (_loadedSourceUrls.Has(sourceName, out var loadedIndex))
            {
                return _loadedSourceFileNames[loadedIndex];
            }
            var data = base.CcGetFileNames(sourceName);
            if (data != null) return data;

            return null;
        }

        #region EventReceiver

        protected override void CcOnRelease(string sourceUrl)
        {
            base.CcOnRelease(sourceUrl);
            if (!_loadedSourceUrls.Has(sourceUrl, out var loadedIndex)) return;
            _loadedSourceUrls = _loadedSourceUrls.Remove(loadedIndex);
            _loadedSourceFileNames = _loadedSourceFileNames.Remove(loadedIndex);
        }

        protected override void OnSourceLoadProgress(string sourceUrl, float progress)
        {
            if (sourceUrl == null || !_loadingSourceUrls.Has(sourceUrl, out var loadingIndex)) return;
            foreach (var device in _loadingDevices[loadingIndex]) device.OnSourceLoadProgress(sourceUrl, progress);
        }

        protected override void OnSourceLoadSuccess(string sourceUrl, string[] fileUrls)
        {
            if (sourceUrl == null || fileUrls == null || !_loadingSourceUrls.Has(sourceUrl, out var loadingIndex)) return;
            ConsoleDebug(
                $"source loaded successfully. {fileUrls.Length} files. device count: {_loadingDevices[loadingIndex].Length}, {sourceUrl}",
                _SourceControllerPrefixes);
            _loadedSourceUrls = _loadedSourceUrls.Append(sourceUrl);
            _loadedSourceFileNames = _loadedSourceFileNames.Append(fileUrls);
            _loadingSourceUrls = _loadingSourceUrls.Remove(loadingIndex);
            _loadingDevices = _loadingDevices.Remove(loadingIndex, out var loadingDevices);
            if (loadingDevices == null) return;
            foreach (var device in loadingDevices) device.OnSourceLoadSuccess(sourceUrl, fileUrls);
        }

        protected override void OnSourceLoadError(string sourceUrl, LoadError error)
        {
            if (sourceUrl == null) return;
            ConsoleDebug($"StringKindArchive load failed: {error}, {sourceUrl} ", _SourceControllerPrefixes);
            if (!_loadingSourceUrls.Has(sourceUrl, out var loadingIndex)) return;
            _loadingSourceUrls = _loadingSourceUrls.Remove(loadingIndex);
            _loadingDevices = _loadingDevices.Remove(loadingIndex, out var loadingDevices);
            if (loadingDevices == null) return;
            foreach (var device in loadingDevices) device.OnSourceLoadFailed(error);
        }

        #endregion
    }
}
