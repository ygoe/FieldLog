using System;
using System.Diagnostics.SymbolStore;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace PdbConvert.Symbols
{
	// We can use reflection-only load context to use reflection to query for metadata information
	// rather than painfully import the com-classic metadata interfaces.
	[Guid("809c652e-7396-11d2-9771-00a0c9b4d50c")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComVisible(true)]
	internal interface IMetadataDispenserPrivate
	{
		// We need to be able to call OpenScope, which is the 2nd vtable slot.
		// Thus we need this one placeholder here to occupy the first slot..
		void DefineScope_Placeholder();

		//STDMETHOD(OpenScope)(                 // Return code.
		//  LPCWSTR     szScope,                // [in] The scope to open.
		//  DWORD       dwOpenFlags,            // [in] Open mode flags.
		//  REFIID      riid,                   // [in] The interface desired.
		//  IUnknown    **ppIUnk) PURE;         // [out] Return interface on success.
		void OpenScope(
			[In, MarshalAs(UnmanagedType.LPWStr)] string szScope,
			[In] int dwOpenFlags,
			[In] ref Guid riid,
			[Out, MarshalAs(UnmanagedType.IUnknown)] out object punk);

		// There are more methods in this interface, but we don't need them.
	}

	// This is the same interface for what we want to do with it, just give it a different IID.
	// Source: cor.h
	[Guid("31bcfce2-dafb-11d2-9f81-00c04f79a0a3")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComVisible(true)]
	internal interface IMetadataDispenserExPrivate
	{
		// We need to be able to call OpenScope, which is the 2nd vtable slot.
		// Thus we need this one placeholder here to occupy the first slot..
		void DefineScope_Placeholder();

		//STDMETHOD(OpenScope)(                 // Return code.
		//  LPCWSTR     szScope,                // [in] The scope to open.
		//  DWORD       dwOpenFlags,            // [in] Open mode flags.
		//  REFIID      riid,                   // [in] The interface desired.
		//  IUnknown    **ppIUnk) PURE;         // [out] Return interface on success.
		void OpenScope(
			[In, MarshalAs(UnmanagedType.LPWStr)] string szScope,
			[In] int dwOpenFlags,
			[In] ref Guid riid,
			[Out, MarshalAs(UnmanagedType.IUnknown)] out object punk);

		// There are more methods in this interface, but we don't need them.
	}

	// Source: metahost.h
	[Guid("bd39d1d2-ba2f-486a-89b0-b4b0cb466891")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComVisible(true)]
	internal interface IICLRRuntimeInfoPrivate
	{
		void GetVersionString_Placeholder();

		void GetRuntimeDirectory_Placeholder();

		void IsLoaded_Placeholder();

		void LoadErrorString_Placeholder();

		void LoadLibrary_Placeholder();

		void GetProcAddress_Placeholder();

		//virtual HRESULT STDMETHODCALLTYPE GetInterface(
		//	/* [in] */ REFCLSID rclsid,
		//	/* [in] */ REFIID riid,
		//	/* [retval][iid_is][out] */	LPVOID* ppUnk) = 0;
		void GetInterface(
			ref Guid rclsid,
			ref Guid riid,
			[Out, MarshalAs(UnmanagedType.IUnknown)] out object ppUnk);
	}

	// Source: metahost.h
	[Guid("d332db9e-b9b3-4125-8207-a14884f53216")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComVisible(true)]
	internal interface ICLRMetaHostPrivate
	{
		//virtual HRESULT STDMETHODCALLTYPE GetRuntime(
		//	/* [in] */ LPCWSTR pwzVersion,
		//	/* [in] */ REFIID riid,
		//	/* [retval][iid_is][out] */	LPVOID* ppRuntime) = 0;
		void GetRuntime(
			[In, MarshalAs(UnmanagedType.LPWStr)] string pwzVersion,
			ref Guid riid,
			[Out, MarshalAs(UnmanagedType.IUnknown)] out object ppRuntime);
	}

	// Since we're just blindly passing this interface through managed code to the Symbinder, we
	// don't care about actually importing the specific methods.
	// This needs to be public so that we can call Marshal.GetComInterfaceForObject() on it to get
	// the underlying metadata pointer.
	// That doesn't mean that you should actually use it though because the interface is basically
	// empty.
	[Guid("7DAC8207-D3AE-4c75-9B67-92801A497D44")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComVisible(true)]
	public interface IMetadataImportPrivateComVisible
	{
		// Just need a single placeholder method so that it doesn't complain about an empty interface.
		void Placeholder();
	}

	[ComVisible(false)]
	internal interface ISymbolConstant
	{
		string GetName();

		object GetValue();

		byte[] GetSignature();
	}

	[ComVisible(false)]
	internal interface ISymbolSearchInfo
	{
		int SearchPathLength { get; }

		string SearchPath { get; }

		int HResult { get; }
	}

	[ComImport]
	[Guid("85E891DA-A631-4c76-ACA2-A44A39C46B8C")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComVisible(false)]
	internal interface ISymENCUnmanagedMethod
	{
		void GetFileNameFromOffset(
			int dwOffset,
			int cchName,
			out int pcchName,
			[MarshalAs(UnmanagedType.LPWStr)] StringBuilder name);

		void GetLineFromOffset(
			int dwOffset,
			out int pline,
			out int pcolumn,
			out int pendLine,
			out int pendColumn,
			out int pdwStartOffset);
	}

	[ComImport]
	[Guid("40DE4037-7C81-3E1E-B022-AE1ABFF2CA08")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComVisible(false)]
	internal interface ISymUnmanagedDocument
	{
		void GetURL(
			int cchUrl,
			out int pcchUrl,
			[MarshalAs(UnmanagedType.LPWStr)] StringBuilder szUrl);

		void GetDocumentType(ref Guid pRetVal);

		void GetLanguage(ref Guid pRetVal);

		void GetLanguageVendor(ref Guid pRetVal);

		void GetCheckSumAlgorithmId(ref Guid pRetVal);

		void GetCheckSum(
			int cData,
			out int pcData,
			[In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] data);

		void FindClosestLine(int line, out int pRetVal);

		void HasEmbeddedSource(out bool pRetVal);

		void GetSourceLength(out int pRetVal);

		void GetSourceRange(
			int startLine,
			int startColumn,
			int endLine,
			int endColumn,
			int cSourceBytes,
			out int pcSourceBytes,
			[In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] source);
	};

	[ComImport]
	[Guid("E502D2DD-8671-4338-8F2A-FC08229628C4")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComVisible(false)]
	internal interface ISymUnmanagedEncUpdate
	{
		void UpdateSymbolStore2(
			IStream stream,
			[MarshalAs(UnmanagedType.LPArray)] SymbolLineDelta[] iSymbolLineDeltas,
			int cDeltaLines);

		void GetLocalVariableCount(
			SymbolToken mdMethodToken,
			out int pcLocals);

		void GetLocalVariables(
			SymbolToken mdMethodToken,
			int cLocals,
			[In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ISymUnmanagedVariable[] rgLocals,
			out int pceltFetched);
	}

	[ComImport]
	[Guid("AA544d42-28CB-11d3-bd22-0000f80849bd")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComVisible(false)]
	internal interface ISymUnmanagedBinder
	{
		// These methods will often return error HRs in common cases.
		// If there are no symbols for the given target, a failing hr is returned.
		// This is pretty common.
		//
		// Using PreserveSig and manually handling error cases provides a big performance win.
		// Far fewer exceptions will be thrown and caught.
		// Exceptions should be reserved for truely "exceptional" cases.
		[PreserveSig]
		int GetReaderForFile(
			IntPtr importer,
			[MarshalAs(UnmanagedType.LPWStr)] string filename,
			[MarshalAs(UnmanagedType.LPWStr)] string SearchPath,
			[MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedReader retVal);

		[PreserveSig]
		int GetReaderFromStream(
			IntPtr importer,
			IStream stream,
			[MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedReader retVal);
	}

	[ComImport]
	[Guid("ACCEE350-89AF-4ccb-8B40-1C2C4C6F9434")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComVisible(false)]
	internal interface ISymUnmanagedBinder2 : ISymUnmanagedBinder
	{
		// ISymUnmanagedBinder methods (need to define the base interface methods also, per COM interop requirements)
		[PreserveSig]
		new int GetReaderForFile(
			IntPtr importer,
			[MarshalAs(UnmanagedType.LPWStr)] string filename,
			[MarshalAs(UnmanagedType.LPWStr)] string SearchPath,
			[MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedReader retVal);

		[PreserveSig]
		new int GetReaderFromStream(
			IntPtr importer,
			IStream stream,
			[MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedReader retVal);

		// ISymUnmanagedBinder2 methods
		[PreserveSig]
		int GetReaderForFile2(
			IntPtr importer,
			[MarshalAs(UnmanagedType.LPWStr)] string fileName,
			[MarshalAs(UnmanagedType.LPWStr)] string searchPath,
			int searchPolicy,
			[MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedReader pRetVal);
	}

	[ComImport]
	[Guid("28AD3D43-B601-4d26-8A1B-25F9165AF9D7")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComVisible(false)]
	internal interface ISymUnmanagedBinder3 : ISymUnmanagedBinder2
	{
		// ISymUnmanagedBinder methods (need to define the base interface methods also, per COM interop requirements)
		[PreserveSig]
		new int GetReaderForFile(
			IntPtr importer,
			[MarshalAs(UnmanagedType.LPWStr)] string filename,
			[MarshalAs(UnmanagedType.LPWStr)] string SearchPath,
			[MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedReader retVal);

		[PreserveSig]
		new int GetReaderFromStream(
			IntPtr importer,
			IStream stream,
			[MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedReader retVal);

		// ISymUnmanagedBinder2 methods (need to define the base interface methods also, per COM interop requirements)
		[PreserveSig]
		new int GetReaderForFile2(
			IntPtr importer,
			[MarshalAs(UnmanagedType.LPWStr)] string fileName,
			[MarshalAs(UnmanagedType.LPWStr)] string searchPath,
			int searchPolicy,
			[MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedReader pRetVal);

		// ISymUnmanagedBinder3 methods
		[PreserveSig]
		int GetReaderFromCallback(
			IntPtr importer,
			[MarshalAs(UnmanagedType.LPWStr)] string fileName,
			[MarshalAs(UnmanagedType.LPWStr)] string searchPath,
			int searchPolicy,
			IntPtr callback,
			[MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedReader pRetVal);
	}

	[ComImport]
	[Guid("48B25ED8-5BAD-41bc-9CEE-CD62FABC74E9")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComVisible(false)]
	internal interface ISymUnmanagedConstant
	{
		void GetName(
			int cchName,
			out int pcchName,
			[MarshalAs(UnmanagedType.LPWStr)] StringBuilder name);

		void GetValue(out object pValue);

		void GetSignature(
			int cSig,
			out int pcSig,
			[In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] sig);
	}

	[ComImport]
	[Guid("B62B923C-B500-3158-A543-24F307A8B7E1")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComVisible(false)]
	internal interface ISymUnmanagedMethod
	{
		void GetToken(out SymbolToken pToken);

		void GetSequencePointCount(out int retVal);

		void GetRootScope([MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedScope retVal);

		void GetScopeFromOffset(int offset, [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedScope retVal);

		void GetOffset(
			ISymUnmanagedDocument document,
			int line,
			int column,
			out int retVal);

		void GetRanges(
			ISymUnmanagedDocument document,
			int line,
			int column,
			int cRanges,
			out int pcRanges,
			[In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] int[] ranges);

		void GetParameters(
			int cParams,
			out int pcParams,
			[In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedVariable[] parms);

		void GetNamespace([MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedNamespace retVal);

		void GetSourceStartEnd(
			ISymUnmanagedDocument[] docs,
			[In, Out, MarshalAs(UnmanagedType.LPArray)] int[] lines,
			[In, Out, MarshalAs(UnmanagedType.LPArray)] int[] columns,
			out bool retVal);

		void GetSequencePoints(
			int cPoints,
			out int pcPoints,
			[In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] offsets,
			[In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedDocument[] documents,
			[In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] lines,
			[In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] columns,
			[In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] endLines,
			[In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] endColumns);
	}

	[ComImport]
	[Guid("0DFF7289-54F8-11d3-BD28-0000F80849BD")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComVisible(false)]
	internal interface ISymUnmanagedNamespace
	{
		void GetName(
			int cchName,
			out int pcchName,
			[MarshalAs(UnmanagedType.LPWStr)] StringBuilder szName);

		void GetNamespaces(
			int cNameSpaces,
			out int pcNameSpaces,
			[In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedNamespace[] namespaces);

		void GetVariables(
			int cVars,
			out int pcVars,
			[In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedVariable[] pVars);
	}

	[ComImport]
	[Guid("B4CE6286-2A6B-3712-A3B7-1EE1DAD467B5")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComVisible(false)]
	internal interface ISymUnmanagedReader
	{
		void GetDocument(
			[MarshalAs(UnmanagedType.LPWStr)] string url,
			Guid language,
			Guid languageVendor,
			Guid documentType,
			[MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedDocument retVal);

		void GetDocuments(
			int cDocs,
			out int pcDocs,
			[In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedDocument[] pDocs);

		// These methods will often return error HRs in common cases.
		// Using PreserveSig and manually handling error cases provides a big performance win.
		// Far fewer exceptions will be thrown and caught.
		// Exceptions should be reserved for truely "exceptional" cases.
		[PreserveSig]
		int GetUserEntryPoint(out SymbolToken EntryPoint);

		[PreserveSig]
		int GetMethod(
			SymbolToken methodToken,
			[MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedMethod retVal);

		[PreserveSig]
		int GetMethodByVersion(
			SymbolToken methodToken,
			int version,
			[MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedMethod retVal);

		void GetVariables(
			SymbolToken parent,
			int cVars,
			out int pcVars,
			[In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ISymUnmanagedVariable[] vars);

		void GetGlobalVariables(
			int cVars,
			out int pcVars,
			[In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedVariable[] vars);

		void GetMethodFromDocumentPosition(
			ISymUnmanagedDocument document,
			int line,
			int column,
			[MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedMethod retVal);

		void GetSymAttribute(
			SymbolToken parent,
			[MarshalAs(UnmanagedType.LPWStr)] string name,
			int sizeBuffer,
			out int lengthBuffer,
			[In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] buffer);

		void GetNamespaces(
			int cNameSpaces,
			out int pcNameSpaces,
			[In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedNamespace[] namespaces);

		void Initialize(
			IntPtr importer,
			[MarshalAs(UnmanagedType.LPWStr)] string filename,
			[MarshalAs(UnmanagedType.LPWStr)] string searchPath,
			IStream stream);

		void UpdateSymbolStore(
			[MarshalAs(UnmanagedType.LPWStr)] string filename,
			IStream stream);

		void ReplaceSymbolStore(
			[MarshalAs(UnmanagedType.LPWStr)] string filename,
			IStream stream);

		void GetSymbolStoreFileName(
			int cchName,
			out int pcchName,
			[MarshalAs(UnmanagedType.LPWStr)] StringBuilder szName);

		void GetMethodsFromDocumentPosition(
			ISymUnmanagedDocument document,
			int line,
			int column,
			int cMethod,
			out int pcMethod,
			[In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] ISymUnmanagedMethod[] pRetVal);

		void GetDocumentVersion(
			ISymUnmanagedDocument pDoc,
			out int version,
			out bool pbCurrent);

		void GetMethodVersion(
			ISymUnmanagedMethod pMethod,
			out int version);
	};

	[ComImport]
	[Guid("20D9645D-03CD-4e34-9C11-9848A5B084F1")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComVisible(false)]
	internal interface ISymUnmanagedReaderSymbolSearchInfo
	{
		void GetSymbolSearchInfoCount(out int pcSearchInfo);

		void GetSymbolSearchInfo(
			int cSearchInfo,
			out int pcSearchInfo,
			[In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedSymbolSearchInfo[] searchInfo);
	}

	[ComImport]
	[Guid("68005D0F-B8E0-3B01-84D5-A11A94154942")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComVisible(false)]
	internal interface ISymUnmanagedScope
	{
		void GetMethod([MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedMethod pRetVal);

		void GetParent([MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedScope pRetVal);

		void GetChildren(
			int cChildren,
			out int pcChildren,
			[In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedScope[] children);

		void GetStartOffset(out int pRetVal);

		void GetEndOffset(out int pRetVal);

		void GetLocalCount(out int pRetVal);

		void GetLocals(
			int cLocals,
			out int pcLocals,
			[In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedVariable[] locals);

		void GetNamespaces(
			int cNameSpaces,
			out int pcNameSpaces,
			[In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedNamespace[] namespaces);
	};

	[ComImport]
	[Guid("AE932FBA-3FD8-4dba-8232-30A2309B02DB")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComVisible(false)]
	internal interface ISymUnmanagedScope2 : ISymUnmanagedScope
	{
		// ISymUnmanagedScope methods (need to define the base interface methods also, per COM interop requirements)
		new void GetMethod([MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedMethod pRetVal);

		new void GetParent([MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedScope pRetVal);

		new void GetChildren(
			int cChildren,
			out int pcChildren,
			[In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedScope[] children);

		new void GetStartOffset(out int pRetVal);

		new void GetEndOffset(out int pRetVal);

		new void GetLocalCount(out int pRetVal);

		new void GetLocals(
			int cLocals,
			out int pcLocals,
			[In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedVariable[] locals);

		new void GetNamespaces(
			int cNameSpaces,
			out int pcNameSpaces,
			[In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedNamespace[] namespaces);

		// ISymUnmanagedScope2 methods
		void GetConstantCount(out int pRetVal);

		void GetConstants(
			int cConstants,
			out int pcConstants,
			[In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedConstant[] constants);
	}

	[ComImport]
	[Guid("F8B3534A-A46B-4980-B520-BEC4ACEABA8F")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComVisible(false)]
	internal interface ISymUnmanagedSymbolSearchInfo
	{
		void GetSearchPathLength(out int pcchPath);

		void GetSearchPath(
			int cchPath,
			out int pcchPath,
			[MarshalAs(UnmanagedType.LPWStr)] StringBuilder szPath);

		void GetHRESULT(out int hr);
	}

	[ComImport]
	[Guid("9F60EEBE-2D9A-3F7C-BF58-80BC991C60BB")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComVisible(false)]
	internal interface ISymUnmanagedVariable
	{
		void GetName(
			int cchName,
			out int pcchName,
			[MarshalAs(UnmanagedType.LPWStr)] StringBuilder szName);

		void GetAttributes(out int pRetVal);

		void GetSignature(
			int cSig,
			out int pcSig,
			[In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] sig);

		void GetAddressKind(out int pRetVal);

		void GetAddressField1(out int pRetVal);

		void GetAddressField2(out int pRetVal);

		void GetAddressField3(out int pRetVal);

		void GetStartOffset(out int pRetVal);

		void GetEndOffset(out int pRetVal);
	}
}
