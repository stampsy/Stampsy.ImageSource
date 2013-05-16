using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Stampsy.ImageSource
{
    public static class Extensions
    {
        public static Task<TRequest> Fetch<TRequest> (this IDestination<TRequest> destination, Uri url)
            where TRequest : Request
        {
            return ImageSource.Fetch (url, destination);
        }

        internal static IObservable<T> SurroundWith<T> (this IObservable<T> a, IObservable<T> b)
        {
            return b.Concat (a).Concat (b);
        }

        internal static void RouteExceptions<T> (this Task task, IObserver<T> observer)
        {
            task.ContinueWith (t => {
                observer.OnError (t.Exception.Flatten ());
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        internal static void RouteExceptions<T> (this Task task, TaskCompletionSource<T> tcs)
        {
            task.ContinueWith (t => {
                tcs.TrySetException (t.Exception.Flatten ());
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

    }
}

