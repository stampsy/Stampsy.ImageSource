using System;
using System.Drawing;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MonoTouch.CoreGraphics;
using MonoTouch.ImageIO;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.Threading;
using System.Runtime.InteropServices;

namespace Stampsy.ImageSource
{
    internal class ScaledSource : ISource
    {
        public IDescription Describe (Uri url)
        {
            return new ScaledDescription {
                Url = url
            };
        }

        public IObservable<Unit> Fetch (Request request)
        {
            return Observable.Create<Unit> (o => {
                var description = request.DescriptionAs<ScaledDescription> ();
                var url = new NSUrl (description.AbsoluteSourceUrl.AbsoluteUri);
                var disp = new CancellationDisposable ();
                var token = disp.Token;

                Task.Factory.StartNew (() => {
                    using (var source = CGImageSource.FromUrl (url)) {
                        if (source.Handle == IntPtr.Zero)
                            throw new Exception (string.Format ("Could not create source for '{0}'", url));

                        var sourceSize = ImageHelper.Measure (source);
                        int maxPixelSize = GetMaxPixelSize (sourceSize, description.Size);

                        using (var scaled = CreateThumbnail (source, maxPixelSize, token))
                        using (var cropped = ScaleAndCrop (scaled, description.Size, description.Mode, token))
                            SaveToRequest (cropped, source.TypeIdentifier, request);

                        o.OnCompleted ();
                    }
                }, token).RouteExceptions (o);

                return disp;
            });
        }

        static CGImage ScaleAndCrop (CGImage image, Size targetSize, ScaledDescription.ScaleMode mode, CancellationToken token)
        {
            token.ThrowIfCancellationRequested ();

            bool crop = (mode == ScaledDescription.ScaleMode.ScaleAspectFill);
            var toWidth = targetSize.Width;
            var toHeight = targetSize.Height;
            
            if (image.Width == toWidth && image.Height == toHeight)
                return image;
            
            if (image.Width < toWidth && image.Height < toHeight && !crop)
                return image;
            
            var widthScale = (double) toWidth / image.Width;
            var heightScale = (double) toHeight / image.Height;
            
            var scale = (crop)
                ? Math.Max (widthScale, heightScale)
                : Math.Min (widthScale, heightScale);

            var sizeAfterScale = (scale == widthScale)
                ? new Size (toWidth, (int) (image.Height * widthScale))
                : new Size ((int) (image.Width * heightScale), toHeight);

            if (!crop) {
                targetSize = sizeAfterScale;
            }
            
            var offsetSize = new Size (
                (sizeAfterScale.Width - targetSize.Width) / 2,
                (sizeAfterScale.Height - targetSize.Height) / 2
            );
            
            if (scale < 1.0) {
                var drawRect = new Rectangle (
                    Point.Subtract (Point.Empty, offsetSize),
                    sizeAfterScale
                );
                
                return ImageHelper.Scale (image, drawRect, targetSize);
            } else {
                var scaledCropRect = new Rectangle(
                    (int) (offsetSize.Width / scale),
                    (int) (offsetSize.Height / scale),
                    (int) (targetSize.Width / scale),
                    (int) (targetSize.Height / scale)
                );
                
                return image.WithImageInRect (scaledCropRect);
            }
        }

        static void SaveToRequest (CGImage image, string typeIdentifier, Request request)
        {
            if (request is FileRequest)
                SaveToFile (image, typeIdentifier, (FileRequest) request);
            else if (request is MemoryRequest)
                SaveToMemory (image, (MemoryRequest) request);
            else
                throw new NotImplementedException ();
        }

        static void SaveToFile (CGImage image, string typeIdentifier, FileRequest request)
        {
            CheckImageArgument (image, "image");

            using (var thumbnail = new UIImage (image))
            using (var data = SerializeImage (thumbnail, typeIdentifier)) {
                NSError err;

                data.Save (NSUrl.FromFilename (request.Filename), false, out err);

                if (err != null)
                    throw new Exception (err.ToString ());
            }
        }

        static void SaveToMemory (CGImage image, MemoryRequest request)
        {
            CheckImageArgument (image, "image");
            request.Image = new UIImage (image);
        }

        static CGImage CreateThumbnail (CGImageSource source, int maxPixelSize, CancellationToken token)
        {
            token.ThrowIfCancellationRequested ();

            return source.CreateThumbnail (0, new CGImageThumbnailOptions {
                CreateThumbnailWithTransform = true,
                CreateThumbnailFromImageAlways = true,
                MaxPixelSize = maxPixelSize,
                ShouldCache = false
            });
        }

        static NSData SerializeImage (UIImage image, string typeIdentifier)
        {
            if (typeIdentifier == "public.png")
                return image.AsPNG ();

            return image.AsJPEG (.95f);
        }

        static int GetMaxPixelSize (Size? source, Size target)
        {
            if (!source.HasValue)
                return Math.Max (target.Width, target.Height);

            var widthScale = (float) target.Width / (float) source.Value.Width;
            var heightScale = (float) target.Height / (float) source.Value.Height;

            return (widthScale > heightScale)
                ? target.Width
                : target.Height;
        }

        static void CheckImageArgument (CGImage image, string argumentName)
        {
            if (image == null || image.Handle == IntPtr.Zero)
                throw new ArgumentException (argumentName, "Image is invalid.");
        }
    }
}

