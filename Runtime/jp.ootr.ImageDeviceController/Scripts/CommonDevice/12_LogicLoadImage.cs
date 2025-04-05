using JetBrains.Annotations;
using VRC.SDK3.Data;

namespace jp.ootr.ImageDeviceController.CommonDevice
{
    public class LogicLoadImage : BaseMethods
    {
        [NotNull] private readonly DataList _oQueueList = new DataList();

        private int _retryCount;
        [NotNull] private QueueList QueueList => (QueueList)_oQueueList;

        protected virtual void LLIFetchImage([CanBeNull] string sourceUrl, SourceType type, [CanBeNull] string options = "")
        {
            if (string.IsNullOrEmpty(sourceUrl))
            {
                ConsoleError("Source is empty");
                return;
            }

            if (options == null) options = UrlUtil.BuildSourceOptions(type, 0, 0);

            var queue = QueueUtils.CreateQueue(sourceUrl, options, (int)type);
            QueueList.AddQueue(queue);

            if (QueueList.Count > 1) return;
            _retryCount = 0;
            FetchImageInternal();
        }

        /**
         * @private
         * <summary>SendCustomEventDelayedSecondsで呼ぶためにpublicにしているが実質的にはprivateでほかから呼ばれることを想定していない</summary>
         */
        public virtual void FetchImageInternal()
        {
            if (QueueList.Count == 0)
            {
                ConsoleError($"Index out of range: {nameof(FetchImageInternal)}");
                return;
            }

            var queue = QueueList.GetQueue(0);
            if (queue == null)
            {
                ConsoleError($"Index out of range: {nameof(FetchImageInternal)}");
                return;
            }

            queue.Get(out var sourceUrl, out var options, out var type);
            if (controller.LoadSource((CommonDevice)this, sourceUrl, type, options)) return;
            if (_retryCount >= SyncURLRetryCountLimit)
            {
                OnSourceLoadFailed(LoadError.URLNotSynced);
                return;
            }

            _retryCount++;
            SendCustomEventDelayedSeconds(nameof(FetchImageInternal), SyncURLRetryInterval);
        }

        private void FetchNextImage()
        {
            if (QueueList.Count == 0) return;
            QueueList.ShiftQueue();
            if (QueueList.Count == 0) return;
            SendCustomEventDelayedFrames(nameof(FetchImageInternal), 1);
        }

        public virtual void OnSourceLoadProgress([NotNull] string sourceUrl, float progress)
        {
        }

        public virtual void OnSourceLoadSuccess([NotNull] string sourceUrl, [NotNull] string[] fileUrls)
        {
            FetchNextImage();
        }

        public virtual void OnSourceLoadFailed(LoadError error)
        {
            FetchNextImage();
        }
    }
}
