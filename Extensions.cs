using System;
using System.Reactive.Linq;

namespace Stampsy.ImageSource
{
    internal static class Extensions
    {
        public static IObservable<T> SurroundWith<T> (this IObservable<T> a, IObservable<T> b)
        {
            return b.Concat (a).Concat (b);
        }
    }
}

