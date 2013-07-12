using System;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Web;

namespace Stampsy.ImageSource
{
    public class ScaledDescription : IDescription
    {
        public enum ScaleMode
        {
            ScaleAspectFit,
            ScaleAspectFill
        }

        public Uri SourceUrl { get; private set; }
        public Size Size { get; private set; }
        public ScaleMode Mode { get; private set; }
        public string Extension { get; private set; }

        private Uri _url;

        public Uri Url {
            get { return _url; }
            set {
                _url = value;

                var query = HttpUtility.ParseQueryString (value.Query);

                SourceUrl = ParseSourceUrl (query);
                Size = ParseRequestedSize (query);
                Mode = ParseScaleMode (query);
                Extension = Path.GetExtension (SourceUrl.OriginalString);
            }
        }

        static Size ParseRequestedSize (NameValueCollection query)
        {
            return new Size (
                int.Parse (query ["width"]),
                int.Parse (query ["height"])
            );
        }
        
        static Uri ParseSourceUrl (NameValueCollection query)
        {
            return new Uri (
                HttpUtility.UrlDecode (query ["src"]),
                UriKind.Absolute
            );
        }
        
        static ScaleMode ParseScaleMode (NameValueCollection query)
        {
            bool crop = Boolean.Parse (query ["crop"]);
            return (crop)
                ? ScaleMode.ScaleAspectFill
                : ScaleMode.ScaleAspectFit;
        }
    }
}

