using System;
using System.Diagnostics.SymbolStore;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace PdbConvert.Symbols
{
	internal class SymbolBinder : ISymbolBinder1
	{
		private ISymUnmanagedBinder unmanagedBinder;

		public SymbolBinder()
		{
			Guid CLSID_CorSymBinder = new Guid("0A29FF9E-7F9C-4437-8B11-F424491E3931");
			unmanagedBinder = (ISymUnmanagedBinder3)Activator.CreateInstance(Type.GetTypeFromCLSID(CLSID_CorSymBinder));
		}

		public ISymbolReader GetReader(IntPtr importer, string filename, string searchPath)
		{
			ISymUnmanagedReader reader;
			int hr = unmanagedBinder.GetReaderForFile(importer, filename, searchPath, out reader);
			if (IsFailingResultNormal(hr))
			{
				return null;
			}
			Marshal.ThrowExceptionForHR(hr);
			return new SymbolReader(reader);
		}

		public ISymbolReader GetReaderForFile(object importer, string filename, string searchPath)
		{
			ISymUnmanagedReader reader;
			IntPtr uImporter = IntPtr.Zero;
			try
			{
				uImporter = Marshal.GetIUnknownForObject(importer);
				int hr = unmanagedBinder.GetReaderForFile(uImporter, filename, searchPath, out reader);
				if (IsFailingResultNormal(hr))
				{
					return null;
				}
				Marshal.ThrowExceptionForHR(hr);
			}
			finally
			{
				if (uImporter != IntPtr.Zero)
					Marshal.Release(uImporter);
			}
			return new SymbolReader(reader);
		}

		public ISymbolReader GetReaderForFile(object importer, string fileName, string searchPath, SymSearchPolicies searchPolicy)
		{
			ISymUnmanagedReader symReader;
			IntPtr uImporter = IntPtr.Zero;
			try
			{
				uImporter = Marshal.GetIUnknownForObject(importer);
				int hr = ((ISymUnmanagedBinder2)unmanagedBinder).GetReaderForFile2(uImporter, fileName, searchPath, (int)searchPolicy, out symReader);
				if (IsFailingResultNormal(hr))
				{
					return null;
				}
				Marshal.ThrowExceptionForHR(hr);
			}
			finally
			{
				if (uImporter != IntPtr.Zero)
					Marshal.Release(uImporter);
			}
			return new SymbolReader(symReader);
		}

		public ISymbolReader GetReaderForFile(object importer, string fileName, string searchPath, SymSearchPolicies searchPolicy, IntPtr callback)
		{
			ISymUnmanagedReader reader;
			IntPtr uImporter = IntPtr.Zero;
			try
			{
				uImporter = Marshal.GetIUnknownForObject(importer);
				int hr = ((ISymUnmanagedBinder3)unmanagedBinder).GetReaderFromCallback(uImporter, fileName, searchPath, (int)searchPolicy, callback, out reader);
				if (IsFailingResultNormal(hr))
				{
					return null;
				}
				Marshal.ThrowExceptionForHR(hr);
			}
			finally
			{
				if (uImporter != IntPtr.Zero)
					Marshal.Release(uImporter);
			}
			return new SymbolReader(reader);
		}

		public ISymbolReader GetReaderFromStream(object importer, IStream stream)
		{
			ISymUnmanagedReader reader;
			IntPtr uImporter = IntPtr.Zero;
			try
			{
				uImporter = Marshal.GetIUnknownForObject(importer);
				int hr = ((ISymUnmanagedBinder2)unmanagedBinder).GetReaderFromStream(uImporter, stream, out reader);
				if (IsFailingResultNormal(hr))
				{
					return null;
				}
				Marshal.ThrowExceptionForHR(hr);
			}
			finally
			{
				if (uImporter != IntPtr.Zero)
					Marshal.Release(uImporter);
			}
			return new SymbolReader(reader);
		}

		private static bool IsFailingResultNormal(int hr)
		{
			// If a pdb is not found, that's a pretty common thing.
			if (hr == (int)HResult.E_PDB_NOT_FOUND)
			{
				return true;
			}
			// Other fairly common things may happen here, but we don't want to hide this from the
			// programmer. You may get 0x806D0014 if the pdb is there, but just old (mismatched) or
			// if you ask for the symbol information on something that's not an assembly. If that
			// may happen for your application, wrap calls to GetReaderForFile in
			// try-catch(COMException) blocks and use the error code in the COMException to report
			// error.
			return false;
		}
	}
}
