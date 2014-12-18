using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;

namespace Unclassified.FieldLogViewer.SourceInfo
{
	/// <summary>
	/// Resolves source code locations from module, token and IL offset with a converted XML file.
	/// </summary>
	internal class SourceResolver
	{
		#region Private data

		private List<string> fileNames = new List<string>();
		private Dictionary<string, XmlDocument> xmlDocs = new Dictionary<string, XmlDocument>();

		#endregion Private data

		#region File management

		/// <summary>
		/// Gets all loaded file names.
		/// </summary>
		public string[] FileNames
		{
			get
			{
				return fileNames.ToArray();
			}
		}

		/// <summary>
		/// Adds a file to the resolver and loads it.
		/// </summary>
		/// <param name="fileName">The name of the source symbols file to load.</param>
		public void AddFile(string fileName)
		{
			if (!fileNames.Contains(fileName, StringComparer.OrdinalIgnoreCase))
			{
				fileNames.Add(fileName);

				XmlDocument xdoc = new XmlDocument();
				if (Path.GetExtension(fileName).Equals(".gz", StringComparison.OrdinalIgnoreCase) ||
					Path.GetExtension(fileName).Equals(".pdbx", StringComparison.OrdinalIgnoreCase))
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
				xmlDocs.Add(fileName, xdoc);
			}
		}

		/// <summary>
		/// Removes a loaded file from the resolver and frees its memory.
		/// </summary>
		/// <param name="fileName">The name of the source symbols file to unload.</param>
		public void RemoveFile(string fileName)
		{
			int index = fileNames.FindIndex(s => s.Equals(fileName, StringComparison.OrdinalIgnoreCase));
			if (index != -1)
			{
				string realFileName = fileNames[index];
				xmlDocs.Remove(realFileName);
				fileNames.RemoveAt(index);
				// XML documents may be large so free up some memory now
				GC.Collect();
			}
		}

		#endregion File management

		#region Resolving

		/// <summary>
		/// Resolves the module, method token and IL offset to a source code location.
		/// </summary>
		/// <param name="module">The name of the module to look up.</param>
		/// <param name="token">The metadata token of the method.</param>
		/// <param name="ilOffset">The IL offset in the method body.</param>
		/// <param name="fileName">The source code file name.</param>
		/// <param name="startLine">The start line number in the source file.</param>
		/// <param name="startColumn">The start column number in the source file.</param>
		/// <param name="endLine">The end line number in the source file.</param>
		/// <param name="endColumn">The end column number in the source file.</param>
		/// <returns>true if the source code location was found; otherwise, false.</returns>
		public bool Resolve(
			string module,
			int token,
			int ilOffset,
			out string fileName,
			out int startLine,
			out int startColumn,
			out int endLine,
			out int endColumn)
		{
			// Initialise return values
			fileName = null;
			startLine = 0;
			startColumn = 0;
			endLine = 0;
			endColumn = 0;

			if (ilOffset < 0)
				return false;   // IL offset not known, we can't find the source for that

			foreach (XmlDocument xdoc in xmlDocs.Values)
			{
				// Search module
				XmlNode moduleNode = xdoc.SelectSingleNode(
					"/symbols" +
					"/module[@file = \"" + Path.GetFileName(module).ToLowerInvariant() + "\"]");
				if (moduleNode == null)
					continue;   // Module not in this XML file, try next one

				// Search method and nearest offset
				XmlNode entryNode = moduleNode.SelectSingleNode(
					"methods" +
					"/method[@token = '0x" + token.ToString("x8") + "']" +
					"/sequencePoints" +
					"/entry[@ilOffsetDec <= " + ilOffset + "][last()]");
				if (entryNode == null)
					return false;   // Found module, but no symbols available for this method/offset

				// Search source code file name
				XmlAttribute fileRefAttr = entryNode.Attributes["fileRef"];
				if (fileRefAttr == null)
					return false;   // Invalid XML file
				string fileRef = fileRefAttr.Value;
				XmlNode fileNode = moduleNode.SelectSingleNode(
					"files" +
					"/file[@id = '" + fileRef + "']");
				if (fileNode == null)
					return false;   // Invalid XML file
				XmlAttribute nameAttr = fileNode.Attributes["name"];
				if (nameAttr == null)
					return false;   // Invalid XML file
				fileName = nameAttr.Value;

				XmlAttribute hiddenAttr = entryNode.Attributes["hidden"];
				if (hiddenAttr != null)
					return true;   // No line/column available

				// Read line and column information
				XmlAttribute startLineAttr = entryNode.Attributes["startLine"];
				XmlAttribute startColumnAttr = entryNode.Attributes["startColumn"];
				XmlAttribute endLineAttr = entryNode.Attributes["endLine"];
				XmlAttribute endColumnAttr = entryNode.Attributes["endColumn"];
				if (startLineAttr == null || startColumnAttr == null || endLineAttr == null || endColumnAttr == null)
					return false;   // Invalid XML file
				if (!int.TryParse(startLineAttr.Value, out startLine))
					return false;   // Invalid XML file
				if (!int.TryParse(startColumnAttr.Value, out startColumn))
					return false;   // Invalid XML file
				if (!int.TryParse(endLineAttr.Value, out endLine))
					return false;   // Invalid XML file
				if (!int.TryParse(endColumnAttr.Value, out endColumn))
					return false;   // Invalid XML file

				// All information available
				return true;
			}

			// Nothing found in all XML documents
			return false;
		}

		#endregion Resolving
	}
}
