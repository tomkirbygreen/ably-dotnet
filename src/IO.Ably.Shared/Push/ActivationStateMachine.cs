﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using IO.Ably.Encryption;

namespace IO.Ably.Push
{
    internal partial class ActivationStateMachine
    {
        private bool _handlingEvent = false;
        private object _handleEventsLock = new object();
        private Queue<Event> _pendingEvents = new Queue<Event>();

        private readonly AblyRest _restClient;
        private readonly IMobileDevice _mobileDevice;
        private readonly ILogger _logger;

        public string ClientId { get; }

        public State CurrentState { get; private set; }

        private Queue<Event> PendingEvents { get; set; } = new Queue<Event>();

        internal ActivationStateMachine(AblyRest restClient, IMobileDevice mobileDevice, ILogger logger)
        {
            _restClient = restClient;
            ClientId = _restClient.Auth.ClientId;
            _mobileDevice = mobileDevice;
            _logger = logger;
        }

        public LocalDevice LocalDevice { get; set; } = new LocalDevice();

        private void CallDeactivatedCallback(ErrorInfo reason)
        {
            SendErrorIntent("PUSH_DEACTIVATE", reason); // TODO: Put intent names in consts
        }

        private async Task ValidateRegistration()
        {
            // TODO: See if I need to get Ably from some kind of context
            var presentClientId = _restClient.Auth.ClientId;
            if (presentClientId.IsNotEmpty() && presentClientId.EqualsTo(LocalDevice.ClientId) == false)
            {
                var error = new ErrorInfo(
                    "Activation failed: present clientId is not compatible with existing device registration",
                    ErrorCodes.ActivationFailedClientIdMismatch,
                    HttpStatusCode.BadRequest);

                // When calling Handle event we don't want to await the operation.
                // I'm sure there is a better way to do it.
                _ = HandleEvent(new SyncRegistrationFailed(error));
            }

            try
            {
                await _restClient.Push.Admin.DeviceRegistrations.SaveAsync(LocalDevice);

                // TODO: SetClientId from returned devices in case it has been set differently
                _ = HandleEvent(new RegistrationSynced());
            }
            catch (AblyException e)
            {
                // TODO: Log
                _ = HandleEvent(new SyncRegistrationFailed(e.ErrorInfo));
            }
        }

        public async Task HandleEvent(Event @event)
        {
            lock (_handleEventsLock)
            {
                if (_handlingEvent)
                {
                    // TODO: Log and queue
                    _pendingEvents.Enqueue(@event);
                    return;
                }

                _handlingEvent = true;
            }

            try
            {
                // Log.d(TAG, String.format("handling event %s from %s", event.getClass().getSimpleName(), current.getClass().getSimpleName()));
                State maybeNext = await CurrentState.Transition(@event);
                if (maybeNext == null)
                {
                    _pendingEvents.Enqueue(@event);
                    PersistState();
                    return;
                }

                CurrentState = maybeNext;

                while (true)
                {
                    var pending = _pendingEvents.Peek();
                    if (pending == null)
                    {
                        break;
                    }

                    // TODO: Log
                    var nextState = await CurrentState.Transition(pending);
                    if (nextState == null)
                    {
                        break;
                    }

                    _ = _pendingEvents.Dequeue(); // Remove the message from the queue
                    CurrentState = nextState;
                }

                PersistState();
            }
            finally
            {
                _handlingEvent = false;
            }
        }

        private void PersistState()
        {
            if (CurrentState != null && CurrentState.Persist)
            {
                _mobileDevice.SetPreference(PersistKeys.StateMachine.CURRENT_STATE, CurrentState.GetType().Name, PersistKeys.StateMachine.SharedName);
            }

            var events = _pendingEvents.ToList();

            _mobileDevice.SetPreference(PersistKeys.StateMachine.PENDING_EVENTS_LENGTH, events.Count.ToString(), PersistKeys.StateMachine.SharedName);

            // Saves pending events as a pipe separated list.
            _mobileDevice.SetPreference(PersistKeys.StateMachine.PENDING_EVENTS, events.Select(x => x.GetType().Name).JoinStrings("|"), PersistKeys.StateMachine.SharedName);
        }

        private void AddToEventQueue(Event @event)
        {
            PendingEvents.Enqueue(@event);
        }

        private void GetRegistrationToken()
        {
            _mobileDevice.RequestRegistrationToken(result =>
            {
                if (result.IsSuccess)
                {
                    var token = result.Value;
                    var previous = LocalDevice.RegistrationToken;
                    if (previous != null)
                    {
                        if (previous.Token.EqualsTo(token))
                        {
                            return;
                        }
                    }

                    // TODO: Log
                    var registrationToken = new RegistrationToken(RegistrationToken.Fcm, token);
                    LocalDevice.RegistrationToken = registrationToken;

                    // TODO: What happens if this errors
                    PersistRegistrationToken(registrationToken);

                    _ = HandleEvent(new GotPushDeviceDetails());
                }
                else
                {
                    // Todo: Log
                    _ = HandleEvent(new GettingPushDeviceDetailsFailed(result.Error));
                }
            });
        }

        private void PersistRegistrationToken(RegistrationToken token)
        {
            _mobileDevice.SetPreference(PersistKeys.Device.TOKEN_TYPE, token.Type, PersistKeys.Device.SharedName);
            _mobileDevice.SetPreference(PersistKeys.Device.TOKEN, token.Token, PersistKeys.Device.SharedName);
        }

        private void PersistLocalDevice(LocalDevice localDevice)
        {
            _mobileDevice.SetPreference(PersistKeys.Device.DEVICE_ID, localDevice.Id, PersistKeys.Device.SharedName);
            _mobileDevice.SetPreference(PersistKeys.Device.CLIENT_ID, localDevice.ClientId, PersistKeys.Device.SharedName);
            _mobileDevice.SetPreference(PersistKeys.Device.DEVICE_SECRET, localDevice.DeviceSecret, PersistKeys.Device.SharedName);
        }

        private void SendErrorIntent(string name, ErrorInfo errorInfo)
        {
            var properties = new Dictionary<string, object>();
            properties.Add("hasError", errorInfo != null);
            if (errorInfo != null)
            {
                properties.Add("error.message", errorInfo.Message);
                properties.Add("error.statusCode", (int?)errorInfo.StatusCode);
                properties.Add("error.code", errorInfo.Code);
            }

            _mobileDevice.SendIntent(name, properties);
        }

        private void SendIntent(string name)
        {
            _mobileDevice.SendIntent(name, new Dictionary<string, object>());
        }

        private void SetDeviceIdentityToken(string deviceIdentityToken)
        {
            LocalDevice.DeviceIdentityToken = deviceIdentityToken;
            _mobileDevice.SetPreference(PersistKeys.Device.DEVICE_TOKEN, deviceIdentityToken, PersistKeys.Device.SharedName);
        }

        private void CallActivatedCallback(ErrorInfo reason)
        {
            SendErrorIntent("PUSH_ACTIVATE", reason);
        }

        /// <summary>
        /// De-registers the current device.
        /// </summary>
        private async Task Deregister()
        {
            // Make sure the call is not completed synchronously
            Task.Yield();

            try
            {
                await _restClient.Push.Admin.DeviceRegistrations.RemoveAsync(LocalDevice.Id);
                _ = HandleEvent(new Deregistered());
            }
            catch (AblyException e)
            {
                // Log
                _ = HandleEvent(new DeregistrationFailed(e.ErrorInfo));
            }
        }

        private async Task UpdateRegistration(DeviceDetails details)
        {
            try
            {
                await _restClient.Push.Admin.PatchDeviceRecipient(details);
                _ = HandleEvent(new RegistrationSynced());
            }
            catch (AblyException ex)
            {
                _ = HandleEvent(new SyncRegistrationFailed(ex.ErrorInfo));
            }
        }

        private LocalDevice LoadPersistedLocalDevice()
        {
            string GetDeviceSetting(string key) => _mobileDevice.GetPreference(key, PersistKeys.Device.SharedName);

            var localDevice = new LocalDevice();
            string id = GetDeviceSetting(PersistKeys.Device.DEVICE_ID);

            localDevice.Id = id;
            if (id.IsNotEmpty())
            {
                // Log.v(TAG, "loadPersisted(): existing deviceId found; id: " + id);
                localDevice.DeviceSecret = GetDeviceSetting(PersistKeys.Device.DEVICE_SECRET);
            }
            else
            {
                // Log.v(TAG, "loadPersisted(): existing deviceId not found.");
            }

            localDevice.ClientId = GetDeviceSetting(PersistKeys.Device.CLIENT_ID);
            localDevice.DeviceIdentityToken = GetDeviceSetting(PersistKeys.Device.DEVICE_TOKEN);

            var tokenType = GetDeviceSetting(PersistKeys.Device.TOKEN_TYPE);

            // Log.d(TAG, "loadPersisted(): token type = " + type);
            if (tokenType.IsNotEmpty())
            {
                string tokenString = GetDeviceSetting(PersistKeys.Device.TOKEN);

                // Log.d(TAG, "loadPersisted(): token string = " + tokenString);
                if (tokenString.IsNotEmpty())
                {
                    var token = new RegistrationToken(tokenType, tokenString);
                    localDevice.RegistrationToken = token;
                }
            }

            return localDevice;
        }

        private LocalDevice EnsureLocalDeviceIsLoaded()
        {
            if (LocalDevice.IsCreated == false)
            {
                LocalDevice = LoadPersistedLocalDevice();
            }

            return LocalDevice;
        }
    }
}
