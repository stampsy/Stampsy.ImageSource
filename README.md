**Stampsy.ImageSource** is a memory-efficient MonoTouch library that helps you fetch images from different sources, such as Asset Library or Dropbox, optionally scale them down and save them to disk.  
It is in very early stage now.

This library needs [rx-monotouch](https://github.com/stampsy/rx-monotouch) to be added to the solution to compile.  
I haven't tested it on Mono 3 yet.

### Rationale

I wrote an earlier version of this while implementing custom image picker that supports different sources, such as Camera Roll and Dropbox. Because a large part of our app uses `UIWebView`, most of the time instead of loading images in memory as `UIImage`, I wanted to save them to disk. But sometimes, I also wanted to load them in `UIImage`, and I wanted to support disk caching for all kinds of images.

This library is opinionated because it solves my problems:

- It uses custom URL schemes to determine where to take image from. Some of them are normal (`http`, `assets-library`), some are made up (`dropbox` and `scaled`).
- It forces you to choose to *either* load result in memory, *or* save it to disk. The rationale is that there are often ways to save something to disk without loading it in memory at all. (For example, [there is a trick to avoid memory warnings when saving full resolution image of an asset to disk](http://stackoverflow.com/a/10062558/458193), whereas accessing the property would often kill your app with images larger than 20MB.)
- It takes care of caching files by URL hashes.
- It will support cancellation and progress reporting as optional features for some image providers.
- It will be asynchronous by default.

### Shut up and show me the code

This is overly verbose now and uses C# 4 syntax, but anyway. It'll get better.  
In this example, we first save a huge image from asset library without ever fully loading it in memory, then we scale it down—again, without loading it fully—and only load thumbnail in memory as a `UIImage` to display it.

    ImageSource.Fetch (
        new Uri ("assets-library://asset/asset.JPG?id=DDB80EF8-D2CE-40B1-AE0D-35356DD9FBF0&ext=JPG"),
        new FileDestination ("../Library/Caches")
    ).ContinueWith (saveTask => {
        ImageSource.Fetch (
            new Uri ("scaled://image/image?width=50&height=50&crop=false&src=" + saveTask.Result.Filename),
            MemoryDestination.Default
        ).ContinueWith (scaleTask => {
            View.Add (new UIImageView (scaleTask.Result.Image) { Frame = new Rectangle (0, 0, 50, 50) });
        }, uiScheduler);
    });
