using System;
using System.Drawing;
using System.Runtime.InteropServices;
using MonoTouch.CoreGraphics;
using MonoTouch.ImageIO;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using CGImageProperties = MonoTouch.ImageIO.CGImageProperties;

namespace Stampsy.ImageSource
{
    internal static class ImageHelper
    {
        // There seems to be an issue with concurrent usage of CGImageSource
        private static readonly object _gate = new object ();
        
        public static Size? Measure (NSData data)
        {
            lock (_gate) {
                if (data == null || data.Handle == IntPtr.Zero)
                    return null;
                
                try {
                    using (var imageSource = CGImageSource.FromData (data)) {
                        return Measure (imageSource);
                    }
                } catch {
                    return null;
                }
            }
        }
        
        public static Size? Measure (string file)
        {
            lock (_gate) {
                using (var source = CGImageSource.FromUrl (NSUrl.FromFilename (file)))
                    return Measure (source);
            }
        }
        
        public static Size? Measure (CGImageSource source)
        {
            lock (_gate) {
                if (source == null || source.Handle == IntPtr.Zero)
                    return null;
                
                try {
                    var props = source.CopyProperties ((NSDictionary) null, 0);
                    if (props == null || !props.ContainsKey ((NSString) "PixelHeight") || !props.ContainsKey ((NSString) "PixelWidth"))
                        return null;
                    
                    var orientation = ReadOrientation (props);
                    
                    return ApplyOrientation (new Size (
                        ((NSNumber) props ["PixelWidth"]).IntValue,
                        ((NSNumber) props ["PixelHeight"]).IntValue
                    ), orientation);
                    
                } catch {
                    return null;
                }
            }
        }
        
        static UIImageOrientation ReadOrientation (NSDictionary props)
        {
            var orientation = props [CGImageProperties.Orientation] as NSNumber;
            return (orientation != null)
                ? FromExifOrientation (orientation.IntValue)
                : UIImageOrientation.Up;
        }
        
        static Size ApplyOrientation (Size size, UIImageOrientation orientation)
        {
            if (!NeedsRotate (orientation))
                return size;

            return new Size (size.Height, size.Width);
        }
        
        static bool NeedsRotate (UIImageOrientation orientation)
        {
            switch (orientation) {
            case UIImageOrientation.Left:
            case UIImageOrientation.Right:
            case UIImageOrientation.LeftMirrored:
            case UIImageOrientation.RightMirrored:
                return true;
            default:
                return false;
            }
        }
        
        public static UIImageOrientation FromExifOrientation (int o)
        {
            switch (o) {
            default:
            case 1:
                return UIImageOrientation.Up;
            case 3:
                return UIImageOrientation.Down;
            case 8:
                return UIImageOrientation.Left;
            case 6:
                return UIImageOrientation.Right;
            case 2:
                return UIImageOrientation.UpMirrored;
            case 4:
                return UIImageOrientation.DownMirrored;
            case 5:
                return UIImageOrientation.LeftMirrored;
            case 7:
                return UIImageOrientation.RightMirrored;
            }
        }
        
        public static int ToExifOrientation (UIImageOrientation o)
        {
            switch (o) {
            default:
            case UIImageOrientation.Up:
                return 1;
            case UIImageOrientation.Down:
                return 3;
            case UIImageOrientation.Left:
                return 8;
            case UIImageOrientation.Right:
                return 6;
            case UIImageOrientation.UpMirrored:
                return 2;
            case UIImageOrientation.DownMirrored:
                return 4;
            case UIImageOrientation.LeftMirrored:
                return 5;
            case UIImageOrientation.RightMirrored:
                return 7;
            }
        }

        public static CGImage Scale (CGImage image, Rectangle drawRect, Size size)
        {
            var bytesPerRow = (size.Width * 4);
            var totalBytes = (bytesPerRow * size.Height);
            
            IntPtr bitmapData = IntPtr.Zero;
            CGBitmapContext context = null;
            CGImage outImage;
            
            try {
                bitmapData = Marshal.AllocHGlobal (totalBytes);
                if (bitmapData == IntPtr.Zero) {
                    return null;
                }
                
                using (var colorSpace = CGColorSpace.CreateDeviceRGB ()) {
                    context = new CGBitmapContext (
                        bitmapData, size.Width, size.Height, 8, bytesPerRow,
                        colorSpace, CGImageAlphaInfo.NoneSkipFirst
                        );
                }
                
                if (context == null)
                    return null;
                
                context.DrawImage (drawRect, image);
                outImage = context.ToImage ();
            } catch {
                return null;
            } finally {
                if (context != null)
                    context.Dispose ();
                
                Marshal.FreeHGlobal (bitmapData);
            }
            
            return outImage;
        }
    }
}

