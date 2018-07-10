using Java.Interop.Tools.Cecil;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Linker;
using Mono.Linker.Steps;
using MonoDroid.Tuner;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xamarin.Android.Tasks;
using MSBuild = Microsoft.Build.Framework;

namespace Xamarin.Android.Lite.Tasks
{
	/// <summary>
	/// NOTE: this is not a "real" linker, it fixes up assemblies that reference netstandard.dll, making them instead point to mscorlib.dll
	/// </summary>
	public class LinkAssemblies : Task, Mono.Linker.ILogger
	{
		[Required]
		public string InputDirectory { get; set; }

		[Required]
		public string OutputDirectory { get; set; }

		public override bool Execute ()
		{
			if (!Directory.Exists (InputDirectory)) {
				Log.LogError ("{0} does not exist at path `{1}`", nameof (InputDirectory), InputDirectory);
				return false;
			}

			var linkerDirectory = Path.Combine (Path.GetDirectoryName (GetType ().Assembly.Location), "linker");
			if (!Directory.Exists (linkerDirectory)) {
				Log.LogError ("Unable to find linker input assemblies at path `{0}`", linkerDirectory);
				return false;
			}

			var rp = new ReaderParameters {
				InMemory = true,
			};
			using (var res = new DirectoryAssemblyResolver (this.CreateTaskLogger (), loadDebugSymbols: false, loadReaderParameters: rp)) {
				var linkerAssemblies = new List<string> ();
				linkerAssemblies.AddRange (Directory.EnumerateFiles (InputDirectory, "*.dll"));
				foreach (var assembly in Directory.EnumerateFiles (linkerDirectory, "*.dll")) {
					linkerAssemblies.Add (assembly);
				}
				foreach (var assembly in linkerAssemblies) {
					res.Load (Path.GetFullPath (assembly));
				}

				using (var resolver = new AssemblyResolver (res.ToResolverCache ())) {
					resolver.AddSearchDirectory (OutputDirectory);
					foreach (var assembly in linkerAssemblies) {
						resolver.AddSearchDirectory (Path.GetDirectoryName (assembly));
					}

					var pipeline = new Pipeline ();
					pipeline.AppendStep (new FixAbstractMethodsStep ());
					pipeline.AppendStep (new OutputStep ());

					foreach (var assembly in linkerAssemblies) {
						pipeline.PrependStep (new ResolveFromAssemblyStep (assembly));
					}

					var context = new AndroidLinkContext (pipeline, resolver) {
						LogMessages = true,
						Logger = this,
						CoreAction = AssemblyAction.Link,
						UserAction = AssemblyAction.Link,
						LinkSymbols = true,
						SymbolReaderProvider = new DefaultSymbolReaderProvider (false),
						SymbolWriterProvider = new DefaultSymbolWriterProvider (),
						OutputDirectory = OutputDirectory
					};

					pipeline.Process (context);
				}

				return !Log.HasLoggedErrors;
			}
		}

		public void LogMessage (Mono.Linker.MessageImportance importance, string message, params object [] values)
		{
			switch (importance) {
				case Mono.Linker.MessageImportance.High:
					Log.LogMessage (MSBuild.MessageImportance.High, message, values);
					break;
				case Mono.Linker.MessageImportance.Low:
					Log.LogMessage (MSBuild.MessageImportance.Low, message, values);
					break;
				case Mono.Linker.MessageImportance.Normal:
					Log.LogMessage (MSBuild.MessageImportance.Normal, message, values);
					break;
				default:
					break;
			}
		}
	}
}
