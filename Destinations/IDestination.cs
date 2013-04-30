using System;

namespace Stampsy.ImageSource
{
    public interface IDestination<TRequest>
        where TRequest : Request
    {
        TRequest CreateRequest (IDescription description);
    }
}

