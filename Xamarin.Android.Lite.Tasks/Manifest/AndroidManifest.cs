using System;
using System.Collections.Generic;
using System.Linq;
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
		public static readonly XName AndroidNamespace = XNamespace.Get ("http://schemas.android.com/apk/res/android") + "android";

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
							int length = ReadInt (buffer, stream); //length of stream
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
							SkipChunk (chunkSize - 8, stream); //-8 is two ints, chunkType and chunkSize
							xml = xml.Parent;
							break;
						}
					case ChunkType.END_DOC: {
							int chunkSize = ReadInt (buffer, stream);
							int fileVersion = ReadInt (buffer, stream);
							int [] dunno = ReadArray (buffer, stream, 3); //-1, android, NS url
							manifest.FileVersion = stringTable [fileVersion];
							break;
						}
					default:
						throw new InvalidOperationException ($"Invalid chunk `{chunk.ToString ("X")}` at position `{stream.Position}`!");
				}
			}

			return manifest;
		}

		static void SkipChunk (int chunkSize, Stream stream)
		{
			byte [] bytes = new byte [chunkSize];
			stream.Read (bytes, 0, bytes.Length);
		}

		static int ReadInt (byte [] buffer, Stream stream)
		{
			if (stream.Read (buffer, 0, 4) != 4) {
				return -1;
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
		/// Apparently there is some kind of file version?
		/// </summary>
		public string FileVersion { get; set; }

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
			if (string.IsNullOrEmpty (FileVersion))
				throw new InvalidOperationException ($"{nameof (FileVersion)} must not be blank!");

			Write (ChunkType.START_DOC, stream);

			//NOTE: have to write to an intermediate stream so we know the chunkSize
			byte [] bytes;
			using (var memory = new MemoryStream ()) {

				//We have to recalculate the strings table, contains empty string by default?
				var strings = new List<string> { "" };
				FindStrings (Document, strings);
				Strings = strings;

				// Strings table
				byte [] stringData;
				var stringOffsets = new List<int> (strings.Count);
				using (var stringsMemory = new MemoryStream ()) {
					foreach (var @string in strings) {
						stringOffsets.Add ((int)stringsMemory.Position);
						var utf8 = Encoding.Unicode.GetBytes (@string + "\0");
						var length = BitConverter.GetBytes ((short)((utf8.Length - 2) / 2));
						stringsMemory.Write (length, 0, length.Length);
						stringsMemory.Write (utf8, 0, utf8.Length);
					}
					stringsMemory.Write (new byte [2], 0, 2);
					stringData = stringsMemory.ToArray ();
				}

				int chunkSize = stringData.Length + stringOffsets.Count * 4 + 7 * 4;
				Write (ChunkType.STR_TABLE, memory);
				Write (chunkSize, memory);                      //chunkSize
				Write (strings.Count, memory);                  //stringCount
				Write (0, memory);                              //styleCount
				Write (0, memory);                              //flags, apparently 0?
				Write (chunkSize - stringData.Length, memory);  //stringsOffset
				Write (0, memory);                              //stylesOffset
				foreach (int offset in stringOffsets) {
					Write (offset, memory);
				}
				memory.Write (stringData, 0, stringData.Length);

				Write (ChunkType.RESOURCES, memory);
				Write ((Resources.Count + 2) * 4, memory);
				foreach (int resource in Resources) {
					Write (resource, memory);
				}

				chunkSize = 6 * 4;
				Write (ChunkType.NS_TABLE, memory);
				Write (chunkSize, memory); //chunkSize
				Write (2, memory);         //namespaceCount
				Write (-1, memory);        //dunno?
				Write (strings.IndexOf (AndroidNamespace.LocalName), memory);
				Write (strings.IndexOf (AndroidNamespace.NamespaceName), memory);

				Write (Document, memory, strings, 1);

				chunkSize = 6 * 4;
				Write (ChunkType.END_DOC, memory);
				Write (chunkSize, memory); //chunkSize
				Write (strings.IndexOf (FileVersion), memory); //Some kind of Android version?
				Write (-1, memory);
				Write (strings.IndexOf (AndroidNamespace.LocalName), memory);
				Write (strings.IndexOf (AndroidNamespace.NamespaceName), memory);

				bytes = memory.ToArray ();
			}

			Write (bytes.Length + 8, stream);
			stream.Write (bytes, 0, bytes.Length);
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

		static void Write (XElement element, Stream stream, List<string> strings, int lineNumber)
		{
			int name = strings.IndexOf (element.Name.LocalName);
			Write (ChunkType.START_TAG, stream);

			//NOTE: have to write to an intermediate stream so we know the chunkSize
			byte [] bytes;
			using (var memory = new MemoryStream ()) {
				Write (lineNumber, memory);
				Write (-1, memory); //dunno?

				//TODO: slow Linq?
				var attributes = element.Attributes ().Where (a => a.Name.Namespace != XNamespace.Xmlns).ToArray ();

				Write (-1, memory); //ns
				Write (name, memory);
				Write (0, memory); //flags
				Write (attributes.Length, memory); //attributeCount
				Write (0, memory); //classAsstribute

				foreach (var attribute in attributes) {
					var annotation = attribute.Annotation (typeof (int));
					var attributeType = annotation == null ? AttributeType.Integer : (AttributeType)(int)annotation;
					if (attribute.Name.Namespace == null) {
						Write (-1, memory); //no ns
					} else {
						Write (strings.IndexOf (attribute.Name.Namespace.NamespaceName), memory);
					}
					Write (strings.IndexOf (attribute.Name.LocalName), memory);
					Write (-1, memory); //attrValue
					Write ((int)attributeType, memory);
					switch (attributeType) {
						case AttributeType.Resource:
						case AttributeType.Integer:
						case AttributeType.Enum:
							int.TryParse (attribute.Value, out int x);
							Write (x, memory);
							break;
						case AttributeType.String:
							Write (strings.IndexOf (attribute.Value), memory);
							break;
						case AttributeType.Bool:
							bool.TryParse (attribute.Value, out bool b);
							Write (b ? -1 : 0, memory);
							break;
						default:
							break;
					}
				}

				bytes = memory.ToArray ();
			}

			//chunkSize
			Write ((bytes.Length + 2) * 4, stream);
			stream.Write (bytes, 0, bytes.Length);

			foreach (var child in element.Elements ()) {
				Write (child, stream, strings, lineNumber);
			}

			Write (ChunkType.END_TAG, stream);
			Write (6 * 4, stream);
			Write (lineNumber++, stream);
			Write (-1, stream); //dunno?
			Write (-1, stream); //dunno?
			Write (name, stream);
		}

		static void FindStrings (XElement element, List<string> strings)
		{
			foreach (var attribute in element.Attributes ()) {
				AddIfNew (strings, attribute.Name.LocalName);

				//HACK: no idea why this is in the strings table
				if (attribute.Name.Namespace == AndroidNamespace.NamespaceName && attribute.Name.LocalName == "targetSdkVersion") {
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
