using System;
using System.Collections.Generic;
using System.Reactive;
using Akka.Actor;
using Akka.Event;
using IO.Ably;
using TestObserver.Messages;

namespace TestObserver.Actors
{
    public class AblyBridgeActor : ReceiveActor
    {
        private IObservable<Message> _messageSubscriber;
        private IObservable<PresenceMessage> _presenceSubscriber;
        private Dictionary<string, IDisposable> _subscriptions = new Dictionary<string, IDisposable>();

        public AblyBridgeActor(AblyService service, IActorRef clientActor, string channelName, bool includePresence = false)
        {
            var logger = Context.GetLogger();
            _messageSubscriber = service.MessageObservable(channelName);

            var messageObserver = Observer.Create<Message>(m =>
                {
                    logger.Info($"Recieved message with name: {m.Name}");
                    clientActor.Tell(new MessageReceived(m));
                }
            );

            _messageSubscriber.Subscribe(messageObserver);

            if (includePresence)
            {
                var presenceObserver =
                    Observer.Create<PresenceMessage>(message =>
                    {
                        logger.Info($"Received presence message with clientId: {message.ClientId}");
                        clientActor.Tell(new PresenceReceived(message));
                    });

                _presenceSubscriber = service.PresenceObservable(channelName);

                _presenceSubscriber.Subscribe(presenceObserver);
            }

            Receive<SendMessage>(data => service.SendMessage(channelName, data.Name, data.Data));
            Receive<SubscribeToChannel>(data =>
                {
                    var subscription = service.MessageObservable(data.ChannelName).Subscribe(messageObserver);
                    _subscriptions.Add(data.ChannelName, subscription);
                });

            Receive<UnSubscribeFromChannel>(data =>
            {
                if (_subscriptions.TryGetValue(data.ChannelName, out IDisposable subscription))
                {
                    subscription?.Dispose();
                    service.Ably.Channels.Release(data.ChannelName);
                }
            });
        }
    }
}