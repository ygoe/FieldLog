using System;
using System.Diagnostics.SymbolStore;
using System.Text;

namespace PdbConvert.Symbols
{
	internal class SymbolNamespace : ISymbolNamespace
	{
		private ISymUnmanagedNamespace unmanagedNamespace;

		internal SymbolNamespace(ISymUnmanagedNamespace unmanagedNamespace)
		{
			if (unmanagedNamespace == null)
			{
				throw new ArgumentNullException("unmanagedNamespace");
			}
			this.unmanagedNamespace = unmanagedNamespace;
		}

		public string Name
		{
			get
			{
				StringBuilder name;
				int cchName = 0;
				unmanagedNamespace.GetName(0, out cchName, null);
				name = new StringBuilder(cchName);
				unmanagedNamespace.GetName(cchName, out cchName, name);
				return name.ToString();
			}
		}

		public ISymbolNamespace[] GetNamespaces()
		{
			uint i;
			int cNamespaces = 0;
			unmanagedNamespace.GetNamespaces(0, out cNamespaces, null);
			ISymUnmanagedNamespace[] unmamagedNamespaces = new ISymUnmanagedNamespace[cNamespaces];
			unmanagedNamespace.GetNamespaces(cNamespaces, out cNamespaces, unmamagedNamespaces);

			ISymbolNamespace[] namespaces = new ISymbolNamespace[cNamespaces];
			for (i = 0; i < cNamespaces; i++)
			{
				namespaces[i] = new SymbolNamespace(unmamagedNamespaces[i]);
			}
			return namespaces;
		}

		public ISymbolVariable[] GetVariables()
		{
			int cVars = 0;
			uint i;
			unmanagedNamespace.GetVariables(0, out cVars, null);
			ISymUnmanagedVariable[] unmanagedVariables = new ISymUnmanagedVariable[cVars];
			unmanagedNamespace.GetVariables(cVars, out cVars, unmanagedVariables);

			ISymbolVariable[] variables = new ISymbolVariable[cVars];
			for (i = 0; i < cVars; i++)
			{
				variables[i] = new SymbolVariable(unmanagedVariables[i]);
			}
			return variables;
		}
	}
}
