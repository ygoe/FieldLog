using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using PdbConvert.Symbols;

namespace PdbConvert
{
	internal class PdbReader
	{
		private List<string> assemblyFileNames;
		private XmlWriter xmlWriter;
		private Assembly assembly;
		private Dictionary<string, int> fileMapping = new Dictionary<string, int>();

		public PdbReader(List<string> assemblyFileNames, XmlWriter xmlWriter)
		{
			this.assemblyFileNames = assemblyFileNames;
			this.xmlWriter = xmlWriter;
		}

		public string SourceBasePath { get; set; }

		public void Convert()
		{
			xmlWriter.WriteStartDocument();
			xmlWriter.WriteStartElement("symbols");
			xmlWriter.WriteAttributeString("program", "PdbConvert");
			xmlWriter.WriteAttributeString("version", "1");

			AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += CurrentDomain_ReflectionOnlyAssemblyResolve;

			string currDir = Environment.CurrentDirectory;
			foreach (string fileName in assemblyFileNames)
			{
				// Change to the file's directory so that we can find referenced assemblies
				Environment.CurrentDirectory = Path.GetDirectoryName(fileName);
				ISymbolReader reader = SymbolAccess.GetReaderForFile(fileName);
				assembly = Assembly.ReflectionOnlyLoadFrom(fileName);

				xmlWriter.WriteStartElement("module");
				xmlWriter.WriteAttributeString("file", Path.GetFileName(fileName).ToLowerInvariant());
				xmlWriter.WriteAttributeString("version", GetAssemblyVersion(assembly));
				xmlWriter.WriteAttributeString("config", GetAssemblyConfiguration(assembly));

				WriteDocList(reader);
				//WriteEntryPoint(reader);
				WriteAllMethods(reader);

				xmlWriter.WriteEndElement();   // </module>
			}
			Environment.CurrentDirectory = currDir;

			AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= CurrentDomain_ReflectionOnlyAssemblyResolve;

			xmlWriter.WriteEndElement();   // </symbols>
		}

		private static string GetAssemblyVersion(Assembly assembly)
		{
			IList<CustomAttributeData> attributes = assembly.GetCustomAttributesData();
			//foreach (CustomAttributeData cad in attributes)
			//{
			//    if (cad.Constructor.DeclaringType == typeof(AssemblyInformationalVersionAttribute))
			//    {
			//        return cad.ConstructorArguments.First().Value.ToString();
			//    }
			//}
			foreach (CustomAttributeData cad in attributes)
			{
				if (cad.Constructor.DeclaringType == typeof(AssemblyFileVersionAttribute))
				{
					return cad.ConstructorArguments.First().Value.ToString();
				}
			}
			Version assemblyVersion = assembly.GetName().Version;
			if (assemblyVersion.Major != 0 ||
				assemblyVersion.Minor != 0 ||
				assemblyVersion.Revision != 0 ||
				assemblyVersion.Build != 0)
			{
				return assembly.GetName().Version.ToString();
			}
			return null;
		}

		private static string GetAssemblyConfiguration(Assembly assembly)
		{
			IList<CustomAttributeData> attributes = assembly.GetCustomAttributesData();
			foreach (CustomAttributeData cad in attributes)
			{
				if (cad.Constructor.DeclaringType == typeof(AssemblyConfigurationAttribute))
				{
					return cad.ConstructorArguments.First().Value.ToString();
				}
			}
			return null;
		}

		// In order to call GetTypes(), we need to manually resolve any assembly references.
		// For example, if a type derives from a type in another module, we need to resolve that module.
		private Assembly CurrentDomain_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
		{
			// args.Name is the assembly name, not the filename.
			// Naive implementation that just assumes the assembly is in the working directory.
			// This does not have any knowledge about the initial assembly we were trying to load.
			Assembly a = System.Reflection.Assembly.ReflectionOnlyLoad(args.Name);
			return a;
		}

		// Write all docs, and add to the m_fileMapping list.
		// Other references to docs will then just refer to this list.
		private void WriteDocList(ISymbolReader reader)
		{
			int id = 0;
			xmlWriter.WriteStartElement("files");
			{
				foreach (ISymbolDocument doc in reader.GetDocuments())
				{
					string url = doc.URL;

					// Symbol store may give out duplicate documents
					if (fileMapping.ContainsKey(url)) continue;
					id++;
					fileMapping.Add(doc.URL, id);

					string srcFileName = doc.URL;
					if (!string.IsNullOrEmpty(SourceBasePath) &&
						srcFileName.StartsWith(SourceBasePath, StringComparison.OrdinalIgnoreCase))
					{
						srcFileName = srcFileName.Substring(SourceBasePath.Length).TrimStart('\\');
					}

					xmlWriter.WriteStartElement("file");
					xmlWriter.WriteAttributeString("id", id.ToString());
					xmlWriter.WriteAttributeString("name", srcFileName);
					xmlWriter.WriteEndElement();   // </file>
				}
			}
			xmlWriter.WriteEndElement();   // </files>
		}

		// Write out a reference to the entry point method (if one exists)
		private void WriteEntryPoint(ISymbolReader reader)
		{
			// If there is no entry point token (such as in a dll), this will throw.
			SymbolToken token = reader.UserEntryPoint;
			if (token.GetToken() == 0)
			{
				// If the Symbol APIs fail when looking for an entry point token, there is no entry point.
				return;
			}

			ISymbolMethod m = reader.GetMethod(token);
			xmlWriter.WriteStartElement("entryPoint");
			WriteMethod(m);
			xmlWriter.WriteEndElement();   // </entryPoint>
		}

		// Write out XML snippet to refer to the given method.
		private void WriteMethod(ISymbolMethod method)
		{
			xmlWriter.WriteElementString("methodRef", method.Token.GetToken().ToString("x8"));
		}

		// Dump all of the methods in the given ISymbolReader to the XmlWriter provided in the ctor.
		private void WriteAllMethods(ISymbolReader reader)
		{
			xmlWriter.WriteStartElement("methods");

			// Use reflection to enumerate all methods.
			// Skip all types that cannot be loaded.
			// Source: http://stackoverflow.com/a/7889272/143684
			List<Type> types;
			try
			{
				types = assembly.GetTypes().ToList();
			}
			catch (ReflectionTypeLoadException ex)
			{
				types = ex.Types.Where(t => t != null).ToList();
			}

			foreach (Type t in types)
			{
				foreach (MethodInfo methodReflection in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
				{
					int token = methodReflection.MetadataToken;

					xmlWriter.WriteStartElement("method");
					xmlWriter.WriteAttributeString("token", "0x" + token.ToString("x8"));
					xmlWriter.WriteAttributeString("name", t.FullName + "." + methodReflection.Name);
					ISymbolMethod methodSymbol = reader.GetMethod(new SymbolToken(token));
					if (methodSymbol != null)
					{
						WriteSequencePoints(methodSymbol);
					}
					xmlWriter.WriteEndElement();   // </method>
				}
			}
			xmlWriter.WriteEndElement();   // </methods>
		}

		// Write the sequence points for the given method
		// Sequence points are the map between IL offsets and source lines.
		// A single method could span multiple files (use C#'s #line directive to see for yourself).
		private void WriteSequencePoints(ISymbolMethod method)
		{
			int count = method.SequencePointCount;
			// Get the sequence points from the symbol store.
			// We could cache these arrays and reuse them.
			int[] offsets = new int[count];
			ISymbolDocument[] docs = new ISymbolDocument[count];
			int[] startColumn = new int[count];
			int[] endColumn = new int[count];
			int[] startRow = new int[count];
			int[] endRow = new int[count];
			method.GetSequencePoints(offsets, docs, startRow, startColumn, endRow, endColumn);

			xmlWriter.WriteStartElement("sequencePoints");
			for (int i = 0; i < count; i++)
			{
				xmlWriter.WriteStartElement("entry");
				// IL offsets are usually written in hexadecimal...
				xmlWriter.WriteAttributeString("ilOffset", "0x" + offsets[i].ToString("x4"));
				// ... but .NET XPath 1.0 cannot compare strings so we need a decimal number as well :-(
				xmlWriter.WriteAttributeString("ilOffsetDec", offsets[i].ToString());
				xmlWriter.WriteAttributeString("fileRef", fileMapping[docs[i].URL].ToString());

				// If it's a special 0xfeefee sequence point (e.g., "hidden"),
				// place an attribute on it to make it very easy for tools to recognize.
				// See http://blogs.msdn.com/b/jmstall/archive/2005/06/19/feefee-sequencepoints.aspx
				if (startRow[i] == 0xfeefee)
				{
					xmlWriter.WriteAttributeString("hidden", "true");
				}
				else
				{
					xmlWriter.WriteAttributeString("startLine", startRow[i].ToString());
					xmlWriter.WriteAttributeString("startColumn", startColumn[i].ToString());
					xmlWriter.WriteAttributeString("endLine", endRow[i].ToString());
					xmlWriter.WriteAttributeString("endColumn", endColumn[i].ToString());
				}
				xmlWriter.WriteEndElement();   // </entry>
			}
			xmlWriter.WriteEndElement();   // </sequencePoints>
		}
	}
}
