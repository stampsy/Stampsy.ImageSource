using System;
using System.Drawing;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using MonoTouch.CoreGraphics;
using MonoTouch.ImageIO;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.Threading.Tasks;

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

                        if (description.Mode != ScaledDescription.ScaleMode.ScaleAspectFit)
                            throw new NotImplementedException ("TODO: learn to apply ScaleAspectFill and other scale modes");

                        var sourceSize = ImageHelper.Measure (source);
                        int maxPixelSize = GetMaxPixelSize (sourceSize, description.Size);

                        if (token.IsCancellationRequested)
                            return;

                        using (var scaled = CreateThumbnail (source, maxPixelSize)) {
                            SaveToRequest (scaled, source.TypeIdentifier, request);
                        }

                        o.OnCompleted ();
                    }
                }, token);

                return disp;
            });
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

        static CGImage CreateThumbnail (CGImageSource source, int maxPixelSize)
        {
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

            float scale = Math.Max (
                (float) target.Width / source.Value.Width,
                (float) target.Height / source.Value.Height
            );

            return (int) Math.Max (
                scale * source.Value.Width,
                scale * source.Value.Height
            );
        }

        static void CheckImageArgument (CGImage image, string argumentName)
        {
            if (image == null || image.Handle == IntPtr.Zero)
                throw new ArgumentException (argumentName, "Image is invalid.");
        }
    }
}

