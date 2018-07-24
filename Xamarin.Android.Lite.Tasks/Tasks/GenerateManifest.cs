using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Xamarin.Android.Lite.Tasks
{
	public class GenerateManifest : Task
	{
		public string PackageName { get; set; }

		public string VersionCode { get; set; }

		public string VersionName { get; set; }

		[Required]
		public string AppClass { get; set; }

		public override bool Execute ()
		{

			return !Log.HasLoggedErrors;
		}
	}
}
