# What are these APKs?

Using from the user's Xamarin.Android installation:
- Mono.Android.DebugRuntime - installed by Xamarin.Android proper, found in `%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Enterprise\MSBuild\Xamarin\Android`

These came from Visual Studio 15.7.5:
- Mono.Android.Platform.ApiLevel - generated on demand by Xamarin.Android proper, cached in `%LocalAppData%\Xamarin.Android\Cache`

We can generate these APKs during Xamarin.Android.Lite's build in the future.

They are here temporarily, if work on Xamarin.Android.Lite continues.

# debug.keystore

I did a one-time generation of this file by running `/t:Install` on a random Xamarin.Android project with VS 15.7.5.

The file was located at `C:\Users\jopepper\AppData\Local\Xamarin\Mono for Android\debug.keystore`.
