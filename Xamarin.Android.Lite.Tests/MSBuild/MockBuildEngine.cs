using Microsoft.Build.Framework;
using NUnit.Framework;
using System.Collections;

namespace Xamarin.Android.Lite.Tests
{
	class MockBuildEngine : IBuildEngine
	{
		public bool ContinueOnError => false;

		public int LineNumberOfTaskNode => -1;

		public int ColumnNumberOfTaskNode => -1;

		public string ProjectFileOfTaskNode => "this.xml";

		public bool BuildProjectFile (string projectFileName, string [] targetNames, IDictionary globalProperties, IDictionary targetOutputs) => true;

		public void LogCustomEvent (CustomBuildEventArgs e) { }

		public void LogErrorEvent (BuildErrorEventArgs e)
		{
			TestContext.Out.WriteLine ("[Error] " + e.Message);
		}

		public void LogMessageEvent (BuildMessageEventArgs e)
		{
			TestContext.Out.WriteLine ("[Message] " + e.Message);
		}

		public void LogWarningEvent (BuildWarningEventArgs e)
		{
			TestContext.Out.WriteLine ("[Warning] " + e.Message);
		}
	}
}
