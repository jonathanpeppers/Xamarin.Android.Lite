using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Diagnostics;

namespace Xamarin.Android.Lite.Tasks
{
	public static class MSBuildExtensions
    {
		public static Action<TraceLevel, string> CreateTaskLogger (this Task task)
		{
			Action<TraceLevel, string> logger = (level, message) => {
				switch (level) {
					case TraceLevel.Error:
						task.Log.LogError (message);
						break;
					case TraceLevel.Warning:
						task.Log.LogWarning (message);
						break;
					case TraceLevel.Info:
						task.Log.LogMessage (message);
						break;
					case TraceLevel.Verbose:
						task.Log.LogMessage (MessageImportance.Low, message);
						break;
					default:
						break;
				}
			};
			return logger;
		}
	}
}
