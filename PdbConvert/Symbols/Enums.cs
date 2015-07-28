using System;

namespace PdbConvert.Symbols
{
	[Serializable, Flags]
	public enum SymSearchPolicies
	{
		/// <summary>Query the registry for symbol search paths.</summary>
		AllowRegistryAccess = 1,
		/// <summary>Access a symbol server.</summary>
		AllowSymbolServerAccess = 2,
		/// <summary>Look at the path specified in Debug directory.</summary>
		AllowOriginalPathAccess = 4,
		/// <summary>Look for PDB in the place where the exe is.</summary>
		AllowReferencePathAccess = 8,
	}

	public enum HResult
	{
		E_FAIL = unchecked((int)0x80004005),
		E_PDB_NOT_FOUND = unchecked((int)0x806D0005),
	}
}
