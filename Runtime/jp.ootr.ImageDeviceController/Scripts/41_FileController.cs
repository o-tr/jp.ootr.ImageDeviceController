using static jp.ootr.common.ArrayUtils;

namespace jp.ootr.ImageDeviceController
{
    public class FileController : ImageLoader
    {
        private string[][] _cachedData = new string[0][];

        private string[] _loadedUrls = new string[0];
        private CommonDevice.CommonDevice[][] _loadingDevices = new CommonDevice.CommonDevice[0][];
        private string[] _loadingUrls = new string[0];

        public virtual bool LoadFilesFromUrl(IControlledDevice _self, string source, URLType type, string options = "")
        {
            if (!UsHasUrl(source))
            {
                ConsoleError($"FileController: url not found in store: {source}");
                return false;
            }

            var self = (CommonDevice.CommonDevice)_self;
            if (CcHasCache(source))
            {
                var files = CcGetCache(source);
                var fileNames = files.GetFileNames();
                ConsoleDebug($"FileController: already loaded. {source}");
                self.OnFilesLoadSuccess(source, fileNames);
                return true;
            }

            if (_loadingUrls.Has(source, out var loadingIndex))
            {
                ConsoleDebug($"FileController: already loading. {source}");
                _loadingDevices[loadingIndex] = _loadingDevices[loadingIndex].Append(self);
                return true;
            }

            ConsoleDebug($"FileController: loading {source}.");
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
            }

            return true;
        }

        public virtual void UnloadFilesFromUrl(IControlledDevice _self, string source)
        {
            var self = (CommonDevice.CommonDevice)_self;
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
            if (!_loadedUrls.Has(source, out var loadedIndex)) return;
            _loadedUrls = _loadedUrls.Remove(loadedIndex);
            _cachedData = _cachedData.Remove(loadedIndex);
        }


        protected override void ZlOnLoadProgress(string source, float progress)
        {
            if (!_loadingUrls.Has(source, out var loadingIndex)) return;
            foreach (var device in _loadingDevices[loadingIndex]) device.OnFileLoadProgress(source, progress);
        }

        protected override void ZlOnLoadSuccess(string source, string[] fileNames)
        {
            if (!_loadingUrls.Has(source, out var loadingIndex)) return;
            ConsoleDebug(
                $"FileController(TextZip): loaded successfully. {fileNames.Length} files. device count: {_loadingDevices[loadingIndex].Length}, {source} ");
            foreach (var device in _loadingDevices[loadingIndex]) device.OnFilesLoadSuccess(source, fileNames);
            _loadingUrls = _loadingUrls.Remove(loadingIndex);
            _loadingDevices = _loadingDevices.Remove(loadingIndex);
            _loadedUrls = _loadedUrls.Append(source);
            _cachedData = _cachedData.Append(fileNames);
        }

        protected override void ZlOnLoadError(string source, LoadError error)
        {
            ConsoleDebug($"FileController(TextZip): load failed: {error}, {source} ");
            if (!_loadingUrls.Has(source, out var loadingIndex)) return;
            foreach (var device in _loadingDevices[loadingIndex]) device.OnFilesLoadFailed(error);
            _loadingUrls = _loadingUrls.Remove(loadingIndex);
            _loadingDevices = _loadingDevices.Remove(loadingIndex);
        }

        protected override void IlOnLoadSuccess(string source, string[] fileNames)
        {
            if (!_loadingUrls.Has(source, out var loadingIndex)) return;
            ConsoleDebug($"FileController(Image): loaded successfully. {source} ");
            foreach (var device in _loadingDevices[loadingIndex]) device.OnFilesLoadSuccess(source, fileNames);
            _loadingUrls = _loadingUrls.Remove(loadingIndex);
            _loadingDevices = _loadingDevices.Remove(loadingIndex);
            _loadedUrls = _loadedUrls.Append(source);
            _cachedData = _cachedData.Append(fileNames);
        }

        protected override void IlOnLoadError(string source, LoadError error)
        {
            if (!_loadingUrls.Has(source, out var loadingIndex)) return;
            ConsoleDebug($"FileController(Image): load failed: {error}, {source}");
            foreach (var device in _loadingDevices[loadingIndex]) device.OnFilesLoadFailed(error);
            _loadingUrls = _loadingUrls.Remove(loadingIndex);
            _loadingDevices = _loadingDevices.Remove(loadingIndex);
        }

        protected override void VlOnLoadError(string source, LoadError error)
        {
            if (!_loadingUrls.Has(source, out var loadingIndex)) return;
            ConsoleDebug($"FileController(Video): load failed: {error}, {source} ");
            foreach (var device in _loadingDevices[loadingIndex]) device.OnFilesLoadFailed(error);
            _loadingUrls = _loadingUrls.Remove(loadingIndex);
            _loadingDevices = _loadingDevices.Remove(loadingIndex);
        }

        protected override void VlOnLoadSuccess(string source, string[] fileNames)
        {
            if (!_loadingUrls.Has(source, out var loadingIndex)) return;
            ConsoleDebug($"FileController(Video): loaded successfully. {source}");
            foreach (var device in _loadingDevices[loadingIndex]) device.OnFilesLoadSuccess(source, fileNames);
            _loadingUrls = _loadingUrls.Remove(loadingIndex);
            _loadingDevices = _loadingDevices.Remove(loadingIndex);
            _loadedUrls = _loadedUrls.Append(source);
            _cachedData = _cachedData.Append(fileNames);
        }

        protected override void VlOnLoadProgress(string source, float progress)
        {
            if (!_loadingUrls.Has(source, out var loadingIndex)) return;
            foreach (var device in _loadingDevices[loadingIndex]) device.OnFileLoadProgress(source, progress);
        }

        #endregion
    }
}