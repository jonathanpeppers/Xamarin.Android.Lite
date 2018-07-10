using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using Xamarin.Android.Tools;

namespace Xamarin.Android.Lite.Tasks
{
	public class ApkSigner : ToolTask
	{
		[Required]
		public string ApkSignerJar { get; set; }

		[Required]
		public string ApkToSign { get; set; }

		[Required]
		public string KeyStore { get; set; }

		[Required]
		public string KeyAlias { get; set; }

		[Required]
		public string KeyPass { get; set; }

		[Required]
		public string StorePass { get; set; }

		public string AdditionalArguments { get; set; }

		protected override string GenerateCommandLineCommands ()
		{
			var cmd = new CommandLineBuilder ();

			//TODO: hardcoded
			int minSdk = 19;
			int maxSdk = 27;

			cmd.AppendSwitchIfNotNull ("-jar ", ApkSignerJar);
			cmd.AppendSwitch ("sign");
			cmd.AppendSwitchIfNotNull ("--ks ", KeyStore);
			cmd.AppendSwitchIfNotNull ("--ks-pass pass:", StorePass);
			cmd.AppendSwitchIfNotNull ("--ks-key-alias ", KeyAlias);
			cmd.AppendSwitchIfNotNull ("--key-pass pass:", KeyPass);
			cmd.AppendSwitchIfNotNull ("--min-sdk-version ", minSdk.ToString ());
			cmd.AppendSwitchIfNotNull ("--max-sdk-version ", maxSdk.ToString ());

			if (!string.IsNullOrEmpty (AdditionalArguments))
				cmd.AppendSwitch (AdditionalArguments);

			cmd.AppendSwitchIfNotNull (" ", Path.GetFullPath (ApkToSign));

			return cmd.ToString ();
		}

		protected override void LogEventsFromTextOutput (string singleLine, MessageImportance importance)
		{
			singleLine = singleLine.Trim ();
			if (singleLine.Length == 0)
				return;

			if (singleLine.StartsWith ("Warning:", StringComparison.OrdinalIgnoreCase)) {
				Log.LogWarning (singleLine);
				return;
			}

			Log.LogMessage (singleLine, importance);
		}

		protected override string ToolName => OS.IsWindows ? "java.exe" : "java";

		protected override string GenerateFullPathToTool () => Path.Combine (ToolPath, ToolExe);
	}
}
