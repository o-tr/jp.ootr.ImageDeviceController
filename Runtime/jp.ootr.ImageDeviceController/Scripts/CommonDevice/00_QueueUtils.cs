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
        public static void AddQueue(this QueueList queues, Queue queue)
        {
            queues.Add(queue);
        }
        
        public static void ShiftQueue(this QueueList queues)
        {
            queues.RemoveAt(0);
        }
        
        public static Queue GetQueue(this QueueList queues, int index)
        {
            if (index < 0 || index >= queues.Count)
            {
                return null;
            }
            return (Queue)queues[index].DataDictionary;
        }
        
        public static Queue CreateQueue(string source, string options, int type)
        {
            var queue = new DataDictionary();
            queue["source"] = source;
            queue["options"] = options;
            queue["type"] = type;
            return (Queue)queue;
        }
        
        public static void Get(this Queue queue, out string source, out string options, out URLType type)
        {
            source = queue["source"].String;
            options = queue["options"].String;
            type = (URLType)(int)queue["type"].Double;
        }
    }
}