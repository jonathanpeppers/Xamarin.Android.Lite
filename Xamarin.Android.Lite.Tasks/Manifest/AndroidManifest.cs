using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace Xamarin.Android.Lite.Tasks
{
	/// <summary>
	/// NOTE: not a *full implementation* but enough to get what we need working
	/// Relevant links
	/// https://github.com/iBotPeaches/Apktool/blob/6231edfcfddebf4b3b8e060d334dfd23f978a5eb/brut.apktool/apktool-lib/src/main/java/brut/androlib/res/decoder/AXmlResourceParser.java
	/// https://github.com/iBotPeaches/Apktool/blob/9fc1ede9910b7b4b0852e587250fcb86dbc7326e/brut.apktool/apktool-lib/src/main/java/brut/androlib/res/decoder/StringBlock.java
	/// </summary>
	class AndroidManifest
	{
		const int START_DOC_UNKOWN = 3996;
		static readonly XName AndroidNS = XNamespace.Get ("http://schemas.android.com/apk/res/android") + "android";

		/// <summary>
		/// NOTE: does not dispose the stream
		/// </summary>
		public static AndroidManifest Create (Stream stream)
		{
			var manifest = new AndroidManifest ();
			var namespaces = new Dictionary<string, XName> ();
			XElement xml = null;

			var buffer = new byte [4];
			var stringTable = 
				manifest.Strings = new List<string> ();
			int chunk;
			while ((chunk = ReadInt (buffer, stream)) != -1) {
				switch ((ChunkType)chunk) {
					case ChunkType.START_DOC: {
							int dunno = ReadInt (buffer, stream); //START_DOC_UNKOWN ???
							break;
						}
					case ChunkType.STR_TABLE: {
							int chunkSize = ReadInt (buffer, stream);
							int stringCount = ReadInt (buffer, stream);
							int styleCount = ReadInt (buffer, stream);
							int flags = ReadInt (buffer, stream);
							int stringsOffset = ReadInt (buffer, stream);
							int stylesOffset = ReadInt (buffer, stream);

							var stringOffsets = ReadArray (buffer, stream, stringCount);
							int [] styleOffsets = ReadArray (buffer, stream, styleCount);
							int size = ((stylesOffset == 0) ? chunkSize : stylesOffset) - stringsOffset;
							var strings = new byte [size];
							stream.Read (strings, 0, size);

							for (int i = 0; i < stringOffsets.Length; i++) {
								stringTable.Add (GetString (stringOffsets [i], strings));
							}
							break;
						}
					case ChunkType.RESOURCES: {
							int chunkSize = ReadInt (buffer, stream);
							int [] resources = ReadArray (buffer, stream, chunkSize / 4 - 2);
							manifest.Resources = new List<int> (resources);
							break;
						}
					case ChunkType.NS_TABLE: {
							int chunkSize = ReadInt (buffer, stream);
							int namespaceCount = ReadInt (buffer, stream);
							int dunno = ReadInt (buffer, stream); //0xFFFFFFF

							int [] namespaceData = ReadArray (buffer, stream, namespaceCount);
							for (int i = 0; i < namespaceCount - 1; i += 2) {
								var name = stringTable [namespaceData [i]];
								var url = stringTable [namespaceData [i + 1]];
								namespaces [url] = XNamespace.Get (url) + name;
							}
							break;
						}
					case ChunkType.START_TAG: {
							if (stringTable.Count == 0) {
								throw new InvalidOperationException ($"Unexpected file format, {nameof (ChunkType.STR_TABLE)} not found!");
							}

							int chunkSize = ReadInt (buffer, stream);
							int lineNumber = ReadInt (buffer, stream);
							int dunno = ReadInt (buffer, stream); //0xFFFFFFFF

							int ns = ReadInt (buffer, stream);
							int name = ReadInt (buffer, stream);
							int flags = ReadInt (buffer, stream);
							int attributeCount = ReadInt (buffer, stream) & 0xFFFF;
							int classAttribute = ReadInt (buffer, stream);

							string nameText = stringTable [name];
							if (xml == null) {
								manifest.Document = xml = new XElement (nameText);

								foreach (var knownNs in namespaces) {
									xml.SetAttributeValue (XNamespace.Xmlns + knownNs.Value.LocalName, knownNs.Value.NamespaceName);
								}
							} else {
								var child = new XElement (nameText);
								xml.Add (child);
								xml = child;
							}

							for (int i = 0; i < attributeCount; i++) {
								int attrNs = ReadInt (buffer, stream);
								int attrName = ReadInt (buffer, stream);
								int attrValue = ReadInt (buffer, stream);
								int attrType = ReadInt (buffer, stream);
								int attrData = ReadInt (buffer, stream);

								string attrNameText = stringTable [attrName];
								XName attributeName;
								if (attrNs == -1) {
									attributeName = attrNameText;
								} else {
									var nsUrl = stringTable [attrNs];
									attributeName = XName.Get (attrNameText, nsUrl);
								}
								XAttribute newAttr;
								switch ((AttributeType)attrType) {
									case AttributeType.Integer:
										newAttr = new XAttribute (attributeName, attrData);
										break;
									case AttributeType.String:
										newAttr = new XAttribute (attributeName, stringTable [attrData]);
										break;
									case AttributeType.Resource:
										//TODO: need the string instead?
										newAttr = new XAttribute (attributeName, attrData);
										break;
									case AttributeType.Enum:
										newAttr = new XAttribute (attributeName, attrData);
										break;
									case AttributeType.Bool:
										//NOTE: looks like this is -1=True 0=False ???
										newAttr = new XAttribute (attributeName, attrData == -1);
										break;
									default:
										newAttr = new XAttribute (attributeName, $"[Unknown Data Type: {attrType.ToString ("X")}, Value: {attrData}]");
										break;
								}
								newAttr.AddAnnotation (attrType);
								xml.Add (newAttr);
							}
							break;
						}
					case ChunkType.END_TAG: {
							int chunkSize = ReadInt (buffer, stream);
							int lineNumber = ReadInt (buffer, stream);
							int dunno = ReadInt (buffer, stream); //0xFFFFFFF

							int [] stuff = ReadArray (buffer, stream, 2);

							xml = xml.Parent;
							break;
						}
					default:
						break;
				}
			}

			return manifest;
		}

		static int ReadInt (byte [] buffer, Stream stream)
		{
			if (stream.Read (buffer, 0, 4) != 4) {
				return 0;
			}
			return BitConverter.ToInt32 (buffer, 0);
		}

		static int [] ReadArray (byte [] buffer, Stream stream, int length)
		{
			int [] value = new int [length];
			for (int i = 0; i < length; i++) {
				value [i] = ReadInt (buffer, stream);
			}
			return value;
		}

		static string GetString (int offset, byte [] strings)
		{
			//First two bytes are the length
			short length = BitConverter.ToInt16 (strings, offset);
			//Get the string two bytes later, length times two
			return Encoding.Unicode.GetString (strings, offset + 2, length * 2);
		}

		public XElement Document { get; set; }

		/// <summary>
		/// Table of resource IDs, found in the binary doc
		/// </summary>
		public List<int> Resources { get; set; }

		/// <summary>
		/// Table of strings, found in the binary doc
		/// </summary>
		public List<string> Strings { get; set; }

		/// <summary>
		/// This will save to the stream but not close it.
		/// NOTE: It will also emit the Strings table. We must generate this every time since it is based off the XML Document.
		/// </summary>
		public void Save (Stream stream)
		{
			if (Document == null)
				throw new InvalidOperationException ($"{nameof (Document)} must not be null!");
			if (Resources == null)
				throw new InvalidOperationException ($"{nameof (Resources)} must not be null!");

			Write (ChunkType.START_DOC, stream);
			Write (START_DOC_UNKOWN, stream);

			//We have to recalculate the strings table, contains empty string by default?
			var strings = new List<string> { "" };
			FindStrings (Document, strings);
			Strings = strings;

			// Strings table
			byte [] stringData;
			var stringOffsets = new List<int> (strings.Count);
			using (var memory = new MemoryStream ()) {
				foreach (var @string in strings) {
					stringOffsets.Add ((int)memory.Position);
					var bytes = Encoding.Unicode.GetBytes (@string + "\0");
					var length = BitConverter.GetBytes ((short)((bytes.Length - 2) / 2));
					memory.Write (length, 0, length.Length);
					memory.Write (bytes, 0, bytes.Length);
				}
				memory.Write (new byte [2], 0, 2);
				stringData = memory.ToArray ();
			}

			int chunkSize = stringData.Length + stringOffsets.Count * 4 + 7 * 4;
			Write (ChunkType.STR_TABLE, stream);
			Write (chunkSize, stream);                      //chunkSize
			Write (strings.Count, stream);                  //stringCount
			Write (0, stream);                              //styleCount
			Write (0, stream);                              //flags, apparently 0?
			Write (chunkSize - stringData.Length, stream);  //stringsOffset
			Write (0, stream);                              //stylesOffset
			foreach (int offset in stringOffsets) {
				Write (offset, stream);
			}
			stream.Write (stringData, 0, stringData.Length);

			Write (ChunkType.RESOURCES, stream);
			Write (Resources.Count * 4 + 2, stream);
			foreach (int resource in Resources) {
				Write (resource, stream);
			}

			chunkSize = 5 * 4;
			Write (ChunkType.NS_TABLE, stream);
			Write (chunkSize, stream); //chunkSize
			Write (2, stream);         //namespaceCount
			Write (-1, stream);        //dunno?
			Write (strings.IndexOf (AndroidNS.LocalName), stream);
			Write (strings.IndexOf (AndroidNS.NamespaceName), stream);
		}

		static void Write (ChunkType value, Stream stream)
		{
			Write ((int)value, stream);
		}

		static void Write (int value, Stream stream)
		{
			var bytes = BitConverter.GetBytes (value);
			stream.Write (bytes, 0, bytes.Length);
		}

		static void FindStrings (XElement element, List<string> strings)
		{
			foreach (var attribute in element.Attributes ()) {
				AddIfNew (strings, attribute.Name.LocalName);

				//HACK: no idea why this is in the strings table
				if (attribute.Name.Namespace == AndroidNS.NamespaceName && attribute.Name.LocalName == "targetSdkVersion") {
					AddIfNew (strings, attribute.Value);
					continue;
				}

				var annotation = attribute.Annotation (typeof (int));
				if (annotation == null || (int)annotation == (int)AttributeType.String) {
					AddIfNew (strings, attribute.Value);
				}
			}

			AddIfNew (strings, element.Name.LocalName);

			foreach (var child in element.Elements ()) {
				FindStrings (child, strings);
			}
		}

		static void AddIfNew (List<string> strings, string value)
		{
			if (!strings.Contains (value))
				strings.Add (value);
		}
	}
}
