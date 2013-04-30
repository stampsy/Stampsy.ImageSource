using System;

namespace Stampsy.ImageSource
{
    public class MemoryDestination : IDestination<MemoryRequest>
    {
        public readonly static MemoryDestination Default = new MemoryDestination ();

        private MemoryDestination ()
        {
        }

        public MemoryRequest CreateRequest (IDescription description)
        {
            return new MemoryRequest (description);
        }
    }
}