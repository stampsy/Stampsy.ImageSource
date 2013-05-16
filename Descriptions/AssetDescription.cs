using System;
using System.Web;

namespace Stampsy.ImageSource
{
    public class AssetDescription : IDescription
    {
        public enum AssetImageKind
        {
            Thumbnail,
            FullResolution
        }

        public AssetImageKind Kind { get; private set; }
        public string AssetUrl { get; private set; }
        public string Extension { get; private set; }

        private Uri _url;

        public Uri Url {
            get { return _url; }
            set {
                _url = value;

                Kind = ParseImageKind (value);
                AssetUrl = GenerateAssetUrl (value, Kind);
                Extension = ParseExtension (value);
            }
        }

        static string GenerateAssetUrl (Uri url, AssetImageKind size)
        {
            if (size == AssetImageKind.Thumbnail)
                return url.AbsoluteUri.Replace ("thumbnail", "asset");
            
            return url.AbsoluteUri;
        }
        
        static AssetImageKind ParseImageKind (Uri url)
        {
            return url.Host == "thumbnail"
                ? AssetImageKind.Thumbnail
                : AssetImageKind.FullResolution;
        }

        static string ParseExtension (Uri url)
        {
            var ext = HttpUtility.ParseQueryString (url.AbsoluteUri) ["ext"];
            if (!string.IsNullOrEmpty (ext))
                ext = "." + ext.ToLower ();

            return ext;
        }
    }
}

