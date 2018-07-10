using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.IO;
using Xamarin.Android.Tools;

namespace Xamarin.Android.Lite.Tasks
{
	public class ZipAlign : ToolTask
	{
		[Required]
		public ITaskItem Source { get; set; }

		[Required]
		public ITaskItem Destination { get; set; }

		public int Alignment { get; set; } = 4;

		protected override string GenerateCommandLineCommands ()
		{
			return $"{Alignment} \"{Source.ItemSpec}\" \"{Destination.ItemSpec}\"";
		}

		protected override string ToolName => OS.IsWindows ? "zipalign.exe" : "zipalign";

		protected override string GenerateFullPathToTool () => Path.Combine (ToolPath, ToolExe);
	}
}
