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
		/// <summary>
		/// NOTE: does not dispose the stream
		/// </summary>
		public static AndroidManifest Create (Stream stream)
		{
			var manifest = new AndroidManifest ();
			var namespaces = new Dictionary<string, XName> ();
			XElement xml = null;

			var buffer = new byte [4];
			int [] stringOffsets = null;
			byte [] strings = null;
			int chunk;
			while ((chunk = ReadInt (buffer, stream)) != -1) {
				switch ((ChunkType)chunk) {
					case ChunkType.START_DOC:
						SkipInt (stream);
						break;
					case ChunkType.START_STR: {
							int chunkSize = ReadInt (buffer, stream);
							int stringCount = ReadInt (buffer, stream);
							int styleCount = ReadInt (buffer, stream);
							int flags = ReadInt (buffer, stream);
							int stringsOffset = ReadInt (buffer, stream);
							int stylesOffset = ReadInt (buffer, stream);

							stringOffsets = ReadArray (buffer, stream, stringCount);
							int [] styleOffsets = ReadArray (buffer, stream, styleCount);
							int size = ((stylesOffset == 0) ? chunkSize : stylesOffset) - stringsOffset;
							strings = new byte [size];
							stream.Read (strings, 0, size);
							break;
						}
					case ChunkType.RESOURCES: {
							int chunkSize = ReadInt (buffer, stream);
							int [] resources = ReadArray (buffer, stream, chunkSize / 4 - 2);
							break;
						}
					case ChunkType.START_NS: {
							int chunkSize = ReadInt (buffer, stream);
							int namespaceCount = ReadInt (buffer, stream);
							int dunno = ReadInt (buffer, stream); //0xFFFFFFF

							int [] namespaceData = ReadArray (buffer, stream, namespaceCount);
							for (int i = 0; i < namespaceCount - 1; i += 2) {
								var name = GetString (stringOffsets [namespaceData [i]], strings);
								var url = GetString (stringOffsets [namespaceData [i + 1]], strings);
								namespaces [url] = XNamespace.Get (url) + name;
							}
							break;
						}
					case ChunkType.START_TAG: {
							if (strings == null || stringOffsets == null) {
								throw new InvalidOperationException ($"Unexpected file format, {nameof (ChunkType.START_STR)} not found!");
							}

							int chunkSize = ReadInt (buffer, stream);
							int lineNumber = ReadInt (buffer, stream);
							int dunno = ReadInt (buffer, stream); //0xFFFFFFFF

							int ns = ReadInt (buffer, stream);
							int name = ReadInt (buffer, stream);
							int flags = ReadInt (buffer, stream);
							int attributeCount = ReadInt (buffer, stream) & 0xFFFF;
							int classAttribute = ReadInt (buffer, stream);

							string nameText = GetString (stringOffsets [name], strings);
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

								string attrNameText = GetString (stringOffsets [attrName], strings);
								XName attributeName;
								if (attrNs == -1) {
									attributeName = attrNameText;
								} else {
									var nsUrl = GetString (stringOffsets [attrNs], strings);
									attributeName = XName.Get (attrNameText, nsUrl);
								}
								switch ((AttributeType)attrType) {
									case AttributeType.Integer:
										xml.SetAttributeValue (attributeName, attrData);
										break;
									case AttributeType.String:
										xml.SetAttributeValue (attributeName, GetString (stringOffsets [attrData], strings));
										break;
									case AttributeType.Resource:
										//TODO: need the string instead?
										xml.SetAttributeValue (attributeName, attrData);
										break;
									case AttributeType.Enum:
										xml.SetAttributeValue (attributeName, attrData);
										break;
									case AttributeType.Bool:
										//NOTE: looks like this is -1=True 0=False ???
										xml.SetAttributeValue (attributeName, attrData == -1);
										break;
									default:
										xml.SetAttributeValue (attributeName, $"[Unknown Data Type: {attrType.ToString ("X")}, Value: {attrData}]");
										break;
								}
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

		const int IntegerLength = 4;

		static int ReadInt (byte [] buffer, Stream stream)
		{
			if (stream.Read (buffer, 0, IntegerLength) != IntegerLength) {
				return 0;
			}
			return BitConverter.ToInt32 (buffer, 0);
		}

		static bool SkipInt (Stream stream)
		{
			for (int i = 0; i < IntegerLength; i++) {
				if (stream.ReadByte () == -1) {
					return false;
				}
			}
			return true;
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
			int val = ((strings [offset + 1] & 0xFF) << 8 | strings [offset] & 0xFF);
			int length;
			if ((val & 0x8000) != 0) {
				int high = (strings [offset + 3] & 0xFF) << 8;
				int low = (strings [offset + 2] & 0xFF);
				int len_value = ((val & 0x7FFF) << 16) + (high + low);
				offset += 4;
				length = len_value * 2;
			} else {
				offset += 2;
				length = val * 2;
			}
			return Encoding.Unicode.GetString (strings, offset, length);
		}

		public XElement Document { get; set; }
	}
}
