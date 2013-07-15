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
            if (request.IsFulfilled) {
                var tcs = new TaskCompletionSource<bool> ();
                tcs.SetResult (true);
                return tcs.Task;
            }

            if (request is FileRequest)
                return FetchToFile ((FileRequest) request, token);

            if (request is MemoryRequest)
                return FetchToMemory ((MemoryRequest) request, token);

            throw new NotSupportedException ();
        }

        protected virtual Task FetchToFile (FileRequest request, CancellationToken token)
        {
            return MemoryDestination.Default.Fetch (request.Url, token).ContinueWith (t => {
                var memoryRequest = t.Result;

                using (var img = memoryRequest.Image)
                using (var data = GetImageData (img, GetImageExtension (memoryRequest))) {
                    NSError err;
                    data.Save (NSUrl.FromFilename (request.Filename), false, out err);

                    if (err != null)
                        throw new Exception (err.ToString ());
                }
            }, token);
        }

        protected virtual Task FetchToMemory (MemoryRequest request, CancellationToken token)
        {
            return FileDestination.InTemp ().Fetch (request.Url, token).ContinueWith (t => {
                var fileRequest = t.Result;
                request.Image = UIImage.FromFile (fileRequest.Filename);
            }, token);
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

