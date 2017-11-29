using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("KrakenCore.Tests")]

namespace KrakenCore.Utils
{
    // Not thread safe.
    internal class RateLimiter
    {
        private readonly int _limit;
        private readonly TimeSpan _decreaseTime;

        private readonly Func<DateTime> _timestamp;
        private readonly Func<TimeSpan, Task> _delay;

        private readonly Queue<DateTime> _processingTimestamps;
        private DateTime _barrier = DateTime.MinValue;

        public RateLimiter(int limit, TimeSpan decreaseTime, Func<DateTime> timestamp, Func<TimeSpan, Task> delay)
        {
            _limit = limit;
            _decreaseTime = decreaseTime;
            _timestamp = timestamp;
            _delay = delay;
            _processingTimestamps = new Queue<DateTime>(limit);
        }

        public async Task WaitAccess(int counterIncrease)
        {
            //DateTime timestamp = _timestamp();

            // Process history.
            while (_processingTimestamps.Count > 0)
            {
                DateTime first = _processingTimestamps.Peek();
                if (first - _barrier >= _decreaseTime)
                {
                    _processingTimestamps.Dequeue();
                    _barrier = first + _decreaseTime;
                }
            }

            int diff = counterIncrease - (_limit - _processingTimestamps.Count);

            if (_processingTimestamps.Count < _limit)
            {
                _processingTimestamps.Enqueue(_timestamp());
            }
            else
            {
                await 
            }

            while (elapsed >= _decreaseTime)
            {
                _callCounter = Math.Max(_callCounter - 1, 0);
                elapsed -= _decreaseTime;
            }

            _callCounter += counterIncrease;
            _stopwatch.Restart();

            if (_callCounter > _limit)
            {
                var toWait = TimeSpan.FromTicks(
                    (_callCounter - _limit) * _decreaseTime.Ticks
                ) - elapsed;
                await _delay(toWait);
            }
        }
    }
}
