using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Stampsy.ImageSource
{
    internal static class Extensions
    {
        public static IObservable<T> SurroundWith<T> (this IObservable<T> a, IObservable<T> b)
        {
            return b.Concat (a).Concat (b);
        }

        public static void RouteExceptions<T> (this Task task, IObserver<T> observer)
        {
            task.ContinueWith (t => {
                observer.OnError (t.Exception.Flatten ());
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public static void RouteExceptions<T> (this Task task, TaskCompletionSource<T> tcs)
        {
            task.ContinueWith (t => {
                tcs.TrySetException (t.Exception.Flatten ());
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

    }
}

