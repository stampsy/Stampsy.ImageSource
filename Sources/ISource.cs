using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Threading.Tasks;

namespace Stampsy.ImageSource
{
    internal interface ISource
    {
        IDescription Describe (Uri url);
        IObservable<Unit> Fetch (Request request);
        IScheduler FetchScheduler { get; }
    }
}