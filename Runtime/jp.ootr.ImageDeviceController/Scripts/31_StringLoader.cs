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
        private string[] _slQueuedSourceUrls = new string[0];
        private string _slCurrentSourceUrl = string.Empty;
        
        protected void SlLoadString([CanBeNull] string sourceUrl)
        {
            if (string.IsNullOrEmpty(sourceUrl)) {
                return;
            }

            _slQueuedSourceUrls = _slQueuedSourceUrls.Append(sourceUrl);
            
            if (_slIsLoading)
            {
                ConsoleDebug($"Loading already in progress, queuing {sourceUrl}", _stringLoaderPrefixes);
                return;
            }
            
            ConsoleDebug($"Loading {sourceUrl}", _stringLoaderPrefixes);
            _slIsLoading = true;
            SlLoadNext();
        }
        
        public void SlLoadNext()
        {
            if (_slQueuedSourceUrls.Length == 0)
            {
                ConsoleDebug("No more URLs to load", _stringLoaderPrefixes);
                _slIsLoading = false;
                return;
            }

            _slQueuedSourceUrls = _slQueuedSourceUrls.Shift(out var sourceUrl, out var success);
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

            var source = UsGetUrl(sourceUrl);
            if (source == null)
            {
                ConsoleError($"Failed to get URL for {sourceUrl}", _stringLoaderPrefixes);
                SlOnLoadError(sourceUrl, LoadError.URLNotSynced);
                SendCustomEvent(nameof(SlLoadNext));
                return;
            }
            
            _slCurrentSourceUrl = sourceUrl;
            VRCStringDownloader.LoadUrl(source, (IUdonEventReceiver)this);
        }

        public override void OnStringLoadSuccess(IVRCStringDownload result)
        {
            if (result.Url.ToString() != _slCurrentSourceUrl)
            {
                ConsoleDebug($"Ignoring success for {result.Url}, current is {_slCurrentSourceUrl}", _stringLoaderPrefixes);
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

        protected sealed override void ZlOnLoadProgress(string sourceUrl, float progress)
        {
            SlOnLoadProgress(sourceUrl, progress);
        }
        
        protected sealed override void ZlOnLoadSuccess(string sourceUrl, string[] fileUrls)
        {
            SlOnLoadSuccess(sourceUrl, fileUrls);
            SendCustomEvent(nameof(SlLoadNext));
        }
        
        protected sealed override void ZlOnLoadError(string sourceUrl, LoadError error)
        {
            SlOnLoadError(sourceUrl, error);
            SendCustomEvent(nameof(SlLoadNext));
        }
        
        protected sealed override void ETIOnLoadProgress(string sourceUrl, float progress)
        {
            SlOnLoadProgress(sourceUrl, progress);
        }
        
        protected sealed override void ETIOnLoadSuccess(string sourceUrl, string[] fileUrls)
        {
            SlOnLoadSuccess(sourceUrl, fileUrls);
            SendCustomEvent(nameof(SlLoadNext));
        }
        
        protected sealed override void ETIOnLoadError(string sourceUrl, LoadError error)
        {
            SlOnLoadError(sourceUrl, error);
            SendCustomEvent(nameof(SlLoadNext));
        }

        protected virtual void SlOnLoadProgress([CanBeNull] string sourceUrl, float progress)
        {
            ConsoleError("ZipOnLoadProgress should not be called from base class", _stringLoaderPrefixes);
        }

        protected virtual void SlOnLoadSuccess([CanBeNull] string sourceUrl, [CanBeNull] string[] fileUrls)
        {
            ConsoleError("ZipOnLoadSuccess should not be called from base class", _stringLoaderPrefixes);
        }

        protected virtual void SlOnLoadError([CanBeNull] string sourceUrl, LoadError error)
        {
            ConsoleError("ZipOnLoadError should not be called from base class", _stringLoaderPrefixes);
        }
    }
}
