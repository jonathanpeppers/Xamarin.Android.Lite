# Xamarin.Android.Lite

Prototype/proof of concept of a "lite" Xamarin.Android that only
supports Xamarin.Forms.

_DISCLAIMER: I created this project during Microsoft's #HackWeek 2018.
It is not "a real thing" or endorsed/supported by Xamarin/Microsoft.
If you would like it to be "a real thing", show your support! Star
this Github repo, like the YouTube video, post on social media,
comment, etc.! Every bit helps!_

Download Xamarin.Android.Lite on [NuGet](https://www.nuget.org/packages/Xamarin.Android.Lite)

[![NuGet](https://img.shields.io/nuget/dt/Xamarin.Android.Lite.svg)](https://www.nuget.org/packages/Xamarin.Android.Lite)

![Xamarin.Android.Lite](docs/Xamarin.Android.Lite.gif)

# Demo Video

[![Xamarin.Android.Lite](https://img.youtube.com/vi/x8v88Ukukj8/0.jpg)](https://youtu.be/x8v88Ukukj8)

_NOTE: in the video, I have `MSBuild.exe` in my `PATH` example on how
to do this [here](https://stackoverflow.com/a/12608705/132442)._

The path to my MSBuild is:

`C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin`

Mac users can just run `msbuild` and it works...

# How do I use it?

The easiest way to create a new project, is to use the Xamarin.Forms
project template in Visual Studio. Just check one platform, use
`NetStandard`, and delete the platform-specific project.

_NOTE: shared projects won't work (or make sense),
Xamarin.Android.Lite is for NetStandard only_

Edit your project file to look something like:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <EmbeddedResource Include="**\*.png" />
    <PackageReference Include="Xamarin.Android.Lite" Version="0.1.0.36-preview" />
  </ItemGroup>
</Project>
```

Remove the existing `<PackageReference />` to Xamarin.Forms from the
Xamarin.Forms project template, as Xamarin.Android.Lite is pinned to a
specific version of Xamarin.Forms.

To run the app:
- Launch the emulator, or connect an Android device via USB
- Use the Android-specific MSBuild targets: `SignAndroidPackage`,
  `Install`, or `Run`.

Details on each target:
- `SignAndroidPackage` drops an APK file in `$(OutputPath)`
- `Install` deploys the APK to the connected device
- `Run` launches the main activity on the device

So, assuming the right `MSBuild.exe` is in your path on Windows (Mac
will "just work"):

    msbuild MyApp.csproj /t:Run

This command gets you going!

Visit the [MSBuild documentation](docs/MSBuild.md) for further details
about MSBuild (project) properties.

# How do the build times compare?

Comparing the `Install` target deploying to an emulator and device
(Pixel 2). I deleted `bin`/`obj` and ran `msbuild /t:Restore` before
timing each test. On the `Second Install`, I modified a XAML file and
ran `/t:Install` again.

Xamarin.Android.Lite
- Emulator / First Install - `Time Elapsed 00:00:06.54`
- Emulator / Second Install - `Time Elapsed 00:00:03.01`
- Device / First Install - `Time Elapsed 00:00:05.60`
- Device / Second Install - `Time Elapsed 00:00:02.71`

Xamarin.Android "proper"
- Emulator / First Install - `Time Elapsed 00:00:49.46`
- Emulator / Second Install - `Time Elapsed 00:00:06.22`
- Device / First Install - `Time Elapsed 00:00:46.87`
- Device / Second Install - `Time Elapsed 00:00:05.95`

_NOTE: I compared this times on Windows with Visual Studio 15.7.5,
using the default Xamarin.Forms Master Detail project template._

# What are the limitations?

Mark Seeman on an episode of [.NET Rocks](https://www.dotnetrocks.com/?show=1542)
talked about: "constraints liberate". I don't know if he originated
the idea, but that is definitely what is happening here.

- `NetStandard` 2.0 projects only, Xamarin.Forms only
- `Mono.Android.dll` or native APIs? Nope.
- Android resources/assets? Nope. Use `EmbeddedResource`.
- Debugging? Sadly, not yet.
- Release builds? Not yet.

# What's in the box?

Xamarin.Android.Lite is bundled with Xamarin.Forms and
Xamarin.Essentials to get the best APIs available for NetStandard.

Currently using:

    <PackageReference Include="Xamarin.Forms" Version="3.1.0.583944" />
    <PackageReference Include="Xamarin.Essentials" Version="0.8.0-preview" />

If another library is deemed useful here, let me know--I could bundle
it!

# How do I use images?

As noted in the project file above, `<EmbeddedResource />` is the way
to go:

    <EmbeddedResource Include="**\*.png" />

Then to load the image, you will need to use the following C#:

    yourImage.Source = ImageSource.FromResource ("YourNameSpace.xamarin_logo.png", typeof (App));

Or *better yet*, make your own XAML markup extension to do this!

# How do I contribute? Or build this repo?

    msbuild build.proj /t:Bootstrap
    msbuild build.proj

Another useful target, which doesn't invalidate `bin\xamarin-android`:

    msbuild build.proj /t:Clean

The `Bootstrap` target will take a while. An OSS build of
xamarin-android will be downloaded and extracted into
`bin\xamarin-android`.

I did this originally because I thought I may need to leverage
Xamarin.Android artifacts from there.

Right now I am only using:
- Xamarin.Android.Tools.AndroidSdk.dll
- libzip.dll (binaries for each platform)
- libZipSharp.dll

So maybe downloading `xamarin-android` could be removed eventually?