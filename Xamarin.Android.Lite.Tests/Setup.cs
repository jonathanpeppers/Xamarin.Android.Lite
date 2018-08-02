using NUnit.Framework;
using System;
using System.Diagnostics;

namespace Xamarin.Android.Lite.Tests
{
	[SetUpFixture]
	public class Setup
	{
		[OneTimeTearDown]
		public void AfterAllTests ()
		{
			if (Debugger.IsAttached)
				return;

			//NOTE: adb.exe can cause a couple issues on Windows
			//	1) it holds a lock on AndroidSdkPath\platform-tools\adb.exe
			//	2) the MSBuild <Exec /> task *can* hang until adb.exe exits

			try {
				AdbUtils.RunCommand ("kill-server");
			} catch (Exception ex) {
				TestContext.Error.WriteLine ("Failed to run adb kill-server: " + ex);
			}

			//NOTE: in case `adb kill-server` fails, kill the process as a last resort
			foreach (var p in Process.GetProcessesByName ("adb.exe"))
				p.Kill ();
		}
	}
}
