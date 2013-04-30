using System;
using MonoTouch.UIKit;

namespace Stampsy.ImageSource
{
    public class MemoryRequest : Request
    {
        public UIImage Image { get; set; }

        public override bool IsFulfilled {
            get {
                return Image != null
                    && Image.Handle != IntPtr.Zero
                    && Image.CGImage.Handle != IntPtr.Zero;
            }
        }

        public MemoryRequest (IDescription description)
            : base (description)
        {
        }
    }
}

