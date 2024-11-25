using JetBrains.Annotations;
using VRC.SDK3.Data;

namespace jp.ootr.ImageDeviceController.CommonDevice
{
    public class LogicLoadImage : BaseMethods
    {
        [NotNull] private readonly DataList _oQueueList = new DataList();

        private int _retryCount;
        [NotNull] private QueueList QueueList => (QueueList)_oQueueList;

        protected virtual void LLIFetchImage([CanBeNull] string source, URLType type, [CanBeNull] string options = "")
        {
            if (string.IsNullOrEmpty(source))
            {
                ConsoleError("Source is empty");
                return;
            }

            if (options == null) options = UrlUtil.BuildSourceOptions(type, 0, 0);

            var queue = QueueUtils.CreateQueue(source, options, (int)type);
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

            queue.Get(out var source, out var options, out var type);
            if (controller.LoadFilesFromUrl((CommonDevice)this, source, type, options)) return;
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
            if (QueueList.Count == 0) return;
            QueueList.ShiftQueue();
            if (QueueList.Count == 0) return;
            SendCustomEventDelayedFrames(nameof(FetchImageInternal), 1);
        }

        public virtual void OnFileLoadProgress([NotNull] string source, float progress)
        {
        }

        public virtual void OnFilesLoadSuccess([NotNull] string source, [NotNull] string[] fileNames)
        {
            FetchNextImage();
        }

        public virtual void OnFilesLoadFailed(LoadError error)
        {
            FetchNextImage();
        }
    }
}
