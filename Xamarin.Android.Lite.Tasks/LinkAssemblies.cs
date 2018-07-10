using Java.Interop.Tools.Cecil;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Linker;
using Mono.Linker.Steps;
using MonoDroid.Tuner;
using System.IO;
using MSBuild = Microsoft.Build.Framework;

namespace Xamarin.Android.Lite.Tasks
{
	/// <summary>
	/// NOTE: this is not a "real" linker, it fixes up assemblies that reference netstandard.dll, making them instead point to mscorlib.dll
	/// </summary>
	public class LinkAssemblies : Task, Mono.Linker.ILogger
	{
		[Required]
		public string MainAssembly { get; set; }

		public string [] ResolvedAssemblies { get; set; }

		[Required]
		public string OutputDirectory { get; set; }

		public override bool Execute ()
		{
			var rp = new ReaderParameters {
				InMemory = true,
			};
			using (var res = new DirectoryAssemblyResolver (this.CreateTaskLogger (), loadDebugSymbols: false, loadReaderParameters: rp)) {

				// Put every assembly we'll need in the resolver
				res.Load (MainAssembly);
				foreach (var assembly in ResolvedAssemblies) {
					res.Load (Path.GetFullPath (assembly));
				}

				var resolver = new AssemblyResolver (res.ToResolverCache ());
				resolver.AddSearchDirectory (OutputDirectory);
				if (ResolvedAssemblies != null) {
					foreach (var assembly in ResolvedAssemblies) {
						resolver.AddSearchDirectory (Path.GetDirectoryName (assembly));
					}
				}

				var pipeline = new Pipeline ();
				pipeline.AppendStep (new FixAbstractMethodsStep ());
				pipeline.AppendStep (new OutputStep ());

				pipeline.PrependStep (new ResolveFromAssemblyStep (MainAssembly));
				if (ResolvedAssemblies != null) {
					foreach (var assembly in ResolvedAssemblies) {
						pipeline.PrependStep (new ResolveFromAssemblyStep (assembly));
					}
				}

				var context = new AndroidLinkContext (pipeline, resolver) {
					Logger = this,
					CoreAction = AssemblyAction.Link,
					UserAction = AssemblyAction.Link,
					LinkSymbols = true,
					SymbolReaderProvider = new DefaultSymbolReaderProvider (true),
					SymbolWriterProvider = new DefaultSymbolWriterProvider (),
					OutputDirectory = OutputDirectory
				};

				pipeline.Process (context);

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
