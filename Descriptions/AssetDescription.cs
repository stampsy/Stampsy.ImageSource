using System;
using System.Web;

namespace Stampsy.ImageSource
{
    public class AssetDescription : IDescription
    {
        public enum AssetSize
        {
            Thumbnail,
            FullResolution
        }

        public AssetSize Size { get; private set; }
        public string AssetUrl { get; private set; }
        public string Extension { get; private set; }

        private Uri _url;

        public Uri Url {
            get { return _url; }
            set {
                _url = value;

                Size = ParseResourceKind (value);
                AssetUrl = GenerateAssetUrl (value, Size);
                Extension = ParseExtension (value);
            }
        }

        static string GenerateAssetUrl (Uri url, AssetSize size)
        {
            if (size == AssetSize.Thumbnail)
                return url.AbsoluteUri.Replace ("thumbnail", "asset");
            
            return url.AbsoluteUri;
        }
        
        static AssetSize ParseResourceKind (Uri url)
        {
            return url.AbsolutePath.Contains ("thumbnail")
                ? AssetSize.Thumbnail
                : AssetSize.FullResolution;
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

