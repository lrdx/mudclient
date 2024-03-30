using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Adan.Client.Plugins.Timers.Model
{

    public class TimerWrapper : IDisposable
    {
        private Timer _timer;
        private bool _disposed = false;

        public delegate void Callback(object obj);

        private readonly Callback _callback;

        public TimerWrapper(Callback callback, object obj, Tuple<TimeSpan, TimeSpan> span)
        {
            _callback = callback;
            _timer = new Timer(On_Timer_Elapsed, obj, Timeout.Infinite, Timeout.Infinite);
            Interval = span;
            Reset();
        }

        ~TimerWrapper()
        {
            Dispose(false);
        }

        public long NextTime
        {
            get;
            private set;
        }

        public Tuple<TimeSpan, TimeSpan> Interval
        {
            get;
            private set;
        }

        public void Reset()
        {
            NextTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            ChangeTimer();
        }

        private void On_Timer_Elapsed(object obj)
        {
            if (!_disposed)
            {
                ChangeTimer();
                _callback(obj);
            }
        }

        private void ChangeTimer()
        {
            if (Interval.Item1 != Interval.Item2)
            {
                // We need to shift time for timer inaccurate
                NextTime += (long)(Interval.Item1.TotalMilliseconds + (new Random().NextDouble()) * (Interval.Item2.TotalMilliseconds - Interval.Item1.TotalMilliseconds));
            }
            else
            {
                NextTime += (long)Interval.Item1.TotalMilliseconds;
            }
            var shift = NextTime - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _timer.Change(shift < 0 ? TimeSpan.Zero : TimeSpan.FromMilliseconds(shift), Timeout.InfiniteTimeSpan);
        }

        private void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                _timer.Dispose();
                _timer = null;
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
