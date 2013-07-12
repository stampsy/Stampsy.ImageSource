using System;
using System.Threading.Tasks;

namespace Stampsy.ImageSource
{
    public static class Extensions
    {
        public static Task<TRequest> Fetch<TRequest> (this IDestination<TRequest> destination, Uri url)
            where TRequest : Request
        {
            return ImageManager.Fetch (url, destination);
        }
    }
}

