using jp.ootr.common;

namespace jp.ootr.ImageDeviceController.CommonDevice
{
    public class LogicLoadImage : BaseMethods
    {
        private string[] _fetchTargetOptions = new string[0];
        private string[] _fetchTargetSources = new string[0];
        private URLType[] _fetchTargetTypes = new URLType[0];

        private int _retryCount;

        protected virtual void LLIFetchImage(string source, URLType type, string options = "")
        {
            _fetchTargetSources = _fetchTargetSources.Append(source);
            _fetchTargetTypes = _fetchTargetTypes.Append(type);
            _fetchTargetOptions = _fetchTargetOptions.Append(options);

            if (_fetchTargetSources.Length > 1) return;
            _retryCount = 0;
            FetchImageInternal();
        }

        /**
         * @private
         * <summary>SendCustomEventDelayedSecondsで呼ぶためにpublicにしているが実質的にはprivateでほかから呼ばれることを想定していない</summary>
         */
        public virtual void FetchImageInternal()
        {
            if (controller.LoadFilesFromUrl((IControlledDevice)this, _fetchTargetSources[0], _fetchTargetTypes[0],
                    _fetchTargetOptions[0])) return;
            if (_retryCount >= SyncURLRetryCountLimit)
            {
                OnFilesLoadFailed(LoadError.URLNotSynced);
                return;
            }

            _retryCount++;
            SendCustomEventDelayedSeconds(nameof(FetchImageInternal), SyncURLRetryInterval);
        }

        private void FetchNextImage()
        {
            _fetchTargetSources = _fetchTargetSources.__Shift();
            _fetchTargetTypes = _fetchTargetTypes.__Shift();
            _fetchTargetOptions = _fetchTargetOptions.__Shift();
            if (_fetchTargetSources.Length == 0) return;
            SendCustomEventDelayedFrames(nameof(FetchImageInternal), 1);
        }

        public virtual void OnFileLoadProgress(string source, float progress)
        {
        }

        public virtual void OnFilesLoadSuccess(string source, string[] fileNames)
        {
            FetchNextImage();
        }

        public virtual void OnFilesLoadFailed(LoadError error)
        {
            FetchNextImage();
        }
    }
}