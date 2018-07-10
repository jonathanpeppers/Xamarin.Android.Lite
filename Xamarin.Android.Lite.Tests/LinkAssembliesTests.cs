using NUnit.Framework;
using System;
using System.IO;
using Xamarin.Android.Lite.Tasks;

namespace Xamarin.Android.Lite.Tests
{
	[TestFixture]
	public class LinkAssembliesTests
	{
		string input, output;

		[SetUp]
		public void SetUp ()
		{
			input = Path.Combine (Path.GetDirectoryName (GetType ().Assembly.Location), "Xamarin.Android.Lite.Sample.dll");
			output = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
		}

		[TearDown]
		public void TearDown ()
		{
			if (File.Exists (output))
				File.Delete (output);
		}

		[Test]
		public void Execute ()
		{
			var task = new LinkAssemblies {
				BuildEngine = new MockBuildEngine (),
				InputAssemblies = new [] { input },
				OutputAssemblies = new [] { output },
			};
			Assert.IsTrue (task.Execute (), "Execute should succeed!");
			FileAssert.Exists (output, $"Output assembly `{output}` should exist!");
		}
	}
}
