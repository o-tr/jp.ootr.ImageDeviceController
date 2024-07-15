using static jp.ootr.common.ArrayUtils;

namespace jp.ootr.ImageDeviceController
{
    public class FileController : ImageLoader
    {
        protected string[][] CachedData = new string[0][];

        protected string[] LoadedUrls = new string[0];
        protected CommonDevice.CommonDevice[][] LoadingDevices = new CommonDevice.CommonDevice[0][];
        protected string[] LoadingUrls = new string[0];

        public virtual bool LoadFilesFromUrl(IControlledDevice _self, string url, URLType type)
        {
            if (!UsHasUrl(url))
            {
                ConsoleError($"FileController: url not found in store: {url}");
                return false;
            }

            var self = (CommonDevice.CommonDevice)_self;
            if (LoadedUrls.Has(url, out var loadedIndex) && FileSources.Has(url))
            {
                ConsoleDebug($"FileController: {url} already loaded.");
                self.OnFilesLoadSuccess(url, CachedData[loadedIndex]);
                return true;
            }

            if (LoadingUrls.Has(url, out var loadingIndex))
            {
                ConsoleDebug($"FileController: {url} is already loading.");
                LoadingDevices[loadingIndex] = LoadingDevices[loadingIndex].Append(self);
                return true;
            }

            ConsoleDebug($"FileController: loading {url}.");
            LoadingUrls = LoadingUrls.Append(url);
            LoadingDevices = LoadingDevices.Append(new[] { self });
            switch (type)
            {
                case URLType.Image:
                    IlLoadImage(url);
                    break;
                case URLType.TextZip:
                    ZlLoadZip(url);
                    break;
                case URLType.Video:
                    VlLoadVideo(url);
                    break;
            }

            return true;
        }

        public virtual void UnloadFilesFromUrl(IControlledDevice _self, string url)
        {
            var self = (CommonDevice.CommonDevice)_self;
            if (LoadingUrls.Has(url, out var loadingIndex))
            {
                if (!LoadingDevices[loadingIndex].Has(self, out var deviceIndex)) return;
                {
                    LoadingDevices[loadingIndex] = LoadingDevices[loadingIndex].Remove(deviceIndex);
                    if (LoadingDevices[loadingIndex].Length == 0)
                    {
                        LoadingUrls = LoadingUrls.Remove(loadingIndex);
                        LoadingDevices = LoadingDevices.Remove(loadingIndex);
                    }
                }
            }
        }

        #region EventReceiver

        protected override void CcOnRelease(string source)
        {
            if (!LoadedUrls.Has(source, out var loadedIndex)) return;
            LoadedUrls = LoadedUrls.Remove(loadedIndex);
            CachedData = CachedData.Remove(loadedIndex);
        }


        protected override void ZlOnLoadProgress(string source, float progress)
        {
            if (!LoadingUrls.Has(source, out var loadingIndex)) return;
            foreach (var device in LoadingDevices[loadingIndex]) device.OnFileLoadProgress(source, progress);
        }

        protected override void ZlOnLoadSuccess(string source, string[] fileNames)
        {
            if (!LoadingUrls.Has(source, out var loadingIndex)) return;
            ConsoleDebug(
                $"FileController(TextZip): {source} loaded successfully. {fileNames.Length} files. device count: {LoadingDevices[loadingIndex].Length}");
            foreach (var device in LoadingDevices[loadingIndex]) device.OnFilesLoadSuccess(source, fileNames);
            LoadingUrls = LoadingUrls.Remove(loadingIndex);
            LoadingDevices = LoadingDevices.Remove(loadingIndex);
            LoadedUrls = LoadedUrls.Append(source);
            CachedData = CachedData.Append(fileNames);
        }

        protected override void ZlOnLoadError(string source, LoadError error)
        {
            ConsoleDebug($"FileController(TextZip): {source} load failed: {error}");
            if (!LoadingUrls.Has(source, out var loadingIndex)) return;
            foreach (var device in LoadingDevices[loadingIndex]) device.OnFilesLoadFailed(error);
            LoadingUrls = LoadingUrls.Remove(loadingIndex);
            LoadingDevices = LoadingDevices.Remove(loadingIndex);
        }

        protected override void IlOnLoadSuccess(string source, string[] fileNames)
        {
            if (!LoadingUrls.Has(source, out var loadingIndex)) return;
            ConsoleDebug($"FileController(Image): {source} loaded successfully.");
            foreach (var device in LoadingDevices[loadingIndex]) device.OnFilesLoadSuccess(source, fileNames);
            LoadingUrls = LoadingUrls.Remove(loadingIndex);
            LoadingDevices = LoadingDevices.Remove(loadingIndex);
            LoadedUrls = LoadedUrls.Append(source);
            CachedData = CachedData.Append(fileNames);
        }

        protected override void IlOnLoadError(string source, LoadError error)
        {
            if (!LoadingUrls.Has(source, out var loadingIndex)) return;
            ConsoleDebug($"FileController(Image): {source} load failed: {error}");
            foreach (var device in LoadingDevices[loadingIndex]) device.OnFilesLoadFailed(error);
            LoadingUrls = LoadingUrls.Remove(loadingIndex);
            LoadingDevices = LoadingDevices.Remove(loadingIndex);
        }

        protected override void VlOnLoadError(string source, LoadError error)
        {
            if (!LoadingUrls.Has(source, out var loadingIndex)) return;
            ConsoleDebug($"FileController(Video): {source} load failed: {error}");
            foreach (var device in LoadingDevices[loadingIndex]) device.OnFilesLoadFailed(error);
            LoadingUrls = LoadingUrls.Remove(loadingIndex);
            LoadingDevices = LoadingDevices.Remove(loadingIndex);
        }

        protected override void VlOnLoadSuccess(string source, string[] fileNames)
        {
            if (!LoadingUrls.Has(source, out var loadingIndex)) return;
            ConsoleDebug($"FileController(Video): {source} loaded successfully.");
            foreach (var device in LoadingDevices[loadingIndex]) device.OnFilesLoadSuccess(source, fileNames);
            LoadingUrls = LoadingUrls.Remove(loadingIndex);
            LoadingDevices = LoadingDevices.Remove(loadingIndex);
            LoadedUrls = LoadedUrls.Append(source);
            CachedData = CachedData.Append(fileNames);
        }

        protected override void VlOnLoadProgress(string source, float progress)
        {
            if (!LoadingUrls.Has(source, out var loadingIndex)) return;
            foreach (var device in LoadingDevices[loadingIndex]) device.OnFileLoadProgress(source, progress);
        }

        #endregion
    }
}