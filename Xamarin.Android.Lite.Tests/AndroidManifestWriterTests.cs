using NUnit.Framework;
using System;
using System.IO;
using System.Text;
using Xamarin.Android.Lite.Tasks;

namespace Xamarin.Android.Lite.Tests
{
	[TestFixture]
	public class AndroidManifestWriterTests
	{
		private byte [] manifest;
		private AndroidManifestWriter writer;

		[SetUp]
		public void SetUp ()
		{
			var path = Path.GetDirectoryName (GetType ().Assembly.Location);
			manifest = File.ReadAllBytes (Path.Combine (path, "Data", "AndroidManifest.xml"));

			writer = new AndroidManifestWriter ();
		}

		string ToHex (int value)
		{
			return value.ToString ("X");
		}

		int PeekInt (int index)
		{
			return BitConverter.ToInt32 (manifest, index);
		}

		int ReadInt (ref int index)
		{
			int value = BitConverter.ToInt32 (manifest, index);
			index += 4;
			return value;
		}

		int[] ReadArray (ref int index, int count)
		{
			int [] value = new int [count];
			for (int i = 0; i < count; i++) {
				value [i] = ReadInt (ref index);
			}
			return value;
		}

		string GetString (int offset, byte[] strings)
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

		void AssertChunkEqual (ChunkType expected, int actual)
		{
			Assert.AreEqual ((int)expected, actual, $"Expected {ToHex ((int)expected)}, {expected.ToString ()} was {ToHex (actual)}");
		}

		/// <summary>
		/// NOTE: not a real test, trying to understand binary XML format
		/// Relevant links
		/// https://github.com/iBotPeaches/Apktool/blob/6231edfcfddebf4b3b8e060d334dfd23f978a5eb/brut.apktool/apktool-lib/src/main/java/brut/androlib/res/decoder/AXmlResourceParser.java
		/// https://github.com/iBotPeaches/Apktool/blob/9fc1ede9910b7b4b0852e587250fcb86dbc7326e/brut.apktool/apktool-lib/src/main/java/brut/androlib/res/decoder/StringBlock.java
		/// </summary>
		[Test]
		public void ValidateFormat ()
		{
			/*
				<?xml version="1.0" encoding="utf-8"?>
				<manifest xmlns:android="http://schemas.android.com/apk/res/android" android:versionCode="1" android:versionName="1.0" package="com.xamarin.android.lite">
					<uses-sdk android:minSdkVersion="19" android:targetSdkVersion="27" />
					<uses-permission android:name="android.permission.INTERNET" />
					<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />
					<uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />
					<application android:label="Xamarin.Android.Lite" android:name="android.app.Application" android:allowBackup="true" android:icon="@mipmap/icon" android:debuggable="true">
					<!--TODO: this is hardcoded for the sample-->
					<meta-data android:name="Xamarin.Android.Lite.Application" android:value="Xamarin.Android.Lite.Sample.App, Xamarin.Android.Lite.Sample" />
					<activity android:configChanges="orientation|screenSize" android:icon="@mipmap/icon" android:label="Xamarin.Android.Lite" android:theme="@style/MainTheme" android:name="md5bff8b7c7908ce4fe5d805acf2300a9b4.MainActivity">
						<intent-filter>
						<action android:name="android.intent.action.MAIN" />
						<category android:name="android.intent.category.LAUNCHER" />
						</intent-filter>
					</activity>
					<provider android:name="mono.MonoRuntimeProvider" android:exported="false" android:initOrder="2147483647" android:authorities="com.xamarin.android.lite.mono.MonoRuntimeProvider.__mono_init__" />
					<!--suppress ExportedReceiver-->
					<receiver android:name="mono.android.Seppuku">
						<intent-filter>
						<action android:name="mono.android.intent.action.SEPPUKU" />
						<category android:name="mono.android.intent.category.SEPPUKU.com.xamarin.android.lite" />
						</intent-filter>
					</receiver>
					</application>
				</manifest>
			*/

			int index = 0;

			int [] stringOffsets;
			byte [] strings;

			int start_doc = ReadInt (ref index);
			AssertChunkEqual (ChunkType.START_DOC, start_doc);

			//dunno
			ReadInt (ref index);

			//Strings
			{
				int start_string = ReadInt (ref index);
				AssertChunkEqual (ChunkType.START_STR, start_string);

				int chunkSize = ReadInt (ref index);
				int stringCount = ReadInt (ref index);
				int styleCount = ReadInt (ref index);
				int flags = ReadInt (ref index);
				int stringsOffset = ReadInt (ref index);
				int stylesOffset = ReadInt (ref index);

				stringOffsets = ReadArray (ref index, stringCount);
				int [] styleOffsets = ReadArray (ref index, styleCount);
				int size = ((stylesOffset == 0) ? chunkSize : stylesOffset) - stringsOffset;
				strings = new byte [size];
				Array.ConstrainedCopy (manifest, index, strings, 0, size);

				TestContext.WriteLine ("STRING TABLE");
				{
					for (int i = 0; i < stringOffsets.Length; i++) {
						TestContext.WriteLine ("\t" + GetString (stringOffsets [i], strings));
					}
				}
				TestContext.WriteLine ("END STRING TABLE");
				TestContext.WriteLine ();

				index += size;
			}

			//Resource IDs
			{
				int start_res = ReadInt (ref index);
				AssertChunkEqual (ChunkType.RESOURCES, start_res);

				int chunkSize = ReadInt (ref index);
				int [] resources = ReadArray (ref index, chunkSize / 4 - 2);
			}

			//START_XML
			{
				int start_xml = ReadInt (ref index);
				AssertChunkEqual (ChunkType.START_XML, start_xml);

				int chunkSize = ReadInt (ref index);
				int lineNumber = ReadInt (ref index);
				int dunno = ReadInt (ref index); //0xFFFFFFF

				int [] stuff = ReadArray (ref index, 2);
			}

			//START_TAG
			bool quit = false;
			while (!quit && index < manifest.Length) {
				var type = (ChunkType)PeekInt (index);
				switch (type) {
					case ChunkType.START_TAG:
						ReadTag (ref index, stringOffsets, strings);
						break;
					case ChunkType.END_TAG:
						{
							int start_xml = ReadInt (ref index);
							AssertChunkEqual (ChunkType.END_TAG, start_xml);

							int chunkSize = ReadInt (ref index);
							int lineNumber = ReadInt (ref index);
							int dunno = ReadInt (ref index); //0xFFFFFFF

							int [] stuff = ReadArray (ref index, 2);
							TestContext.WriteLine ("END_TAG");
						}
						break;
					default:
						quit = true;
						break;
				}
			}

			TestContext.WriteLine ($"Reading {index} / {manifest.Length}");

			while (index < manifest.Length) {
				int val = ReadInt (ref index);
				TestContext.WriteLine ($"{(ChunkType)val} {val} {ToHex (val)}");
			}
		}

		void ReadTag (ref int index, int [] stringOffsets, byte [] strings)
		{
			int start_tag = ReadInt (ref index);
			AssertChunkEqual (ChunkType.START_TAG, start_tag);

			int chunkSize = ReadInt (ref index);
			int lineNumber = ReadInt (ref index);
			int dunno = ReadInt (ref index); //0xFFFFFFFF

			int ns = ReadInt (ref index);
			int name = ReadInt (ref index);
			int flags = ReadInt (ref index);
			int attributeCount = ReadInt (ref index) & 0xFFFF;
			int classAttribute = ReadInt (ref index);

			//Try to lookup the name
			string nameText = GetString (stringOffsets [name], strings);
			TestContext.WriteLine ("XML tag: " + nameText);

			for (int i = 0; i < attributeCount; i++) {
				int attrNs = ReadInt (ref index);
				int attrName = ReadInt (ref index);
				int attrValue = ReadInt (ref index);
				int attrType = ReadInt (ref index);
				int attrData = ReadInt (ref index);

				string attrNameText = GetString (stringOffsets [attrName], strings);
				TestContext.WriteLine ($"\tAttribute: {attrNameText}, {ToHex (attrType)} = { attrValue}");
			}
		}
	}
}
