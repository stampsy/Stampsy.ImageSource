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
                    .Select (_ => Unit.Default)
                    .Subscribe (o);

                return new CompositeDisposable (client, cancel, subscription);
            });
        }

        static Task<DBMetadata> StartDropboxTask (DBRestClient client, FileRequest request, CancellationToken token)
        {
            var description = request.DescriptionAs<DropboxDescription> ();
            var path = description.Path;

            switch (description.Kind) {
            case DropboxDescription.DropboxImageKind.LargeThumbnail:
                return client.LoadThumbnailTask (path, "large", request.Filename, token);
            case DropboxDescription.DropboxImageKind.FullResolution:
                return client.LoadFileTask (path, request.Filename, token);
            default:
                throw new NotImplementedException ();
            }
        }
    }
}

