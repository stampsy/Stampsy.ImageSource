using System;

namespace Stampsy.ImageSource
{
    public abstract class Request
    {
        public abstract bool IsFulfilled { get; }

        private IDescription _description;

        public Uri Url { 
            get { return _description.Url; }
        }

        internal TDescription DescriptionAs<TDescription> () where TDescription : IDescription
        {
            return (TDescription) _description;
        }

        public Request (IDescription description)
        {
            _description = description;
        }
    }
}

