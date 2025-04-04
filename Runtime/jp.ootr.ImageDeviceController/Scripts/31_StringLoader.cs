using JetBrains.Annotations;
using jp.ootr.common;
using VRC.SDK3.StringLoading;
using VRC.Udon.Common.Interfaces;

namespace jp.ootr.ImageDeviceController
{
    public class StringLoader : ETILoader
    {

        private readonly string[] _stringLoaderPrefixes = { "StringLoader" };
        private bool _slIsLoading;
        private string[] _slQueuedUrlStrings = new string[0];
        private string _slCurrentUrlString = string.Empty;
        
        protected void SlLoadString([CanBeNull] string url)
        {
            if (string.IsNullOrEmpty(url)) {
                return;
            }

            _slQueuedUrlStrings = _slQueuedUrlStrings.Append(url);
            
            if (_slIsLoading)
            {
                ConsoleDebug($"Loading already in progress, queuing {url}", _stringLoaderPrefixes);
                return;
            }
            
            ConsoleDebug($"Loading {url}", _stringLoaderPrefixes);
            _slIsLoading = true;
            SlLoadNext();
        }
        
        public void SlLoadNext()
        {
            if (_slQueuedUrlStrings.Length == 0)
            {
                ConsoleDebug("No more URLs to load", _stringLoaderPrefixes);
                _slIsLoading = false;
                return;
            }

            _slQueuedUrlStrings = _slQueuedUrlStrings.Shift(out var sourceUrl, out var success);
            if (!success)
            {
                _slIsLoading = false;
                ConsoleDebug("no more URLs to load", _stringLoaderPrefixes);
                return;
            }
            
            if (string.IsNullOrEmpty(sourceUrl))
            {
                ConsoleDebug("Empty URL, skipping", _stringLoaderPrefixes);
                SendCustomEvent(nameof(SlLoadNext));
                return;
            }

            var url = UsGetUrl(sourceUrl);
            if (url == null)
            {
                ConsoleError($"Failed to get URL for {sourceUrl}", _stringLoaderPrefixes);
                SlOnLoadError(sourceUrl, LoadError.URLNotSynced);
                SendCustomEvent(nameof(SlLoadNext));
                return;
            }
            
            _slCurrentUrlString = sourceUrl;
            VRCStringDownloader.LoadUrl(url, (IUdonEventReceiver)this);
        }

        public override void OnStringLoadSuccess(IVRCStringDownload result)
        {
            if (result.Url.ToString() != _slCurrentUrlString)
            {
                ConsoleDebug($"Ignoring success for {result.Url}, current is {_slCurrentUrlString}", _stringLoaderPrefixes);
                return;
            }
            ConsoleLog($"Download success from {result.Url}", _stringLoaderPrefixes);
            if (result.IsValidTextZip())
            {
                OnZipLoadSuccess(result);
                return;
            }

            if (result.IsValidETI())
            {
                OnETILoadSuccess(result);
                return;
            }
            
            ConsoleError($"Invalid file format from {result.Url}", _stringLoaderPrefixes);
        }

        protected sealed override void ZlOnLoadProgress(string source, float progress)
        {
            SlOnLoadProgress(source, progress);
        }
        
        protected sealed override void ZlOnLoadSuccess(string source, string[] fileNames)
        {
            SlOnLoadSuccess(source, fileNames);
            SendCustomEvent(nameof(SlLoadNext));
        }
        
        protected sealed override void ZlOnLoadError(string source, LoadError error)
        {
            SlOnLoadError(source, error);
            SendCustomEvent(nameof(SlLoadNext));
        }
        
        protected sealed override void ETIOnLoadProgress(string source, float progress)
        {
            SlOnLoadProgress(source, progress);
        }
        
        protected sealed override void ETIOnLoadSuccess(string source, string[] fileNames)
        {
            SlOnLoadSuccess(source, fileNames);
            SendCustomEvent(nameof(SlLoadNext));
        }
        
        protected sealed override void ETIOnLoadError(string source, LoadError error)
        {
            SlOnLoadError(source, error);
            SendCustomEvent(nameof(SlLoadNext));
        }

        protected virtual void SlOnLoadProgress([CanBeNull] string source, float progress)
        {
            ConsoleError("ZipOnLoadProgress should not be called from base class", _stringLoaderPrefixes);
        }

        protected virtual void SlOnLoadSuccess([CanBeNull] string source, [CanBeNull] string[] fileNames)
        {
            ConsoleError("ZipOnLoadSuccess should not be called from base class", _stringLoaderPrefixes);
        }

        protected virtual void SlOnLoadError([CanBeNull] string source, LoadError error)
        {
            ConsoleError("ZipOnLoadError should not be called from base class", _stringLoaderPrefixes);
        }
    }
}
