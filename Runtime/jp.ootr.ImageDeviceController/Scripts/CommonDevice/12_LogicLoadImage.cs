using VRC.SDK3.Data;

namespace jp.ootr.ImageDeviceController.CommonDevice
{
    public class LogicLoadImage : BaseMethods
    {
        private readonly DataList _queueList = new DataList();

        private int _retryCount;

        protected virtual void LLIFetchImage(string source, URLType type, string options = "")
        {
            var queue = QueueUtils.CreateQueue(source, options, (int)type);
            ((QueueList)_queueList).AddQueue(queue);

            if (_queueList.Count > 1) return;
            _retryCount = 0;
            FetchImageInternal();
        }

        /**
         * @private
         * <summary>SendCustomEventDelayedSecondsで呼ぶためにpublicにしているが実質的にはprivateでほかから呼ばれることを想定していない</summary>
         */
        public virtual void FetchImageInternal()
        {
            ((QueueList)_queueList).GetQueue(0).Get(out var source, out var options, out var type);
            if (controller.LoadFilesFromUrl((IControlledDevice)this, source, type, options)) return;
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
            ((QueueList)_queueList).ShiftQueue();
            if (_queueList.Count == 0) return;
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