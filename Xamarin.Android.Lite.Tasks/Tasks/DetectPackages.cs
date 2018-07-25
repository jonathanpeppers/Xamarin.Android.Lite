using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.Text.RegularExpressions;
using Xamarin.Android.Tools;

namespace Xamarin.Android.Lite.Tasks
{
	public class DetectPackages : ToolTask
	{
		[Required]
		public string ApiLevel { get; set; }

		[Required]
		public string PackageName { get; set; }

		[Output]
		public string RuntimeVersion { get; set; }

		[Output]
		public string PlatformRuntimeVersion { get; set; }

		[Output]
		public string PackageVersion { get; set; }

		const string IntentAction = "mono.android.intent.action.PACKAGE_VERSIONS";
		const string DebugRuntime = "Mono.Android.DebugRuntime";
		const string PlatformRuntime = "Mono.Android.Platform.ApiLevel";
		const RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Compiled;

		static Regex result = new Regex (@"result=(\d+)", options);
		static Regex data = new Regex (@"data=""([^""]+)""", options);

		protected override string GenerateCommandLineCommands ()
		{
			return $"shell am broadcast -a {IntentAction} -e packages \"{DebugRuntime},{PlatformRuntime}_{ApiLevel},{PackageName}\" -n \"{DebugRuntime}/com.xamarin.mono.android.PackageVersions\"";
		}

		protected override void LogEventsFromTextOutput (string singleLine, MessageImportance messageImportance)
		{
			base.LogEventsFromTextOutput (singleLine, messageImportance);

			string text = singleLine.Trim ();

			if (text.StartsWith ("adb: usage:", StringComparison.OrdinalIgnoreCase)) {
				Log.LogError (text);
				return;
			}

			// Broadcasting: Intent { act=mono.android.intent.action.PACKAGE_VERSIONS flg=0x400000 cmp=Mono.Android.DebugRuntime/com.xamarin.mono.android.PackageVersions (has extras) }
			// Broadcast completed: result=0, data="Mono.Android.DebugRuntime=1529065494,Mono.Android.Platform.ApiLevel_27=1531422460,com.xamarin.android.lite.sample=1"
			if (text.StartsWith ("Broadcast completed:", StringComparison.OrdinalIgnoreCase)) {
				var resultMatch = result.Match (text);
				if (!resultMatch.Success || resultMatch.Groups[1].Value != "0") {
					Log.LogError ("Query failed with: " + text);
					return;
				}

				var dataMatch = data.Match (text);
				if (dataMatch.Success) {
					foreach (var pair in dataMatch.Groups [1].Value.Split (',')) {
						var split = pair.Split ('=');
						if (split.Length == 2) {
							var key = split [0];
							var value = split [1];
							if (key == DebugRuntime) {
								RuntimeVersion = value;
							} else if (key == PlatformRuntime + "_" + ApiLevel) {
								PlatformRuntimeVersion = value;
							} else if (key == PackageName) {
								PackageVersion = value;
							}
						}
					}
				}
			}
		}

		protected override string ToolName => OS.IsWindows ? "adb.exe" : "adb";

		protected override string GenerateFullPathToTool () => Path.Combine (ToolPath, ToolExe);
	}
}
