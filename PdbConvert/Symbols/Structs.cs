using System;
using System.Diagnostics.SymbolStore;
using System.Runtime.InteropServices;

namespace PdbConvert.Symbols
{
	[StructLayout(LayoutKind.Sequential)]
	internal struct SymbolLineDelta
	{
		private SymbolToken mdMethod;
		private int delta;
	};
}
