using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Diagnostics;
using System.IO;
using Xamarin.Android.Tools;

namespace Xamarin.Android.Lite.Tasks
{
	public class ResolveSdks : Task
    {
		[Output]
		public string AndroidSdkPath { get; set; }

		[Output]
		public string AndroidNdkPath { get; set; }

		[Output]
		public string JavaSdkPath { get; set; }

		[Output]
		public string AndroidSdkBuildToolsPath { get; set; }

		[Output]
		public string ZipAlignPath { get; set; }

		[Output]
		public string ApkSignerJar { get; set; }

		static readonly bool IsWindows = Path.DirectorySeparatorChar == '\\';
		static readonly string ZipAlign = IsWindows ? "zipalign.exe" : "zipalign";
		static readonly string ApkSigner = "apksigner.jar";

		public override bool Execute ()
		{
			var sdk = new AndroidSdkInfo (this.CreateTaskLogger (), AndroidSdkPath, AndroidNdkPath, JavaSdkPath);

			AndroidSdkPath = sdk.AndroidSdkPath;
			AndroidNdkPath = sdk.AndroidNdkPath;
			JavaSdkPath = sdk.JavaSdkPath;

			foreach (var dir in sdk.GetBuildToolsPaths ()) {
				var zipAlign = Path.Combine (dir, ZipAlign);
				if (File.Exists (zipAlign))
					ZipAlignPath = dir;

				var apkSigner = Path.Combine (dir, "lib", ApkSigner);
				if (File.Exists (apkSigner))
					ApkSignerJar = apkSigner;

				AndroidSdkBuildToolsPath = dir;

				break;
			}

			return !Log.HasLoggedErrors;
		}
	}
}
