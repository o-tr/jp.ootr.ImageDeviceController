using jp.ootr.common;

namespace jp.ootr.ImageDeviceController
{
    public class FileController : SourceController
    {
        private readonly string[] _fileControllerPrefixes = { "FileController" };
        
        private string[] _loadedFileUrls = new string[0];
        
        private string[] _loadingFileSourceUrls = new string[0];
        private string[] _loadingFileUrls = new string[0];
        private CommonDevice.CommonDevice[][] _loadingFileDevices = new CommonDevice.CommonDevice[0][];
        private string[][] _loadingFileDeviceChannels = new string[0][];
        
        public bool LoadFile(CommonDevice.CommonDevice self, string sourceUrl, string fileUrl, int priority = 1, string channel = null)
        {
            if (self == null || fileUrl == null)
            {
                ConsoleDebug("LoadFile called with null self or fileUrl", _fileControllerPrefixes);
                return false;
            }
            
            if (!fileUrl.StartsWith(PROTOCOL_EIA))
            {
                self.OnFileLoadSuccess(sourceUrl, fileUrl, channel);
                return true;
            }
            
            if (_loadedFileUrls.Has(fileUrl))
            {
                ConsoleDebug($"File already loaded: {fileUrl}", _fileControllerPrefixes);
                self.OnFileLoadSuccess(sourceUrl, fileUrl, channel);
                return true;
            }
            
            if (_loadingFileUrls.Has(fileUrl, out var loadingIndex))
            {
                ConsoleDebug($"File already loading: {fileUrl}, devices: {_loadingFileDevices[loadingIndex].Length}", _fileControllerPrefixes);
                _loadingFileDevices[loadingIndex] = _loadingFileDevices[loadingIndex].Append(self);
                _loadingFileDeviceChannels[loadingIndex] = _loadingFileDeviceChannels[loadingIndex].Append(channel);
                return true;
            }
            
            ConsoleDebug($"Loading file: {fileUrl}, devices: 1", _fileControllerPrefixes);
            _loadingFileSourceUrls = _loadingFileSourceUrls.Append(sourceUrl);
            _loadingFileUrls = _loadingFileUrls.Append(fileUrl);
            _loadingFileDevices = _loadingFileDevices.Append(new[] { self });
            _loadingFileDeviceChannels = _loadingFileDeviceChannels.Append(new[] { channel });

            EIALoadFile(sourceUrl, fileUrl, priority);
            return true;
        }

        protected override void OnFileLoadSuccess(string fileUrl)
        {
            if (fileUrl == null || !_loadingFileUrls.Has(fileUrl, out var loadingIndex))
            {
                ConsoleDebug($"OnFileLoadSuccess called with null or unknown fileUrl: {fileUrl}", _fileControllerPrefixes);
                return;
            }
            ConsoleDebug($"File loaded successfully: {fileUrl}, devices: {_loadingFileDevices[loadingIndex].Length}", _fileControllerPrefixes);
            _loadingFileSourceUrls = _loadingFileSourceUrls.Remove(loadingIndex, out var sourceUrl);
            _loadedFileUrls = _loadedFileUrls.Append(fileUrl);
            _loadingFileUrls = _loadingFileUrls.Remove(loadingIndex);
            _loadingFileDevices = _loadingFileDevices.Remove(loadingIndex, out var devices);
            _loadingFileDeviceChannels = _loadingFileDeviceChannels.Remove(loadingIndex, out var channels);
            if (devices == null || devices.Length == 0 || channels == null) return;
            for (var i = 0; i < devices.Length; i++)
            {
                var device = devices[i];
                if (!device) continue;
                var channel = channels.Length > i ? channels[i] : null;
                device.OnFileLoadSuccess(sourceUrl, fileUrl, channel);
            }
        }

        protected override void OnFileLoadError(string fileUrl, LoadError error)
        {
            if (fileUrl == null || !_loadingFileUrls.Has(fileUrl, out var loadingIndex))
            {
                ConsoleDebug($"OnFileLoadError called with null or unknown fileUrl: {fileUrl}", _fileControllerPrefixes);   
                return;
            }
            ConsoleDebug($"File load error: {fileUrl}, devices: {_loadingFileDevices[loadingIndex].Length}", _fileControllerPrefixes);
            _loadingFileSourceUrls = _loadingFileSourceUrls.Remove(loadingIndex, out var sourceUrl);
            _loadingFileUrls = _loadingFileUrls.Remove(loadingIndex);
            _loadingFileDevices = _loadingFileDevices.Remove(loadingIndex, out var devices);
            _loadingFileDeviceChannels = _loadingFileDeviceChannels.Remove(loadingIndex, out var channels);
            if (devices == null || devices.Length == 0 || channels == null) return;
            for (var i = 0; i < devices.Length; i++)
            {
                var device = devices[i];
                if (!device) continue;
                var channel = channels.Length > i ? channels[i] : null;
                device.OnFileLoadError(sourceUrl, fileUrl, channel, error);
            }
        }
    }
}
