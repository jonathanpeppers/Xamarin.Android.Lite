using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using Xamarin.Tools.Zip;

namespace Xamarin.Android.Lite.Tasks
{
    public class AppendToApk : Task
    {
		[Required]
		public ITaskItem[] Files { get; set; }

		[Required]
		public ITaskItem[] Destination { get; set; }

		[Required]
		public ITaskItem Apk { get; set; }

		public override bool Execute ()
		{
			using (var zip = ZipArchive.Open (Apk.ItemSpec, FileMode.Create)) {
				ITaskItem file;
				for (int i = 0; i < Files.Length; i++) {
					file = Files [i];
					zip.AddFile (file.ItemSpec, Destination [i].ItemSpec, compressionMethod: GetCompression (file), overwriteExisting: true);
				}
			}

			return !Log.HasLoggedErrors;
		}

		static CompressionMethod GetCompression (ITaskItem file)
		{
			if (Enum.TryParse (file.GetMetadata ("Compression"), out CompressionMethod compression))
				return compression;

			return CompressionMethod.Default;
		}
	}
}
