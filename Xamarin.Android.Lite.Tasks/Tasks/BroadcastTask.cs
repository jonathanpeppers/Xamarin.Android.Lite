using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.Text.RegularExpressions;
using Xamarin.Android.Tools;

namespace Xamarin.Android.Lite.Tasks
{
	public abstract class BroadcastTask : ToolTask
	{
		const RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Compiled;

		static Regex result = new Regex (@"result=(\d+)", options);
		static Regex data = new Regex (@"data=""([^""]+)""", options);

		/// <summary>
		/// This task will run `adb shell am broadcast $(WhatYouReturn)`
		/// </summary>
		protected abstract string GenerateBroadcastCommand ();

		/// <summary>
		/// Only called if result=0
		/// 
		/// Broadcast completed: result=0, data="1234"
		/// </summary>
		protected abstract void OnData (string data);

		protected override string GenerateCommandLineCommands ()
		{
			return $"shell am broadcast {GenerateBroadcastCommand ()}";
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
				if (!resultMatch.Success || resultMatch.Groups [1].Value != "0") {
					Log.LogError ("Query failed with: " + text);
					return;
				}

				var dataMatch = data.Match (text);
				if (dataMatch.Success) {
					OnData (dataMatch.Groups [1].Value);
				}
			}
		}

		protected override string ToolName => OS.IsWindows ? "adb.exe" : "adb";

		protected override string GenerateFullPathToTool () => Path.Combine (ToolPath, ToolExe);
	}
}
