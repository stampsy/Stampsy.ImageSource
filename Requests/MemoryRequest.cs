using System;
using MonoTouch.UIKit;

namespace Stampsy.ImageSource
{
    public class MemoryRequest : Request, IDisposable
    {
        public UIImage Image { get; private set; }
        public FileDestination Cache { get; private set; }

        private bool _disposed;

        public override bool IsFulfilled {
            get {
                CheckDisposed ();
                return Image != null;
            }
        }

        static bool IsValidImage (UIImage image)
        {
            return image != null
                && image.Handle != IntPtr.Zero
                && image.CGImage.Handle != IntPtr.Zero;
        }

        public bool TryFulfill (UIImage image)
        {
            CheckDisposed ();

            if (!IsValidImage (image))
                return false;

            Image = image;
            return true;
        }

        public MemoryRequest (IDescription description, FileDestination cache)
            : base (description)
        {
            Cache = cache;
        }

        void CheckDisposed ()
        {
            if (_disposed)
                throw new ObjectDisposedException ("MemoryRequest");
        }

        public void Dispose ()
        {
            Image.Dispose ();
            Image = null;
            _disposed = true;
        }
    }
}

