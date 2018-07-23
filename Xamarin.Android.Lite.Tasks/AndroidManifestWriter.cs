using System.Runtime.CompilerServices;

//TODO: move this to a common place?
[assembly: InternalsVisibleTo ("Xamarin.Android.Lite.Tests")]

namespace Xamarin.Android.Lite.Tasks
{
	enum ChunkType
	{
		START_DOC = 0x080003,
		START_NS  = 0x100100,
		START_STR = 0x1C0001,
		END_NS    = 0x100101,
		RESOURCES = 0x080180,
		START_XML = 0x100100,
		START_TAG = 0x100102,
		END_TAG   = 0x100103,
	}

	class AndroidManifestWriter
	{
		public const int ATTR_TYPE_STRING = 0x3000008;

		public enum Encoding
		{
			Utf16 = 0,
			Utf8 = 0x100,
		}
	}
}
