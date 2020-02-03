using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace CS_console_core
{
    static class Program
    {
        public static void Util_Tmp()
        {
            (int TimeMs, int ShotCount)[] msgData =
            {
                (1000, 0), (20, 1), (20, 2), (30, 3), (1100, 0), (20, -1), (20, -2), (30, -3), (20, -4)
            };

            var o = msgData
                .Select(v => new TimeInterval<int>(v.ShotCount, TimeSpan.FromMilliseconds(v.TimeMs)))
                .ObserveInTime()
                .TimeInterval();
            var dueTime = TimeSpan.FromMilliseconds(500);
            var oP = o
                .Publish();
            oP.Connect();
            var s = oP
                .StopOnTimeout(TimeSpan.FromMilliseconds(1e9), dueTime)
                .ToArray()
                .Repeat(3)
                .ToArray()
                .Wait();
        }

        public static void Util_ThrowOnTimeout()
        {
            (int TimeMs, int ShotCount)[] msgData =
            {
                (1000, 0), (20, 1), (20, 2), (30, 3), (1100, 0), (20, 1), (20, 2), (30, 3), (20, 4)
            };

            var o = msgData
                .Select(v => new TimeInterval<int>(v.ShotCount, TimeSpan.FromMilliseconds(v.TimeMs)))
                .ObserveInTime()
                .TimeInterval();
            var dueTime = TimeSpan.FromMilliseconds(500);

            var nExcepts = 0;

            var s0 = o
                .ThrowOnTimeout(dueTime, dueTime)
                .Catch((Exception e) =>
                {
                    ++nExcepts;
                    return Observable.Empty<TimeInterval<int>>();
                })
                .ToArray()
                .Wait();
            var p0 = (0, s0.Length);

            var s1 = o
                .ThrowOnTimeout(TimeSpan.FromMilliseconds(1e9), dueTime)
                .Catch((Exception e) =>
                {
                    ++nExcepts;
                    return Observable.Empty<TimeInterval<int>>();
                })
                .ToArray()
                .Wait();
            var p1 = (4, s1.Length);

            var s2 = o
                .ThrowOnTimeout(TimeSpan.FromMilliseconds(1e9), TimeSpan.FromMilliseconds(1e9))
                .Catch((Exception e) =>
                {
                    ++nExcepts;
                    return Observable.Empty<TimeInterval<int>>();
                })
                .ToArray()
                .Wait();
            var p2 = (msgData.Length, s2.Length);
        }

        [Pure]
        public static IObservable<T> ThrowOnTimeout<T>(this IObservable<T> o, TimeSpan firstTimeout,
            TimeSpan otherTimeouts, IScheduler scheduler = null)
        {
            if (scheduler == null)
                scheduler = Scheduler.Default;

            return o
                .Timeout(Observable.Timer(firstTimeout, scheduler), _ => Observable.Timer(otherTimeouts, scheduler));
        }


        [Pure]
        public static IObservable<T> ObserveInTime<T>(this IEnumerable<TimeInterval<T>> enumerable,
            IScheduler scheduler = null)
        {
            if (scheduler == null)
                scheduler = Scheduler.Default;
            var timeIntervals = enumerable as TimeInterval<T>[] ?? enumerable.ToArray();

            return Observable.Generate
            (
                0,
                i => i < timeIntervals.Length,
                i => i + 1,
                i => timeIntervals[i].Value,
                i => timeIntervals[i].Interval,
                scheduler
            );
        }

        [Pure]
        public static IObservable<T> StopOnTimeout<T>(this IObservable<T> o, TimeSpan firstTimeout,
            TimeSpan otherTimeouts, IScheduler scheduler = null)
        {
            if (scheduler == null)
                scheduler = Scheduler.Default;

            return o
                .Timeout(Observable.Timer(firstTimeout, scheduler), _ => Observable.Timer(otherTimeouts, scheduler),
                    Observable.Empty<T>());
        }




        static void Main(string[] args)
        {



            Util_ThrowOnTimeout();

            Console.WriteLine("Hello World!");
        }
    }
}
