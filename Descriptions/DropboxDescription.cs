using System;

namespace Stampsy.ImageSource
{
    public class DropboxDescription : IDescription
    {
        public enum DropboxImageKind {
            LargeThumbnail,
            FullResolution
        }

        public string Path { get; private set; }
        public string Extension { get; private set; }
        public DropboxImageKind Kind { get; private set; }

        private Uri _url;

        public Uri Url {
            get { return _url; }
            set {
                _url = value;
                Path = ParsePath (_url);
                Kind = ParseImageKind (_url);
                Extension = System.IO.Path.GetExtension (Path);
            }
        }

        static string ParsePath (Uri url)
        {
            return Uri.UnescapeDataString (url.PathAndQuery);
        }

        static DropboxImageKind ParseImageKind (Uri url)
        {
            return url.Host == "thumbnail"
                ? DropboxImageKind.LargeThumbnail
                : DropboxImageKind.FullResolution;
        }
    }
}

