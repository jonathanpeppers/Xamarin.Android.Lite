using FileProvider = global::Android.Support.V4.Content.FileProvider;

namespace Xamarin.Android.Lite
{
	public class LinkerInclude
	{
		public void FileProvider (FileProvider provider)
		{
			provider = new FileProvider ();
		}
	}
}