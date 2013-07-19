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

        public static void RegisterSource (string scheme, ISource source)
        {
            _sources [scheme] = source;
        }

        public static ISource GetSource (Uri url)
        {
            return _sources [url.Scheme];
        }

        public static Task<MemoryRequest> Fetch (Uri url, CancellationToken token = default (CancellationToken))
        {
            return Fetch (MemoryDestination.Default, url, token);
        }

        public static Task<TRequest> Fetch<TRequest> (Destination<TRequest> destination, Uri url, CancellationToken token = default (CancellationToken))
            where TRequest : Request
        {
            return destination.Fetch (url, token);
        }
    }
}

