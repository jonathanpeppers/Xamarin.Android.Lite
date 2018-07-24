using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.IO;
using Xamarin.Tools.Zip;

namespace Xamarin.Android.Lite.Tasks
{
	/// <summary>
	/// NOTE: currently only used to remove the base AndroidManifest.xml out of the prebuilt APK
	/// </summary>
	public class RemoveFromApk : Task
	{
		[Required]
		public string [] Files { get; set; }

		[Required]
		public ITaskItem Apk { get; set; }

		public override bool Execute ()
		{
			if (Files.Length == 0) {
				Log.LogError ("No files were specified to be removed from the APK!");
				return false;
			}

			using (var zip = ZipArchive.Open (Apk.ItemSpec, FileMode.Create)) {
				foreach (var file in Files) {
					//NOTE: always use / on Android
					var pathInZip = file.Replace (Path.DirectorySeparatorChar, '/');
					if (zip.ContainsEntry (pathInZip)) {
						Log.LogMessage (MessageImportance.Low, "Removing `{0}` from zip `{1}`", pathInZip, Apk.ItemSpec);
						zip.DeleteEntry (pathInZip);
					} else {
						Log.LogWarning ("Cannot remove `{0}` from zip `{1}`, it was not found!", file, Apk.ItemSpec);
					}
				}
			}

			return !Log.HasLoggedErrors;
		}
	}
}
