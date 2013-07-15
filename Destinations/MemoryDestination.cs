using System;

namespace Stampsy.ImageSource
{
    public class MemoryDestination : Destination<MemoryRequest>
    {
        public readonly static MemoryDestination Default = new MemoryDestination ();

        private MemoryDestination ()
        {
        }

        protected override MemoryRequest CreateRequest (IDescription description)
        {
            return new MemoryRequest (description);
        }
    }
}