namespace jp.ootr.ImageDeviceController.CommonDevice
{
    public class LogicLoadImage : BaseMethods
    {
        private string _fetchTargetSource;
        private URLType _fetchTargetType;
        private int _retryCount;

        protected virtual void LLIFetchImage(string source, URLType type)
        {
            _fetchTargetSource = source;
            _fetchTargetType = type;
            _retryCount = 0;
            FetchImageInternal();
        }

        /**
         * @private
         * <summary>SendCustomEventDelayedSecondsで呼ぶためにpublicにしているが実質的にはprivateでほかから呼ばれることを想定していない</summary>
         */
        public virtual void FetchImageInternal()
        {
            if (Controller.LoadFilesFromUrl((IControlledDevice)this, _fetchTargetSource, _fetchTargetType)) return;
            if (_retryCount >= SyncURLRetryCountLimit)
            {
                OnFilesLoadFailed(LoadError.URLNotSynced);
                return;
            }

            _retryCount++;
            SendCustomEventDelayedSeconds(nameof(FetchImageInternal), SyncURLRetryInterval);
        }

        public virtual void OnFileLoadProgress(string source, float progress)
        {
        }

        public virtual void OnFilesLoadSuccess(string source, string[] fileNames)
        {
        }

        public virtual void OnFilesLoadFailed(LoadError error)
        {
        }
    }
}