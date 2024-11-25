using JetBrains.Annotations;
using VRC.SDK3.Data;

namespace jp.ootr.ImageDeviceController.CommonDevice
{
    public abstract class QueueList : DataList
    {
    }

    public abstract class Queue : DataDictionary
    {
    }

    public static class QueueUtils
    {
        public static void AddQueue([CanBeNull] this QueueList queues, Queue queue)
        {
            if (queues == null) return;
            queues.Add(queue);
        }

        public static void ShiftQueue([CanBeNull] this QueueList queues)
        {
            if (queues == null || queues.Count == 0) return;
            queues.RemoveAt(0);
        }

        [CanBeNull]
        public static Queue GetQueue([CanBeNull] this QueueList queues, int index)
        {
            if (queues == null || index < 0 || index >= queues.Count) return null;
            return (Queue)queues[index].DataDictionary;
        }

        [NotNull]
        public static Queue CreateQueue([NotNull] string source, [NotNull] string options, int type)
        {
            var queue = new DataDictionary();
            queue["source"] = source;
            queue["options"] = options;
            queue["type"] = type;
            return (Queue)queue;
        }

        public static void Get([NotNull] this Queue queue, out string source, out string options, out URLType type)
        {
            source = queue["source"].String;
            options = queue["options"].String;
            type = (URLType)(int)queue["type"].Double;
        }
    }
}
