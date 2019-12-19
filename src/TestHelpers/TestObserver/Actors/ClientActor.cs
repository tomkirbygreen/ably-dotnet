using System;
using Akka.Actor;
using Akka.Event;
using Akka.Logger.Serilog;
using Newtonsoft.Json.Linq;
using Serilog.Core;
using TestObserver.Messages;

namespace TestObserver.Actors
{
    public class PingPongPayload
    {
        public int Counter { get; set; }
    }

    public class ClientActor : ReceiveActor
    {
        private readonly string _clientName;
        private int _health;
        private ICancelable _pingPongSchedule;
        private IActorRef AblyProxy;
        private int _pingCounter = 0;
        private AblyService _ably;
        private ILoggingAdapter _logger;

        public ClientActor(string clientName, AblyService ably)
        {
            AblyProxy = Context.ActorOf(
                Props.Create(() => new AblyBridgeActor(ably, Self, clientName, false))
            );

            _clientName = clientName;
            _ably = ably;
            _health = 100;

            _logger = Context.GetLogger<SerilogLoggingAdapter>();

            // Start Ping Pong test
            StartPing();

            Receive<MessageReceived>(HandleReceived);
        }

        private void StartPing()
        {
            _logger.Info("Starting PingPong test");
            _ably.SendMessage(_clientName, "Test", new { Name = "PingPong", Action = "Start", Channel = _clientName + "-pp"});
        }

        private void StopPing(string reason)
        {
            _ably.SendMessage(_clientName, "Test", new { Name = "PingPong", Action = "Stop", Reason = reason, Channel = _clientName + "-pp"});
        }

        private void SendNextPingMessage()
        {
            _pingCounter++;
            _ably.SendMessage(_clientName + "-pp", "PingPong", new PingPongPayload() { Counter = _pingCounter });
        }

        private void HandleReceived(MessageReceived obj)
        {
            _logger.Info("Received: " + obj.ToString());
            if (obj.Message.Name == "PingPong")
            {
                _pingPongSchedule.Cancel();

                var data = obj.Message.Data;
                if (data is JObject jObject)
                {
                    var payload = jObject.ToObject<PingPongPayload>();
                    var expected = _pingCounter + 1;
                    if (_pingCounter + 1 != payload.Counter)
                    {
                        _logger.Error("Invalid counter received. Expected: {expected}. Received: {received}", _pingCounter + 1, payload.Counter);
                        StopPing($"Invalid counter received '{payload.Counter}' but expected '{expected}'");
                    }
                    else
                    {
                        _logger.Info("Correct pong counter received. Counter: {expected}", payload.Counter);
                        SendNextPingMessage();
                        _pingPongSchedule = Context.System.Scheduler.ScheduleTellOnceCancelable(TimeSpan.FromMinutes(5), Self,
                            new PingPongTimeOut(), ActorRefs.NoSender);
                    }
                }

                StopPing("Invalid data received. Data: " + data?.ToString());
            }
            else if (obj.Message.Name == "Start")
            {
                SendNextPingMessage();
            }
        }

        private void PingPongTest()
        {
        }
    }
}