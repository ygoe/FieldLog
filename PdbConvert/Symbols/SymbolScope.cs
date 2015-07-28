using System;
using System.Diagnostics.SymbolStore;

namespace PdbConvert.Symbols
{
	internal class SymbolScope : ISymbolScope
	{
		private ISymUnmanagedScope unmanagedScope;

		internal SymbolScope(ISymUnmanagedScope unmanagedScope)
		{
			if (unmanagedScope == null)
			{
				throw new ArgumentNullException("unmanagedScope");
			}
			this.unmanagedScope = unmanagedScope;
		}

		public ISymbolMethod Method
		{
			get
			{
				ISymUnmanagedMethod uMethod;
				unmanagedScope.GetMethod(out uMethod);
				return new SymbolMethod(uMethod);
			}
		}

		public ISymbolScope Parent
		{
			get
			{
				ISymUnmanagedScope uScope;
				unmanagedScope.GetParent(out uScope);
				return new SymbolScope(uScope);
			}
		}

		public ISymbolScope[] GetChildren()
		{
			int count;
			unmanagedScope.GetChildren(0, out count, null);
			ISymUnmanagedScope[] uScopes = new ISymUnmanagedScope[count];
			unmanagedScope.GetChildren(count, out count, uScopes);

			int i;
			ISymbolScope[] scopes = new ISymbolScope[count];
			for (i = 0; i < count; i++)
			{
				scopes[i] = new SymbolScope(uScopes[i]);
			}
			return scopes;
		}

		public int StartOffset
		{
			get
			{
				int offset;
				unmanagedScope.GetStartOffset(out offset);
				return offset;
			}
		}

		public int EndOffset
		{
			get
			{
				int offset;
				unmanagedScope.GetEndOffset(out offset);
				return offset;
			}
		}

		public ISymbolVariable[] GetLocals()
		{
			int count;
			unmanagedScope.GetLocals(0, out count, null);
			ISymUnmanagedVariable[] uVariables = new ISymUnmanagedVariable[count];
			unmanagedScope.GetLocals(count, out count, uVariables);

			int i;
			ISymbolVariable[] variables = new ISymbolVariable[count];
			for (i = 0; i < count; i++)
			{
				variables[i] = new SymbolVariable(uVariables[i]);
			}
			return variables;
		}

		public ISymbolNamespace[] GetNamespaces()
		{
			int count;
			unmanagedScope.GetNamespaces(0, out count, null);
			ISymUnmanagedNamespace[] uNamespaces = new ISymUnmanagedNamespace[count];
			unmanagedScope.GetNamespaces(count, out count, uNamespaces);

			int i;
			ISymbolNamespace[] namespaces = new ISymbolNamespace[count];
			for (i = 0; i < count; i++)
			{
				namespaces[i] = new SymbolNamespace(uNamespaces[i]);
			}
			return namespaces;
		}

		public int LocalCount
		{
			get
			{
				int count;
				unmanagedScope.GetLocalCount(out count);
				return count;
			}
		}

		public int ConstantCount
		{
			get
			{
				int count;
				((ISymUnmanagedScope2)unmanagedScope).GetConstantCount(out count);
				return count;
			}
		}

		public ISymbolConstant[] GetConstants()
		{
			int count;
			((ISymUnmanagedScope2)unmanagedScope).GetConstants(0, out count, null);
			ISymUnmanagedConstant[] uConstants = new ISymUnmanagedConstant[count];
			((ISymUnmanagedScope2)unmanagedScope).GetConstants(count, out count, uConstants);

			int i;
			ISymbolConstant[] constants = new ISymbolConstant[count];
			for (i = 0; i < count; i++)
			{
				constants[i] = new SymbolConstant(uConstants[i]);
			}
			return constants;
		}
	}
}
