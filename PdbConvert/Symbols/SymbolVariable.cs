using System;
using System.Diagnostics.SymbolStore;
using System.Text;

namespace PdbConvert.Symbols
{
	internal class SymbolVariable : ISymbolVariable
	{
		private ISymUnmanagedVariable unmanagedVariable;

		internal SymbolVariable(ISymUnmanagedVariable unmanagedVariable)
		{
			if (unmanagedVariable == null)
			{
				throw new ArgumentNullException("unmanagedVariable");
			}
			this.unmanagedVariable = unmanagedVariable;
		}

		public string Name
		{
			get
			{
				StringBuilder name;
				int cchName;
				unmanagedVariable.GetName(0, out cchName, null);
				name = new StringBuilder(cchName);
				unmanagedVariable.GetName(cchName, out cchName, name);
				return name.ToString();
			}
		}

		public object Attributes
		{
			get
			{
				int retVal;
				unmanagedVariable.GetAttributes(out retVal);
				return retVal;
			}
		}

		public byte[] GetSignature()
		{
			byte[] data;
			int cData;
			unmanagedVariable.GetSignature(0, out cData, null);
			data = new byte[cData];
			unmanagedVariable.GetSignature(cData, out cData, data);
			return data;
		}

		public SymAddressKind AddressKind
		{
			get
			{
				int retVal;
				unmanagedVariable.GetAddressKind(out retVal);
				return (SymAddressKind) retVal;
			}
		}

		public int AddressField1
		{
			get
			{
				int retVal;
				unmanagedVariable.GetAddressField1(out retVal);
				return retVal;
			}
		}

		public int AddressField2
		{
			get
			{
				int retVal;
				unmanagedVariable.GetAddressField2(out retVal);
				return retVal;
			}
		}

		public int AddressField3
		{
			get
			{
				int retVal;
				unmanagedVariable.GetAddressField3(out retVal);
				return retVal;
			}
		}

		public int StartOffset
		{
			get
			{
				int retVal;
				unmanagedVariable.GetStartOffset(out retVal);
				return retVal;
			}
		}

		public int EndOffset
		{
			get
			{
				int retVal;
				unmanagedVariable.GetEndOffset(out retVal);
				return retVal;
			}
		}
	}
}
