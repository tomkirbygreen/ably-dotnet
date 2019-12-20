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

        public override string ToString()
        {
            return "Counter: " + Counter;
        }
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
        private bool hasValidatedLastCounter = true;
        private bool inProgress = false;

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
            Receive<SendNextPingMessage>(_ =>
            {
                if (inProgress == false) return;

                if (hasValidatedLastCounter == false)
                {
                    SendNextPingMessage(TimeSpan.FromSeconds(10));
                }
                else
                {
                    hasValidatedLastCounter = false;
                    _pingCounter++;
                    AblyProxy.Tell(new SendMessage("PingPong", new PingPongPayload()
                        {
                            Counter = _pingCounter
                        }, _clientName + "-pp"));
                }
            });
        }

        private void StartPing()
        {
            inProgress = true;
            _logger.Info("Starting PingPong test");
            AblyProxy.Tell(new SubscribeToChannel(_clientName + "-pp"));
            AblyProxy.Tell(new SendMessage("Test",
                new {Name = "PingPong", Action = "Start", Channel = _clientName + "-pp"}));
        }

        private void StopPing(string reason)
        {
            inProgress = false;
            AblyProxy.Tell(new UnSubscribeFromChannel(_clientName + "-pp"));
            AblyProxy.Tell(new SendMessage("Test",
                new {Name = "PingPong", Action = "Stop", Reason = reason, Channel = _clientName + "-pp"}));
        }

        private void SendNextPingMessage(TimeSpan delay)
        {
            _logger.Info("Sending next message in: " + delay.TotalSeconds);
            Context.System.Scheduler.ScheduleTellOnceCancelable(
                delay, Self,
                new SendNextPingMessage(), ActorRefs.NoSender);
        }

        private void HandleReceived(MessageReceived obj)
        {
            try
            {
                _logger.Info("Received: " + obj.Message.ToString());
                if (obj.Message.Name == "PingPong")
                {
                    _pingPongSchedule?.Cancel();

                    var data = obj.Message.Data;
                    if (data is JObject jObject)
                    {
                        var payload = jObject.ToObject<PingPongPayload>();
                        var expected = _pingCounter + 1;
                        if (expected != payload.Counter)
                        {
                            _logger.Error("Invalid counter received. Expected: {expected}. Received: {received}",
                                _pingCounter + 1, payload.Counter);
                            StopPing($"Invalid counter received '{payload.Counter}' but expected '{expected}'");
                        }
                        else
                        {
                            hasValidatedLastCounter = true;
                            _logger.Info("Correct pong counter received. Counter: {expected}", payload.Counter);
                            SendNextPingMessage(TimeSpan.FromSeconds(10));
                            _pingPongSchedule = Context.System.Scheduler.ScheduleTellOnceCancelable(
                                TimeSpan.FromMinutes(5), Self,
                                new PingPongTimeOut(), ActorRefs.NoSender);
                        }
                    }
                    else
                    {
                        StopPing("Invalid data received. Data: " + data?.ToString());
                    }
                }
                else if (obj.Message.Name == "Start")
                {
                    SendNextPingMessage(TimeSpan.Zero);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error in receive message");
            }
        }
    }
}