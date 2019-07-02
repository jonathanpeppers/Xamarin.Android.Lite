using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using Xamarin.Android.Tools;

namespace Xamarin.Android.Lite.Tests
{
	public static class AdbUtils
	{
		static AndroidSdkInfo RefreshSdk ()
		{
			//HACK: workaround for Azure DevOps 2019 Pool
			string java_home = Environment.GetEnvironmentVariable ("JAVA_HOME_8_X64");

			return new AndroidSdkInfo ((l, m) => {
				switch (l) {
					case TraceLevel.Error:
						TestContext.Error.WriteLine (m);
						break;
					default:
						TestContext.WriteLine (m);
						break;
				}
			}, javaSdkPath: java_home);
		}

		static readonly Lazy<AndroidSdkInfo> androidSdk = new Lazy<AndroidSdkInfo> (RefreshSdk);

		public static AndroidSdkInfo AndroidSdk => androidSdk.Value;

		public static (string stdout, string stderr) RunCommand (string command)
		{
			string ext = Environment.OSVersion.Platform != PlatformID.Unix ? ".exe" : "";
			string adb = Path.Combine (AndroidSdk.AndroidSdkPath, "platform-tools", "adb" + ext);
			var proc = Process.Start (new ProcessStartInfo (adb, command) {
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden,
			});
			TestContext.WriteLine ("adb " + command);
			if (!proc.WaitForExit ((int)TimeSpan.FromSeconds (30).TotalMilliseconds)) {
				proc.Kill ();
				proc.WaitForExit ();
			}
			string stdout = proc.StandardOutput.ReadToEnd ().Trim ();
			if (!string.IsNullOrEmpty (stdout)) {
				TestContext.WriteLine (stdout);
			}
			string stderr = proc.StandardError.ReadToEnd ().Trim ();
			if (!string.IsNullOrEmpty (stderr)) {
				TestContext.Error.Write (stderr);
			}
			return (stdout, stderr);
		}

		static int? GetDeviceApiLevel ()
		{
			var (stdout, stderr) = RunCommand ("shell getprop ro.build.version.sdk");
			if (int.TryParse (stdout, out int apiLevel)) {
				return apiLevel;
			}
			return null;
		}

		static readonly Lazy<int?> deviceApiLevel = new Lazy<int?> (GetDeviceApiLevel);

		public static int? DeviceApiLevel => deviceApiLevel.Value;

		public static bool IsDeviceConnected => DeviceApiLevel > 0;
	}
}
