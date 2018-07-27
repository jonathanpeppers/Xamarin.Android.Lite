using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Util;
using System;
using System.Linq;
using System.Reflection;

namespace Xamarin.Android.Lite
{
	[Activity (Label = "Xamarin.Android.Lite", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class MainActivity : Forms.Platform.Android.FormsAppCompatActivity
	{
		const string Tag = "XALite";
		const string ApplicationMetadata = "Xamarin.Android.Lite.Application";

		protected override void OnCreate (Bundle bundle)
		{
			TabLayoutResource = Resource.Layout.Tabbar;
			ToolbarResource = Resource.Layout.Toolbar;

			base.OnCreate (bundle);
			Essentials.Platform.Init(this, bundle);
			Forms.Forms.Init (this, bundle);

			var applicationInfo = PackageManager.GetApplicationInfo (PackageName, PackageInfoFlags.MetaData);
			var metadata = applicationInfo?.MetaData;
			if (metadata != null) {
				var applicationType = metadata.GetString (ApplicationMetadata);
				if (!string.IsNullOrEmpty (applicationType)) {
					var assemblyName = applicationType.Split (',').Last ();
					var assembly = Assembly.Load (assemblyName);
					if (assembly == null) {
						Log.Error (Tag, "Unable to load assembly `{0}`!", assemblyName);
						return;
					}

					var type = Type.GetType (applicationType);
					if (type != null) {
						try {
							LoadApplication ((Forms.Application)Activator.CreateInstance (type));
						} catch (InvalidCastException) {
							Log.Error (Tag, "Unable to cast type `{0}` from metadata to Xamarin.Forms.Application!", type);
						}
					} else {
						Log.Error (Tag, "Unable to create type `{0}` from metadata in AndroidManifest.xml!", applicationType);
					}
				} else {
					Log.Error (Tag, "Unable to find `{0}` metadata in AndroidManifest.xml!", ApplicationMetadata);
				}
			} else {
				Log.Error (Tag, "Unable to find *any* meta-data in AndroidManifest.xml!");
			}
		}

		public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] global::Android.Content.PM.Permission[] grantResults) {
			Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
			base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
		}
    }
}

