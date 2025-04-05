using JetBrains.Annotations;
using static jp.ootr.common.ArrayUtils;

namespace jp.ootr.ImageDeviceController
{
    public class SourceController : LocalLoader
    {
        private readonly string[] _fileControllerPrefixes = { "FileController" };
        private string[][] _cachedData = new string[0][];

        private string[] _loadedSourceUrls = new string[0];
        private CommonDevice.CommonDevice[][] _loadingDevices = new CommonDevice.CommonDevice[0][];
        private string[] _loadingSourceUrls = new string[0];

        public virtual bool LoadSource([CanBeNull] CommonDevice.CommonDevice self, [CanBeNull] string sourceUrl,
            SourceType type, string options = "")
        {
            if (self == null || sourceUrl == null)
            {
                ConsoleError("self or source is null.", _fileControllerPrefixes);
                return false;
            }

            if (!UsHasUrl(sourceUrl))
            {
                ConsoleError($"source url not found in store: {sourceUrl}", _fileControllerPrefixes);
                return false;
            }

            // ロード中でも部分的なキャッシュが見える場合があるため、ロード中かの判定を先に行う
            if (_loadingSourceUrls.Has(sourceUrl, out var loadingIndex))
            {
                ConsoleDebug($"already loading. {sourceUrl}", _fileControllerPrefixes);
                _loadingDevices[loadingIndex] = _loadingDevices[loadingIndex].Append(self);
                return true;
            }
            
            if (CcHasCache(sourceUrl))
            {
                var files = CcGetCache(sourceUrl);
                var fileNames = files.GetFileUrls();
                ConsoleDebug($"already loaded. {sourceUrl}", _fileControllerPrefixes);
                self.OnSourceLoadSuccess(sourceUrl, fileNames);
                return true;
            }

            ConsoleDebug($"loading {sourceUrl}.", _fileControllerPrefixes);
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

        public virtual void UnloadSource([CanBeNull] CommonDevice.CommonDevice self, [CanBeNull] string sourceUrl)
        {
            if (self == null || sourceUrl == null)
            {
                ConsoleError("self or source is null.", _fileControllerPrefixes);
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

        #region EventReceiver

        protected override void CcOnRelease(string sourceUrl)
        {
            base.CcOnRelease(sourceUrl);
            if (!_loadedSourceUrls.Has(sourceUrl, out var loadedIndex)) return;
            _loadedSourceUrls = _loadedSourceUrls.Remove(loadedIndex);
            _cachedData = _cachedData.Remove(loadedIndex);
        }


        protected override void SlOnLoadProgress([CanBeNull] string sourceUrl, float progress)
        {
            if (sourceUrl == null || !_loadingSourceUrls.Has(sourceUrl, out var loadingIndex)) return;
            foreach (var device in _loadingDevices[loadingIndex]) device.OnSourceLoadProgress(sourceUrl, progress);
        }

        protected override void SlOnLoadSuccess([CanBeNull] string sourceUrl, [CanBeNull] string[] fileUrls)
        {
            if (sourceUrl == null || fileUrls == null || !_loadingSourceUrls.Has(sourceUrl, out var loadingIndex)) return;
            ConsoleDebug(
                $"StringKindArchive loaded successfully. {fileUrls.Length} files. device count: {_loadingDevices[loadingIndex].Length}, {sourceUrl}",
                _fileControllerPrefixes);
            _loadedSourceUrls = _loadedSourceUrls.Append(sourceUrl);
            _cachedData = _cachedData.Append(fileUrls);
            _loadingSourceUrls = _loadingSourceUrls.Remove(loadingIndex);
            _loadingDevices = _loadingDevices.Remove(loadingIndex, out var loadingDevices);
            if (loadingDevices == null) return;
            foreach (var device in loadingDevices) device.OnSourceLoadSuccess(sourceUrl, fileUrls);
        }

        protected override void SlOnLoadError([CanBeNull] string sourceUrl, LoadError error)
        {
            if (sourceUrl == null) return;
            ConsoleDebug($"StringKindArchive load failed: {error}, {sourceUrl} ", _fileControllerPrefixes);
            if (!_loadingSourceUrls.Has(sourceUrl, out var loadingIndex)) return;
            _loadingSourceUrls = _loadingSourceUrls.Remove(loadingIndex);
            _loadingDevices = _loadingDevices.Remove(loadingIndex, out var loadingDevices);
            if (loadingDevices == null) return;
            foreach (var device in loadingDevices) device.OnSourceLoadFailed(error);
        }

        protected override void IlOnLoadSuccess([CanBeNull] string sourceUrl, [CanBeNull] string[] fileUrls)
        {
            if (sourceUrl == null || fileUrls == null || !_loadingSourceUrls.Has(sourceUrl, out var loadingIndex)) return;
            ConsoleDebug($"Image loaded successfully. {sourceUrl} ", _fileControllerPrefixes);
            _loadedSourceUrls = _loadedSourceUrls.Append(sourceUrl);
            _cachedData = _cachedData.Append(fileUrls);
            _loadingSourceUrls = _loadingSourceUrls.Remove(loadingIndex);
            _loadingDevices = _loadingDevices.Remove(loadingIndex, out var loadingDevices);
            if (loadingDevices == null) return;
            foreach (var device in loadingDevices) device.OnSourceLoadSuccess(sourceUrl, fileUrls);
        }

        protected override void IlOnLoadError([CanBeNull] string sourceUrl, LoadError error)
        {
            if (!_loadingSourceUrls.Has(sourceUrl, out var loadingIndex)) return;
            ConsoleDebug($"Image load failed: {error}, {sourceUrl}", _fileControllerPrefixes);
            _loadingSourceUrls = _loadingSourceUrls.Remove(loadingIndex);
            _loadingDevices = _loadingDevices.Remove(loadingIndex, out var loadingDevices);
            if (loadingDevices == null) return;
            foreach (var device in loadingDevices) device.OnSourceLoadFailed(error);
        }

        protected override void VlOnLoadError([CanBeNull] string sourceUrl, LoadError error)
        {
            if (!_loadingSourceUrls.Has(sourceUrl, out var loadingIndex)) return;
            ConsoleDebug($"Video load failed: {error}, {sourceUrl} ", _fileControllerPrefixes);
            _loadingSourceUrls = _loadingSourceUrls.Remove(loadingIndex);
            _loadingDevices = _loadingDevices.Remove(loadingIndex, out var loadingDevices);
            if (loadingDevices == null) return;
            foreach (var device in loadingDevices) device.OnSourceLoadFailed(error);
        }

        protected override void VlOnLoadSuccess([CanBeNull] string sourceUrl, [CanBeNull] string[] fileNames)
        {
            if (!_loadingSourceUrls.Has(sourceUrl, out var loadingIndex) || sourceUrl == null || fileNames == null) return;
            ConsoleDebug($"Video loaded successfully. {sourceUrl}", _fileControllerPrefixes);
            _loadedSourceUrls = _loadedSourceUrls.Append(sourceUrl);
            _cachedData = _cachedData.Append(fileNames);
            _loadingSourceUrls = _loadingSourceUrls.Remove(loadingIndex);
            _loadingDevices = _loadingDevices.Remove(loadingIndex, out var loadingDevices);
            if (loadingDevices == null) return;
            foreach (var device in loadingDevices) device.OnSourceLoadSuccess(sourceUrl, fileNames);
        }

        protected override void VlOnLoadProgress([CanBeNull] string sourceUrl, float progress)
        {
            if (!_loadingSourceUrls.Has(sourceUrl, out var loadingIndex) || sourceUrl == null) return;
            foreach (var device in _loadingDevices[loadingIndex]) device.OnSourceLoadProgress(sourceUrl, progress);
        }

        protected override void LlOnLoadError([CanBeNull] string sourceUrl, LoadError error)
        {
            if (!_loadingSourceUrls.Has(sourceUrl, out var loadingIndex)) return;
            ConsoleDebug($"Local file load failed: {error}, {sourceUrl} ", _fileControllerPrefixes);
            _loadingSourceUrls = _loadingSourceUrls.Remove(loadingIndex);
            _loadingDevices = _loadingDevices.Remove(loadingIndex, out var loadingDevices);
            if (loadingDevices == null) return;
            foreach (var device in loadingDevices) device.OnSourceLoadFailed(error);
        }

        protected override void LlOnLoadSuccess([CanBeNull] string sourceUrl, [CanBeNull] string[] fileUrls)
        {
            if (!_loadingSourceUrls.Has(sourceUrl, out var loadingIndex) || sourceUrl == null || fileUrls == null) return;
            ConsoleDebug($"Local file loaded successfully. {sourceUrl}", _fileControllerPrefixes);
            _loadedSourceUrls = _loadedSourceUrls.Append(sourceUrl);
            _cachedData = _cachedData.Append(fileUrls);
            _loadingSourceUrls = _loadingSourceUrls.Remove(loadingIndex);
            _loadingDevices = _loadingDevices.Remove(loadingIndex, out var loadingDevices);
            if (loadingDevices == null) return;
            foreach (var device in loadingDevices) device.OnSourceLoadSuccess(sourceUrl, fileUrls);
        }

        #endregion
    }
}
