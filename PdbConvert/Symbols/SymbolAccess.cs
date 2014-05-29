using System;
using System.Diagnostics.SymbolStore;
using System.Runtime.InteropServices;

namespace PdbConvert.Symbols
{
	internal static class SymbolAccess
	{
		// GUIDs for imported metadata interfaces.
		private static Guid dispenserClassID = new Guid(0xe5cb7a31, 0x7512, 0x11d2, 0x89, 0xce, 0x00, 0x80, 0xc7, 0x92, 0xe5, 0xd8);   // CLSID_CorMetaDataDispenser
		private static Guid dispenserIID = new Guid(0x809c652e, 0x7396, 0x11d2, 0x97, 0x71, 0x00, 0xa0, 0xc9, 0xb4, 0xd5, 0x0c);   // IID_IMetaDataDispenser
		private static Guid importerIID = new Guid(0x7dac8207, 0xd3ae, 0x4c75, 0x9b, 0x67, 0x92, 0x80, 0x1a, 0x49, 0x7d, 0x44);   // IID_IMetaDataImport
		private static Guid emitterIID = new Guid(0xba3fee4c, 0xecb9, 0x4e41, 0x83, 0xb7, 0x18, 0x3f, 0xa4, 0x1c, 0xd8, 0x59);   // IID_IMetaDataEmit

		private const int OPEN_READ = 0;
		private const int OPEN_WRITE = 1;

		internal static class NativeMethods
		{
			[DllImport("ole32.dll")]
			internal static extern int CoCreateInstance(
				[In] ref Guid rclsid,
				[In, MarshalAs(UnmanagedType.IUnknown)] object pUnkOuter,
				[In] uint dwClsContext,
				[In] ref Guid riid,
				[Out, MarshalAs(UnmanagedType.Interface)] out object ppv);
		}

		// If you want a SymbolReader for a given exe, just use this function.
		public static ISymbolReader GetReaderForFile(string pathModule)
		{
			return GetReaderForFile(pathModule, null);
		}

		// If you know the name of the exe and a searchPath where the file may exist, use this one.
		public static ISymbolReader GetReaderForFile(string pathModule, string searchPath)
		{
			return GetReaderForFile(new SymbolBinder(), pathModule, searchPath);
		}

		// This private function provides implementation for the two public versions.
		// searchPath is a semicolon-delimited list of paths on which to search for pathModule.
		// If searchPath is null, pathModule must be a full path to the assembly.
		private static ISymbolReader GetReaderForFile(SymbolBinder binder, string pathModule, string searchPath)
		{
			// First create the Metadata dispenser.
			object objDispenser;
			NativeMethods.CoCreateInstance(ref dispenserClassID, null, 1, ref dispenserIID, out objDispenser);

			// Now open an Importer on the given filename. We'll end up passing this importer straight
			// through to the Binder.
			object objImporter;
			IMetadataDispenserPrivate dispenser = (IMetadataDispenserPrivate) objDispenser;
			dispenser.OpenScope(pathModule, OPEN_READ, ref importerIID, out objImporter);

			IntPtr importerPtr = IntPtr.Zero;
			ISymbolReader reader;
			try
			{
				// This will manually AddRef the underlying object, so we need to be very careful to Release it.
				importerPtr = Marshal.GetComInterfaceForObject(objImporter, typeof(IMetadataImportPrivateComVisible));

				reader = binder.GetReader(importerPtr, pathModule, searchPath);
			}
			finally
			{
				if (importerPtr != IntPtr.Zero)
				{
					Marshal.Release(importerPtr);
				}
			}
			return reader;
		}
	}
}
