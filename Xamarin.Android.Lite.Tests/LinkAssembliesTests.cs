using NUnit.Framework;
using System.IO;
using Xamarin.Android.Lite.Tasks;

namespace Xamarin.Android.Lite.Tests
{
	[TestFixture]
	public class LinkAssembliesTests
	{
		const string Assembly = "Xamarin.Android.Lite.Sample.dll";
		string input, output, temp;

		[SetUp]
		public void SetUp ()
		{
			temp = Path.Combine (Path.GetTempPath (), nameof (LinkAssembliesTests));
			if (Directory.Exists (temp))
				Directory.Delete (temp, recursive: true);

			input = Path.Combine (temp, "input");
			output = Path.Combine (temp, "output");

			Directory.CreateDirectory (input);
			File.Copy (Path.Combine (Path.GetDirectoryName (GetType ().Assembly.Location), Assembly), Path.Combine (input, Assembly), true);
		}

		[TearDown]
		public void TearDown ()
		{
			if (Directory.Exists (temp))
				Directory.Delete (temp, recursive: true);
		}

		[Test]
		public void Execute ()
		{
			var task = new LinkAssemblies {
				BuildEngine = new MockBuildEngine (),
				InputDirectory = input,
				OutputDirectory = output,
			};
			Assert.IsTrue (task.Execute (), "Execute should succeed!");
			var assembly = Path.Combine (output, Assembly);
			FileAssert.Exists (assembly, $"Output assembly `{assembly}` should exist!");
		}
	}
}
