using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xamarin.Android.Lite.Tasks
{
	static class Utils
	{
		public const string DebugRuntime = "Mono.Android.DebugRuntime";
		public const string PlatformRuntime = "Mono.Android.Platform.ApiLevel";

		/// <summary>
		/// NOTE: always use / on Android
		/// </summary>
		public static string ToAndroidPath (this string path)
		{
			return path.Replace (Path.DirectorySeparatorChar, '/');
		}
	}
}
