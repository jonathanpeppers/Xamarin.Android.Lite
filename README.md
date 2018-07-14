# Xamarin.Android.Lite

Prototype/proof of concept of a "lite" Xamarin.Android that only supports Xamarin.Forms

# How to build

    msbuild build.proj /t:Bootstrap
    msbuild build.proj

Another useful target, which doesn't invalidate `bin\xamarin-android`:

    msbuild build.proj /t:Clean

The `Bootstrap` target will take a while. An OSS build of xamarin-android will be downloaded and extracted into `bin\xamarin-android`.

I did this originally because I thought I may need to leverage Xamarin.Android artifacts from there.

Right now I am only using:
- Xamarin.Android.Tools.AndroidSdk.dll
- libzip.dll (binaries for each platform)
- libZipSharp.dll

So maybe downloading `xamarin-android` could be removed eventually?