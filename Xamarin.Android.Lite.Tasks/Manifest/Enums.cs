using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xamarin.Android.Lite.Tasks
{
	enum ChunkType
	{
		START_DOC = 0x080003,
		STR_TABLE = 0x1C0001,
		END_DOC   = 0x100101,
		RESOURCES = 0x080180,
		NS_TABLE  = 0x100100,
		START_TAG = 0x100102,
		END_TAG   = 0x100103,
	}

	enum AttributeType
	{
		Resource = 0x01000008,
		String   = 0x03000008,
		Integer  = 0x10000008,
		Enum     = 0x11000008,
		Bool     = 0x12000008,
	}
}
