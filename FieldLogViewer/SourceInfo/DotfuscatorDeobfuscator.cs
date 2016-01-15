using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Unclassified.FieldLog;

namespace Unclassified.FieldLogViewer.SourceInfo
{
	/// <summary>
	/// Deobfuscates stack traces with an obfuscation map file for Dotfuscator CE.
	/// </summary>
	internal class DotfuscatorDeobfuscator : IDeobfuscator
	{
		#region Private data

		private List<string> fileNames = new List<string>();
		private Dictionary<string, Dictionary<string, string>> types = new Dictionary<string, Dictionary<string, string>>();
		private Dictionary<string, Dictionary<Tuple<string, string>, string>> methods = new Dictionary<string, Dictionary<Tuple<string, string>, string>>();

		#endregion Private data

		#region File analysis

		/// <summary>
		/// Analyses a file to determine whether it can be handled by this deobfuscator implementation.
		/// </summary>
		/// <param name="fileName">The name of the obfuscation map file to analyse.</param>
		/// <returns></returns>
		public static bool SupportsFile(string fileName)
		{
			XmlDocument xdoc = new XmlDocument();
			if (Path.GetExtension(fileName).Equals(".gz", StringComparison.OrdinalIgnoreCase))
			{
				using (FileStream fileStream = new FileStream(fileName, FileMode.Open))
				using (GZipStream gzStream = new GZipStream(fileStream, CompressionMode.Decompress))
				{
					xdoc.Load(gzStream);
				}
			}
			else
			{
				xdoc.Load(fileName);
			}
			XmlNode node = xdoc.SelectSingleNode("/dotfuscatorMap[@version='1.1']");
			if (node != null)
			{
				return true;
			}
			return false;
		}

		#endregion File analysis

		#region File management

		/// <summary>
		/// Gets all loaded map file names, or an empty array if no map file is loaded.
		/// </summary>
		public string[] FileNames
		{
			get
			{
				return fileNames.ToArray();
			}
		}

		/// <summary>
		/// Adds a file to the deobfuscator and loads it.
		/// </summary>
		/// <param name="fileName">The name of the obfuscation map file to load.</param>
		public void AddFile(string fileName)
		{
			if (!SupportsFile(fileName))
			{
				throw new NotSupportedException("The obfuscation map file is not supported. Unload all other map files and try again.");
			}

			if (!fileNames.Contains(fileName, StringComparer.OrdinalIgnoreCase))
			{
				fileNames.Add(fileName);

				XmlDocument xdoc = new XmlDocument();
				if (Path.GetExtension(fileName).Equals(".gz", StringComparison.OrdinalIgnoreCase))
				{
					using (FileStream fileStream = new FileStream(fileName, FileMode.Open))
					using (GZipStream gzStream = new GZipStream(fileStream, CompressionMode.Decompress))
					{
						xdoc.Load(gzStream);
					}
				}
				else
				{
					xdoc.Load(fileName);
				}

				// Read data from file into efficient dictionary
				foreach (XmlNode moduleNode in xdoc.SelectNodes("/dotfuscatorMap/mapping/module"))
				{
					ReadModule(moduleNode);
				}
			}
		}

		private void ReadModule(XmlNode moduleNode)
		{
			string moduleName = moduleNode.SelectSingleNode("name").InnerText;
			moduleName = Path.GetFileNameWithoutExtension(moduleName).ToLowerInvariant();

			foreach (XmlNode typeNode in moduleNode.SelectNodes("type"))
			{
				ReadType(typeNode, moduleName);
			}
		}

		private void ReadType(XmlNode typeNode, string moduleName)
		{
			XmlNode node;
			string typeName = typeNode.SelectSingleNode("name").InnerText;
			string newTypeName = typeName;
			node = typeNode.SelectSingleNode("newname");
			if (node != null)
				newTypeName = node.InnerText;

			Dictionary<string, string> moduleTypes;
			if (!types.TryGetValue(moduleName, out moduleTypes))
			{
				moduleTypes = new Dictionary<string, string>();
				types.Add(moduleName, moduleTypes);
			}
			moduleTypes[newTypeName] = typeName;

			foreach (XmlNode methodNode in typeNode.SelectNodes("methodlist/method"))
			{
				ReadMethod(methodNode, moduleName, typeName, newTypeName);
			}
		}

		private void ReadMethod(XmlNode methodNode, string moduleName, string typeName, string newTypeName)
		{
			XmlNode node;
			string methodName = methodNode.SelectSingleNode("name").InnerText;
			string newMethodName = methodName;
			node = methodNode.SelectSingleNode("newname");
			if (node != null)
				newMethodName = node.InnerText;
			string signature = methodNode.SelectSingleNode("signature").InnerText;

			Dictionary<Tuple<string, string>, string> moduleMethods;
			if (!methods.TryGetValue(moduleName, out moduleMethods))
			{
				moduleMethods = new Dictionary<Tuple<string, string>, string>();
				methods.Add(moduleName, moduleMethods);
			}
			moduleMethods[new Tuple<string, string>(newTypeName + "." + newMethodName, signature)] = typeName + "." + methodName;
		}

		#endregion File management

		#region Deobfuscation

		/// <summary>
		/// Deobfuscates a stack frame.
		/// </summary>
		/// <param name="module">The name of the module to look up.</param>
		/// <param name="typeName">The full name of the type of the method to look up.</param>
		/// <param name="methodName">The name of the method to look up.</param>
		/// <param name="signature">The signature of the method.</param>
		/// <param name="token">The metadata token of the method.</param>
		/// <param name="originalName">The deobfuscated method name.</param>
		/// <param name="originalNameWithSignature">The deobfuscated method name with signature.</param>
		/// <param name="originalToken">The original metadata token of the method.</param>
		/// <returns>true if the item was found; otherwise, false.</returns>
		public bool Deobfuscate(
			string module,
			string typeName,
			string methodName,
			string signature,
			int token,
			out string originalName,
			out string originalNameWithSignature,
			out int originalToken)
		{
			// Initialise return values
			originalName = null;
			originalNameWithSignature = null;
			originalToken = 0;

			if (!module.Contains("<"))
			{
				try
				{
					module = Path.GetFileNameWithoutExtension(module);
				}
				catch (ArgumentException ex)
				{
					FL.Warning(ex);
				}
			}
			module = module.ToLowerInvariant();

			// Resolve the signature first: it is logged with obfuscated type names and the map file
			// contains original names
			string returnType = null;
			string parameters = null;
			Match match = Regex.Match(signature, @"^([^(]+)\(([^)]*)\)$");
			if (match.Success)
			{
				returnType = match.Groups[1].Value;
				parameters = match.Groups[2].Value;

				returnType = TranslateTypeName(module, returnType);

				StringBuilder paramsSb = new StringBuilder();
				SplitAndTranslateParameters(module, parameters, paramsSb);
				parameters = paramsSb.ToString();

				StringBuilder sigSb = new StringBuilder();
				sigSb.Append(returnType);
				sigSb.Append("(");
				sigSb.Append(parameters);
				sigSb.Append(")");
				signature = sigSb.ToString();
			}

			// Find the method entry
			string name = typeName + "." + methodName;
			Dictionary<Tuple<string, string>, string> moduleMethods;
			if (methods.TryGetValue(module, out moduleMethods) &&
				moduleMethods.TryGetValue(new Tuple<string, string>(name, signature), out originalName))
			{
				originalNameWithSignature = originalName;
				//if (returnType != null)
				//{
				//    originalNameWithSignature = returnType + " " + originalNameWithSignature;
				//}
				if (parameters != null)
				{
					originalNameWithSignature += "(" + parameters + ")";
				}
				return true;
			}
			return false;
		}

		/// <summary>
		/// Translates a type name from C# keywords to Dotfuscator terminology.
		/// </summary>
		/// <param name="typeName">The type name as C# keyword.</param>
		/// <returns></returns>
		private string TranslateTypeName(string module, string typeName)
		{
			if (typeName == "void") return "void";

			if (typeName == "bool") return "bool";
			if (typeName == "byte") return "unsigned int8";
			if (typeName == "sbyte") return "int8";
			if (typeName == "char") return "char";
			if (typeName == "decimal") return "System.Decimal";
			if (typeName == "double") return "float64";
			if (typeName == "float") return "float32";
			if (typeName == "int") return "int32";
			if (typeName == "uint") return "unsigned int32";
			if (typeName == "long") return "int64";
			if (typeName == "ulong") return "unsigned int64";
			if (typeName == "object") return "object";
			if (typeName == "short") return "int16";
			if (typeName == "ushort") return "unsigned int16";
			if (typeName == "string") return "string";

			if (typeName == "bool?") return "System.Nullable`1<bool>";
			if (typeName == "byte?") return "System.Nullable`1<unsigned int8>";
			if (typeName == "sbyte?") return "System.Nullable`1<int8>";
			if (typeName == "char?") return "System.Nullable`1<char>";
			if (typeName == "decimal?") return "System.Nullable`1<System.Decimal>";
			if (typeName == "double?") return "System.Nullable`1<float64>";
			if (typeName == "float?") return "System.Nullable`1<float32>";
			if (typeName == "int?") return "System.Nullable`1<int32>";
			if (typeName == "uint?") return "System.Nullable`1<unsigned int32>";
			if (typeName == "long?") return "System.Nullable`1<int64>";
			if (typeName == "ulong?") return "System.Nullable`1<unsigned int64>";
			if (typeName == "short?") return "System.Nullable`1<int16>";
			if (typeName == "ushort?") return "System.Nullable`1<unsigned int16>";

			// Generic arguments remain untranslated
			if (typeName == "!0" || typeName == "!1" || typeName == "!2" || typeName == "!3" ||
				typeName == "!!0" || typeName == "!!1" || typeName == "!!2" || typeName == "!!3")
				return typeName;

			// Find the type entry
			Dictionary<string, string> moduleTypes;
			string originalName;
			if (types.TryGetValue(module, out moduleTypes) &&
				moduleTypes.TryGetValue(typeName, out originalName))
			{
				return originalName;
			}

			// Unknown type name, don't change it
			return typeName;
		}

		/// <summary>
		/// Splits method signature parameters into type names, resolves them and appends to a
		/// StringBuilder containing the resolved signature.
		/// </summary>
		/// <param name="parameters">The parameters to analyse.</param>
		/// <param name="signatureBuilder">The StringBuilder to append to.</param>
		private void SplitAndTranslateParameters(string module, string parameters, StringBuilder signatureBuilder)
		{
			// Input examples:
			// Abc, Ns.Ns.Def, Ns.Ghi/Jk
			// Abc<Xyz>, Def<X>
			// A<Xyz, !0, !!2>, Bcd, Ef
			// <a>bc, D
			// Ab, <c>d_efg<X, Y>, H

			SplitState state = SplitState.IdentifierStart;
			parameters = parameters.Trim() + " ";   // Append input character that leads to final state
			int startIndex = 0;
			for (int i = 0; i < parameters.Length; i++)
			{
				char ch = parameters[i];
				switch (state)
				{
					case SplitState.IdentifierStart:
						if (ch == ',' || ch == ' ')   // Separator character
						{
							// No type name
							// Append separator character (if it's not the last)
							if (i < parameters.Length - 1) signatureBuilder.Append(ch);
						}
						else if (ch == '<')   // Identifier starts with "<"
						{
							// Remember where the identifier started
							startIndex = i;
							state = SplitState.IdentifierAngles;
						}
						else   // Normal identifier starts
						{
							// Remember where the identifier started
							startIndex = i;
							state = SplitState.Identifier;
						}
						break;

					case SplitState.IdentifierAngles:
						if (ch == ',' || ch == ' ')   // Separator character
						{
							// This is unusual! Type names only start with "<" if they're compiler-
							// generated and those will also contain ">" afterwards.
							// Translate and append identifier
							string typeName = parameters.Substring(startIndex, i - startIndex);
							signatureBuilder.Append(TranslateTypeName(module, typeName));
							// Append separator character (if it's not the last)
							if (i < parameters.Length - 1) signatureBuilder.Append(ch);
							state = SplitState.IdentifierStart;
						}
						else if (ch == '>')
						{
							state = SplitState.Identifier;
						}
						break;

					case SplitState.Identifier:
						if (ch == ',' || ch == ' ' || ch == '<' || ch == '>')   // Separator character
						{
							// Translate and append identifier
							string typeName = parameters.Substring(startIndex, i - startIndex);
							signatureBuilder.Append(TranslateTypeName(module, typeName));
							// Append separator character (if it's not the last)
							if (i < parameters.Length - 1) signatureBuilder.Append(ch);
							state = SplitState.IdentifierStart;
						}
						break;
				}
			}
		}

		private enum SplitState
		{
			IdentifierStart,
			IdentifierAngles,
			Identifier
		}

		#endregion Deobfuscation
	}
}
