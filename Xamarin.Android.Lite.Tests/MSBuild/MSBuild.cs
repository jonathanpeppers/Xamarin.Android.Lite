using Microsoft.Build.Locator;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using static System.IO.Path;

namespace Xamarin.Android.Lite.Tests
{
	static class MSBuild
	{
		static readonly XNamespace ns = XNamespace.Get ("http://schemas.microsoft.com/developer/msbuild/2003");

		public static XElement NewElement (string name) => new XElement (ns + name);

		public static XElement WithAttribute (this XElement element, string name, object value)
		{
			element.SetAttributeValue (name, value);
			return element;
		}

		public static XElement WithValue (this XElement element, object value)
		{
			element.SetValue (value);
			return element;
		}

		static string FindMSBuild ()
		{
			//On Windows we have to "find" MSBuild
			if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
				foreach (var visualStudioInstance in MSBuildLocator.QueryVisualStudioInstances ().OrderByDescending (v => v.Version)) {
					return Combine (visualStudioInstance.MSBuildPath, "MSBuild.exe");
				}
			}

			return "msbuild";
		}

		static readonly Lazy<string> path = new Lazy<string> (FindMSBuild);

		public static string Path => path.Value;

		public const string TargetFramework = "netstandard2.0";

#if DEBUG
		public const string Configuration = "Debug";
#else
		public const string Configuration = "Release";
#endif

		/// <summary>
		/// Creates a base csproj file for these unit tests
		/// </summary>
		/// <param name="sdkStyle">If true, uses a new SDK-style project</param>
		public static XElement NewProject (string testDirectory)
		{
			var project = NewElement ("Project");

			var propertyGroup = NewElement ("PropertyGroup");
			project.WithAttribute ("Sdk", "Microsoft.NET.Sdk");
			propertyGroup.Add (NewElement ("TargetFramework").WithValue (TargetFramework));
			propertyGroup.Add (NewElement ("IncludePackageReference").WithValue ("True"));
			project.Add (propertyGroup);

			var topDirectory = GetFullPath (Combine (testDirectory, "..", "..", ".."));
			//Importing Configuration.props gets us Xamarin.Forms and Xamarin.Essentials
			project.Add (NewElement ("Import").WithAttribute ("Project", Combine (topDirectory, "Configuration.props")));
			project.Add (NewElement ("Import").WithAttribute ("Project", Combine (topDirectory, "bin", Configuration, "build", "Xamarin.Android.Lite.targets")));

			return project;
		}

		public static void Restore (string projectFile)
		{
			Build (projectFile, "Restore");
		}

		public static void Clean (string projectFile)
		{
			Build (projectFile, "Clean");
		}

		public static void Build (string projectFile, string target = "Build", string additionalArgs = "")
		{
			//HACK: workaround for Azure DevOps 2019 Pool
			string java_args = "";
			string java_home = Environment.GetEnvironmentVariable ("JAVA_HOME_8_X64");
			if (!string.IsNullOrEmpty (java_home)) {
				java_args = $"/p:JavaSdkDirectory=\"{java_home}\"";
			}

			var psi = new ProcessStartInfo {
				FileName = FindMSBuild (),
				Arguments = $"/v:minimal /nologo {projectFile} /t:{target} /bl {java_args} {additionalArgs}",
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				WorkingDirectory = GetDirectoryName (projectFile),
			};
			using (var p = new Process { StartInfo = psi }) {
				p.ErrorDataReceived += (s, e) => Console.Error.WriteLine (e.Data);
				p.OutputDataReceived += (s, e) => Console.WriteLine (e.Data);

				p.Start ();
				p.BeginErrorReadLine ();
				p.BeginOutputReadLine ();
				p.WaitForExit ();
				Assert.AreEqual (0, p.ExitCode, "MSBuild exited with {0}", p.ExitCode);
			}
		}
	}
}
