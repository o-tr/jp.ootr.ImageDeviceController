using JetBrains.Annotations;
using static jp.ootr.common.ArrayUtils;

namespace jp.ootr.ImageDeviceController
{
    public class FileController : LocalLoader
    {
        private readonly string[] _fileControllerPrefixes = { "FileController" };
        private string[][] _cachedData = new string[0][];

        private string[] _loadedUrls = new string[0];
        private CommonDevice.CommonDevice[][] _loadingDevices = new CommonDevice.CommonDevice[0][];
        private string[] _loadingUrls = new string[0];

        public virtual bool LoadFilesFromUrl([CanBeNull] CommonDevice.CommonDevice self, [CanBeNull] string source,
            URLType type,
            string options = "")
        {
            if (self == null || source == null)
            {
                ConsoleError("self or source is null.", _fileControllerPrefixes);
                return false;
            }

            if (!UsHasUrl(source))
            {
                ConsoleError($"url not found in store: {source}", _fileControllerPrefixes);
                return false;
            }

            // ロード中でも部分的なキャッシュが見える場合があるため、ロード中かの判定を先に行う
            if (_loadingUrls.Has(source, out var loadingIndex))
            {
                ConsoleDebug($"already loading. {source}", _fileControllerPrefixes);
                _loadingDevices[loadingIndex] = _loadingDevices[loadingIndex].Append(self);
                return true;
            }
            
            if (CcHasCache(source))
            {
                var files = CcGetCache(source);
                var fileNames = files.GetFileNames();
                ConsoleDebug($"already loaded. {source}", _fileControllerPrefixes);
                self.OnFilesLoadSuccess(source, fileNames);
                return true;
            }

            ConsoleDebug($"loading {source}.", _fileControllerPrefixes);
            _loadingUrls = _loadingUrls.Append(source);
            _loadingDevices = _loadingDevices.Append(new[] { self });
            switch (type)
            {
                case URLType.Image:
                    IlLoadImage(source);
                    break;
                case URLType.TextZip:
                    ZlLoadZip(source);
                    break;
                case URLType.Video:
                    VlLoadVideo(source, options);
                    break;
                case URLType.Local:
                    LlLoadImage(source);
                    break;
            }

            return true;
        }

        public virtual void UnloadFilesFromUrl([CanBeNull] CommonDevice.CommonDevice self, [CanBeNull] string source)
        {
            if (self == null || source == null)
            {
                ConsoleError("self or source is null.", _fileControllerPrefixes);
                return;
            }

            if (_loadingUrls.Has(source, out var loadingIndex))
            {
                if (!_loadingDevices[loadingIndex].Has(self, out var deviceIndex)) return;
                {
                    _loadingDevices[loadingIndex] = _loadingDevices[loadingIndex].Remove(deviceIndex);
                    if (_loadingDevices[loadingIndex].Length == 0)
                    {
                        _loadingUrls = _loadingUrls.Remove(loadingIndex);
                        _loadingDevices = _loadingDevices.Remove(loadingIndex);
                    }
                }
            }

            CcOnRelease(source);
        }

        #region EventReceiver

        protected override void CcOnRelease(string source)
        {
            base.CcOnRelease(source);
            if (!_loadedUrls.Has(source, out var loadedIndex)) return;
            _loadedUrls = _loadedUrls.Remove(loadedIndex);
            _cachedData = _cachedData.Remove(loadedIndex);
        }


        protected override void ZlOnLoadProgress([CanBeNull] string source, float progress)
        {
            if (source == null || !_loadingUrls.Has(source, out var loadingIndex)) return;
            foreach (var device in _loadingDevices[loadingIndex]) device.OnFileLoadProgress(source, progress);
        }

        protected override void ZlOnLoadSuccess([CanBeNull] string source, [CanBeNull] string[] fileNames)
        {
            if (source == null || fileNames == null || !_loadingUrls.Has(source, out var loadingIndex)) return;
            ConsoleDebug(
                $"TextZip loaded successfully. {fileNames.Length} files. device count: {_loadingDevices[loadingIndex].Length}, {source}",
                _fileControllerPrefixes);
            _loadedUrls = _loadedUrls.Append(source);
            _cachedData = _cachedData.Append(fileNames);
            foreach (var device in _loadingDevices[loadingIndex]) device.OnFilesLoadSuccess(source, fileNames);
            _loadingUrls = _loadingUrls.Remove(loadingIndex);
            _loadingDevices = _loadingDevices.Remove(loadingIndex);
        }

        protected override void ZlOnLoadError([CanBeNull] string source, LoadError error)
        {
            if (source == null) return;
            ConsoleDebug($"TextZip load failed: {error}, {source} ", _fileControllerPrefixes);
            if (!_loadingUrls.Has(source, out var loadingIndex)) return;
            foreach (var device in _loadingDevices[loadingIndex]) device.OnFilesLoadFailed(error);
            _loadingUrls = _loadingUrls.Remove(loadingIndex);
            _loadingDevices = _loadingDevices.Remove(loadingIndex);
        }

        protected override void IlOnLoadSuccess([CanBeNull] string source, [CanBeNull] string[] fileNames)
        {
            if (source == null || fileNames == null || !_loadingUrls.Has(source, out var loadingIndex)) return;
            ConsoleDebug($"Image loaded successfully. {source} ", _fileControllerPrefixes);
            _loadedUrls = _loadedUrls.Append(source);
            _cachedData = _cachedData.Append(fileNames);
            foreach (var device in _loadingDevices[loadingIndex]) device.OnFilesLoadSuccess(source, fileNames);
            _loadingUrls = _loadingUrls.Remove(loadingIndex);
            _loadingDevices = _loadingDevices.Remove(loadingIndex);
        }

        protected override void IlOnLoadError([CanBeNull] string source, LoadError error)
        {
            if (!_loadingUrls.Has(source, out var loadingIndex)) return;
            ConsoleDebug($"Image load failed: {error}, {source}", _fileControllerPrefixes);
            foreach (var device in _loadingDevices[loadingIndex]) device.OnFilesLoadFailed(error);
            _loadingUrls = _loadingUrls.Remove(loadingIndex);
            _loadingDevices = _loadingDevices.Remove(loadingIndex);
        }

        protected override void VlOnLoadError([CanBeNull] string source, LoadError error)
        {
            if (!_loadingUrls.Has(source, out var loadingIndex)) return;
            ConsoleDebug($"Video load failed: {error}, {source} ", _fileControllerPrefixes);
            foreach (var device in _loadingDevices[loadingIndex]) device.OnFilesLoadFailed(error);
            _loadingUrls = _loadingUrls.Remove(loadingIndex);
            _loadingDevices = _loadingDevices.Remove(loadingIndex);
        }

        protected override void VlOnLoadSuccess([CanBeNull] string source, [CanBeNull] string[] fileNames)
        {
            if (!_loadingUrls.Has(source, out var loadingIndex) || source == null || fileNames == null) return;
            ConsoleDebug($"Video loaded successfully. {source}", _fileControllerPrefixes);
            _loadedUrls = _loadedUrls.Append(source);
            _cachedData = _cachedData.Append(fileNames);
            foreach (var device in _loadingDevices[loadingIndex]) device.OnFilesLoadSuccess(source, fileNames);
            _loadingUrls = _loadingUrls.Remove(loadingIndex);
            _loadingDevices = _loadingDevices.Remove(loadingIndex);
        }

        protected override void VlOnLoadProgress([CanBeNull] string source, float progress)
        {
            if (!_loadingUrls.Has(source, out var loadingIndex) || source == null) return;
            foreach (var device in _loadingDevices[loadingIndex]) device.OnFileLoadProgress(source, progress);
        }

        protected override void LlOnLoadError([CanBeNull] string source, LoadError error)
        {
            if (!_loadingUrls.Has(source, out var loadingIndex)) return;
            ConsoleDebug($"Local file load failed: {error}, {source} ", _fileControllerPrefixes);
            foreach (var device in _loadingDevices[loadingIndex]) device.OnFilesLoadFailed(error);
            _loadingUrls = _loadingUrls.Remove(loadingIndex);
            _loadingDevices = _loadingDevices.Remove(loadingIndex);
        }

        protected override void LlOnLoadSuccess([CanBeNull] string source, [CanBeNull] string[] fileNames)
        {
            if (!_loadingUrls.Has(source, out var loadingIndex) || source == null || fileNames == null) return;
            ConsoleDebug($"Local file loaded successfully. {source}", _fileControllerPrefixes);
            _loadedUrls = _loadedUrls.Append(source);
            _cachedData = _cachedData.Append(fileNames);
            foreach (var device in _loadingDevices[loadingIndex]) device.OnFilesLoadSuccess(source, fileNames);
            _loadingUrls = _loadingUrls.Remove(loadingIndex);
            _loadingDevices = _loadingDevices.Remove(loadingIndex);
        }

        #endregion
    }
}
