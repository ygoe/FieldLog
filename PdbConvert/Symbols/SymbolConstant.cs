using System;
using System.Text;

namespace PdbConvert.Symbols
{
	internal class SymbolConstant : ISymbolConstant
	{
		private ISymUnmanagedConstant unmanagedConstant;

		public SymbolConstant(ISymUnmanagedConstant unmanagedConstant)
		{
			if (unmanagedConstant == null)
			{
				throw new ArgumentNullException("unmanagedConstant");
			}
			this.unmanagedConstant = unmanagedConstant;
		}

		public string GetName()
		{
			int count;
			unmanagedConstant.GetName(0, out count, null);
			StringBuilder name = new StringBuilder(count);
			unmanagedConstant.GetName(count, out count, name);
			return name.ToString();
		}

		public object GetValue()
		{
			object value;
			unmanagedConstant.GetValue(out value);
			return value;
		}

		public byte[] GetSignature()
		{
			int count;
			unmanagedConstant.GetSignature(0, out count, null);
			byte[] sig = new byte[count];
			unmanagedConstant.GetSignature(count, out count, sig);
			return sig;
		}
	}
}
