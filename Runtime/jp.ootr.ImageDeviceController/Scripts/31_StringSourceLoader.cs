using JetBrains.Annotations;
using jp.ootr.common;
using VRC.SDK3.StringLoading;
using VRC.Udon.Common.Interfaces;

namespace jp.ootr.ImageDeviceController
{
    public class StringSourceLoader : EiaSourceLoader
    {

        private readonly string[] _stringLoaderPrefixes = { "StringLoader" };
        private bool _slIsLoading;
        private string[] _slQueuedSourceUrls = new string[0];
        private string _slCurrentSourceUrl = string.Empty;

        protected void SlLoadString([CanBeNull] string sourceUrl)
        {
            if (string.IsNullOrEmpty(sourceUrl))
            {
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
                OnSourceLoadError(sourceUrl, LoadError.URLNotSynced);
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

            if (result.IsValidEIA())
            {
                OnEIALoadSuccess(result);
                return;
            }

            ConsoleError($"Invalid file format from {result.Url}", _stringLoaderPrefixes);
            OnSourceLoadError(_slCurrentSourceUrl, LoadError.UnknownFileFormat);
            SendCustomEvent(nameof(SlLoadNext));
        }

        public override void OnStringLoadError(IVRCStringDownload result)
        {
            base.OnStringLoadError(result);
            ConsoleDebug($"{result.Error}/{result.ErrorCode}");
            OnSourceLoadError(_slCurrentSourceUrl, (LoadError)result.ErrorCode);
            SendCustomEvent(nameof(SlLoadNext));
        }

        protected sealed override void ZlOnLoadProgress(string sourceUrl, float progress)
        {
            OnSourceLoadProgress(sourceUrl, progress);
        }

        protected sealed override void ZlOnLoadSuccess(string sourceUrl, string[] fileUrls)
        {
            OnSourceLoadSuccess(sourceUrl, fileUrls);
            SendCustomEvent(nameof(SlLoadNext));
        }

        protected sealed override void ZlOnLoadError(string sourceUrl, LoadError error)
        {
            OnSourceLoadError(sourceUrl, error);
            SendCustomEvent(nameof(SlLoadNext));
        }

        protected sealed override void EIAOnLoadProgress(string sourceUrl, float progress)
        {
            OnSourceLoadProgress(sourceUrl, progress);
        }

        protected sealed override void EIAOnLoadSuccess(string sourceUrl, string[] fileUrls)
        {
            OnSourceLoadSuccess(sourceUrl, fileUrls);
            SendCustomEvent(nameof(SlLoadNext));
        }

        protected sealed override void EIAOnLoadError(string sourceUrl, LoadError error)
        {
            OnSourceLoadError(sourceUrl, error);
            SendCustomEvent(nameof(SlLoadNext));
        }
    }
}
