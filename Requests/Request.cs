using System;

namespace Stampsy.ImageSource
{
    public abstract class Request
    {
        public abstract bool IsFulfilled { get; }
        public IDescription Description { get; private set; }

        public Uri Url { 
            get { return Description.Url; }
        }

        public Request (IDescription description)
        {
            Description = description;
        }
    }
}

