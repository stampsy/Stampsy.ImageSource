using System;
using System.Threading;
using System.Threading.Tasks;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace Stampsy.ImageSource
{
    public abstract class Source : ISource
    {
        public abstract IDescription Describe (Uri url);

        public Task Fetch (Request request, CancellationToken token)
        {
            if (request is FileRequest)
                return FetchToFile ((FileRequest) request, token);

            if (request is MemoryRequest)
                return FetchToMemory ((MemoryRequest) request, token);

            throw new NotSupportedException ();
        }

        protected virtual Task FetchToFile (FileRequest request, CancellationToken token)
        {
            // By default, fall back to FetchToMemory, but save to file

            return MemoryDestination.Default.Fetch (request.Url, token).ContinueWith (t => {
                using (var memoryRequest = t.Result)
                    SaveToFile (request, memoryRequest);
            }, token);
        }

        protected virtual Task FetchToMemory (MemoryRequest request, CancellationToken token)
        {
            // By default, fall back to FetchToFile, but load in memory
 
            return request.Cache.Fetch (request.Url, token).ContinueWith (t => {
                var fileRequest = t.Result;
                ReadToMemory (request, fileRequest);
            }, token);

        }

        void SaveToFile (FileRequest target, MemoryRequest source)
        {
            if (!source.IsFulfilled)
                throw new InvalidOperationException ();

            var img = source.Image;
            using (var data = GetImageData (img, GetImageExtension (source))) {
                NSError err;
                data.Save (NSUrl.FromFilename (target.Filename), false, out err);

                if (err != null)
                    throw new Exception (err.ToString ());
            }
        }

        void ReadToMemory (MemoryRequest target, FileRequest source)
        {
            if (!source.IsFulfilled)
                throw new InvalidOperationException ();

            if (!target.TryFulfill (UIImage.FromFile (source.Filename)))
                throw new Exception ("Invalid image");
        }

        protected virtual string GetImageExtension (MemoryRequest request)
        {
            return request.Description.Extension;
        }

        protected NSData GetImageData (UIImage image, string extension)
        {
            switch (extension) {
            case "jpeg":
            case "jpg":
                return image.AsJPEG ();
            default:
                return image.AsJPEG ();
            }
        }
    }
}

