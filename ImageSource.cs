using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Stampsy.ImageSource
{
    public class ImageSource
    {
        static readonly Dictionary<string, ISource> _sources = new Dictionary<string, ISource> {
            { "assets-library", new AssetSource () },
            { "scaled", new ScaledSource { JpegCompressionQuality = .95f }},
        };

        public static Task<TRequest> Fetch<TRequest> (Uri url, IDestination<TRequest> destination)
            where TRequest : Request
        {
            var source = _sources [url.Scheme];
            var description = source.Describe (url);
            var request = destination.CreateRequest (description);

            return new ImageSource<TRequest> (source, request).Task;
        }
    }

    internal class ImageSource<TRequest> : IObserver<bool>, IDisposable
        where TRequest : Request
    {
        private IDisposable _subscription;
        private TRequest _request;
        private TaskCompletionSource<TRequest> _tcs;
        private object _gate;
        private bool _disposed;

        public Task<TRequest> Task {
            get { return _tcs.Task; }
        }

        public ImageSource (ISource source, TRequest request)
        {
            _request = request;
            _tcs = new TaskCompletionSource<TRequest> ();
            _gate = new object ();

            _subscription = source.Fetch (request)
                .SubscribeOn (CurrentThreadScheduler.Instance)
                .SurroundWith (Observable.Return (Unit.Default))
                .Any (_ => request.IsFulfilled)
                .SubscribeSafe (this);

            UnsubscribeIfDisposed ();
        }

        public void OnNext (bool any)
        {
            if (!any) {
                _tcs.TrySetException (new Exception (string.Format ("Request for {0} was never fulfilled", _request.Url)));
            } else {
                _tcs.TrySetResult (_request);
            }

            Dispose ();
        }

        public void OnError (Exception error)
        {
            _tcs.TrySetException (error);
            Dispose ();
        }

        public void OnCompleted ()
        {
            Dispose ();
        }

        public void Dispose ()
        {
            _disposed = true;
            UnsubscribeIfDisposed ();
        }

        void UnsubscribeIfDisposed ()
        {
            if (_disposed) {
                lock (_gate) {
                    if (_subscription != null) {
                        _subscription.Dispose ();
                        _subscription = null;
                    }
                }
            }
        }
    }
}

