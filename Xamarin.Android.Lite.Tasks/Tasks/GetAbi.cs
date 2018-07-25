using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Linq;
using System.IO;
using Xamarin.Android.Tools;

namespace Xamarin.Android.Lite.Tasks
{
	public class GetAbi : ToolTask
    {
		[Output]
		public string PreferredAbi { get; set; }

		[Output]
		public string SupportedAbis { get; set; }

		protected override string GenerateCommandLineCommands ()
		{
			return $"shell getprop ro.product.cpu.abilist";
		}

		protected override void LogEventsFromTextOutput (string singleLine, MessageImportance messageImportance)
		{
			base.LogEventsFromTextOutput (singleLine, messageImportance);

			string text = singleLine.Trim ();

			if (text.StartsWith ("adb: usage:", StringComparison.OrdinalIgnoreCase)) {
				Log.LogError (text);
				return;
			}

			SupportedAbis = text;
			PreferredAbi = text.Split (',').First ();
		}

		protected override string ToolName => OS.IsWindows ? "adb.exe" : "adb";

		protected override string GenerateFullPathToTool () => Path.Combine (ToolPath, ToolExe);
	}
}
