using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using MonoTouch.CoreGraphics;
using MonoTouch.ImageIO;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.Threading;
using System.Runtime.InteropServices;

namespace Stampsy.ImageSource
{
    internal class ScaledSource : Source
    {
        public float JpegCompressionQuality { get; set; }

        public ScaledSource ()
        {
            JpegCompressionQuality = 1;
        }

        public override IDescription Describe (Uri url)
        {
            return new ScaledDescription {
                Url = url
            };
        }

        CGImageSource CreateImageSource (ScaledDescription description)
        {
            var url = new NSUrl (description.AbsoluteSourceUrl.AbsoluteUri);
            var source = CGImageSource.FromUrl (url);

            if (source.Handle == IntPtr.Zero)
                throw new Exception (string.Format ("Could not create source for '{0}'", url));

            return source;
        }

        protected override Task FetchToMemory (MemoryRequest request, CancellationToken token)
        {
            var description = (ScaledDescription) request.Description;

            return Task.Factory.StartNew (() => {
                using (var source = CreateImageSource (description)) {
                    token.ThrowIfCancellationRequested ();

                    var sourceSize = ImageHelper.Measure (source);
                    int maxPixelSize = GetMaxPixelSize (sourceSize, description.Size);

                    using (var scaled = CreateThumbnail (source, maxPixelSize, token))
                    using (var cropped = ScaleAndCrop (scaled, description.Size, description.Mode, token)) {
                        if (cropped == null || cropped.Handle == IntPtr.Zero)
                            throw new Exception ("Bad image.");

                        request.Image = new UIImage (cropped);
                    }
                }
            }, token);
        }

        protected override string GetImageExtension (MemoryRequest request)
        {
            using (var source = CreateImageSource ((ScaledDescription) request.Description)) {
                try {
                    return Path.GetExtension (source.TypeIdentifier) ?? "png";
                } catch {
                    return "png";
                }
            }
        }

        static int GetMaxPixelSize (Size? source, Size target)
        {
            if (!source.HasValue)
                return Math.Max (target.Width, target.Height);

            var widthScale = (float) target.Width / source.Value.Width;
            var heightScale = (float) target.Height / source.Value.Height;

            return (widthScale < heightScale)
                ? target.Width
                : target.Height;
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
                var scaledCropRect = new Rectangle (
                    (int) (offsetSize.Width / scale),
                    (int) (offsetSize.Height / scale),
                    (int) (targetSize.Width / scale),
                    (int) (targetSize.Height / scale)
                );

                return image.WithImageInRect (scaledCropRect);
            }
        }
    }
}