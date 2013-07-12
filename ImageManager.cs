using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Stampsy.ImageSource
{
    public class ImageManager
    {
        static readonly Dictionary<string, ISource> _sources = new Dictionary<string, ISource> {
            { "assets-library", new AssetSource () },
            { "scaled", new ScaledSource { JpegCompressionQuality = .95f }}
        };

        static readonly ConcurrentDictionary<Uri, Task> _tasks = new ConcurrentDictionary<Uri, Task> ();

        public static Task<TRequest> Fetch<TRequest> (Uri url, IDestination<TRequest> destination, CancellationToken token = default (CancellationToken))
            where TRequest : Request
        {
            return (Task<TRequest>) _tasks.GetOrAdd (url, _ => {
                var source = _sources [url.Scheme];
                var description = source.Describe (url);
                var request = destination.CreateRequest (description);

                return source.Fetch (request, token).ContinueWith (t => {
                    Task ignored;
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

