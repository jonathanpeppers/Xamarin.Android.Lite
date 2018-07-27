# Xamarin.Android.Lite MSBuild properties

_See [Xamarin.Android.Lite.targets](../Xamarin.Android.Lite.Tasks/Xamarin.Android.Lite.targets) for implementation._

Here is a list of MSBuild properties you can configure in your
Xamarin.Android.Lite project. Reasonable defaults are used if they are
omitted. These are meant to align with the same property names in
Xamarin.Android, but there may be differences...

## Important properties

`<AndroidSdkDirectory></AndroidSdkDirectory>`
- path to your Android
SDK

`<AndroidNdkDirectory></AndroidNdkDirectory>`
- path to your Android
NDK

`<JavaSdkDirectory></JavaSdkDirectory>`
- path to Java

`<AndroidPackageName></AndroidPackageName>`
- your package name for the `AndroidManifest.xml`, defaults to
  `com.$(AssemblyName)` if omitted.

`<AndroidApplicationClass></AndroidApplicationClass>`
- your Xamarin.Forms `App` type. This is used to load your `App` via
  `Type.GetType`. It defaults to `$(RootNamespace).App, $(AssemblyName)`
  if omitted.

### Signing properties

_TODO: Xamarin.Android.Lite assumes these already exist! Meaning you
have build and deployed a Xamarin.Android (proper) app in the past!_

`<AndroidSigningKeyStore></AndroidSigningKeyStore>`
- Keystore file to sign the app, defaults to
  `$(LocalAppData)\Xamarin\Mono for Android\debug.keystore`

`<AndroidSigningKeyAlias></AndroidSigningKeyAlias>`
- Keystore alias, defaults to `androiddebugkey`

`<AndroidSigningKeyPass></AndroidSigningKeyPass>`
- Keystore "key password", defaults to `android`

`<AndroidSigningStorePass></AndroidSigningStorePass>`
- Keystore "store password", defaults to `android`
