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
                return FetchToMemoryWithFileCache ((MemoryRequest) request, token);

            throw new NotSupportedException ();
        }

        Task FetchToMemoryWithFileCache (MemoryRequest request, CancellationToken token)
        {
            FileRequest cacheRequest = request.Cache.CreateRequest (request.Url);
            if (cacheRequest.IsFulfilled)
                return Task.Factory.StartNew (() => {
                    SaveToMemory (request, cacheRequest);
                }, token);

            return FetchToMemory (request, token).ContinueWith (t => {
                SaveToFile (cacheRequest, request);
            }, token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
        }

        protected virtual Task FetchToFile (FileRequest request, CancellationToken token)
        {
            // By default, fall back to FetchToMemory, but save to file
            return MemoryDestination.Default.Fetch (request.Url, token).ContinueWith (t => {
                using (var memoryRequest = t.Result)
                    SaveToFile (request, memoryRequest);
            }, token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
        }

        protected virtual Task FetchToMemory (MemoryRequest request, CancellationToken token)
        {
            // By default, fall back to FetchToFile, but load in memory
            return request.Cache.Fetch (request.Url, token).ContinueWith (t => {
                SaveToMemory (request, t.Result);
            }, token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
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

        void SaveToMemory (MemoryRequest target, FileRequest source)
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

