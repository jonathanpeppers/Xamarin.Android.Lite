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

		static readonly Dictionary<string, AssemblyRef> mapping = new Dictionary<string, AssemblyRef> {
			{
				"netstandard",
				new AssemblyRef {
					Name = "mscorlib",
					Version = new Version(2, 0, 5, 0),
				}
			}
		};

		class AssemblyRef
		{
			public string Name { get; set; }

			public Version Version { get; set; }

			public AssemblyRef[] Children { get; set; }
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

					Directory.CreateDirectory (Path.GetDirectoryName (output));
					assembly.Write (output);
				}

				return !Log.HasLoggedErrors;
			}
		}
	}
}
