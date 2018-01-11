using KrakenCore.Tests.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace KrakenCore.Tests
{
    public abstract class KrakenClientTests : IClassFixture<KrakenFixture>
    {
        protected const string ApiKey = "<INSERT_API_KEY>";
        protected const string PrivateKey = "<INSERT_PRIVATE_KEY>";

        protected const string DefaultBase = "XETH";
        protected const string DefaultBaseAlternate = "ETH";
        protected const string DefaultQuote = "ZEUR";
        protected const string DefaultPair = DefaultBase + DefaultQuote;
        protected const string DefaultPairAlternate = "ETHEUR";

        public KrakenClientTests(ITestOutputHelper output, KrakenFixture fixture)
        {
            if (ApiKey.Length != KrakenClient.DummyApiKey.Length ||
                PrivateKey.Length != KrakenClient.DummyPrivateKey.Length)
            {
                throw new InvalidOperationException(
$@"Please configure {nameof(ApiKey)} and {nameof(PrivateKey)} in {nameof(KrakenFixture)}!
Use {nameof(KrakenClient)}.{nameof(KrakenClient.DummyApiKey)} and {nameof(KrakenClient)}.{nameof(KrakenClient.DummyPrivateKey)} to test only public API.");
            }

            Client = new KrakenClient(ApiKey, PrivateKey)
            {
                // If the API key has two factor password enabled, set the line below to return it.
                //GetTwoFactorPassword = () => Task.FromResult("<INSERT_PASSWORD>")

                ErrorsAsExceptions = true,
                WarningsAsExceptions = true,

                // Log request and response for each test.
                InterceptRequest = async req =>
                {
                    output.WriteLine("REQUEST");
                    output.WriteLine(req.ToString());
                    string content = await req.HttpRequest.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(content)) output.WriteLine(content);

                    // Wait if we have hit the API rate limit.
                    RateLimiter limiter = req.HttpRequest.RequestUri.Query.Contains("/private/")
                        ? fixture.PrivateApiRateLimiter
                        : fixture.PublicApiRateLimiter;

                    await limiter.WaitAccess(req.ApiCallCost);
                },
                InterceptResponse = async res =>
                {
                    output.WriteLine("");
                    output.WriteLine("RESPONSE");
                    output.WriteLine(res.ToString());
                    string content = await res.HttpResponse.Content.ReadAsStringAsync();
                    output.WriteLine(JToken.Parse(content).ToString(Formatting.Indented));
                }
            };
        }

        protected KrakenClient Client { get; }

        [DebuggerStepThrough]
        protected void AssertNotDefault<T>(T value) => Assert.NotEqual(default(T), value);
    }

    // Share the fixture between tests in order to respect API rate limits.
    public class KrakenFixture
    {
        protected static readonly RateLimit PrivateApiRateLimit = RateLimit.Tier2;

        public KrakenFixture()
        {
            // Public API rate limiter is not dependent on account tier.
            PublicApiRateLimiter = new RateLimiter(RateLimit.Tier4);
            // Account tier only applies to private limiter!
            PrivateApiRateLimiter = new RateLimiter(PrivateApiRateLimit);
        }

        public RateLimiter PublicApiRateLimiter { get; }
        public RateLimiter PrivateApiRateLimiter { get; }
    }
}
