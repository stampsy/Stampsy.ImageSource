using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using DropboxSDK;

namespace Stampsy.ImageSource
{
    public class DropboxSource : ISource
    {
        public IDescription Describe (Uri url)
        {
            return new DropboxDescription {
                Url = url
            };
        }

        public IObservable<Unit> Fetch (Request request)
        {
            return Observable.Create<Unit> (o => {
                var client = new DBRestClient (DBSession.SharedSession);
                var cancel = new CancellationDisposable ();

                var fileRequest = request as FileRequest;
                if (fileRequest == null)
                    throw new NotImplementedException ("DropboxSource only supports saving images to disk");

                var subscription = StartDropboxTask (client, fileRequest, cancel.Token)
                    .ToObservable ()
                    .Subscribe (o);

                return new CompositeDisposable (client, cancel, subscription);
            });
        }

        static Task StartDropboxTask (DBRestClient client, FileRequest request, CancellationToken token)
        {
            var description = request.DescriptionAs<DropboxDescription> ();
            var path = description.Path;
            var filename = System.IO.Path.GetFullPath (request.Filename);

            switch (description.Kind) {
            case DropboxDescription.DropboxImageKind.LargeThumbnail:
                return client.LoadThumbnailTask (path, "large", filename, token);
            case DropboxDescription.DropboxImageKind.FullResolution:
                return client.LoadFileTask (path, filename, token);
            default:
                throw new NotImplementedException ();
            }
        }
    }
}

