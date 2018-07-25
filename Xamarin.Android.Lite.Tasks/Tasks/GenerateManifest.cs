using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Xamarin.Android.Lite.Tasks
{
	public class GenerateManifest : Task
	{
		const string ApplicationMetadata = "Xamarin.Android.Lite.Application";

		[Required]
		public string DestinationFile { get; set; }

		[Required]
		public string PackageName { get; set; }

		[Required]
		public string ApplicationClass { get; set; }

		public string VersionCode { get; set; }

		public string VersionName { get; set; }

		public override bool Execute ()
		{
			AndroidManifest manifest;
			using (var stream = GetType ().Assembly.GetManifestResourceStream ("Xamarin.Android.Lite.Tasks.AndroidManifest.xml")) {
				manifest = AndroidManifest.Read (stream);
			}

			string versionCode = string.IsNullOrEmpty (VersionCode) ? "1" : VersionCode;
			string versionName = string.IsNullOrEmpty (VersionName) ? "1.0" : VersionName;

			var manifestElement = manifest.Document;
			if (manifestElement.Name != "manifest") {
				Log.LogError ("No `manifest` element found!");
				return false;
			}
			var application = manifestElement.Element ("application");
			if (application == null) {
				Log.LogError ("No `application` element found!");
				return false;
			}

			var ns = AndroidManifest.AndroidNamespace.Namespace;
			manifestElement.SetAttributeValue ("package", PackageName);
			manifestElement.SetAttributeValue (ns + "versionCode", versionCode);
			manifestElement.SetAttributeValue (ns + "versionName", versionName);

			var metadata = application.Elements ("meta-data").Where (e => e.Attribute (ns + "name")?.Value == ApplicationMetadata).FirstOrDefault ();
			if (metadata == null) {
				metadata = new XElement ("meta-data");
				metadata.SetAttributeValue (ns + "name", ApplicationMetadata);
				application.Add (metadata);
			}
			metadata.SetAttributeValue (ns + "value", ApplicationClass);

			//NOTE: two other Xamarin.Android implementation-specific places *may* need the package name replaced
			var authorities = application.Element ("provider")?.Attribute (ns + "authorities");
			if (authorities != null)
				authorities.Value = PackageName + ".mono.MonoRuntimeProvider.__mono_init_";

			var category = application.Element ("receiver")?.Element ("intent-filter")?.Element ("category")?.Attribute (ns + "name");
			if (category != null)
				category.Value = "mono.android.intent.category.SEPPUKU." + PackageName;

			using (var stream = File.Create (DestinationFile)) {
				manifest.Write (stream);
			}

			return !Log.HasLoggedErrors;
		}
	}
}
