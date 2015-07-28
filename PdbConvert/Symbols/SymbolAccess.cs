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

		private static Guid metaHostClassID = new Guid(0x9280188d, 0xe8e, 0x4867, 0xb3, 0xc, 0x7f, 0xa8, 0x38, 0x84, 0xe8, 0xde);   // CLSID_CLRMetaHost
		private static Guid metaHostIID = new Guid("d332db9e-b9b3-4125-8207-a14884f53216");   // IID_ICLRMetaHost
		private static Guid runtimeInfoIID = new Guid("bd39d1d2-ba2f-486a-89b0-b4b0cb466891");   // IID_ICLRRuntimeInfo
		private static Guid dispenserExIID = new Guid("31bcfce2-dafb-11d2-9f81-00c04f79a0a3");   // IID_IMetaDataDispenserEx

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

			[DllImport("mscoree.dll", PreserveSig = false)]
			[return: MarshalAs(UnmanagedType.Interface)]
			internal static extern object CLRCreateInstance(
				[MarshalAs(UnmanagedType.LPStruct)] Guid clsid,
				[MarshalAs(UnmanagedType.LPStruct)] Guid riid);
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
			// OLD:
			//NativeMethods.CoCreateInstance(ref dispenserClassID, null, 1, ref dispenserIID, out objDispenser);
			// NEW:
			// Source: https://social.msdn.microsoft.com/Forums/en-US/3a0a48bf-a308-45c7-8dcc-f85eaf1d32e5
			ICLRMetaHostPrivate metaHost = (ICLRMetaHostPrivate)NativeMethods.CLRCreateInstance(metaHostClassID, metaHostIID);
			object objRuntime;
			metaHost.GetRuntime("v4.0.30319", ref runtimeInfoIID, out objRuntime);
			IICLRRuntimeInfoPrivate runtime = (IICLRRuntimeInfoPrivate)objRuntime;
			runtime.GetInterface(ref dispenserClassID, ref dispenserExIID, out objDispenser);

			IMetadataDispenserPrivate dispenser = (IMetadataDispenserPrivate)objDispenser;

			// Now open an Importer on the given filename. We'll end up passing this importer straight
			// through to the Binder.
			object objImporter;
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
