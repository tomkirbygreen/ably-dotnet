using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Input;
using IO.Ably;
using Newtonsoft.Json.Linq;

namespace NetFrameworkTestClient
{
    public class MainViewModel
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
            _ably.Connect();

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

            ably.SubsrcibeToChannel("test")
                .Subscribe(messageObserver);
            ably.SubsrcibeToChannel("gameroom")
                .Subscribe(messageObserver);
            ably.Ably.Channels.Get("gameroom").Presence.Enter();
            ably.SubsrcibeToChannel(ably.ClientId)
                .Subscribe(testObserver);
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
            set => Console.WriteLine("Connection: " + value);
        }

        public string Info
        {
            set { Console.WriteLine("Info: " + value); }
        }

        public string SentMessage
        {
            set => Console.WriteLine("Sent: " + value);
        }

        public string ReceivedMessage
        {
            set => Console.WriteLine("Received: " + value);
        }
    }
}