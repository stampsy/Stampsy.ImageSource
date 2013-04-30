using System;
using System.IO;

namespace Stampsy.ImageSource
{
    public class FileRequest : Request
    {
        public string Filename { get; set; }

        public override bool IsFulfilled {
            get { return File.Exists (Filename); }
        }
        
        public FileRequest (IDescription description)
            : base (description)
        {
        }
    }
}

