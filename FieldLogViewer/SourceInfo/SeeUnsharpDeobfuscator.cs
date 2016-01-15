using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using Unclassified.FieldLog;

namespace Unclassified.FieldLogViewer.SourceInfo
{
	/// <summary>
	/// Deobfuscates stack traces with an obfuscation map file for SeeUnsharp.
	/// </summary>
	internal class SeeUnsharpDeobfuscator : IDeobfuscator
	{
		#region Private data

		private List<string> fileNames = new List<string>();
		private Dictionary<string, Dictionary<int, Tuple<string, int>>> data = new Dictionary<string, Dictionary<int, Tuple<string, int>>>();

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
			if (Path.GetExtension(fileName).Equals(".gz", StringComparison.OrdinalIgnoreCase) ||
				Path.GetExtension(fileName).Equals(".mapz", StringComparison.OrdinalIgnoreCase))
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
			XmlNode node = xdoc.SelectSingleNode("/map[@program='SeeUnsharp']");
			if (node != null)
			{
				// TODO: Add version checking
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
				if (Path.GetExtension(fileName).Equals(".gz", StringComparison.OrdinalIgnoreCase) ||
					Path.GetExtension(fileName).Equals(".mapz", StringComparison.OrdinalIgnoreCase))
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
				foreach (XmlNode renamingNode in xdoc.SelectNodes("/map/renamings//renaming"))
				{
					string newAssembly;
					int newToken;
					string name;
					int originalToken;

					XmlAttribute newAssemblyAttr = renamingNode.Attributes["newassembly"];
					if (newAssemblyAttr == null)
						continue;   // Invalid XML file
					newAssembly = newAssemblyAttr.Value;

					XmlAttribute newTokenAttr = renamingNode.Attributes["newtoken"];
					if (newTokenAttr == null ||
						newTokenAttr.Value == null ||
						!newTokenAttr.Value.StartsWith("0x") ||
						!int.TryParse(newTokenAttr.Value.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out newToken))
						continue;   // Invalid XML file

					XmlAttribute tokenAttr = renamingNode.Attributes["token"];
					if (tokenAttr == null ||
						tokenAttr.Value == null ||
						!tokenAttr.Value.StartsWith("0x") ||
						!int.TryParse(tokenAttr.Value.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out originalToken))
						continue;   // Invalid XML file

					XmlNode originalNode = renamingNode.SelectSingleNode("original");
					if (originalNode == null)
						continue;   // Invalid XML file

					XmlAttribute scopeAttr = originalNode.Attributes["scope"];
					if (scopeAttr == null)
						continue;   // Invalid XML file
					name = scopeAttr.Value + ".";
					XmlAttribute nameAttr = originalNode.Attributes["name"];
					if (nameAttr == null)
						continue;   // Invalid XML file
					name += nameAttr.Value;
					XmlAttribute paramsAttr = originalNode.Attributes["params"];
					if (paramsAttr != null)
						name += "(" + paramsAttr.Value + ")";

					Dictionary<int, Tuple<string, int>> dict;
					if (!data.TryGetValue(newAssembly, out dict))
					{
						dict = new Dictionary<int, Tuple<string, int>>();
						data.Add(newAssembly, dict);
					}
					dict[newToken] = new Tuple<string, int>(name, originalToken);
				}
			}
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

			try
			{
				module = Path.GetFileNameWithoutExtension(module);
			}
			catch (ArgumentException ex)
			{
				FL.Warning(ex);
			}
			module = module.ToLowerInvariant();

			Dictionary<int, Tuple<string, int>> dict;
			Tuple<string, int> tuple;
			if (data.TryGetValue(module, out dict) &&
				dict.TryGetValue(token, out tuple))
			{
				originalName = tuple.Item1;
				originalNameWithSignature = originalName;
				originalToken = tuple.Item2;
				return true;
			}
			return false;
		}

		#endregion Deobfuscation
	}
}
