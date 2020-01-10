using System;
using System.Reactive;
using System.Reactive.Linq;
using IO.Ably;
using Newtonsoft.Json.Linq;
using Xamarin.Forms;

namespace AndroidSample
{
    public class PingPongPayload
    {
        public int Counter { get; set; }
        public override string ToString()
        {
            return "Counter: " + Counter;
        }
    }

    public class TestInfoPayload
    {
        public string Name { get; set; }
        public string Action { get; set; }
        public string Channel { get; set; }
        public string Reason { get; set; }
    }

    public class MainViewModel : ObservableObject
    {
        private readonly AblyService _ably;
        private string _connectionStatus = "Initialised";
        private string _info;
        private string _sentMessage;
        private string _receivedMessage;
        private IDisposable _pingPongSubscription;

        public MainViewModel(AblyService ably)
        {
            _ably = ably;
            SendMessageCommand = new Command(() =>
            {
                _ably.SendMessage("test", "test", "Martin");
            });
            StartCommand = new Command(() =>
            {
                _ably.Connect();
            });

            StopCommand = new Command(() =>  _ably.Close());
            var messageObserver = Observer.Create<Message>(m =>
            {
                ReceivedMessage = "Received: " + m.ToString();
            });

            var testObserver = Observer.Create<Message>(m =>
            {
                if (m.Name == "Test")
                {
                    if(m.Data is JObject)
                    {
                        var payload = ((JObject) m.Data).ToObject<TestInfoPayload>();

                        switch (payload.Action)
                        {
                            case "Start":
                                Info = "Starting PingPongTest";
                                StartPingPongTest(payload);
                                break;
                            case "Stop":
                                Info = "Stopping PingPong test";
                                StopPingPongTest(payload);
                                break;
                        }
                    }
                }

                ReceivedMessage = m.ToString();
            });

            ably.SubsrcibeToChannel("test").Subscribe(messageObserver);
            ably.SubsrcibeToChannel("gameroom").Subscribe(messageObserver);
            ably.Ably.Channels.Get("gameroom").Presence.Enter();
            ably.SubsrcibeToChannel(ably.ClientId).Subscribe(testObserver);
        }

        private void StopPingPongTest(TestInfoPayload payload)
        {
            _pingPongSubscription?.Dispose();
            _ably.Ably.Channels.Release(payload.Channel);
            Info = $"Error: {payload.Reason}";
        }

        private void StartPingPongTest(TestInfoPayload testInfo)
        {
            var observer = Observer.Create<PingPongPayload>(payload =>
            {
                var newData = new PingPongPayload {Counter = payload.Counter + 1 };
                SentMessage = "Sent: PingPong - Counter: " + newData.Counter;
                _ably.SendMessage(testInfo.Channel, "PingPong", newData);
            });

            _pingPongSubscription = _ably.SubsrcibeToChannel(testInfo.Channel)
                .Where(x => x.Name == "PingPong" && x.Data is JObject)
                .Select(x => ((JObject)x.Data).ToObject<PingPongPayload>())
                .Subscribe(observer);

            _ably.SendMessage(testInfo.Channel, "Start", "test");
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }

        public string Info
        {
            get => _info;
            set => SetProperty(ref _info, value);
        }

        public string SentMessage
        {
            get => _sentMessage;
            set => SetProperty(ref _sentMessage, value);
        }

        public string ReceivedMessage
        {
            get => _receivedMessage;
            set => SetProperty(ref _receivedMessage, value);
        }

        public Command SendMessageCommand { get; }
        public Command StartCommand { get; }
        public Command StopCommand { get; }
    }
}
