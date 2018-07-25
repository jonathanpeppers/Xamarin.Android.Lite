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
		/// <summary>
		/// NOTE: always use / on Android
		/// </summary>
		public static string ToAndroidPath (this string path)
		{
			return path.Replace (Path.DirectorySeparatorChar, '/');
		}
	}
}
