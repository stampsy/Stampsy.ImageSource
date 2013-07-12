using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace Stampsy.ImageSource
{
    internal interface ISource
    {
        IDescription Describe (Uri url);
        Task Fetch (Request request, CancellationToken token);
    }
}