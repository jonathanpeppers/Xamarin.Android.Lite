using NUnit.Framework;
using System.IO;
using Xamarin.Android.Lite.Tasks;

namespace Xamarin.Android.Lite.Tests
{
	[TestFixture]
	public class LinkAssembliesTests
	{
		//TODO: bring this test back one day
		//string input, temp;

		//[SetUp]
		//public void SetUp ()
		//{
		//	input = Path.Combine (Path.GetDirectoryName (GetType ().Assembly.Location), "Xamarin.Android.Lite.Sample.dll");
		//	temp = Path.Combine (Path.GetTempPath (), nameof (LinkAssembliesTests));

		//	if (Directory.Exists (temp))
		//		Directory.Delete (temp, recursive: true);
		//}

		//[TearDown]
		//public void TearDown ()
		//{
		//	if (Directory.Exists (temp))
		//		Directory.Delete (temp, recursive: true);
		//}

		//[Test]
		//public void Execute ()
		//{
		//	var task = new LinkAssemblies {
		//		BuildEngine = new MockBuildEngine (),
		//		MainAssembly = input,
		//		OutputDirectory = temp,
		//	};
		//	Assert.IsTrue (task.Execute (), "Execute should succeed!");
		//	var output = Path.Combine (temp, Path.GetFileName (input));
		//	FileAssert.Exists (output, $"Output assembly `{output}` should exist!");
		//}
	}
}
