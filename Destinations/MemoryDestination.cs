using System;

namespace Stampsy.ImageSource
{
    public class MemoryDestination : Destination<MemoryRequest>
    {
        public readonly static MemoryDestination Default = new MemoryDestination ();

        private FileDestination _cache;

        public MemoryDestination (FileDestination cache = null)
        {
            _cache = cache ?? FileDestination.InTemp ();
        }

        protected override MemoryRequest CreateRequest (IDescription description)
        {
            return new MemoryRequest (description, _cache);
        }
    }
}