using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Stampsy.ImageSource
{
    public class FileDestination : IDestination<FileRequest>
    {
        public string Folder { get; private set; }

        public FileDestination (string folder)
        {
            Folder = folder;
        }

        public FileRequest CreateRequest (IDescription description)
        {
            var url = description.Url;
            var filename = ComputeHash (url) + description.Extension;

            return new FileRequest (description) {
                Filename = Path.Combine (Folder, filename)
            };
        }

        static string ComputeHash (Uri url)
        {
            string hash;
            using (var sha1 = new SHA1CryptoServiceProvider ()) {
                var bytes = Encoding.ASCII.GetBytes (url.ToString ());
                hash = BitConverter.ToString (sha1.ComputeHash (bytes));
            }
            
            return hash.Replace ("-", string.Empty).ToLower ();
        }
    }
}

