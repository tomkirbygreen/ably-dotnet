using Ably.Auth;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace Ably
{
    public static class Config
    {
        public static ILogger AblyLogger = Logger.Current;
        internal static string DefaultHost = "ably.io";
    }

    public interface IAuthCommands
    {
        Token RequestToken(AuthOptions options);
        Token Authorise(AuthOptions options, TokenRequest request);
        Token CreateTokenRequest(AuthOptions options, TokenRequest request);
    }

    public class Rest : IAuthCommands
    {
        private AblyHttpClient _client;
        private AblyOptions _options;
        private ILogger Logger = Config.AblyLogger;
        private Token _token;

        public Rest()
        {
            var key = GetConnectionString();
            if (string.IsNullOrEmpty(key))
            {
                new ConfigurationMissingException("A connection strig with key 'Ably' doesn't exist in the application configuration").Throw();
            }
            //Parse it when I know how things work
        }

        public Rest(string apiKey) : this(new AblyOptions { Key = apiKey})
        {

        }

        public Rest(Action<AblyOptions> init)
        {
            _options = new AblyOptions();

            init(_options);

            InitialiseAbly();
        }

        public Rest(AblyOptions ablyOptions)
        {
            _options = ablyOptions;
            InitialiseAbly();
        }

        internal virtual string GetConnectionString()
        {
            var connString = ConfigurationManager.ConnectionStrings["Ably"];
            if (connString == null)
            {
                return string.Empty;
            }

            return connString.ConnectionString;
        }

        private void InitialiseAbly()
        {
            if(_options == null)
            {
                Logger.Error("No options provider to Ably rest");
                new ArgumentNullException("Options").Throw();
            }

            if(_options.Key.IsNotEmpty())
            {
                var key = ApiKey.Parse(_options.Key);
                _options.AppId = key.AppId;
                _options.KeyId = key.KeyId;
                _options.KeyValue = key.KeyValue;
            }

            if(_options.AppId.IsEmpty())
            {
                Logger.Error("Cannot initialise Ably without AppId");
                new ArgumentException("Cannot initialise Ably without an AppId").Throw();
            }

            string host = _options.Host.IsNotEmpty() ? _options.Host : Config.DefaultHost;
            _client = new AblyHttpClient(_options.AppId, host, _options.Port, _options.Encrypted);
        }

        public IAuthCommands Auth
        {
            get { return this; }
        }

        internal Func<AblyRequest, AblyResponse> ExecuteRequest = ExecuteRequestInternal;

        internal Func<DateTime> Now = () => DateTime.Now;
        
        private static AblyResponse ExecuteRequestInternal(AblyRequest request)
        {
            return null;
        }

        public string RequestToken(RequestTokenParams options)
        {
            //var request = new AblyRequest(String.Format("/apps/{0}/requestToken", _key.AppId));
            //request.PostParameters.Add("id", _key.KeyId);
            //TimeSpan expiresInterval = options.Ttl.HasValue ? options.Ttl.Value :  TimeSpan.FromHours(1);
            //string expiresUnixTime = Now().Add(expiresInterval).ToUnixTime().ToString();
            //request.PostParameters.Add("expires", expiresUnixTime);
            //if(string.IsNullOrWhiteSpace(options.Capability) == false )
            //    request.PostParameters.Add("capability", options.Capability);
            //if(string.IsNullOrWhiteSpace(options.ClientId) == false )
            //    request.PostParameters.Add("client_id", options.ClientId);

            //request.PostParameters.Add("timestamp", Now().ToUnixTime().ToString());
            //request.PostParameters.Add("nonce", Guid.NewGuid().ToString("N").ToLower());
            //request.PostParameters.Add("mac", CalculateMac(request.PostParameters, _key.KeyValue));
            //ExecuteRequest(request);
            return "";
        }

        private string CalculateMac(Dictionary<string, string> postParameters, string key)
        {
            var values = new[] 
            { 
                postParameters.Get("id"), 
                postParameters.Get("expires"),
                postParameters.Get("capability", ""), 
                postParameters.Get("client_id", ""), 
                postParameters.Get("timestamp"),
                postParameters.Get("nonce")
            };

            var signText = string.Join("\n", values) + "\n";

            return signText.ComputeHMacSha256(key);
        }

        Token IAuthCommands.RequestToken(AuthOptions options)
        {
            throw new NotImplementedException();
        }

        Token IAuthCommands.Authorise(AuthOptions options, TokenRequest request)
        {
            throw new NotImplementedException();
        }

        Token IAuthCommands.CreateTokenRequest(AuthOptions options, TokenRequest request)
        {
            throw new NotImplementedException();
        }
    }
}