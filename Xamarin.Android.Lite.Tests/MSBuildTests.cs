using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xamarin.Android.Lite.Tasks;

namespace Xamarin.Android.Lite.Tests
{
	[TestFixture]
	public class MSBuildTests
	{
		string testDirectory;
		string tempDirectory;
		string objDirectory;
		string binDirectory;

		[SetUp]
		public void SetUp ()
		{
			testDirectory = TestContext.CurrentContext.TestDirectory;
			tempDirectory = Path.Combine (testDirectory, "temp", TestContext.CurrentContext.Test.Name);
			objDirectory = Path.Combine (tempDirectory, "obj", MSBuild.Configuration, MSBuild.TargetFramework);
			binDirectory = Path.Combine (tempDirectory, "bin", MSBuild.Configuration, MSBuild.TargetFramework);

			//NOTE: could be leftover from development
			if (Directory.Exists (tempDirectory)) {
				Directory.Delete (tempDirectory, true);
			}
			Directory.CreateDirectory (tempDirectory);
		}

		[TearDown]
		public void TearDown ()
		{
			//NOTE: leave the directory during failure, need to archive these on CI
			if (TestContext.CurrentContext.Result.Outcome.Status != NUnit.Framework.Interfaces.TestStatus.Failed) {
				if (Directory.Exists (tempDirectory)) {
					Directory.Delete (tempDirectory, true);
				}
			}
		}

		[Test]
		public void SignAndroidPackage_DefaultProperties ()
		{
			string versionCode = "1",
				versionName = "1.0",
				packageName = "com.test";
			var project = MSBuild.NewProject (testDirectory);

			var projectFile = Path.Combine (tempDirectory, "test.csproj");
			project.Save (projectFile);
			MSBuild.Restore (projectFile);
			MSBuild.Build (projectFile, "SignAndroidPackage");

			var propsPath = Path.Combine (objDirectory, "Xamarin.Android.Lite.props");
			FileAssert.Exists (propsPath);
			var manifestPath = Path.Combine (objDirectory, "AndroidManifest.xml");
			FileAssert.Exists (manifestPath);
			var ns = AndroidManifest.AndroidNamespace.Namespace;
			var manifest = AndroidManifest.Read (manifestPath);
			Assert.AreEqual (versionCode, manifest.Document.Attribute (ns + "versionCode")?.Value, "versionCode should match");
			Assert.AreEqual (versionName, manifest.Document.Attribute (ns + "versionName")?.Value, "versionName should match");
			Assert.AreEqual (packageName, manifest.Document.Attribute ("package")?.Value, "package should match");

			var apkPath = Path.Combine (binDirectory, packageName + "-Signed.apk");
			FileAssert.Exists (apkPath);
		}

		[Test]
		public void SignAndroidPackage_WithProperties ()
		{
			string versionCode = "1234",
				versionName = "1.2.3.4",
				packageName = "com.mycompany.myapp";
			var project = MSBuild.NewProject (testDirectory);
			var propertyGroup = MSBuild.NewElement ("PropertyGroup");
			propertyGroup.Add (MSBuild.NewElement ("AndroidVersionCode").WithValue (versionCode));
			propertyGroup.Add (MSBuild.NewElement ("AndroidVersionName").WithValue (versionName));
			propertyGroup.Add (MSBuild.NewElement ("AndroidPackageName").WithValue (packageName));
			project.AddFirst (propertyGroup);

			var projectFile = Path.Combine (tempDirectory, "test.csproj");
			project.Save (projectFile);
			MSBuild.Restore (projectFile);
			MSBuild.Build (projectFile, "SignAndroidPackage");

			var propsPath = Path.Combine (objDirectory, "Xamarin.Android.Lite.props");
			FileAssert.Exists (propsPath);
			var manifestPath = Path.Combine (objDirectory, "AndroidManifest.xml");
			FileAssert.Exists (manifestPath);
			var ns = AndroidManifest.AndroidNamespace.Namespace;
			var manifest = AndroidManifest.Read (manifestPath);
			Assert.AreEqual (versionCode, manifest.Document.Attribute (ns + "versionCode")?.Value, "versionCode should match");
			Assert.AreEqual (versionName, manifest.Document.Attribute (ns + "versionName")?.Value, "versionName should match");
			Assert.AreEqual (packageName, manifest.Document.Attribute ("package")?.Value, "package should match");

			var apkPath = Path.Combine (binDirectory, packageName + "-Signed.apk");
			FileAssert.Exists (apkPath);
		}
	}
}
