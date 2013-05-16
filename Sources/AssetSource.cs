using System;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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
    internal class AssetSource : ISource
    {
        static readonly Lazy<ALAssetsLibrary> _library = new Lazy<ALAssetsLibrary> (
            () => new ALAssetsLibrary ()
        );

        public IDescription Describe (Uri url)
        {
            return new AssetDescription {
                Url = url
            };
        }

        public IObservable<Unit> Fetch (Request request)
        {
            var description = request.DescriptionAs<AssetDescription> ();

            return (description.Kind == AssetDescription.AssetImageKind.Thumbnail)
                ? SaveThumbnail (request)
                : SaveFullResolutionImage (request);
        }

        Task<ALAsset> GetAsset (AssetDescription description, CancellationToken token)
        {
            var tcs = new TaskCompletionSource<ALAsset> ();

            Task.Factory.StartNew (() => {
                if (token.IsCancellationRequested) {
                    tcs.SetCanceled ();
                    return;
                }

                _library.Value.AssetForUrl (new NSUrl (description.AssetUrl), (asset) => {
                    if (asset == null) {
                        tcs.SetException (new Exception ("No asset found for url"));
                        return;
                    }

                    if (asset.DefaultRepresentation == null) {
                        tcs.SetException (new Exception ("No representation found for the asset"));
                        return;
                    }

                    tcs.SetResult (asset);
                }, error => {
                    tcs.SetException (new Exception (error.ToString ()));
                });
            }, token).RouteExceptions (tcs);

            return tcs.Task;
        }

        IObservable<Unit> SaveThumbnail (Request request)
        {
            if (request is FileRequest)
                return SaveThumbnailToFile ((FileRequest) request);

            if (request is MemoryRequest)
                return SaveThumbnailToMemory ((MemoryRequest) request);

            throw new NotImplementedException ();
        }

        IObservable<Unit> SaveThumbnailToFile (FileRequest request)
        {
            return Observable.Create<Unit> (o => {
                var description = request.DescriptionAs<AssetDescription> ();
                var disp = new CancellationDisposable ();
                var token = disp.Token;

                GetAsset (description, token).ContinueWith (t => {
                    var saveUrl = NSUrl.FromFilename (request.Filename);
                    
                    using (var asset = t.Result)
                    using (var representation = asset.DefaultRepresentation)
                    using (var thumbnail = new UIImage (asset.Thumbnail))
                    using (var destination = CGImageDestination.FromUrl (saveUrl, representation.Uti, 1))
                    using (var cgImage = thumbnail.CGImage) {
                        destination.AddImage (cgImage, new NSMutableDictionary {
                            { CGImageProperties.Orientation, NSNumber.FromInt32 (ImageHelper.ToExifOrientation (thumbnail.Orientation)) }
                        });
                        
                        destination.Close ();
                    }
                    
                    o.OnCompleted ();
                }, token).RouteExceptions (o);
                
                return disp;
            });
        }

        IObservable<Unit> SaveThumbnailToMemory (MemoryRequest request)
        {
            return Observable.Create<Unit> (o => {
                var description = request.DescriptionAs<AssetDescription> ();
                var disp = new CancellationDisposable ();
                var token = disp.Token;

                GetAsset (description, token).ContinueWith (t => {
                    using (var asset = t.Result)
                        request.Image = new UIImage (asset.Thumbnail);

                    o.OnCompleted ();
                }, token).RouteExceptions (o);

                return disp;
            });
        }

        IObservable<Unit> SaveFullResolutionImage (Request request)
        {
            if (request is FileRequest)
                return SaveFullResolutionImageToFile ((FileRequest) request);
            
            if (request is MemoryRequest)
                return SaveFullResolutionImageToMemory ((MemoryRequest) request);
            
            throw new NotImplementedException ();
        }

        IObservable<Unit> SaveFullResolutionImageToFile (FileRequest request)
        {
            return Observable.Create<Unit> (o => {
                var description = request.DescriptionAs<AssetDescription> ();
                var disp = new CancellationDisposable ();
                var token = disp.Token;

                GetAsset (description, token).ContinueWith (t => {
                    using (File.Create (request.Filename))
                    using (var asset = t.Result)
                    using (var representation = asset.DefaultRepresentation) 
                    using (var stream = new NSOutputStream (request.Filename, true)) {
                        stream.Open ();

                        long offset = 0;
                        uint bytesRead = 0;

                        NSError err;

                        // A large enough buffer that shouldn't cause memory warnings
                        byte [] buffer = new byte [131072];

                        GCHandle handle = GCHandle.Alloc (buffer, GCHandleType.Pinned);
                        IntPtr pointer = handle.AddrOfPinnedObject ();
                        
                        unsafe {
                            while (offset < representation.Size && stream.HasSpaceAvailable ()) {
                                bytesRead = representation.GetBytes (pointer, offset, (uint)buffer.Length, out err);
                                stream.Write (buffer, bytesRead);
                                offset += bytesRead;
                            }
                        }
                        
                        stream.Close ();
                        handle.Free ();
                    }

                    o.OnCompleted ();
                }, token).RouteExceptions (o);

                return disp;
            });
        }

        IObservable<Unit> SaveFullResolutionImageToMemory (MemoryRequest request)
        {
            return Observable.Create<Unit> (o => {
                var description = request.DescriptionAs<AssetDescription> ();
                var disp = new CancellationDisposable ();
                var token = disp.Token;

                GetAsset (description, token).ContinueWith (t => {
                    using (var asset = t.Result)
                    using (var representation = asset.DefaultRepresentation)
                        request.Image = new UIImage (representation.GetImage ());

                    o.OnCompleted ();
                }, token).RouteExceptions (o);

                return disp;
            });
        }
    }
}

