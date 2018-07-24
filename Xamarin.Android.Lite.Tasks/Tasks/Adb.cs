using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using Xamarin.Android.Tools;

namespace Xamarin.Android.Lite.Tasks
{
	public class Adb : ToolTask
	{
		[Required]
		public string Command { get; set; }

		protected override string GenerateCommandLineCommands ()
		{
			return Command;
		}

		protected override void LogEventsFromTextOutput (string singleLine, MessageImportance messageImportance)
		{
			string text = singleLine.Trim ();

			if (text.StartsWith ("adb: usage:", StringComparison.OrdinalIgnoreCase)) {
				Log.LogError (text);
				return;
			}

			base.LogEventsFromTextOutput (singleLine, messageImportance);
		}

		protected override string ToolName => OS.IsWindows ? "adb.exe" : "adb";

		protected override string GenerateFullPathToTool () => Path.Combine (ToolPath, ToolExe);
	}
}
