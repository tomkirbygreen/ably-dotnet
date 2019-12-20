using System;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using IO.Ably;
using IO.Ably.Realtime;
using Serilog;
using TestObserver.Actors;
using LogLevel = IO.Ably.LogLevel;

namespace TestObserver
{
    class Program
    {
        private static ActorSystem ActorSystemInstance;
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().WriteTo.ColoredConsole().CreateLogger();
            ActorSystemInstance = ActorSystem.Create("TestSystem");

            var testController = ActorSystemInstance.ActorOf(Props.Create(() => new TestControllerActor("lNj80Q.iGyVcQ:2QKX7FFASfX-7H9H")), "TestController");

            Console.WriteLine("Hello World!");
            Console.ReadLine();
        }
    }

    public class AblyService : IObservable<ConnectionStateChange>
    {
        public AblyRealtime Ably { get; }
        private readonly ISubject<LogMessage> _logSubject = new Subject<LogMessage>();
        private readonly ISubject<ConnectionStateChange> _connectionSubject = new Subject<ConnectionStateChange>();

        public AblyService(string apiKey)
        {
            Ably = new AblyRealtime(new ClientOptions(apiKey)
            {
                LogHander = new SerilogSink(null),
                LogLevel = LogLevel.Debug,
                AutoConnect = false,
                UseBinaryProtocol = false,
                EchoMessages = false
            });

            Ably.Connection.On(change =>
            {
                _connectionSubject.OnNext(change);
            });
        }

        public void Connect()
        {
            Ably.Connect();
        }

        public void SendMessage(string channel, string name, object value)
        {
            Ably.Channels.Get(channel).Publish(name, value);
        }

        public IObservable<Message> MessageObservable(string channelName)
        {
            var subject = new Subject<Message>();
            Ably.Channels.Get(channelName).Subscribe(subject.OnNext);
            return subject;
        }

        public IObservable<PresenceMessage> PresenceObservable(string channelName)
        {
            var subject = new Subject<PresenceMessage>();
            Ably.Channels.Get(channelName).Presence.Subscribe(subject.OnNext);
            return subject;
        }

        public IDisposable Subscribe(IObserver<ConnectionStateChange> observer)
        {
            return _connectionSubject.Subscribe();
        }
    }
}