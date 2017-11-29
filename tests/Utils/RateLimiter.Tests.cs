using KrakenCore.Utils;
using System;
using System.Threading.Tasks;
using Xunit;

namespace KrakenCore.Tests.Utils
{
    public class RateLimiterTests
    {
        private readonly MockStopwatch _stopwatch = new MockStopwatch();
        private readonly RateLimiter _rateLimiter;

        private long _delay;

        public RateLimiterTests()
        {
            _rateLimiter = new RateLimiter(2, TimeSpan.FromTicks(1), _stopwatch, time =>
            {
                _delay += time.Ticks;
                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task WaitAccess_NotLimited()
        {
            await _rateLimiter.WaitAccess(2);
            Assert.Equal(0, _delay);
        }

        [Fact]
        public async Task WaitAccess_Limited()
        {
            await _rateLimiter.WaitAccess(1);
            await _rateLimiter.WaitAccess(2);
            Assert.Equal(1, _delay);
        }

        [Fact]
        public async Task WaitAccess_EnoughTimePassed_NotLimited()
        {
            await _rateLimiter.WaitAccess(2);
            _stopwatch.Set(1);
            await _rateLimiter.WaitAccess(1);
            Assert.Equal(0, _delay);
        }

        [Fact]
        public async Task WaitAccess_DoubleRateHalfTimePassed_Limited()
        {
            await _rateLimiter.WaitAccess(2);
            _stopwatch.Set(1);
            await _rateLimiter.WaitAccess(2);
            Assert.Equal(1, _delay);
        }

        [Fact]
        public async Task WaitAccess_Complex()
        {
            await _rateLimiter.WaitAccess(1);
            Assert.Equal(0, _delay);

            await _rateLimiter.WaitAccess(2);
            Assert.Equal(1, _delay);
            _stopwatch.Set(1);

            await _rateLimiter.WaitAccess(2);
            Assert.Equal(3, _delay);
            _stopwatch.Set(3);

            await _rateLimiter.WaitAccess(2);
            Assert.Equal(5, _delay);
            _stopwatch.Set(7);

            await _rateLimiter.WaitAccess(2);
            Assert.Equal(5, _delay);
        }
    }

    internal class MockStopwatch : IStopwatch
    {
        public TimeSpan Elapsed { get; private set; }

        public void Restart() => Set(0);

        public void Set(long ticks) => Elapsed = TimeSpan.FromTicks(ticks);
    }
}
