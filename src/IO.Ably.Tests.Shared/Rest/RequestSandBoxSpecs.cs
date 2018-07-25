using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    [Trait("requires", "sandbox")]
    [Trait("spec", "RSC19")]
    [Trait("spec", "RSC19a")]
    [Trait("spec", "RSC19b")]
    [Trait("spec", "RSC19c")]
    [Trait("spec", "RSC19d")]
    [Trait("spec", "RSC19e")]
    public class RequestSandBoxSpecs : SandboxSpecs, IAsyncLifetime
    {
        private string _channelName;
        private string _channelAltName;
        private string _channelNamePrefix;
        private string _channelPath;
        private string _channelsPath;
        private string _channelMessagesPath;

        private AblyRequest _lastRequest = null;

        public RequestSandBoxSpecs(ITestOutputHelper output)
            : base(new AblySandboxFixture(), output)
        {
            _channelNamePrefix = "rest_request".AddRandomSuffix();
            _channelName = $"{_channelNamePrefix}_channel";
            _channelAltName = $"{_channelNamePrefix}_alt_channel";
            _channelsPath = "/channels";
            _channelPath = $"{_channelsPath}/{_channelName}";
            _channelMessagesPath = $"{_channelPath}/messages";
        }

        public async Task InitializeAsync()
        {
            var client = await GetRestClient(Protocol.Json);
            var channel = client.Channels.Get(_channelName);
            for (int i = 0; i < 4; i++)
            {
                channel.Publish("Test event", "Test data " + i);
            }

            var altChannel = client.Channels.Get(_channelAltName);
            for (int i = 0; i < 4; i++)
            {
                altChannel.Publish("Test event", "Test alt data " + i);
            }
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Trait("spec", "RSC19")]
        [Trait("spec", "RSC19a")]
        [Trait("spec", "RSC19b")]
        [Trait("spec", "RSC19c")]
        [Trait("spec", "RSC19d")]
        [Theory]
        [ProtocolData]
        public async Task Request_SimpleGet(Protocol protocol)
        {
            var client = TrackLastRequest(await GetRestClient(protocol));

            var testParams = new Dictionary<string, string> { { "testParams", "testParamValue" } };
            var testHeaders = new Dictionary<string, string> { { "X-Test-Header", "testHeaderValue" } };

            var paginatedResponse = await client.Request(HttpMethod.Get, _channelPath, testParams, null, testHeaders);

            _lastRequest.Headers.Should().ContainKey("Authorization");
            _lastRequest.Headers.Should().ContainKey("X-Test-Header");
            _lastRequest.Headers["X-Test-Header"].Should().Be("testHeaderValue");
            paginatedResponse.Should().NotBeNull();
            paginatedResponse.StatusCode.Should().Be(HttpStatusCode.OK); // 200
            paginatedResponse.Success.Should().BeTrue();
            paginatedResponse.ErrorCode.Should().Be(0);
            paginatedResponse.ErrorMessage.Should().BeNull();
            paginatedResponse.Items.Should().HaveCount(1);
            paginatedResponse.Items.First().Should().BeOfType<JObject>();
            paginatedResponse.Response.ContentType.Should().Be("application/json");
            var channelDetails = paginatedResponse.Items.First() as JObject; // cast from JToken
            channelDetails["id"].ToString().Should().BeEquivalentTo(_channelName);
        }

        [Trait("spec", "RSC19")]
        [Trait("spec", "RSC19a")]
        [Trait("spec", "RSC19b")]
        [Trait("spec", "RSC19c")]
        [Trait("spec", "RSC19d")]
        [Theory]
        [ProtocolData]
        public async Task Request_Paginated(Protocol protocol)
        {
            var client = TrackLastRequest(await GetRestClient(protocol));

            var testParams = new Dictionary<string, string> { { "prefix", _channelNamePrefix } };

            var paginatedResponse = await client.Request(HttpMethod.Get, _channelsPath, testParams, null, null);

            _lastRequest.Headers.Should().ContainKey("Authorization");
            paginatedResponse.Should().NotBeNull();
            paginatedResponse.StatusCode.Should().Be(HttpStatusCode.OK); // 200
            paginatedResponse.Success.Should().BeTrue();
            paginatedResponse.ErrorCode.Should().Be(0);
            paginatedResponse.Response.ContentType.Should().Be("application/json");
            var items = paginatedResponse.Items;
            items.Should().HaveCount(2);
            foreach (var item in items)
            {
                (item as JObject)["id"].ToString().Should().StartWith(_channelNamePrefix);
            }
        }

        [Trait("spec", "RSC19")]
        [Trait("spec", "RSC19a")]
        [Trait("spec", "RSC19b")]
        [Trait("spec", "RSC19c")]
        [Trait("spec", "RSC19d")]
        [Theory]
        [ProtocolData]
        public async Task Request_PaginatedWithLimit(Protocol protocol)
        {
            var client = TrackLastRequest(await GetRestClient(protocol));

            var testParams = new Dictionary<string, string> { { "prefix", _channelNamePrefix }, { "limit", "1" } };
            var testHeader = new Dictionary<string, string> { { "X-test-header", "test-header" } };

            var paginatedResponse = await client.Request(HttpMethod.Get, _channelsPath, testParams, null, testHeader);

            _lastRequest.Headers.Should().ContainKey("Authorization");
            paginatedResponse.Should().NotBeNull();
            paginatedResponse.StatusCode.Should().Be(HttpStatusCode.OK); // 200
            paginatedResponse.Success.Should().BeTrue();
            paginatedResponse.ErrorCode.Should().Be(0);
            paginatedResponse.Response.ContentType.Should().Be("application/json");
            var items = paginatedResponse.Items;
            items.Should().HaveCount(1);
            foreach (var item in items)
            {
                (item as JObject)["id"].ToString().Should().StartWith(_channelNamePrefix);
            }

            var page2 = await paginatedResponse.NextAsync();
            page2.Items.Should().HaveCount(1);
            page2.StatusCode.Should().Be(HttpStatusCode.OK); // 200
            page2.Success.Should().BeTrue();
            page2.ErrorCode.Should().Be(0);
            page2.Response.ContentType.Should().Be("application/json");
        }

        [Trait("spec", "RSC19")]
        [Trait("spec", "RSC19a")]
        [Trait("spec", "RSC19b")]
        [Trait("spec", "RSC19c")]
        [Trait("spec", "RSC19d")]
        [Theory]
        [ProtocolData]
        public async Task Request_Post(Protocol protocol)
        {
            var client = TrackLastRequest(await GetRestClient(protocol));

            var body1 = JToken.Parse("{ \"name\": \"rsc19test\", \"data\": \"from-json-string\" }");
            var body2 = JToken.FromObject(new Message("rsc19test", "from-message"));

            var paginatedResponse = await client.Request(HttpMethod.Post, _channelMessagesPath, null, body1, null);

            _lastRequest.Headers.Should().ContainKey("Authorization");
            paginatedResponse.Should().NotBeNull();
            paginatedResponse.StatusCode.Should().Be(HttpStatusCode.Created); // 201
            paginatedResponse.Success.Should().BeTrue();
            paginatedResponse.ErrorCode.Should().Be(0);
            paginatedResponse.ErrorMessage.Should().BeNull();
            paginatedResponse.Response.ContentType.Should().Be("application/json");

            await client.Request(HttpMethod.Post, _channelMessagesPath, null, body2, null);

            var ch = client.Channels.Get(_channelName);
            var body3 = new Message("rsc19test", "from-publish");
            ch.Publish(body3);

            await Task.Delay(1000);

            var paginatedResult = client.Channels.Get(_channelName).History(new PaginatedRequestParams { Limit = 3 });
            paginatedResult.Should().NotBeNull();
            paginatedResult.Items.Should().HaveCount(3);
            paginatedResult.Items[2].Data.ShouldBeEquivalentTo("from-json-string");
            paginatedResult.Items[1].Data.ShouldBeEquivalentTo("from-message");
            paginatedResult.Items[0].Data.ShouldBeEquivalentTo("from-publish");
        }

        [Trait("spec", "RSC19e")]
        [Theory]
        [ProtocolData]
        public async Task RequestFails_NotFound(Protocol protocol)
        {
            var client = TrackLastRequest(await GetRestClient(protocol));
            try
            {
                var paginatedResponse = await client.Request(HttpMethod.Post, "/does-not-exist");
                paginatedResponse.ErrorMessage.Should().NotBeNullOrEmpty();
            }
            catch (AblyException e)
            {
                e.ErrorInfo.Code.Should().Be(40400);
                e.ErrorInfo.Message.Should().NotBeNullOrEmpty();
            }
        }

        [Trait("spec", "RSC19e")]
        [Theory]
        [ProtocolData]
        public async Task RequestFails_ErrorConnecting(Protocol protocol)
        {
            var client = TrackLastRequest(await GetRestClient(protocol, options =>
            {
                options.Environment = "fake.environment";
            }));
            try
            {
                var paginatedResponse = await client.Request(HttpMethod.Post, "/");
                paginatedResponse.ErrorMessage.Should().NotBeNullOrEmpty();
            }
            catch (AblyException e)
            {
                e.ErrorInfo.Code.Should().Be(50000);
                e.ErrorInfo.Message.Should().NotBeNullOrEmpty();
            }
        }

        private AblyRest TrackLastRequest(AblyRest client)
        {
            var exec = client.ExecuteHttpRequest;
            client.ExecuteHttpRequest = request =>
            {
                _lastRequest = request;
                return exec(request);
            };
            return client;
        }
    }
}
