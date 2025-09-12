using System;

namespace Message_Client
{
    public class MessageItem
    {
        public string Time { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty;
        public MessageType Type { get; set; } = MessageType.Normal;

        public MessageItem(string message, string sender = "", MessageType type = MessageType.Normal)
        {
            Time = DateTime.Now.ToString("HH:mm:ss");
            Message = message;
            Sender = sender;
            Type = type;
        }
    }

    public enum MessageType
    {
        Normal,      // Tin nhắn bình thường
        System,      // Tin nhắn hệ thống
        Error,       // Tin nhắn lỗi
        Connected,   // Thông báo kết nối
        Disconnected // Thông báo ngắt kết nối
    }
}
