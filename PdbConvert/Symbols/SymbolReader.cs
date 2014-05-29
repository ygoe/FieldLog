using System;
using System.Diagnostics.SymbolStore;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace PdbConvert.Symbols
{
	internal class SymbolReader : ISymbolReader, IDisposable
	{
		private ISymUnmanagedReader unmanagedReader;

		internal SymbolReader(ISymUnmanagedReader unmanagedReader)
		{
			this.unmanagedReader = unmanagedReader;
		}

		public void Dispose()
		{
			unmanagedReader = null;
		}

		public ISymbolDocument GetDocument(string url, Guid language, Guid languageVendor, Guid documentType)
		{
			ISymUnmanagedDocument document;
			unmanagedReader.GetDocument(url, language, languageVendor, documentType, out document);
			if (document == null)
			{
				return null;
			}
			return new SymbolDocument(document);
		}

		public ISymbolDocument[] GetDocuments()
		{
			int cDocs;
			unmanagedReader.GetDocuments(0, out cDocs, null);
			ISymUnmanagedDocument[] unmanagedDocuments = new ISymUnmanagedDocument[cDocs];
			unmanagedReader.GetDocuments(cDocs, out cDocs, unmanagedDocuments);

			ISymbolDocument[] documents = new SymbolDocument[cDocs];
			uint i;
			for (i = 0; i < cDocs; i++)
			{
				documents[i] = new SymbolDocument(unmanagedDocuments[i]);
			}
			return documents;
		}

		public SymbolToken UserEntryPoint
		{
			get
			{
				SymbolToken entryPoint;
				int hr = unmanagedReader.GetUserEntryPoint(out entryPoint);
				if (hr == (int) HResult.E_FAIL)
				{
					// Not all assemblies have entry points
					// dlls for example...
					return new SymbolToken(0);
				}
				else
				{
					Marshal.ThrowExceptionForHR(hr);
				}
				return entryPoint;
			}
		}

		public ISymbolMethod GetMethod(SymbolToken method)
		{
			ISymUnmanagedMethod unmanagedMethod;
			int hr = unmanagedReader.GetMethod(method, out unmanagedMethod);
			if (hr == (int) HResult.E_FAIL)
			{
				// This means that the method has no symbol info because it's probably empty
				// This can happen for virtual methods with no IL
				return null;
			}
			else
			{
				Marshal.ThrowExceptionForHR(hr);
			}
			return new SymbolMethod(unmanagedMethod);
		}

		public ISymbolMethod GetMethod(SymbolToken method, int version)
		{
			ISymUnmanagedMethod unmanagedMethod;
			int hr = unmanagedReader.GetMethodByVersion(method, version, out unmanagedMethod);
			if (hr == (int) HResult.E_FAIL)
			{
				// This means that the method has no symbol info because it's probably empty
				// This can happen for virtual methods with no IL
				return null;
			}
			else
			{
				Marshal.ThrowExceptionForHR(hr);
			}
			return new SymbolMethod(unmanagedMethod);
		}

		public ISymbolVariable[] GetVariables(SymbolToken parent)
		{
			int cVars;
			uint i;
			unmanagedReader.GetVariables(parent, 0, out cVars, null);
			ISymUnmanagedVariable[] unmanagedVariables = new ISymUnmanagedVariable[cVars];
			unmanagedReader.GetVariables(parent, cVars, out cVars, unmanagedVariables);
			SymbolVariable[] variables = new SymbolVariable[cVars];

			for (i = 0; i < cVars; i++)
			{
				variables[i] = new SymbolVariable(unmanagedVariables[i]);
			}
			return variables;
		}

		public ISymbolVariable[] GetGlobalVariables()
		{
			int cVars;
			uint i;
			unmanagedReader.GetGlobalVariables(0, out cVars, null);
			ISymUnmanagedVariable[] unmanagedVariables = new ISymUnmanagedVariable[cVars];
			unmanagedReader.GetGlobalVariables(cVars, out cVars, unmanagedVariables);
			SymbolVariable[] variables = new SymbolVariable[cVars];

			for (i = 0; i < cVars; i++)
			{
				variables[i] = new SymbolVariable(unmanagedVariables[i]);
			}
			return variables;
		}

		public ISymbolMethod GetMethodFromDocumentPosition(ISymbolDocument document, int line, int column)
		{
			ISymUnmanagedMethod unmanagedMethod;
			unmanagedReader.GetMethodFromDocumentPosition(((SymbolDocument) document).InternalDocument, line, column, out unmanagedMethod);
			return new SymbolMethod(unmanagedMethod);
		}

		public byte[] GetSymAttribute(SymbolToken parent, string name)
		{
			byte[] Data;
			int cData;
			unmanagedReader.GetSymAttribute(parent, name, 0, out cData, null);
			Data = new byte[cData];
			unmanagedReader.GetSymAttribute(parent, name, cData, out cData, Data);
			return Data;
		}

		public ISymbolNamespace[] GetNamespaces()
		{
			int count;
			uint i;
			unmanagedReader.GetNamespaces(0, out count, null);
			ISymUnmanagedNamespace[] unmanagedNamespaces = new ISymUnmanagedNamespace[count];
			unmanagedReader.GetNamespaces(count, out count, unmanagedNamespaces);
			ISymbolNamespace[] namespaces = new SymbolNamespace[count];

			for (i = 0; i < count; i++)
			{
				namespaces[i] = new SymbolNamespace(unmanagedNamespaces[i]);
			}
			return namespaces;
		}

		public void Initialize(object importer, string filename, string searchPath, IStream stream)
		{
			IntPtr uImporter = IntPtr.Zero;
			try
			{
				uImporter = Marshal.GetIUnknownForObject(importer);
				unmanagedReader.Initialize(uImporter, filename, searchPath, stream);
			}
			finally
			{
				if (uImporter != IntPtr.Zero)
					Marshal.Release(uImporter);
			}
		}

		public void UpdateSymbolStore(string fileName, IStream stream)
		{
			unmanagedReader.UpdateSymbolStore(fileName, stream);
		}

		public void ReplaceSymbolStore(string fileName, IStream stream)
		{
			unmanagedReader.ReplaceSymbolStore(fileName, stream);
		}

		public string GetSymbolStoreFileName()
		{
			StringBuilder fileName;
			int count;

			// There's a known issue in Diasymreader where we can't query the size of the pdb filename.
			// So we'll just estimate large as a workaround.

			count = 300;
			fileName = new StringBuilder(count);
			unmanagedReader.GetSymbolStoreFileName(count, out count, fileName);
			return fileName.ToString();
		}

		public ISymbolMethod[] GetMethodsFromDocumentPosition(ISymbolDocument document, int line, int column)
		{
			ISymUnmanagedMethod[] unmanagedMethods;
			ISymbolMethod[] methods;
			int count;
			uint i;
			unmanagedReader.GetMethodsFromDocumentPosition(((SymbolDocument) document).InternalDocument, line, column, 0, out count, null);
			unmanagedMethods = new ISymUnmanagedMethod[count];
			unmanagedReader.GetMethodsFromDocumentPosition(((SymbolDocument) document).InternalDocument, line, column, count, out count, unmanagedMethods);
			methods = new ISymbolMethod[count];

			for (i = 0; i < count; i++)
			{
				methods[i] = new SymbolMethod(unmanagedMethods[i]);
			}
			return methods;
		}

		public int GetDocumentVersion(ISymbolDocument document, out bool isCurrent)
		{
			int version;
			unmanagedReader.GetDocumentVersion(((SymbolDocument) document).InternalDocument, out version, out isCurrent);
			return version;
		}

		public int GetMethodVersion(ISymbolMethod method)
		{
			int version;
			unmanagedReader.GetMethodVersion(((SymbolMethod) method).InternalMethod, out version);
			return version;
		}

		public void UpdateSymbolStore(IStream stream, SymbolLineDelta[] iSymbolLineDeltas)
		{
			((ISymUnmanagedEncUpdate) unmanagedReader).UpdateSymbolStore2(stream, iSymbolLineDeltas, iSymbolLineDeltas.Length);
		}

		public int GetLocalVariableCount(SymbolToken mdMethodToken)
		{
			int count;
			((ISymUnmanagedEncUpdate) unmanagedReader).GetLocalVariableCount(mdMethodToken, out count);
			return count;
		}

		public ISymbolVariable[] GetLocalVariables(SymbolToken mdMethodToken)
		{
			int count;
			((ISymUnmanagedEncUpdate) unmanagedReader).GetLocalVariables(mdMethodToken, 0, null, out count);
			ISymUnmanagedVariable[] unmanagedVariables = new ISymUnmanagedVariable[count];
			((ISymUnmanagedEncUpdate) unmanagedReader).GetLocalVariables(mdMethodToken, count, unmanagedVariables, out count);

			ISymbolVariable[] variables = new ISymbolVariable[count];
			uint i;
			for (i = 0; i < count; i++)
			{
				variables[i] = new SymbolVariable(unmanagedVariables[i]);
			}
			return variables;
		}

		public int GetSymbolSearchInfoCount()
		{
			int count;
			((ISymUnmanagedReaderSymbolSearchInfo) unmanagedReader).GetSymbolSearchInfoCount(out count);
			return count;
		}

		public ISymbolSearchInfo[] GetSymbolSearchInfo()
		{
			int count;
			((ISymUnmanagedReaderSymbolSearchInfo) unmanagedReader).GetSymbolSearchInfo(0, out count, null);
			ISymUnmanagedSymbolSearchInfo[] unmanagedSearchInfo = new ISymUnmanagedSymbolSearchInfo[count];
			((ISymUnmanagedReaderSymbolSearchInfo) unmanagedReader).GetSymbolSearchInfo(count, out count, unmanagedSearchInfo);

			ISymbolSearchInfo[] searchInfo = new ISymbolSearchInfo[count];

			uint i;
			for (i = 0; i < count; i++)
			{
				searchInfo[i] = new SymbolSearchInfo(unmanagedSearchInfo[i]);
			}
			return searchInfo;
		}
	}
}
