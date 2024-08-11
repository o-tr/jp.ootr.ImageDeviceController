using static jp.ootr.common.ArrayUtils;

namespace jp.ootr.ImageDeviceController
{
    public class FileController : ImageLoader
    {
        protected string[][] CachedData = new string[0][];

        protected string[] LoadedUrls = new string[0];
        protected CommonDevice.CommonDevice[][] LoadingDevices = new CommonDevice.CommonDevice[0][];
        protected string[] LoadingUrls = new string[0];

        public virtual bool LoadFilesFromUrl(IControlledDevice _self, string source, URLType type, string options = "")
        {
            if (!UsHasUrl(source))
            {
                ConsoleError($"FileController: url not found in store: {source}");
                return false;
            }

            var self = (CommonDevice.CommonDevice)_self;
            if (((Cache)CacheFiles).HasSource(source))
            {
                var files = ((Cache)CacheFiles).GetSource(source);
                var fileNames = files.GetFileNames();
                ConsoleDebug($"FileController: {source} already loaded.");
                self.OnFilesLoadSuccess(source, fileNames);
                return true;
            }

            if (LoadingUrls.Has(source, out var loadingIndex))
            {
                ConsoleDebug($"FileController: {source} is already loading.");
                LoadingDevices[loadingIndex] = LoadingDevices[loadingIndex].Append(self);
                return true;
            }

            ConsoleDebug($"FileController: loading {source}.");
            LoadingUrls = LoadingUrls.Append(source);
            LoadingDevices = LoadingDevices.Append(new[] { self });
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
            if (LoadingUrls.Has(source, out var loadingIndex))
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

            CcOnRelease(source);
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