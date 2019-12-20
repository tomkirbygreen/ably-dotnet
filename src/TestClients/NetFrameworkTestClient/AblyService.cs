using System;
using System.Diagnostics;
using System.Reactive.Subjects;
using IO.Ably;
using IO.Ably.Realtime;

namespace NetFrameworkTestClient
{

    public class LogMessage
    {
        public string Level { get; set; }
        public string Message { get; set; }

        public LogMessage(string message, string level)
        {
            Message = message;
            Level = level;
        }
    }

    public class AblyService : IObservable<LogMessage>, IObservable<string>, ILoggerSink
    {
        public AblyRealtime Ably { get; private set; }
        private string ApiKey { get; }
        private readonly ISubject<LogMessage> _logSubject = new Subject<LogMessage>();
        private readonly ISubject<string> _connectionSubject = new Subject<string>();

        public string ClientId { get; set; }
        public void Init(string clientId)
        {
            ClientId = clientId;

            Ably = new AblyRealtime(new ClientOptions(ApiKey)
            {
                LogHander = this,
                LogLevel = LogLevel.Debug,
                AutoConnect = false,
                UseBinaryProtocol = false,
                ClientId = clientId,
                EchoMessages = false
            });
            Ably.Connection.On(change =>
            {
                if(change.Current == ConnectionState.Connected)
                    foreach(var channel in Ably.Channels)
                        channel.Attach();

                _connectionSubject.OnNext(change.Current.ToString());
            });
        }

        public void Connect()
        {
            Ably.Connect();
        }

        public void Close()
        {
            Ably.Close();
        }

        public void SendMessage(string channel, string name, string value)
        {
            Ably.Channels.Get(channel).Publish(name, value);
        }

        public void SendMessage<T>(string channel, string name, T value) where T : class
        {
            Ably.Channels.Get(channel).Publish(name, value);
        }

        public IObservable<Message> SubsrcibeToChannel(string channelName)
        {
            var subject = new Subject<Message>();
            Ably.Channels.Get(channelName).Subscribe(subject.OnNext);
            return subject;
        }

        public void LogEvent(LogLevel level, string message)
        {
            Debug.WriteLine("ably", $"[{level}] {message}");
            _logSubject.OnNext(new LogMessage(message, level.ToString()));
        }

        public IDisposable Subscribe(IObserver<LogMessage> observer)
        {
            return _logSubject.Subscribe(observer);
        }

        public IDisposable Subscribe(IObserver<string> observer)
        {
            return _connectionSubject.Subscribe(observer);
        }

        public AblyService(string apiKey)
        {
            ApiKey = apiKey;
        }
    }
}