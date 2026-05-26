using System;

namespace TomatoRadar.Models
{
    public enum MessageType
    {
        INFO,
        ERROR,
    }
    class NotificationMessage
    {
        public DateTimeOffset Time { get; set; }
        public MessageType Type { get; set; }
        public string Message { get; set; }

        public NotificationMessage(DateTimeOffset time, MessageType type, string message)
        {
            Time = time;
            Type = type;
            Message = message;
        }
    }
}
