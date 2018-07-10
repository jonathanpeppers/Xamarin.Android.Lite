using Microsoft.Build.Framework;
using System;
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

		public void LogErrorEvent (BuildErrorEventArgs e) { }

		public void LogMessageEvent (BuildMessageEventArgs e) { }

		public void LogWarningEvent (BuildWarningEventArgs e) { }
	}
}
