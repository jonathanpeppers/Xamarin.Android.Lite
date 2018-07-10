using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;

namespace Xamarin.Android.Lite.Tasks
{
	/// <summary>
	/// NOTE: this is not a "real" linker, it fixes up assemblies that reference netstandard.dll, making them instead point to mscorlib.dll
	/// </summary>
	public class LinkAssemblies : Task
	{
		[Required]
		public string [] InputAssemblies { get; set; }

		[Required]
		public string [] OutputAssemblies { get; set; }

		static readonly Version DotNetVersion = new Version (2, 0, 5, 0);

		/// <summary>
		/// Mapping of referenced assemblies that need "fixed"
		/// </summary>
		static readonly Dictionary<string, AssemblyRef> mapping = new Dictionary<string, AssemblyRef> {
			{
				"netstandard",
				new AssemblyRef {
					Name = "mscorlib",
					Alternates = new Dictionary<string, AssemblyRef> {
						{ "System.Xml", new AssemblyRef { Name = "System.Xml" } }
					}
				}
			}
		};

		class AssemblyRef
		{
			public string Name { get; set; }

			public Version Version { get; set; } = DotNetVersion;

			/// <summary>
			/// References of namespaces that need "fixed"
			/// </summary>
			public Dictionary<string, AssemblyRef> Alternates { get; set; }
		}

		public override bool Execute ()
		{
			using (var resolver = new DefaultAssemblyResolver ()) {
				var rp = new ReaderParameters () {
					AssemblyResolver = resolver,
				};
				string input, output;
				for (int i = 0; i < InputAssemblies.Length; i++) {
					input = InputAssemblies [i];
					output = OutputAssemblies [i];

					resolver.AddSearchDirectory (Path.GetDirectoryName (input));

					var assembly = AssemblyDefinition.ReadAssembly (input, rp);
					var references = assembly.MainModule.AssemblyReferences;
					for (int j = 0; j < references.Count; j++) {
						if (mapping.TryGetValue (references[j].Name, out AssemblyRef target)) {
							references [j] = new AssemblyNameReference (target.Name, target.Version);
						}
					}

					foreach (var typeReference in assembly.MainModule.GetTypeReferences ()) {
						if (mapping.TryGetValue (typeReference.Scope.Name, out AssemblyRef target)) {
							if (target.Alternates != null && target.Alternates.TryGetValue (typeReference.Namespace, out AssemblyRef alternate)) {
								typeReference.Scope = new AssemblyNameReference (alternate.Name, alternate.Version);
							} else {
								typeReference.Scope = new AssemblyNameReference (target.Name, target.Version);
							}
						}
					}

					Directory.CreateDirectory (Path.GetDirectoryName (output));
					assembly.Write (output);
				}

				return !Log.HasLoggedErrors;
			}
		}
	}
}
