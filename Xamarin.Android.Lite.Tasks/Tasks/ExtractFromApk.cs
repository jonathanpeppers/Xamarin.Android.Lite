using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Linq;
using System.IO;
using Xamarin.Tools.Zip;

namespace Xamarin.Android.Lite.Tasks
{
	/// <summary>
	/// NOTE: currently only used to extract the base AndroidManifest.xml out of the prebuilt APK
	/// </summary>
	public class ExtractFromApk : Task
	{
		[Required]
		public string [] SourceFiles { get; set; }

		[Required]
		public string [] DestinationFiles { get; set; }

		[Required]
		public ITaskItem Apk { get; set; }

		public override bool Execute ()
		{
			if (SourceFiles.Length == 0) {
				Log.LogError ("No files were specified to be removed from the APK!");
				return false;
			}
			if (SourceFiles.Length != DestinationFiles.Length) {
				Log.LogError ("{0} and {1} should be the same length!", nameof (SourceFiles), nameof (DestinationFiles));
				return false;
			}

			using (var zip = ZipArchive.Open (Apk.ItemSpec, FileMode.Open)) {

				var entries = zip.ToDictionary (e => e.FullName);

				for (int i = 0; i < SourceFiles.Length; i++) {
					var file = SourceFiles [i];
					var destination = DestinationFiles [i];

					var pathInZip = file.ToAndroidPath ();
					if (entries.TryGetValue (pathInZip, out ZipEntry entry)) {
						Log.LogMessage (MessageImportance.Low, "Extracting `{0}` to `{1}`", pathInZip, destination);
						using (var stream = File.Create (destination))
							entry.Extract (stream);
					} else {
						Log.LogError ("Cannot remove `{0}` from zip `{1}`, it was not found!", file, Apk.ItemSpec);
					}
				}
			}

			return !Log.HasLoggedErrors;
		}
	}
}
