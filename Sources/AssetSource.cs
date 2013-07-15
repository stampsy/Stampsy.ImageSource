using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using MonoTouch.AssetsLibrary;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.ImageIO;
using CGImageProperties = MonoTouch.ImageIO.CGImageProperties;

namespace Stampsy.ImageSource
{
    internal class AssetSource : Source
    {
        static readonly Lazy<ALAssetsLibrary> _library = new Lazy<ALAssetsLibrary> (
            () => new ALAssetsLibrary ()
        );

        public override IDescription Describe (Uri url)
        {
            return new AssetDescription {
                Url = url
            };
        }

        protected override Task FetchToFile (FileRequest request, CancellationToken token)
        {
            var description = (AssetDescription) request.Description;

            return GetAsset (description, token).ContinueWith ((t) => {
                SaveAssetToFile (t.Result, request, token);
            }, token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
        }

        protected override Task FetchToMemory (MemoryRequest request, CancellationToken token)
        {
            var description = (AssetDescription) request.Description;

            return GetAsset (description, token).ContinueWith ((t) => {
                SaveAssetToMemory (t.Result, request, token);
            }, token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
        }

        bool IsThumbnail (Request request)
        {
            var description = (AssetDescription) request.Description;
            return (description.Kind == AssetDescription.AssetImageKind.Thumbnail);
        }

        void SaveAssetToFile (ALAsset asset, FileRequest request, CancellationToken token)
        {
            using (asset) {
                if (IsThumbnail (request))
                    SaveThumbnailToFile (asset, request, token);
                else
                    SaveFullResolutionImageToFile (asset, request, token);
            }
        }

        void SaveAssetToMemory (ALAsset asset, MemoryRequest request, CancellationToken token)
        {
            using (asset) {
                if (IsThumbnail (request))
                    SaveThumbnailToMemory (asset, request, token);
                else
                    SaveFullResolutionImageToMemory (asset, request, token);
            }
        }

        void SaveThumbnailToFile (ALAsset asset, FileRequest request, CancellationToken token)
        {
            var saveUrl = NSUrl.FromFilename (request.Filename);

            using (var representation = asset.DefaultRepresentation)
            using (var thumbnail = new UIImage (asset.Thumbnail))
            using (var destination = CGImageDestination.FromUrl (saveUrl, representation.Uti, 1))
            using (var cgImage = thumbnail.CGImage) {
                destination.AddImage (cgImage, new NSMutableDictionary {
                    { CGImageProperties.Orientation, NSNumber.FromInt32 (ImageHelper.ToExifOrientation (thumbnail.Orientation)) }
                });                        
                destination.Close ();
            }
        }

        void SaveThumbnailToMemory (ALAsset asset, MemoryRequest request, CancellationToken token)
        {
            request.Image = new UIImage (asset.Thumbnail);
        }

        void SaveFullResolutionImageToFile (ALAsset asset, FileRequest request, CancellationToken token)
        {
            using (File.Create (request.Filename))
            using (var representation = asset.DefaultRepresentation)
            using (var stream = new NSOutputStream (request.Filename, true)) {
                // A large enough buffer that shouldn't cause memory warnings
                byte [] buffer = new byte [131072];

                GCHandle handle = GCHandle.Alloc (buffer, GCHandleType.Pinned);
                IntPtr pointer = handle.AddrOfPinnedObject ();

                stream.Open ();

                try {
                    long offset = 0;
                    uint bytesRead = 0;

                    NSError err;

                    unsafe {
                        while (offset < representation.Size && stream.HasSpaceAvailable ()) {
                            bytesRead = representation.GetBytes (pointer, offset, (uint)buffer.Length, out err);
                            stream.Write (buffer, bytesRead);
                            offset += bytesRead;

                            token.ThrowIfCancellationRequested ();
                        }
                    }
                } finally {
                    stream.Close ();
                    handle.Free ();
                }  
            }
        }

        void SaveFullResolutionImageToMemory (ALAsset asset, MemoryRequest request, CancellationToken token)
        {
            using (var representation = asset.DefaultRepresentation)
                request.Image = new UIImage (representation.GetImage ());
        }

        Task<ALAsset> GetAsset (AssetDescription description, CancellationToken token)
        {
            return Task.Factory.StartNew (() => {
                token.ThrowIfCancellationRequested ();

                var tcs = new TaskCompletionSource<ALAsset> ();
                _library.Value.AssetForUrl (new NSUrl (description.AssetUrl), (asset) => {
                    if (asset == null) {
                        tcs.SetException (new Exception ("No asset found for url"));
                    } else if (asset.DefaultRepresentation == null) {
                        tcs.SetException (new Exception ("No representation found for the asset"));
                    } else {
                        tcs.SetResult (asset);
                    }
                }, error => {
                    tcs.SetException (new Exception (error.ToString ()));
                });

                return tcs.Task;
            }, token).Unwrap ();
        }
    }
}

