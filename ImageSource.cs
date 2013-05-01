using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Stampsy.ImageSource
{
    public class ImageSource
    {
        static readonly Dictionary<string, ISource> _sources = new Dictionary<string, ISource> {
            { "assets-library", new AssetSource () },
            { "scaled", new ScaledSource () },
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

    internal class ImageSource<TRequest> : IObserver<Unit>, IDisposable
        where TRequest : Request
    {
        private IDisposable _subscription;
        private TRequest _request;
        private TaskCompletionSource<TRequest> _tcs;
        private object _gate;

        public Task<TRequest> Task {
            get { return _tcs.Task; }
        }

        public ImageSource (ISource source, TRequest request)
        {
            _request = request;
            _tcs = new TaskCompletionSource<TRequest> ();
            _gate = new object ();

            _subscription = source.Fetch (request)
                .SurroundWith (Observable.Return (Unit.Default))
                .FirstOrDefaultAsync (unit => request.IsFulfilled)
                .SubscribeSafe (this);
        }

        public void OnNext (Unit value)
        {
            if (value == null) {
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
            lock (_gate) {
                if (_subscription != null) {
                    _subscription.Dispose ();
                    _subscription = null;
                }
            }
        }
    }
}

