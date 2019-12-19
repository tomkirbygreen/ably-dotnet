using IO.Ably;

namespace TestObserver.Messages
{
    public class JoinTestClient
    {
        public string ClientName { get; private set; }

        public JoinTestClient(string clientName)
        {
            ClientName = clientName;
        }
    }

    public class LeaveTestClient
    {
        public string ClientName { get; set; }

        public LeaveTestClient(string clientName)
        {
            ClientName = clientName;
        }
    }

    public class SendMessage
    {
        public string Name { get; }
        public object Data { get; }

        public SendMessage(string name, object data)
        {
            Name = name;
            Data = data;
        }
    }

    public class MessageReceived
    {
        public Message Message { get; }

        public MessageReceived(Message message)
        {
            Message = message;
        }
    }

    public class PresenceReceived
    {
        public PresenceMessage Message { get; }

        public PresenceReceived(PresenceMessage message)
        {
            Message = message;
        }
    }

    public class SubscribeToChannel
    {
        public string ChannelName { get; }

        public SubscribeToChannel(string channelName)
        {
            ChannelName = channelName;
        }
    }

    public class UnSubscribeFromChannel
    {
        public string ChannelName { get; }

        public UnSubscribeFromChannel(string channelName)
        {
            ChannelName = channelName;
        }
    }


    public class PingPongTimeOut
    {
    }
}