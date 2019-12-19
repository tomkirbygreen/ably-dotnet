using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Event;
using IO.Ably;
using TestObserver.Messages;

namespace TestObserver.Actors
{
    public class TestControllerActor : ReceiveActor
    {
        private readonly Dictionary<string, IActorRef> _clients;
        private AblyService _ablyService;
        private IActorRef AblyProxy;
        private ILoggingAdapter logger;
        public TestControllerActor(string ablyKey)
        {
            _clients = new Dictionary<string, IActorRef>();
            _ablyService = new AblyService(ablyKey);

            AblyProxy = Context.ActorOf(
                Props.Create(() => new AblyBridgeActor(_ablyService, Self, "gameroom", true))
            );

            logger = Context.GetLogger();
            logger.Info("Initialising TestController");

            Receive<MessageReceived>(received => logger.Info(received.Message.Data.ToString()));

            Receive<PresenceReceived>(HandlePresence);
        }

        private void HandlePresence(PresenceReceived received)
        {
            logger.Info($"Presence message received. Client: {received.Message.ClientId}. Action: {received.Message.Action}");

            switch (received.Message.Action)
            {
                case PresenceAction.Present:
                case PresenceAction.Enter:
                    JoinTestClient(received.Message.ClientId);
                    break;
                case PresenceAction.Leave:
                    RemoveTestClient(received.Message.ClientId);
                    break;
            }
        }

        private void RemoveTestClient(string messageClientId)
        {
            logger.Info($"Removing client: {messageClientId}");

            if (_clients.ContainsKey(messageClientId))
            {
                _clients.Remove(messageClientId, out IActorRef clientActor);
                clientActor.GracefulStop(TimeSpan.FromSeconds(5));
            }
        }

        private void JoinTestClient(string clientName)
        {
            logger.Info($"Test client: {clientName} joined");
            var needsCreating = _clients.ContainsKey(clientName) == false;

            if (needsCreating)
            {
                IActorRef clientActor =
                    Context.ActorOf(
                        Props.Create(() => new ClientActor(clientName, _ablyService))
                    );

                _clients.Add(clientName, clientActor);
            }
        }
    }
}