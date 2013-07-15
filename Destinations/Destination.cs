using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Stampsy.ImageSource
{
    public abstract class Destination<TRequest>
        where TRequest : Request
    {
        private readonly ConcurrentDictionary<Uri, Task<TRequest>> _tasks = new ConcurrentDictionary<Uri, Task<TRequest>> ();

        protected abstract TRequest CreateRequest (IDescription description);

        public Task<TRequest> Fetch (Uri url, CancellationToken token = default (CancellationToken))
        {
            return _tasks.GetOrAdd (url, _ => {
                var source = ImageManager.GetSource (url);
                var description = source.Describe (url);
                var request = CreateRequest (description);

                return source.Fetch (request, token).ContinueWith (t => {
                    Task<TRequest> ignored;
                    _tasks.TryRemove (url, out ignored);

                    if (t.IsCanceled)
                        throw new OperationCanceledException ("Download was canceled.");

                    if (t.IsFaulted)
                        throw new Exception (string.Format ("Could not download {0}", url), t.Exception.Flatten ().InnerException);

                    if (!request.IsFulfilled)
                        throw new Exception (string.Format ("Could not download {0}", url));

                    return request;
                });
            });
        }
    }
}