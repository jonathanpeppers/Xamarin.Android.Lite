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
		public ITaskItem Apk { get; set; }

		public override bool Execute ()
		{
			using (var zip = ZipArchive.Open (Apk.ItemSpec, FileMode.Create)) {
				ITaskItem file;
				string archivePath;
				for (int i = 0; i < Files.Length; i++) {
					file = Files [i];
					archivePath = file.GetMetadata ("ArchivePath");

					//NOTE: always use / on Android
					if (archivePath != null)
						archivePath = archivePath.Replace (Path.DirectorySeparatorChar, '/');

					zip.AddFile (file.ItemSpec, archivePath, compressionMethod: GetCompression (file), overwriteExisting: true);
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
